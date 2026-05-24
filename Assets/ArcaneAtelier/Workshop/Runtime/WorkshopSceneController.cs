using ArcaneAtelier.Audio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Workshop
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorkshopGridView))]
    [RequireComponent(typeof(WorkshopHudPresenter))]
    public sealed class WorkshopSceneController : MonoBehaviour
    {
        private const int MaxSimulationCatchUpStepsPerFrame = 16;
        private const int HackPreparationTickBudget = 3500;
        private const int HackWarmupSteps = 160;
        private const string GeneratedWorkshopScenePath = "Assets/Scenes/SpellAssemblyScene.unity";
        private const float DefaultWorkshopCellSize = 1.22f;

        [SerializeField] private WorkshopContentDatabase contentDatabase;
        [SerializeField] private WorkshopGridView gridView;
        [SerializeField] private WorkshopHudPresenter hudPresenter;
        [SerializeField] private bool autoConfigureSceneEnvironment = true;
        [SerializeField, Min(1)] private int defaultPreparationTickBudget = 600;

        private float accumulatedSimulationTime;
        private string statusMessage = "Spell Assembly ready.";
        private bool isPaused;
        private bool isDeploying;
        private bool isReturnToMenuPromptOpen;
        private bool wasPausedBeforeReturnToMenuPrompt;
        private int totalPreparationTicks;
        private int remainingPreparationTicks;
        private string encounterLabel = "Skirmish";
        private WorkshopContentDatabase ownedRuntimeDatabase;

        public WorkshopSimulation Simulation { get; private set; }
        public WorkshopNodeDefinition SelectedPaletteNode { get; private set; }
        public Vector2Int SelectedCell { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int HoveredCell { get; private set; } = new Vector2Int(-1, -1);
        public int PlacementRotationQuarterTurns { get; private set; }
        public string StatusMessage => statusMessage;
        public bool IsPaused => isPaused;
        public float GridCellSize => gridView != null ? gridView.CellSize : 1.22f;
        public int TotalPreparationTicks => totalPreparationTicks;
        public int RemainingPreparationTicks => remainingPreparationTicks;
        public int UsedPreparationTicks => Mathf.Max(0, totalPreparationTicks - remainingPreparationTicks);
        public string EncounterLabel => encounterLabel;

        public WorkshopNodeState SelectedNode =>
            Simulation != null && Simulation.TryGetNode(SelectedCell, out var nodeState) ? nodeState : null;
        public WorkshopNodeState HoveredNode =>
            Simulation != null && Simulation.TryGetNode(HoveredCell, out var nodeState) ? nodeState : null;

        public WorkshopNodeDefinition[] PlaceableNodes =>
            contentDatabase == null ? new WorkshopNodeDefinition[0] : contentDatabase.PlaceableNodes;

        public WorkshopRewardDefinition[] DebugRewards =>
            contentDatabase == null ? new WorkshopRewardDefinition[0] : contentDatabase.DebugRewards;

        public void Configure(WorkshopContentDatabase database, WorkshopGridView view, WorkshopHudPresenter hud)
        {
            contentDatabase = database;
            gridView = view;
            hudPresenter = hud;
        }

        private void Awake()
        {
            if (gridView == null)
            {
                gridView = GetComponent<WorkshopGridView>();
            }

            if (hudPresenter == null)
            {
                hudPresenter = GetComponent<WorkshopHudPresenter>();
            }

            if (contentDatabase == null)
            {
                ownedRuntimeDatabase = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
                contentDatabase = ownedRuntimeDatabase;
                Debug.LogWarning("WorkshopSceneController could not find a serialized WorkshopContentDatabase. Falling back to runtime-generated content.", this);
            }
            else if (!HasCompleteWorkshopContent(contentDatabase))
            {
                ownedRuntimeDatabase = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
                contentDatabase = ownedRuntimeDatabase;
                Debug.LogWarning("WorkshopSceneController found stale workshop content. Falling back to the complete runtime content set.", this);
            }

            EnsureSceneIs2DPlayable();

            var validationErrors = contentDatabase.ValidateContent();
            if (validationErrors.Count > 0)
            {
                statusMessage = $"WorkshopContentDatabase '{contentDatabase.name}' is invalid ({validationErrors.Count} error(s)); workshop disabled.";
                Debug.LogError(
                    $"{statusMessage}\n- {string.Join("\n- ", validationErrors)}",
                    this);
                enabled = false;
                return;
            }

            Simulation = new WorkshopSimulation(contentDatabase);
            Simulation.StateChanged += HandleSimulationStateChanged;

            if (WorkshopRunStateBridge.TryGet(out WorkshopRunStateSnapshot savedRunState))
            {
                Simulation.RestoreRunState(savedRunState);
            }

            gridView?.Initialize(this);
            hudPresenter?.Initialize(this);
            gridView?.FrameLayout(Simulation.BuildCurrentLayout(), contentDatabase.GridSize);
            SetPaletteNode(PlaceableNodes.FirstOrDefault(node => node != null && Simulation.IsUnlocked(node)));
            SetPreparationBudget(defaultPreparationTickBudget, "Skirmish");
            HandleSimulationStateChanged();

            AudioManager.PlayMusic(MusicTrack.Workshop);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Simulation != null)
            {
                Simulation.StateChanged -= HandleSimulationStateChanged;
            }

            if (ownedRuntimeDatabase != null)
            {
                Destroy(ownedRuntimeDatabase);
            }
        }

        private void Update()
        {
            HandleGlobalShortcuts();

            if (Simulation == null || isPaused || isDeploying)
            {
                return;
            }

            var stepSeconds = contentDatabase.SimulationStepSeconds;
            accumulatedSimulationTime += Time.deltaTime;
            var iterations = 0;
            while (accumulatedSimulationTime >= stepSeconds && iterations < MaxSimulationCatchUpStepsPerFrame)
            {
                AdvancePreparationStep(stepSeconds);
                accumulatedSimulationTime -= stepSeconds;
                iterations++;

                if (isDeploying)
                {
                    break;
                }
            }

            if (iterations == MaxSimulationCatchUpStepsPerFrame && accumulatedSimulationTime > stepSeconds)
            {
                accumulatedSimulationTime = stepSeconds;
            }
        }

        public void SetSelectedCell(Vector2Int cell)
        {
            if (!Simulation.IsInsideGrid(cell))
            {
                return;
            }

            SelectedCell = cell;
            gridView.RefreshVisuals();
        }

        public void SetHoveredCell(Vector2Int cell)
        {
            HoveredCell = Simulation != null && Simulation.IsInsideGrid(cell) ? cell : new Vector2Int(-1, -1);
        }

        public void SetPaletteNode(WorkshopNodeDefinition definition)
        {
            if (definition != null && !Simulation.IsUnlocked(definition))
            {
                statusMessage = $"{definition.DisplayName} is still locked.";
                return;
            }

            SelectedPaletteNode = definition;
            statusMessage = definition == null
                ? "Node palette cleared."
                : $"Placement armed: {definition.DisplayName}.";
        }

        public void RotatePlacementClockwise()
        {
            PlacementRotationQuarterTurns = (PlacementRotationQuarterTurns + 1) % 4;
            statusMessage = $"Placement rotation: {PlacementRotationQuarterTurns * 90}°.";
            gridView.RefreshVisuals();
        }

        public void RotatePlacementCounterClockwise()
        {
            PlacementRotationQuarterTurns = (PlacementRotationQuarterTurns + 3) % 4;
            statusMessage = $"Placement rotation: {PlacementRotationQuarterTurns * 90}°.";
            gridView.RefreshVisuals();
        }

        public void TryPlaceSelectedNode(Vector2Int cell)
        {
            SetSelectedCell(cell);
            if (Simulation.TryGetNode(cell, out var existingNode))
            {
                statusMessage = $"Selected {existingNode.Definition.DisplayName}. Press R to rotate or RMB to remove.";
                return;
            }

            var result = Simulation.PlaceNode(cell, SelectedPaletteNode, PlacementRotationQuarterTurns);
            if (result.Success)
                AudioManager.PlaySFX(SFXType.NodePlacement);
            statusMessage = result.Message;
        }

        public void TryRemoveNode(Vector2Int cell)
        {
            SetSelectedCell(cell);
            var result = Simulation.RemoveNode(cell);
            if (result.Success)
                AudioManager.PlaySFX(SFXType.NodeRemoval);
            statusMessage = result.Message;
        }

        public void RotatePlacedNode(Vector2Int cell)
        {
            SetSelectedCell(cell);
            Simulation.RotateNodeClockwise(cell);
            AudioManager.PlaySFX(SFXType.NodeRotation);
            statusMessage = SelectedNode == null ? "No node selected." : $"Rotated {SelectedNode.Definition.DisplayName}.";
        }

        public void CyclePlacedNodePort(Vector2Int cell, NodePortMask direction)
        {
            SetSelectedCell(cell);
            var result = Simulation.CycleNodePort(cell, direction);
            statusMessage = result.Message;
        }

        public void ApplyReward(WorkshopRewardDefinition reward)
        {
            if (reward == null)
            {
                statusMessage = "No reward selected.";
                return;
            }

            if (Simulation != null && Simulation.IsRewardAlreadyOwned(reward))
            {
                statusMessage = $"{reward.DisplayName} is already active.";
                return;
            }

            Simulation.ApplyReward(reward);
            statusMessage = $"Applied reward: {reward.DisplayName}.";
        }

        public bool TryApplyRewardById(string rewardId, out WorkshopRewardDefinition reward)
        {
            reward = contentDatabase != null ? contentDatabase.FindReward(rewardId) : null;
            if (reward == null)
            {
                return false;
            }

            ApplyReward(reward);
            return true;
        }

        public int Tokens => Simulation != null ? Simulation.Tokens : 0;

        public void AddTokens(int amount)
        {
            if (Simulation == null || amount <= 0)
            {
                return;
            }

            Simulation.AddTokens(amount);
        }

        public bool TryPurchaseReward(string rewardId, out WorkshopRewardDefinition reward, out string message)
        {
            reward = contentDatabase != null ? contentDatabase.FindReward(rewardId) : null;
            if (reward == null)
            {
                message = "Reward not found.";
                return false;
            }

            if (Simulation == null)
            {
                message = "Workshop is not ready.";
                return false;
            }

            int cost = Mathf.Max(0, reward.TokenCost);
            if (cost <= 0)
            {
                message = $"{reward.DisplayName} is not for sale.";
                return false;
            }

            if (Simulation.IsRewardAlreadyOwned(reward))
            {
                message = $"{reward.DisplayName} is already active.";
                statusMessage = message;
                return false;
            }

            if (Simulation.Tokens < cost)
            {
                message = $"Need {cost} tokens (have {Simulation.Tokens}).";
                return false;
            }

            if (!Simulation.TrySpendTokens(cost))
            {
                message = "Purchase failed.";
                return false;
            }

            Simulation.ApplyReward(reward);
            message = $"Purchased {reward.DisplayName} for {cost} tokens.";
            statusMessage = message;
            return true;
        }

        public bool IsRewardAlreadyOwned(WorkshopRewardDefinition reward)
        {
            return Simulation != null && Simulation.IsRewardAlreadyOwned(reward);
        }

        public void SetStatusMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            statusMessage = message;
        }

        public void ResetWorkshop()
        {
            Simulation.ResetToDefaultLayout();
            SelectedCell = new Vector2Int(-1, -1);
            HoveredCell = new Vector2Int(-1, -1);
            PlacementRotationQuarterTurns = 0;
            gridView?.FrameLayout(contentDatabase.DefaultLayout, contentDatabase.GridSize);
            HandleSimulationStateChanged();
            statusMessage = "Workshop reset to default layout.";
        }

        public void LoadHackFactoryLayout()
        {
            if (Simulation == null || contentDatabase == null)
            {
                return;
            }

            WorkshopPlacedNodeSeed[] hackLayout = BuildHackFactoryLayout(contentDatabase);
            Simulation.ResetToLayout(hackLayout);
            SelectedCell = new Vector2Int(-1, -1);
            HoveredCell = new Vector2Int(-1, -1);
            PlacementRotationQuarterTurns = 0;
            totalPreparationTicks = HackPreparationTickBudget;
            remainingPreparationTicks = HackPreparationTickBudget;
            accumulatedSimulationTime = 0f;
            isPaused = false;
            isDeploying = false;
            Time.timeScale = 1f;
            WarmHackFactory();
            int finalCardCount = CountFinalHackCards();
            gridView?.FrameLayout(hackLayout, contentDatabase.GridSize);
            HandleSimulationStateChanged();
            statusMessage = $"Hack factory loaded: dual Fusion II branches feed each Fusion III. Final cards warmed: {finalCardCount}.";
        }

        private void WarmHackFactory()
        {
            if (Simulation == null || contentDatabase == null)
            {
                return;
            }

            for (var i = 0; i < HackWarmupSteps; i++)
            {
                Simulation.Step(contentDatabase.SimulationStepSeconds);
            }
        }

        private int CountFinalHackCards()
        {
            if (Simulation == null)
            {
                return 0;
            }

            return Simulation.BuildInventoryView().PreparedCards
                .Where(pair => pair.Key != null && pair.Key.Id.StartsWith("spell.ultimate.", System.StringComparison.Ordinal))
                .Sum(pair => pair.Value);
        }

        public void SetPreparationBudget(int tickBudget, string label)
        {
            totalPreparationTicks = Mathf.Max(1, tickBudget);
            remainingPreparationTicks = totalPreparationTicks;
            encounterLabel = string.IsNullOrWhiteSpace(label) ? "Skirmish" : label;
            accumulatedSimulationTime = 0f;
            isDeploying = false;
            statusMessage = $"{encounterLabel} preparation started: {remainingPreparationTicks} ticks before breach.";
        }

        public void StepPreparationOnce()
        {
            if (Simulation == null || isDeploying)
            {
                return;
            }

            AdvancePreparationStep(contentDatabase.SimulationStepSeconds);
        }

        public void DeployToBattle()
        {
            if (isDeploying)
            {
                return;
            }

            isDeploying = true;
            AudioManager.PlaySFX(SFXType.PayloadCommit);
            Time.timeScale = 1f;
            WorkshopRunStateBridge.Commit(Simulation != null ? Simulation.CaptureRunState() : null);
            CommitBattlePayload();
            SceneManager.LoadScene("BattleScene");
        }

        public void SetReturnToMenuPromptOpen(bool isOpen)
        {
            if (isReturnToMenuPromptOpen == isOpen)
            {
                return;
            }

            isReturnToMenuPromptOpen = isOpen;
            if (isOpen)
            {
                wasPausedBeforeReturnToMenuPrompt = isPaused;
                isPaused = true;
                Time.timeScale = 0f;
                return;
            }

            isPaused = wasPausedBeforeReturnToMenuPrompt;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        public void ReturnToMainMenuWithoutSaving()
        {
            if (isDeploying)
            {
                return;
            }

            isDeploying = true;
            isReturnToMenuPromptOpen = false;
            isPaused = false;
            Time.timeScale = 1f;
            WorkshopBattlePayloadBridge.Clear();
            WorkshopRunStateBridge.Clear();
            RunProgressBridge.Reset();
            SceneManager.LoadScene("MainMenuScene");
        }

        public void CommitBattlePayload()
        {
            if (Simulation == null)
            {
                statusMessage = "Workshop is still booting.";
                return;
            }

            Simulation.CommitBattlePayload();
            int earlyDeployShieldBonus = remainingPreparationTicks > 0 ? 4 : 0;
            int legacyShieldBonus = MetaProgressionStore.GetOpeningShieldBonus();
            int openingShieldBonus = earlyDeployShieldBonus + legacyShieldBonus;
            RunProgressBridge.RegisterPreparation(
                UsedPreparationTicks,
                WorkshopBattlePayloadBridge.CurrentPayload,
                openingShieldBonus);
            statusMessage = WorkshopBattlePayloadBridge.CurrentPayload.HasCards
                ? openingShieldBonus > 0
                    ? $"Battle payload committed. Opening shield +{openingShieldBonus}."
                    : "Battle payload committed."
                : "No crafted cards to commit.";
        }

        public WorkshopInventoryView BuildInventoryView()
        {
            return Simulation != null
                ? Simulation.BuildInventoryView()
                : new WorkshopInventoryView(
                    new Dictionary<WorkshopItemDefinition, int>(),
                    new Dictionary<WorkshopItemDefinition, int>());
        }

        public WorkshopFlowStatsView BuildFlowStatsView()
        {
            return Simulation != null
                ? Simulation.BuildFlowStatsView()
                : new WorkshopFlowStatsView(0f, 0f, 0f, 0f);
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            statusMessage = isPaused ? "Factory time paused." : "Factory time resumed.";
        }

        private void HandleSimulationStateChanged()
        {
            gridView?.RefreshVisuals();
            hudPresenter?.Repaint();
        }

        private void AdvancePreparationStep(float stepSeconds)
        {
            if (remainingPreparationTicks <= 0)
            {
                DeployToBattle();
                return;
            }

            Simulation.Step(stepSeconds);
            remainingPreparationTicks = Mathf.Max(0, remainingPreparationTicks - 1);

            if (remainingPreparationTicks == 0)
            {
                statusMessage = "Breach opened. Deploying battle deck.";
                DeployToBattle();
            }
        }

        private void HandleGlobalShortcuts()
        {
            if (Simulation == null)
            {
                return;
            }

            if (isReturnToMenuPromptOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                RotatePlacementCounterClockwise();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                RotatePlacementClockwise();
            }
        }

        private void EnsureSceneIs2DPlayable()
        {
            if (gridView == null)
            {
                gridView = gameObject.AddComponent<WorkshopGridView>();
            }

            if (hudPresenter == null)
            {
                hudPresenter = gameObject.AddComponent<WorkshopHudPresenter>();
            }

            if (!autoConfigureSceneEnvironment || !ShouldConfigureSceneEnvironment())
            {
                return;
            }

            var activeCamera = Camera.main;
            if (activeCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform, false);
                activeCamera = cameraObject.AddComponent<Camera>();
            }

            activeCamera.orthographic = true;
            activeCamera.orthographicSize = 4.8f;
            activeCamera.clearFlags = CameraClearFlags.SolidColor;
            activeCamera.backgroundColor = new Color(0.018f, 0.026f, 0.045f);
            FocusCameraOnStarterArea(activeCamera);
        }

        private void FocusCameraOnStarterArea(Camera activeCamera)
        {
            if (activeCamera == null)
            {
                return;
            }

            float cellSize = gridView != null ? gridView.CellSize : DefaultWorkshopCellSize;
            Vector2 focusCenter = new Vector2(
                (contentDatabase != null ? contentDatabase.GridSize.x - 1 : 49) * cellSize * 0.5f,
                (contentDatabase != null ? contentDatabase.GridSize.y - 1 : 49) * cellSize * 0.5f);
            float targetOrthographicSize = activeCamera.orthographicSize;
            if (contentDatabase != null && contentDatabase.DefaultLayout.Length > 0)
            {
                var min = contentDatabase.DefaultLayout[0].Position;
                var max = min;
                foreach (var seed in contentDatabase.DefaultLayout)
                {
                    if (seed == null)
                    {
                        continue;
                    }

                    min = Vector2Int.Min(min, seed.Position);
                    max = Vector2Int.Max(max, seed.Position);
                }

                Vector2 minWorld = new Vector2(min.x * cellSize, min.y * cellSize) - Vector2.one * (cellSize * 1.6f);
                Vector2 maxWorld = new Vector2(max.x * cellSize, max.y * cellSize) + Vector2.one * (cellSize * 1.6f);
                focusCenter = (minWorld + maxWorld) * 0.5f;

                float contentHeight = Mathf.Max(cellSize * 4f, maxWorld.y - minWorld.y);
                float contentWidth = Mathf.Max(cellSize * 6f, maxWorld.x - minWorld.x);
                float fitHeight = contentHeight * 0.5f;
                float fitWidth = contentWidth * 0.5f / Mathf.Max(0.01f, activeCamera.aspect);
                targetOrthographicSize = Mathf.Max(4.8f, fitHeight, fitWidth);
            }

            activeCamera.orthographicSize = targetOrthographicSize;
            float padding = 1.2f;
            float verticalExtent = targetOrthographicSize;
            float horizontalExtent = verticalExtent * activeCamera.aspect;
            float maxX = Mathf.Max(focusCenter.x, (contentDatabase != null ? contentDatabase.GridSize.x - 1 : 49) * cellSize + padding);
            float maxY = Mathf.Max(focusCenter.y, (contentDatabase != null ? contentDatabase.GridSize.y - 1 : 49) * cellSize + padding);
            float clampedX = ClampCameraAxis(focusCenter.x, -padding, maxX, horizontalExtent);
            float clampedY = ClampCameraAxis(focusCenter.y, -padding, maxY, verticalExtent);
            activeCamera.transform.position = new Vector3(clampedX, clampedY, -10f);
        }

        private static float ClampCameraAxis(float value, float min, float max, float extent)
        {
            if (max - min <= extent * 2f)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, min + extent, max - extent);
        }

        private static bool ShouldConfigureSceneEnvironment()
        {
            var activeScene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(activeScene.path) || activeScene.path == GeneratedWorkshopScenePath;
        }

        private static bool HasCompleteWorkshopContent(WorkshopContentDatabase database)
        {
            if (database == null)
            {
                return false;
            }

            if (database.GridSize.x < 50 || database.GridSize.y < 50)
            {
                return false;
            }

            string[] requiredNodeIds =
            {
                "node.factory.element_fusion",
                "node.factory.element_shaping",
                "node.factory.conduit",
                "node.factory.turn_conduit",
                "node.factory.turn_conduit.mirror",
                "node.factory.spell_conduit",
                "node.factory.turn_spell_conduit",
                "node.factory.turn_spell_conduit.mirror",
                "node.factory.spell_fusion.basic",
                "node.factory.spell_fusion.intermediate",
                "node.factory.spell_fusion.advanced"
            };

            var nodes = database.PlaceableNodes;
            foreach (string nodeId in requiredNodeIds)
            {
                if (!nodes.Any(node => node != null && node.Id == nodeId))
                {
                    return false;
                }
            }

            var shaper = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.element_shaping");
            if (shaper == null || shaper.Recipes.Count < 8)
            {
                return false;
            }

            var fusionOne = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.spell_fusion.basic");
            if (fusionOne == null || fusionOne.Recipes.Count < 8)
            {
                return false;
            }

            var fusionTwo = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.spell_fusion.intermediate");
            if (fusionTwo == null || fusionTwo.Recipes.Count < 4)
            {
                return false;
            }

            var fusionThree = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.spell_fusion.advanced");
            if (fusionThree == null || fusionThree.Recipes.Count < 4)
            {
                return false;
            }

            if (!nodes.Any(node => node != null && node.Id == "node.factory.deck_collector"))
            {
                return false;
            }

            if (!HasCurrentDefaultDemoLayout(database))
            {
                return false;
            }

            string[] requiredRecipeIds =
            {
                "recipe.shape.fire",
                "recipe.shape.water",
                "recipe.shape.wind",
                "recipe.shape.earth",
                "recipe.shape.ice",
                "recipe.shape.thunder",
                "recipe.shape.light",
                "recipe.shape.dark",
                "recipe.fusion.basic.fire",
                "recipe.fusion.basic.water",
                "recipe.fusion.basic.wind",
                "recipe.fusion.basic.earth",
                "recipe.fusion.basic.ice",
                "recipe.fusion.basic.thunder",
                "recipe.fusion.basic.light",
                "recipe.fusion.basic.dark",
                "recipe.fusion.intermediate.steam",
                "recipe.fusion.intermediate.tempest",
                "recipe.fusion.intermediate.prism",
                "recipe.fusion.intermediate.polarity",
                "recipe.fusion.advanced.steam",
                "recipe.fusion.advanced.tempest",
                "recipe.fusion.advanced.prism",
                "recipe.fusion.advanced.polarity"
            };

            var recipeIds = new HashSet<string>(
                nodes.Where(node => node != null)
                    .SelectMany(node => node.Recipes)
                    .Where(recipe => recipe != null)
                    .Select(recipe => recipe.Id));

            return requiredRecipeIds.All(recipeIds.Contains);
        }

        private static bool HasCurrentDefaultDemoLayout(WorkshopContentDatabase database)
        {
            var layout = database.DefaultLayout;
            return layout != null &&
                   layout.Length == 1 &&
                   ContainsSeed(layout, "node.factory.deck_collector", new Vector2Int(24, 24), 0);
        }

        public static WorkshopPlacedNodeSeed[] BuildHackFactoryLayout(WorkshopContentDatabase database)
        {
            if (database == null)
            {
                return new WorkshopPlacedNodeSeed[0];
            }

            WorkshopNodeDefinition fireSpirit = FindNodeDefinition(database, "node.spirit.fire");
            WorkshopNodeDefinition waterSpirit = FindNodeDefinition(database, "node.spirit.water");
            WorkshopNodeDefinition windSpirit = FindNodeDefinition(database, "node.spirit.wind");
            WorkshopNodeDefinition earthSpirit = FindNodeDefinition(database, "node.spirit.earth");
            WorkshopNodeDefinition iceSpirit = FindNodeDefinition(database, "node.spirit.ice");
            WorkshopNodeDefinition thunderSpirit = FindNodeDefinition(database, "node.spirit.thunder");
            WorkshopNodeDefinition lightSpirit = FindNodeDefinition(database, "node.spirit.light");
            WorkshopNodeDefinition darkSpirit = FindNodeDefinition(database, "node.spirit.dark");
            WorkshopNodeDefinition elementFusion = FindNodeDefinition(database, "node.factory.element_fusion");
            WorkshopNodeDefinition elementShaper = FindNodeDefinition(database, "node.factory.element_shaping");
            WorkshopNodeDefinition spellFusionBasic = FindNodeDefinition(database, "node.factory.spell_fusion.basic");
            WorkshopNodeDefinition spellFusionIntermediate = FindNodeDefinition(database, "node.factory.spell_fusion.intermediate");
            WorkshopNodeDefinition spellFusionAdvanced = FindNodeDefinition(database, "node.factory.spell_fusion.advanced");
            WorkshopNodeDefinition spellConduit = FindNodeDefinition(database, "node.factory.spell_conduit");
            WorkshopNodeDefinition deckCollector = FindNodeDefinition(database, "node.factory.deck_collector");

            var layout = new List<WorkshopPlacedNodeSeed>();

            AddElementFusionCluster(layout, waterSpirit, windSpirit, elementFusion, elementShaper, new Vector2Int(12, 40));
            AddElementFusionCluster(layout, fireSpirit, windSpirit, elementFusion, elementShaper, new Vector2Int(20, 40));
            AddElementFusionCluster(layout, earthSpirit, fireSpirit, elementFusion, elementShaper, new Vector2Int(28, 40));
            AddElementFusionCluster(layout, earthSpirit, waterSpirit, elementFusion, elementShaper, new Vector2Int(36, 40));

            AddFinalSpellChain(layout, fireSpirit, waterSpirit, elementShaper, spellFusionBasic, spellFusionIntermediate, spellFusionAdvanced, spellConduit, deckCollector, new Vector2Int(15, 34));
            AddFinalSpellChain(layout, windSpirit, earthSpirit, elementShaper, spellFusionBasic, spellFusionIntermediate, spellFusionAdvanced, spellConduit, deckCollector, new Vector2Int(35, 34));
            AddFinalSpellChain(layout, lightSpirit, darkSpirit, elementShaper, spellFusionBasic, spellFusionIntermediate, spellFusionAdvanced, spellConduit, deckCollector, new Vector2Int(15, 18));
            AddFinalSpellChain(layout, iceSpirit, thunderSpirit, elementShaper, spellFusionBasic, spellFusionIntermediate, spellFusionAdvanced, spellConduit, deckCollector, new Vector2Int(35, 18));

            return layout.ToArray();
        }

        private static WorkshopNodeDefinition FindNodeDefinition(WorkshopContentDatabase database, string nodeId)
        {
            return database.PlaceableNodes.First(node => node != null && node.Id == nodeId);
        }

        private static void AddElementFusionCluster(List<WorkshopPlacedNodeSeed> layout, WorkshopNodeDefinition westSpirit, WorkshopNodeDefinition southSpirit, WorkshopNodeDefinition elementFusion, WorkshopNodeDefinition elementShaper, Vector2Int fusionCell)
        {
            layout.Add(WorkshopPlacedNodeSeed.Create(westSpirit, fusionCell + Vector2Int.left, 0));
            layout.Add(WorkshopPlacedNodeSeed.Create(southSpirit, fusionCell + Vector2Int.down, 3));
            layout.Add(WorkshopPlacedNodeSeed.Create(elementFusion, fusionCell, 0));
            layout.Add(WorkshopPlacedNodeSeed.Create(elementShaper, fusionCell + Vector2Int.right, 0));
        }

        private static void AddFinalSpellChain(List<WorkshopPlacedNodeSeed> layout, WorkshopNodeDefinition westFusionSpirit, WorkshopNodeDefinition southFusionSpirit, WorkshopNodeDefinition elementShaper, WorkshopNodeDefinition spellFusionBasic, WorkshopNodeDefinition spellFusionIntermediate, WorkshopNodeDefinition spellFusionAdvanced, WorkshopNodeDefinition spellConduit, WorkshopNodeDefinition deckCollector, Vector2Int finalFusionCell)
        {
            Vector2Int westFusionTwoCell = finalFusionCell + new Vector2Int(-4, 0);
            Vector2Int southFusionTwoCell = finalFusionCell + new Vector2Int(0, -4);

            AddSpellFusionTwoBranch(layout, westFusionSpirit, southFusionSpirit, elementShaper, spellFusionBasic, spellFusionIntermediate, westFusionTwoCell, 0);
            AddSpellFusionTwoBranch(layout, westFusionSpirit, southFusionSpirit, elementShaper, spellFusionBasic, spellFusionIntermediate, southFusionTwoCell, 3);
            AddSpellConduitRun(layout, spellConduit, westFusionTwoCell + Vector2Int.right, finalFusionCell + Vector2Int.left, 0);
            AddSpellConduitRun(layout, spellConduit, southFusionTwoCell + Vector2Int.up, finalFusionCell + Vector2Int.down, 3);

            layout.Add(WorkshopPlacedNodeSeed.Create(spellFusionAdvanced, finalFusionCell, 0));
            layout.Add(WorkshopPlacedNodeSeed.Create(spellConduit, finalFusionCell + Vector2Int.right, 0));
            layout.Add(WorkshopPlacedNodeSeed.Create(deckCollector, finalFusionCell + new Vector2Int(2, 0), 0));
        }

        private static void AddSpellFusionTwoBranch(List<WorkshopPlacedNodeSeed> layout, WorkshopNodeDefinition westFusionSpirit, WorkshopNodeDefinition southFusionSpirit, WorkshopNodeDefinition elementShaper, WorkshopNodeDefinition spellFusionBasic, WorkshopNodeDefinition spellFusionIntermediate, Vector2Int fusionTwoCell, int fusionTwoRotation)
        {
            AddSpellFusionBasicWestFeed(layout, westFusionSpirit, elementShaper, spellFusionBasic, fusionTwoCell + Vector2Int.left);
            AddSpellFusionBasicNorthFeed(layout, southFusionSpirit, elementShaper, spellFusionBasic, fusionTwoCell + Vector2Int.down);
            layout.Add(WorkshopPlacedNodeSeed.Create(spellFusionIntermediate, fusionTwoCell, fusionTwoRotation));
        }

        private static void AddSpellConduitRun(List<WorkshopPlacedNodeSeed> layout, WorkshopNodeDefinition spellConduit, Vector2Int startCell, Vector2Int endCell, int rotation)
        {
            Vector2Int direction = new Vector2Int(Mathf.Clamp(endCell.x - startCell.x, -1, 1), Mathf.Clamp(endCell.y - startCell.y, -1, 1));
            Vector2Int cell = startCell;
            while (true)
            {
                layout.Add(WorkshopPlacedNodeSeed.Create(spellConduit, cell, rotation));
                if (cell == endCell)
                {
                    break;
                }

                cell += direction;
            }
        }

        private static void AddSpellFusionBasicWestFeed(List<WorkshopPlacedNodeSeed> layout, WorkshopNodeDefinition spirit, WorkshopNodeDefinition elementShaper, WorkshopNodeDefinition spellFusionBasic, Vector2Int fusionCell)
        {
            layout.Add(WorkshopPlacedNodeSeed.Create(spirit, fusionCell + new Vector2Int(-2, 0), 0));
            layout.Add(WorkshopPlacedNodeSeed.Create(elementShaper, fusionCell + new Vector2Int(-1, 0), 0));
            layout.Add(WorkshopPlacedNodeSeed.Create(spirit, fusionCell + new Vector2Int(0, -2), 3));
            layout.Add(WorkshopPlacedNodeSeed.Create(elementShaper, fusionCell + new Vector2Int(0, -1), 3));
            layout.Add(WorkshopPlacedNodeSeed.Create(spellFusionBasic, fusionCell, 0));
        }

        private static void AddSpellFusionBasicNorthFeed(List<WorkshopPlacedNodeSeed> layout, WorkshopNodeDefinition spirit, WorkshopNodeDefinition elementShaper, WorkshopNodeDefinition spellFusionBasic, Vector2Int fusionCell)
        {
            layout.Add(WorkshopPlacedNodeSeed.Create(spirit, fusionCell + new Vector2Int(0, -2), 3));
            layout.Add(WorkshopPlacedNodeSeed.Create(elementShaper, fusionCell + new Vector2Int(0, -1), 3));
            layout.Add(WorkshopPlacedNodeSeed.Create(spirit, fusionCell + new Vector2Int(2, 0), 2));
            layout.Add(WorkshopPlacedNodeSeed.Create(elementShaper, fusionCell + new Vector2Int(1, 0), 2));
            layout.Add(WorkshopPlacedNodeSeed.Create(spellFusionBasic, fusionCell, 3));
        }

        private static bool ContainsSeed(WorkshopPlacedNodeSeed[] layout, string nodeId, Vector2Int position, int rotation)
        {
            return layout.Any(seed =>
                seed != null &&
                seed.NodeDefinition != null &&
                seed.NodeDefinition.Id == nodeId &&
                seed.Position == position &&
                seed.RotationQuarterTurns == rotation);
        }
    }
}
