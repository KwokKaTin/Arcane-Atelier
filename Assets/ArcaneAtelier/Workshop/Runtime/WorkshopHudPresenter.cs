using System.Linq;
using ArcaneAtelier;
using ArcaneAtelier.Audio;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopHudPresenter : MonoBehaviour
    {
        private const float Margin = 5f;
        private const float ThroughputPanelWidth = 352f;
        private const float ControlPanelWidth = 236f;
        private const float TopPanelGap = 20f;
        private const float RightRailWidth = 380f;
        private const float PaletteVerticalScale = 0.8f;
        private const float PaletteHeaderHeightBase = 124f;
        private const float PaletteCardSpacingBase = 12f;
        private const float PaletteCardHeightBase = 108f;
        private const float PaletteBottomPaddingBase = 16f;
        private const int PaletteVisibleRows = 2;
        private const float ThroughputPanelHeight = 126f;
        private const float ControlPanelHeight = 76f;
        private const float StatusPanelHeight = 76f;
        private const float LegacySigilStripHeight = 44f;
        private const float LegacySigilStripOffsetY = 82f;
        private const float TopHudHeight = 136f;

        private static float PaletteHeaderHeight => PaletteHeaderHeightBase * PaletteVerticalScale;
        private static float PaletteCardSpacing => PaletteCardSpacingBase * PaletteVerticalScale;
        private static float PaletteCardHeight => PaletteCardHeightBase * PaletteVerticalScale;
        private static float PaletteBottomPadding => PaletteBottomPaddingBase * PaletteVerticalScale;
        private static float BottomDockHeight => PaletteHeaderHeight + PaletteVisibleRows * PaletteCardHeight + (PaletteVisibleRows + 1) * PaletteCardSpacing + PaletteBottomPadding;

        private static readonly Color HudBackground = new Color(0.035f, 0.052f, 0.085f, 0.94f);
        private static readonly Color HudPanel = new Color(0.075f, 0.105f, 0.15f, 0.93f);
        private static readonly Color HudPanelSoft = new Color(0.095f, 0.13f, 0.19f, 0.82f);
        private static readonly Color HudStroke = new Color(0.24f, 0.3f, 0.38f, 0.86f);
        private static readonly Color HudText = new Color(0.97f, 0.95f, 0.9f, 1f);
        private static readonly Color HudMuted = new Color(0.65f, 0.71f, 0.78f, 1f);
        private static readonly Color AtelierGold = new Color(0.88f, 0.72f, 0.3f, 1f);
        private static readonly Color ArcaneBlue = new Color(0.42f, 0.72f, 0.94f, 1f);
        private static readonly Color SpellViolet = new Color(0.72f, 0.5f, 0.96f, 1f);

        private Vector2 rewardScroll;
        private Vector2 guideScroll;
        private bool showGuide;
        private bool showRewards;
        private bool showReturnToMenuPrompt;
        private int paletteTabIndex;
        private WorkshopSceneController controller;

        private Texture2D whiteTexture;
        private Texture2D circleTexture;
        private Sprite panelMainSprite;
        private Sprite panelSubSprite;
        private Sprite statusBarSprite;
        private Sprite topLeftPanelSprite;
        private Sprite rightRailPanelSprite;
        private Sprite paletteDockSprite;
        private Sprite subPanelColumnSprite;
        private Sprite ornateFrameSprite;
        private Sprite buttonSprite;
        private Sprite buttonSmallSprite;
        private Sprite tabActiveSprite;
        private Sprite tabInactiveSprite;
        private Sprite blueprintCardSprite;
        private Sprite slotFrameSprite;
        private Sprite tooltipFrameSprite;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle statValueStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle iconStyle;
        private GUIStyle chipStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardBodyStyle;
        private GUIStyle statusBarStyle;
        private GUIStyle blueprintTitleStyle;
        private GUIStyle blueprintMetaStyle;
        private GUIStyle blueprintModeStyle;
        private GUIStyle tinyLabelStyle;
        private GUIStyle centeredTinyLabelStyle;
        private GUIStyle compactRowLabelStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle tooltipPrimaryStyle;
        private GUIStyle tooltipSecondaryStyle;
        private GUIStyle tooltipEmptyStyle;
        private string hoveredMetaTitle = string.Empty;
        private string hoveredMetaBody = string.Empty;
        private Color hoveredMetaAccent = Color.white;

        public void Initialize(WorkshopSceneController sceneController)
        {
            controller = sceneController;
        }

        public void Repaint()
        {
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (showReturnToMenuPrompt)
                {
                    HideReturnToMenuPrompt();
                    return;
                }

                showGuide = false;
                showRewards = false;
                ShowReturnToMenuPrompt();
                return;
            }

            if (showReturnToMenuPrompt)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                showGuide = !showGuide;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                showRewards = !showRewards;
            }
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            EnsureTheme();
            ClearMetaHover();
            ApplyCameraLayout();

            DrawBackdrop();

            Rect topLeftRect = BuildThroughputPanelRect();
            Rect topRightRect = BuildControlPanelRect();
            Rect topCenterRect = BuildStatusPanelRect(topLeftRect, topRightRect);
            Rect metaRect = BuildLegacySigilStripRect(topLeftRect, topRightRect);
            Rect rightRailRect = BuildRightRailRect(metaRect);
            var paletteRect = new Rect(Margin, Screen.height - BottomDockHeight - Margin, Screen.width - Margin * 2f, BottomDockHeight);

            bool previousGuiEnabled = GUI.enabled;
            if (showReturnToMenuPrompt)
            {
                GUI.enabled = false;
            }

            DrawThroughputPanel(topLeftRect);
            DrawStatusPanel(topCenterRect);
            DrawControlPanel(topRightRect);
            DrawRightRail(rightRailRect);
            DrawPaletteDock(paletteRect);
            DrawLegacySigilStrip(metaRect);
            GUI.enabled = previousGuiEnabled;

            if (showRewards)
            {
                DrawRewardDrawer(GetRewardDrawerRect());
            }

            DrawHoverTooltip();
            DrawMetaHoverTooltip();

            if (showGuide)
            {
                DrawGuideOverlay(BuildGuideOverlayRect());
            }

            if (showReturnToMenuPrompt)
            {
                DrawReturnToMenuPrompt(BuildReturnToMenuPromptRect());
            }
        }

        private void DrawBackdrop()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.015f, 0.026f, 0.14f));
            DrawRect(new Rect(0f, 0f, Screen.width, 3f), new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.64f));
            DrawRect(new Rect(0f, TopHudHeight + Margin - 2f, Screen.width, 2f), new Color(ArcaneBlue.r, ArcaneBlue.g, ArcaneBlue.b, 0.22f));
            DrawRect(new Rect(0f, Screen.height - BottomDockHeight - Margin * 2f, Screen.width, BottomDockHeight + Margin * 2f), new Color(0.015f, 0.025f, 0.045f, 0.82f));
            DrawRect(new Rect(0f, Screen.height - BottomDockHeight - Margin * 2f, Screen.width, 3f), new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.3f));
            DrawRect(new Rect(Screen.width - RightRailWidth - Margin * 2f, TopHudHeight, RightRailWidth + Margin * 2f, Screen.height - TopHudHeight), new Color(0f, 0f, 0f, 0.12f));
        }

        private void DrawThroughputPanel(Rect rect)
        {
            var stats = controller != null && controller.Simulation != null
                ? controller.BuildFlowStatsView()
                : new WorkshopFlowStatsView(0f, 0f, 0f, 0f);
            DrawRegionFrame(rect, topLeftPanelSprite, new Color(0.78f, 0.61f, 0.31f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(32f, 18f, 220f, 24f), "Arcane Atelier", titleStyle);
            GUI.Label(new Rect(32f, 44f, 220f, 18f), "Workshop", mutedStyle);

            DrawMiniStat(new Rect(32f, 72f, 58f, 40f), $"{controller.RemainingPreparationTicks}", "Ticks");
            DrawMiniStat(new Rect(96f, 72f, 58f, 40f), $"{stats.ElementProductionPerSecond:0.0}", "Flow");
            DrawMiniStat(new Rect(160f, 72f, 52f, 40f), $"{stats.SpellProductionPerSecond:0.0}", "Spell");
            DrawMiniStat(new Rect(218f, 72f, 52f, 40f), $"{stats.ElementConsumptionPerSecond:0.0}", "Use");
            DrawMiniStat(new Rect(276f, 72f, 56f, 40f), $"{controller.Tokens}", "Tokens");
            GUI.EndGroup();
        }

        private void DrawStatusPanel(Rect rect)
        {
            DrawRegionFrame(rect, statusBarSprite, new Color(0.54f, 0.4f, 0.85f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(60f, 12f, rect.width - 68f, 18f), "Status", sectionStyle);
            GUI.Label(new Rect(60f, 32f, rect.width - 80f, 36f), controller.StatusMessage, statusBarStyle);
            GUI.EndGroup();
        }

        private void DrawControlPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.92f, 0.45f, 0.24f));

            GUI.BeginGroup(rect);
            const float buttonSize = 26f;
            const float buttonGap = 6f;
            const float rotationChipWidth = 25f;
            var buttonStartX = rect.width - 22f - (rotationChipWidth + 12f + buttonSize * 5f + buttonGap * 4f);
            DrawRect(new Rect(buttonStartX, 17f, rotationChipWidth, 24f), new Color(0.1f, 0.12f, 0.16f, 0.94f));
            DrawOutline(new Rect(buttonStartX, 17f, rotationChipWidth, 24f), new Color(0.32f, 0.28f, 0.22f));
            GUI.Label(new Rect(buttonStartX, 21f, rotationChipWidth, 14f), $"{controller.PlacementRotationQuarterTurns * 90}°", chipStyle);
            buttonStartX += rotationChipWidth + 12f;

            if (DrawThemedButton(new Rect(buttonStartX, 15f, buttonSize, buttonSize), "?", ArcaneBlue, smallButtonStyle, "toggle_guide"))
            {
                showGuide = !showGuide;
            }

            if (DrawThemedButton(new Rect(buttonStartX + buttonSize + buttonGap, 15f, buttonSize, buttonSize), "✦", SpellViolet, smallButtonStyle, "toggle_rewards"))
            {
                showRewards = !showRewards;
            }

            var pauseLabel = controller.IsPaused ? "▶" : "⏸";
            if (DrawThemedButton(new Rect(buttonStartX + (buttonSize + buttonGap) * 2f, 15f, buttonSize, buttonSize), pauseLabel, AtelierGold, smallButtonStyle, "toggle_pause"))
            {
                controller.TogglePause();
            }

            if (DrawThemedButton(new Rect(buttonStartX + (buttonSize + buttonGap) * 3f, 15f, buttonSize, buttonSize), "↺", new Color(0.9f, 0.5f, 0.34f, 1f), smallButtonStyle, "reset_workshop"))
            {
                controller.ResetWorkshop();
            }

            if (DrawThemedButton(new Rect(buttonStartX + (buttonSize + buttonGap) * 4f, 15f, buttonSize, buttonSize), "H", new Color(0.54f, 0.78f, 0.54f, 1f), smallButtonStyle, "load_hack_layout"))
            {
                controller.LoadHackFactoryLayout();
            }
            GUI.EndGroup();
        }

        private void DrawLegacySigilStrip(Rect rect)
        {
            DrawRect(rect, new Color(0.045f, 0.06f, 0.09f, 0.88f));
            DrawOutline(rect, new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.5f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.72f));
            DrawRect(new Rect(rect.x + rect.width * 0.5f - 0.5f, rect.y + 5f, 1f, rect.height - 10f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.45f));

            float leftX = rect.x + 10f;
            float rightX = rect.x + rect.width * 0.5f + 10f;
            float y = rect.y + 8f;
            float segmentHeight = rect.height - 16f;
            float gap = 5f;
            float maxLeftWidth = rect.width * 0.5f - 22f;
            float maxRightWidth = rect.width * 0.5f - 20f;
            int boonTokenBonus = Mathf.Max(0, MetaProgressionStore.GetVictoryTokenBonus() - (MetaProgressionStore.ActiveOmen != null ? MetaProgressionStore.ActiveOmen.VictoryTokenBonus : 0));
            LegacyOmenView omen = MetaProgressionStore.ActiveOmen;
            float segmentWidth = Mathf.Clamp(
                Mathf.Min(
                    (maxLeftWidth - gap * 5f) / 6f,
                    (maxRightWidth - gap * (omen != null ? 3f : 2f)) / (omen != null ? 4.15f : 3f)),
                22f,
                32f);
            float omenWidth = segmentWidth * 1.15f;

            float usedLeft = 0f;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Prep, MetaProgressionStore.GetPreparationTickBonus() / 60f, AtelierGold, "Kindled Start", $"+{MetaProgressionStore.GetPreparationTickBonus()} prep ticks at run start.", "meta_prep") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Shield, MetaProgressionStore.GetOpeningShieldBonus() / 12f, ArcaneBlue, "Warden Reserve", $"+{MetaProgressionStore.GetOpeningShieldBonus()} opening shield each battle.", "meta_ward") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Tokens, MetaProgressionStore.GetStartingRunTokens() / 55f, SpellViolet, "Ember Float", $"+{MetaProgressionStore.GetStartingRunTokens()} Tokens before the workshop store opens.", "meta_tokens") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Vitality, MetaProgressionStore.GetPlayerMaxHealthBonus() / 12f, new Color(0.72f, 0.94f, 0.52f, 1f), "Vital Script", $"+{MetaProgressionStore.GetPlayerMaxHealthBonus()} max HP in battle.", "meta_vital") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Healing, MetaProgressionStore.GetVictoryHealBonus() / 9f, new Color(0.46f, 0.9f, 0.7f, 1f), "Afterglow Seal", $"+{MetaProgressionStore.GetVictoryHealBonus()} healing after each win.", "meta_heal") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Bounty, boonTokenBonus / 28f, new Color(0.95f, 0.56f, 0.28f, 1f), "Bounty Seal", $"+{boonTokenBonus} Tokens added to victory payouts.", "meta_bounty");
            DrawGhostMetaSlots(new Rect(leftX + Mathf.Min(usedLeft + gap, maxLeftWidth - (segmentWidth * 2f + gap)), y, maxLeftWidth - Mathf.Min(usedLeft + gap, maxLeftWidth), segmentHeight), segmentWidth, gap);

            int pressureHealth = Mathf.Max(0, Mathf.RoundToInt((MetaProgressionStore.GetEnemyHealthScaleMultiplier() - 1f) * 100f));
            int pressureDamage = Mathf.Max(0, Mathf.RoundToInt((MetaProgressionStore.GetEnemyDamageScaleMultiplier() - 1f) * 100f));
            int pressureWard = MetaProgressionStore.GetEnemyStartingShieldBonus();

            float usedRight = 0f;
            usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, segmentWidth, segmentHeight), MetaHudIconKind.BreachPressure, pressureHealth / 120f, new Color(0.86f, 0.34f, 0.28f, 1f), "Breach Vitality", $"+{pressureHealth}% enemy health this cycle.", "pressure_hp") + gap;
            usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, segmentWidth, segmentHeight), MetaHudIconKind.BreachPressure, pressureDamage / 90f, new Color(0.94f, 0.5f, 0.22f, 1f), "Breach Force", $"+{pressureDamage}% enemy damage this cycle.", "pressure_dmg") + gap;
            usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, segmentWidth, segmentHeight), MetaHudIconKind.BreachPressure, pressureWard / 12f, ArcaneBlue, "Breach Ward", $"+{pressureWard} starting shield on enemies.", "pressure_ward") + gap;
            if (omen != null)
            {
                usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, omenWidth, segmentHeight), MetaHudIconKind.Omen, 1f, SpellViolet, omen.DisplayName, omen.Description, "pressure_omen");
            }

            DrawGhostMetaSlots(new Rect(rightX + Mathf.Min(usedRight + gap, maxRightWidth - (segmentWidth * 2f + gap)), y, maxRightWidth - Mathf.Min(usedRight + gap, maxRightWidth), segmentHeight), segmentWidth, gap);
        }

        private float DrawMetaSegment(Rect rect, MetaHudIconKind iconKind, float normalizedValue, Color accent, string title, string body, string hoverId)
        {
            float clamped = Mathf.Clamp01(normalizedValue);
            DrawRect(rect, new Color(0.05f, 0.08f, 0.12f, 0.94f));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.42f));
            DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), new Color(accent.r, accent.g, accent.b, Mathf.Lerp(0.06f, 0.16f, clamped)));

            float iconSize = Mathf.Min(rect.width - 8f, rect.height - 8f);
            Rect iconRect = new Rect(rect.x + (rect.width - iconSize) * 0.5f, rect.y + 2f, iconSize, iconSize);
            DrawMetaIcon(iconRect, iconKind);

            Rect meterRect = new Rect(rect.x + 4f, rect.yMax - 4f, rect.width - 8f, 2f);
            DrawRect(meterRect, new Color(0.1f, 0.13f, 0.18f, 0.92f));
            if (clamped > 0.001f)
            {
                DrawRect(new Rect(meterRect.x, meterRect.y, Mathf.Max(2f, meterRect.width * clamped), meterRect.height), new Color(accent.r, accent.g, accent.b, 0.96f));
            }

            TryRegisterMetaHover(rect, title, body, accent, hoverId);
            return rect.width;
        }

        private void DrawMetaIcon(Rect rect, MetaHudIconKind kind)
        {
            Texture2D atlas = MetaHudIconAtlas.GetTexture();
            if (atlas == null)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(rect, atlas, MetaHudIconAtlas.GetUv(kind), true);
            GUI.color = previous;
        }

        private void DrawGhostMetaSlots(Rect rect, float segmentWidth, float gap)
        {
            if (rect.width < segmentWidth)
            {
                return;
            }

            int count = Mathf.FloorToInt((rect.width + gap) / (segmentWidth + gap));
            for (int i = 0; i < count; i++)
            {
                Rect slotRect = new Rect(rect.x + i * (segmentWidth + gap), rect.y, segmentWidth, rect.height);
                DrawRect(slotRect, new Color(0.06f, 0.08f, 0.11f, 0.48f));
                DrawOutline(slotRect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.22f));
            }
        }

        private void TryRegisterMetaHover(Rect rect, string title, string body, Color accent, string hoverId)
        {
            Event current = Event.current;
            if (current == null || !rect.Contains(current.mousePosition))
            {
                return;
            }

            hoveredMetaTitle = title;
            hoveredMetaBody = body;
            hoveredMetaAccent = accent;
            AudioManager.ReportUIHover($"workshop:{hoverId}");
        }

        private void DrawMetaHoverTooltip()
        {
            if (string.IsNullOrWhiteSpace(hoveredMetaTitle))
            {
                return;
            }

            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            float width = 312f;
            float contentWidth = width - 56f;
            float titleHeight = CalculateTooltipTextHeight(sectionStyle, hoveredMetaTitle, contentWidth, 20f);
            float bodyHeight = CalculateTooltipTextHeight(bodyStyle, hoveredMetaBody, contentWidth, 20f);
            Rect rect = PositionUiTooltip(mouse, width, 30f + titleHeight + bodyHeight);
            DrawTooltipFrame(rect, hoveredMetaAccent);
            GUI.BeginGroup(rect);
            float contentY = 12f;
            GUI.Label(new Rect(28f, contentY, contentWidth, titleHeight), hoveredMetaTitle, sectionStyle);
            contentY += titleHeight + 6f;
            GUI.Label(new Rect(28f, contentY, contentWidth, bodyHeight), hoveredMetaBody, bodyStyle);
            GUI.EndGroup();
        }

        private void ClearMetaHover()
        {
            hoveredMetaTitle = string.Empty;
            hoveredMetaBody = string.Empty;
            hoveredMetaAccent = Color.white;
        }

        private void DrawRightRail(Rect rect)
        {
            DrawTallRegionFrame(rect, rightRailPanelSprite, new Color(0.37f, 0.72f, 0.94f));
            //DrawRect(new Rect(rect.x + 28f, rect.y + 14f, rect.width - 56f, 58f), new Color(0.03f, 0.045f, 0.07f, 0.7f));

            GUI.BeginGroup(rect);
            var contentX = 30f;
            var contentWidth = rect.width - 60f;
            GUI.Label(new Rect(contentX+130f, 20f, contentWidth, 20f), "Selected", sectionStyle);
            GUI.Label(new Rect(contentX+77f, 40f, contentWidth, 18f), $"{controller.EncounterLabel}  {controller.RemainingPreparationTicks}/{controller.TotalPreparationTicks} ticks", tinyLabelStyle);
            GUI.Label(new Rect(contentX+135f, 55f, contentWidth, 18f), $"Cell {controller.SelectedCell.x}, {controller.SelectedCell.y}", tinyLabelStyle);

            var node = controller.SelectedNode;
            const float detailBottom = 166f;
            if (node == null)
            {
                DrawSubPanel(new Rect(contentX + 5f, 90f, contentWidth - 10f, 46f), ArcaneBlue);
                GUI.Label(new Rect(contentX + 21f, 94f, contentWidth - 32f, 30f), "Choose a tile to inspect a machine. Place with LMB on empty cells.", bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(contentX, 74f, contentWidth, 22f), node.Definition.DisplayName, sectionStyle);
                GUI.Label(new Rect(contentX, 96f, contentWidth, 18f), node.Definition.Category.ToString(), tinyLabelStyle);
                GUI.Label(new Rect(contentX, 116f, contentWidth, 18f), $"Rot {node.RotationQuarterTurns * 90}°   Spd x{node.SpeedMultiplier:0.00}", mutedStyle);

                var bufferRows = node.EnumerateBuffer()
                    .Take(rect.height < 420f ? 2 : 3)
                    .Select(pair => (ShortItemName(pair.Key.DisplayName), pair.Value, pair.Key.Tint, pair.Key))
                    .ToArray();

                var y = 140f;
                if (bufferRows.Length > 0)
                {
                    DrawCompactList(new Rect(contentX, y, contentWidth, bufferRows.Length * 18f), bufferRows);
                    y += bufferRows.Length * 18f + 8f;
                }
                else
                {
                    GUI.Label(new Rect(contentX, y, contentWidth, 18f), "Buffer empty", tinyLabelStyle);
                    y += 24f;
                }

                GUI.Label(new Rect(contentX, y + 2f, contentWidth, 18f), "R rotate   RMB remove", tinyLabelStyle);
            }

            var inventory = controller.BuildInventoryView();
            var deployButtonY = rect.height - 42f;
            var stepButtonY = deployButtonY - 34f;
            var payloadBottom = stepButtonY - 14f;
            var payloadTop = detailBottom + 18f;
            var payloadHeight = Mathf.Max(150f, payloadBottom - payloadTop);
            var columnGap = 12f;
            var columnWidth = (contentWidth - columnGap) * 0.5f;
            var inventoryItems = inventory.NetworkItems
                .OrderBy(pair => pair.Key.DisplayName)
                .Select(pair => (ShortItemName(pair.Key.DisplayName), pair.Value, pair.Key.Tint, pair.Key))
                .ToArray();
            var deckItems = inventory.PreparedCards
                .OrderBy(pair => pair.Key.DisplayName)
                .Select(pair => (ShortItemName(pair.Key.DisplayName), pair.Value, pair.Key.Tint, pair.Key))
                .ToArray();

            DrawRect(new Rect(contentX, detailBottom, contentWidth, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.66f));

            DrawSubPanel(new Rect(contentX, payloadTop, columnWidth, payloadHeight), ArcaneBlue);
            GUI.Label(new Rect(contentX + 14f, payloadTop + 14f, columnWidth - 28f, 20f), "Inventory", sectionStyle);
            GUI.Label(new Rect(contentX + 14f, payloadTop + 34f, columnWidth - 28f, 18f), "Network + reserve", tinyLabelStyle);
            DrawCompactList(new Rect(contentX + 14f, payloadTop + 60f, columnWidth - 28f, payloadHeight - 76f), inventoryItems);

            var deckX = contentX + columnWidth + columnGap;
            DrawSubPanel(new Rect(deckX, payloadTop, columnWidth, payloadHeight), AtelierGold);
            GUI.Label(new Rect(deckX + 14f, payloadTop + 14f, columnWidth - 28f, 20f), "Battle Deck", sectionStyle);
            GUI.Label(new Rect(deckX + 14f, payloadTop + 34f, columnWidth - 28f, 18f), "Cards that reached collectors", tinyLabelStyle);
            DrawCompactList(new Rect(deckX + 14f, payloadTop + 60f, columnWidth - 28f, payloadHeight - 76f), deckItems);

            if (DrawThemedButton(new Rect(contentX, stepButtonY, contentWidth, 30f), "Advance 1 Prep Tick", ArcaneBlue, buttonStyle, "advance_tick"))
            {
                controller.StepPreparationOnce();
            }

            if (DrawThemedButton(new Rect(contentX, deployButtonY, contentWidth, 30f), "Forge And Deploy", AtelierGold, buttonStyle, "forge_and_deploy", playClickSound: false))
            {
                controller.DeployToBattle();
            }

            GUI.EndGroup();
        }

        private void DrawRewardDrawer(Rect rect)
        {
            DrawTallRegionFrame(rect, ornateFrameSprite, new Color(0.57f, 0.45f, 0.89f));
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(28f, 22f, rect.width - 56f, 20f), "Atelier Exchange", sectionStyle);
            int wallet = controller.Tokens;
            GUI.Label(new Rect(28f, 42f, rect.width - 56f, 18f), $"Tokens: {wallet}   ·   TAB or ✦ to close", tinyLabelStyle);

            var contentRect = new Rect(26f, 70f, rect.width - 52f, rect.height - 92f);
            var rewards = controller.DebugRewards
                .Where(reward => reward != null && reward.TokenCost > 0)
                .ToArray();

            if (rewards.Length == 0)
            {
                GUI.Label(new Rect(14f, 70f, contentRect.width, 40f), "No purchasable boons yet. Win breaches to earn tokens.", mutedStyle);
                GUI.EndGroup();
                return;
            }

            const float itemHeight = 142f;
            var viewHeight = rewards.Length * (itemHeight + 12f) + 8f;
            rewardScroll = GUI.BeginScrollView(contentRect, rewardScroll, new Rect(0f, 0f, contentRect.width - 18f, viewHeight), false, true);

            var y = 0f;
            foreach (var reward in rewards)
            {
                var itemRect = new Rect(0f, y, contentRect.width - 24f, itemHeight);
                DrawSubPanel(itemRect, SpellViolet);
                DrawRewardIcon(new Rect(12f, y + 12f, 42f, 42f), reward);

                float textWidth = Mathf.Max(80f, itemRect.width - 78f);
                GUI.Label(new Rect(62f, y + 10f, textWidth, 18f), reward.DisplayName, sectionStyle);
                GUI.Label(new Rect(62f, y + 32f, textWidth, 56f), reward.Description, bodyStyle);

                int cost = reward.TokenCost;
                bool owned = controller.IsRewardAlreadyOwned(reward);
                bool canAfford = wallet >= cost;
                string stateLabel = owned ? "Owned" : canAfford ? "Available" : "Need Tokens";
                string buyLabel = owned ? "Owned" : canAfford ? $"Buy {cost}" : $"{cost}";
                Color buyColor = owned ? ArcaneBlue : canAfford ? AtelierGold : HudMuted;

                Rect actionRect = new Rect(12f, y + 96f, itemRect.width - 24f, 34f);
                DrawRect(new Rect(actionRect.x, actionRect.y, actionRect.width, actionRect.height), new Color(0.04f, 0.055f, 0.082f, 0.72f));
                DrawOutline(new Rect(actionRect.x, actionRect.y, actionRect.width, actionRect.height), new Color(buyColor.r, buyColor.g, buyColor.b, 0.45f));
                GUI.Label(new Rect(actionRect.x + 10f, actionRect.y + 8f, actionRect.width * 0.46f, 16f), stateLabel, tinyLabelStyle);
                //GUI.Label(new Rect(actionRect.x + actionRect.width * 0.42f, actionRect.y + 8f, actionRect.width * 0.22f, 16f), $"{cost} Tokens", centeredTinyLabelStyle);

                bool previouslyEnabled = GUI.enabled;
                GUI.enabled = canAfford && !owned;
                if (DrawThemedButton(new Rect(actionRect.x + actionRect.width - 86f, actionRect.y + 6f, 74f, 22f), buyLabel, buyColor, buttonStyle, $"reward_buy_{reward.Id}"))
                {
                    controller.TryPurchaseReward(reward.Id, out _, out _);
                    wallet = controller.Tokens;
                }
                GUI.enabled = previouslyEnabled;

                y += itemHeight + 12f;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawGuideOverlay(Rect rect)
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
            DrawTallRegionFrame(rect, ornateFrameSprite, new Color(0.88f, 0.74f, 0.33f));
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(85f, 45f, rect.width - 156f, 24f), "Workshop Guide", titleStyle);
            GUI.Label(new Rect(85f, 75f, rect.width - 156f, 40f), "Start from an empty floor, place machines from the palette, route crafted spell cards into the battle deck collector, then enter battle.", mutedStyle);
            if (DrawThemedButton(new Rect(rect.width - 78f, 30f, 38f, 28f), "X", new Color(0.9f, 0.5f, 0.34f, 1f), buttonStyle, "close_guide"))
            {
                showGuide = false;
            }

            Rect contentViewport = new Rect(38f, 108f, rect.width - 76f, rect.height - 134f);
            float columnGap = 18f;
            float availableWidth = contentViewport.width - columnGap;
            float columnWidth = Mathf.Max(280f, availableWidth * 0.5f);
            bool stackColumns = contentViewport.width < 760f;
            if (stackColumns)
            {
                columnWidth = contentViewport.width - 16f;
                columnGap = 0f;
            }

            float leftX = 8f;
            float rightX = stackColumns ? 8f : leftX + columnWidth + columnGap;
            float leftColumnHeight = 478f;
            float rightColumnHeight = 352f;
            float stackedOffsetY = stackColumns ? leftColumnHeight + 16f : 0f;
            float contentHeight = stackColumns
                ? leftColumnHeight + rightColumnHeight + 24f
                : Mathf.Max(leftColumnHeight, rightColumnHeight);

            guideScroll = GUI.BeginScrollView(contentViewport, guideScroll, new Rect(0f, 0f, contentViewport.width - 18f, contentHeight), false, true);

            DrawSubPanel(new Rect(leftX, 0f, columnWidth, 180f), AtelierGold);
            GUI.Label(new Rect(leftX + 35f, 14f, columnWidth - 28f, 24f), "Build From Empty", sectionStyle);
            GUI.Label(new Rect(leftX + 35f, 42f, columnWidth - 28f, 44f), "The workshop no longer starts with a preset starter layout. Choose blueprints from the palette and place every production line yourself.", bodyStyle);
            GUI.Label(new Rect(leftX + 35f, 90f, columnWidth - 28f, 44f), "Send finished spell cards through spell conduits into the Battle Deck Collector. Only collected cards are added to the next battle deck.", bodyStyle);
            GUI.Label(new Rect(leftX + 35f, 138f, columnWidth - 28f, 24f), "Wrong facing or blocked ports will stall a recipe.", tinyLabelStyle);

            DrawSubPanel(new Rect(leftX, 172f, columnWidth, 290f), ArcaneBlue);
            GUI.Label(new Rect(leftX + 35f, 186f, columnWidth - 28f, 24f), "Controls", sectionStyle);
            DrawGuideRow(new Rect(leftX + 35f, 218f, columnWidth - 28f, 24f), "LMB", "Click place/select, hold-drag pan map");
            DrawGuideRow(new Rect(leftX + 35f, 247f, columnWidth - 28f, 24f), "RMB Tile", "Remove selected tile");
            DrawGuideRow(new Rect(leftX + 35f, 276f, columnWidth - 28f, 24f), "RMB Card", "Arm mirror corner conduit");
            DrawGuideRow(new Rect(leftX + 35f, 305f, columnWidth - 28f, 24f), "R", "Rotate selected machine");
            DrawGuideRow(new Rect(leftX + 35f, 334f, columnWidth - 28f, 24f), "Q / E", "Rotate next placement");
            DrawGuideRow(new Rect(leftX + 35f, 363f, columnWidth - 28f, 38f), "Fusion Edge", "Click edge cycles input, output, off");
            DrawGuideRow(new Rect(leftX + 35f, 405f, columnWidth - 28f, 24f), "Wheel", "Zoom workshop map");
            DrawGuideRow(new Rect(leftX + 35f, 434f, columnWidth - 28f, 24f), "Esc", "Return to menu");

            DrawSubPanel(new Rect(rightX, stackedOffsetY, columnWidth, 208f), ArcaneBlue);
            GUI.Label(new Rect(rightX + 35f, stackedOffsetY + 14f, columnWidth - 28f, 24f), "Element Fusion", sectionStyle);
            DrawElementRecipeRow(new Rect(rightX + 35f, stackedOffsetY + 44f, columnWidth - 28f, 24f), WorkshopElementAttribute.Wind, WorkshopElementAttribute.Water, WorkshopElementAttribute.Ice);
            DrawElementRecipeRow(new Rect(rightX + 35f, stackedOffsetY + 74f, columnWidth - 28f, 24f), WorkshopElementAttribute.Wind, WorkshopElementAttribute.Fire, WorkshopElementAttribute.Thunder);
            DrawElementRecipeRow(new Rect(rightX + 35f, stackedOffsetY + 104f, columnWidth - 28f, 24f), WorkshopElementAttribute.Earth, WorkshopElementAttribute.Fire, WorkshopElementAttribute.Light);
            DrawElementRecipeRow(new Rect(rightX + 35f, stackedOffsetY + 134f, columnWidth - 28f, 24f), WorkshopElementAttribute.Earth, WorkshopElementAttribute.Water, WorkshopElementAttribute.Dark);

            DrawSubPanel(new Rect(rightX, stackedOffsetY + 196f, columnWidth, 186f), SpellViolet);
            GUI.Label(new Rect(rightX + 35f, stackedOffsetY + 210f, columnWidth - 28f, 24f), "Spell Ladder", sectionStyle);
            GUI.Label(new Rect(rightX + 35f, stackedOffsetY + 238f, columnWidth - 28f, 28f), "Element Shaper: one element becomes one basic spell.", bodyStyle);
            GUI.Label(new Rect(rightX + 35f, stackedOffsetY + 268f, columnWidth - 28f, 44f), "Spell Fusion I:\ntwo same-element basic spells become an intermediate spell.", bodyStyle);
            GUI.Label(new Rect(rightX + 35f, stackedOffsetY + 306f, columnWidth - 28f, 28f), "Spell Fusion II: compatible intermediate spells become advanced spells.", bodyStyle);
            GUI.Label(new Rect(rightX + 35f, stackedOffsetY + 336f, columnWidth - 28f, 24f), "Spell Fusion III: two matching advanced spells become final cards.", tinyLabelStyle);

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawReturnToMenuPrompt(Rect rect)
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.62f));
            DrawTallRegionFrame(rect, ornateFrameSprite, AtelierGold);

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(42f, 34f, rect.width - 84f, 28f), "Return To Main Menu?", titleStyle);
            GUI.Label(
                new Rect(44f, 76f, rect.width - 88f, 54f),
                "Current workshop progress will be abandoned. Nothing from this run will be saved.",
                bodyStyle);

            float buttonWidth = 128f;
            float buttonHeight = 34f;
            float gap = 18f;
            float startX = (rect.width - buttonWidth * 2f - gap) * 0.5f;
            float buttonY = rect.height - 64f;

            if (DrawThemedButton(new Rect(startX, buttonY, buttonWidth, buttonHeight), "No", ArcaneBlue, buttonStyle, "cancel_return_to_menu"))
            {
                HideReturnToMenuPrompt();
            }

            if (DrawThemedButton(new Rect(startX + buttonWidth + gap, buttonY, buttonWidth, buttonHeight), "Yes", AtelierGold, buttonStyle, "confirm_return_to_menu"))
            {
                showReturnToMenuPrompt = false;
                controller.ReturnToMainMenuWithoutSaving();
            }

            GUI.EndGroup();
        }

        private void ShowReturnToMenuPrompt()
        {
            showReturnToMenuPrompt = true;
            showGuide = false;
            showRewards = false;
            controller.SetReturnToMenuPromptOpen(true);
        }

        private void HideReturnToMenuPrompt()
        {
            showReturnToMenuPrompt = false;
            controller.SetReturnToMenuPromptOpen(false);
        }

        private void DrawPaletteDock(Rect rect)
        {
            DrawRegionFrame(rect, paletteDockSprite, new Color(0.88f, 0.74f, 0.33f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(65f, ScalePaletteY(25f), 220f, 24f), "Workshop Palette", titleStyle);
            GUI.Label(new Rect(65f, ScalePaletteY(49f), rect.width - 68f, 18f), "Choose a blueprint. LMB arms default, RMB arms mirror on corner conduits. Short LMB places/selects; hold LMB and drag pans.", mutedStyle);
            DrawElementLegend(new Rect(65f, ScalePaletteY(69f), 272f, 18f));

            DrawPaletteTabs(new Rect(65f, ScalePaletteY(95f), rect.width - 68f, ScalePaletteY(28f)));

            var contentRect = new Rect(47f, PaletteHeaderHeight, rect.width - 80f, rect.height - PaletteHeaderHeight - PaletteBottomPadding);
            var nodes = GetPaletteNodesForActiveTab();
            int columns = Mathf.Max(1, Mathf.FloorToInt((contentRect.width + PaletteCardSpacing) / (312f + PaletteCardSpacing)));
            float cardWidth = Mathf.Floor((contentRect.width - PaletteCardSpacing * (columns + 1)) / columns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(nodes.Length / (float)columns));
            int visibleRows = Mathf.Min(rows, PaletteVisibleRows);

            for (int index = 0; index < nodes.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                if (row >= visibleRows)
                {
                    break;
                }

                float x = contentRect.x + PaletteCardSpacing + column * (cardWidth + PaletteCardSpacing);
                float y = contentRect.y + PaletteCardSpacing + row * (PaletteCardHeight + PaletteCardSpacing);
                DrawBlueprintCard(new Rect(x, y, cardWidth, PaletteCardHeight), nodes[index]);
            }
            GUI.EndGroup();
        }

        private void DrawHoverTooltip()
        {
            if (controller == null || controller.HoveredCell.x < 0 || showGuide || showReturnToMenuPrompt)
            {
                return;
            }

            var node = controller.HoveredNode;
            var mouse = Event.current.mousePosition;
            if (IsPointerOverWorkshopUi(mouse))
            {
                return;
            }

            bool showBufferDetails = node != null && Input.GetKey(KeyCode.T);
            var bufferEntries = !showBufferDetails
                ? System.Array.Empty<System.Collections.Generic.KeyValuePair<WorkshopItemDefinition, int>>()
                : node.EnumerateBuffer().Where(pair => pair.Key != null && pair.Value > 0).ToArray();
            float tooltipWidth = showBufferDetails ? 404f : node == null ? 246f : 320f;
            float contentWidth = tooltipWidth - 56f;
            string titleText = node == null ? "Empty Tile" : node.Definition.DisplayName;
            string cellText = $"Cell {controller.HoveredCell.x}, {controller.HoveredCell.y}";
            float titleHeight = CalculateTooltipTextHeight(sectionStyle, titleText, contentWidth, 20f);
            float cellHeight = CalculateTooltipTextHeight(tinyLabelStyle, cellText, contentWidth, 14f);
            float descriptionHeight = node == null
                ? 0f
                : CalculateTooltipTextHeight(bodyStyle, node.Definition.Description, contentWidth, 34f);
            float bufferHeight = showBufferDetails ? CalculateTooltipBufferHeight(node, bufferEntries) : 0f;
            float tooltipHeight;
            float categoryHeight = 0f;
            float statsHeight = 0f;
            float emptyTileHintHeight = 0f;
            if (node == null)
            {
                emptyTileHintHeight = CalculateTooltipTextHeight(bodyStyle, "LMB place armed machine", contentWidth, 18f);
                tooltipHeight = 30f + titleHeight + cellHeight + emptyTileHintHeight;
            }
            else
            {
                categoryHeight = CalculateTooltipTextHeight(tinyLabelStyle, node.Definition.Category.ToString(), contentWidth, 14f);
                string activeText = node.IsRecentlyActive ? "Active" : "Idle";
                string statsText = $"Rot {node.RotationQuarterTurns * 90}°  Buffer {node.BufferedItemCount}/{node.Definition.BufferCapacity}  {activeText}";
                statsHeight = CalculateTooltipTextHeight(tinyLabelStyle, statsText, contentWidth, 14f);
                tooltipHeight = 46f + titleHeight + cellHeight + categoryHeight + descriptionHeight + statsHeight + (showBufferDetails ? bufferHeight + 14f : 0f);
            }
            Rect rect = PositionTooltip(mouse, tooltipWidth, tooltipHeight);

            DrawHoverCardFrame(rect, node == null ? new Color(0.42f, 0.54f, 0.7f) : GetCategoryColor(node.Definition.Category, node.Definition.Tint));
            GUI.BeginGroup(rect);
            float contentY = 18f;
            GUI.Label(new Rect(28f, contentY, contentWidth, titleHeight), titleText, sectionStyle);
            contentY += titleHeight + 2f;
            GUI.Label(new Rect(28f, contentY, contentWidth, cellHeight), cellText, tinyLabelStyle);
            contentY += cellHeight + 6f;

            if (node == null)
            {
                GUI.Label(new Rect(28f, contentY, contentWidth, emptyTileHintHeight), "LMB place armed machine", bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(28f, contentY, contentWidth, categoryHeight), node.Definition.Category.ToString(), tinyLabelStyle);
                contentY += categoryHeight + 4f;
                GUI.Label(new Rect(28f, contentY, contentWidth, descriptionHeight), node.Definition.Description, bodyStyle);
                contentY += descriptionHeight + 8f;
                string activeText = node.IsRecentlyActive ? "Active" : "Idle";
                GUI.Label(new Rect(28f, contentY, contentWidth, statsHeight), $"Rot {node.RotationQuarterTurns * 90}°  Buffer {node.BufferedItemCount}/{node.Definition.BufferCapacity}  {activeText}", tinyLabelStyle);
                if (showBufferDetails)
                {
                    DrawTooltipBuffer(new Rect(28f, contentY + statsHeight + 8f, rect.width - 56f, bufferHeight), node, bufferEntries);
                }
            }

            GUI.EndGroup();
        }

        private static float CalculateTooltipTextHeight(GUIStyle style, string text, float width, float minHeight)
        {
            if (style == null)
            {
                return minHeight;
            }

            float calculatedHeight = style.CalcHeight(new GUIContent(string.IsNullOrEmpty(text) ? " " : text), width);
            return Mathf.Max(minHeight, Mathf.Ceil(calculatedHeight + 4f));
        }

        private Rect PositionTooltip(Vector2 mouse, float tooltipWidth, float tooltipHeight)
        {
            Rect viewport = GetFactoryViewportRect();
            if (showRewards)
            {
                Rect rewardRect = GetRewardDrawerRect();
                viewport.xMax = Mathf.Min(viewport.xMax, rewardRect.xMin - 8f);
            }

            float x = mouse.x + 18f;
            float y = mouse.y + 18f;

            if (x + tooltipWidth > viewport.xMax)
            {
                x = mouse.x - tooltipWidth - 18f;
            }

            if (y + tooltipHeight > viewport.yMax)
            {
                y = mouse.y - tooltipHeight - 18f;
            }

            x = Mathf.Clamp(x, viewport.xMin + 8f, viewport.xMax - tooltipWidth - 8f);
            y = Mathf.Clamp(y, viewport.yMin + 8f, viewport.yMax - tooltipHeight - 8f);
            return new Rect(x, y, tooltipWidth, tooltipHeight);
        }

        private Rect PositionUiTooltip(Vector2 mouse, float tooltipWidth, float tooltipHeight)
        {
            float x = mouse.x + 16f;
            float y = mouse.y + 16f;
            if (x + tooltipWidth > Screen.width - 12f)
            {
                x = mouse.x - tooltipWidth - 16f;
            }

            if (y + tooltipHeight > Screen.height - 12f)
            {
                y = mouse.y - tooltipHeight - 16f;
            }

            x = Mathf.Clamp(x, 12f, Screen.width - tooltipWidth - 12f);
            y = Mathf.Clamp(y, 12f, Screen.height - tooltipHeight - 12f);
            return new Rect(x, y, tooltipWidth, tooltipHeight);
        }

        private void DrawTooltipBuffer(Rect rect, WorkshopNodeState node, System.Collections.Generic.KeyValuePair<WorkshopItemDefinition, int>[] bufferEntries)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 18f), "Buffer", sectionStyle);
            var listY = rect.y + 24f;
            if (node == null || bufferEntries.Length == 0)
            {
                DrawRect(new Rect(rect.x + 2f, listY, rect.width - 4f, 38f), new Color(0.05f, 0.06f, 0.08f, 0.84f));
                GUI.Label(new Rect(rect.x + 12f, listY + 8f, rect.width - 24f, 22f), "Empty", tooltipEmptyStyle);
                return;
            }

            foreach (var pair in bufferEntries)
            {
                var rowRect = new Rect(rect.x, listY, rect.width, 44f);
                DrawRect(rowRect, new Color(pair.Key.Tint.r * 0.22f, pair.Key.Tint.g * 0.22f, pair.Key.Tint.b * 0.22f, 0.82f));
                DrawItemIcon(new Rect(rowRect.x + 7f, rowRect.y + 7f, 30f, 30f), pair.Key);
                GUI.Label(new Rect(rowRect.x + 46f, rowRect.y + 5f, rowRect.width - 104f, 18f), pair.Key.DisplayName, tooltipPrimaryStyle);
                GUI.Label(new Rect(rowRect.x + 46f, rowRect.y + 23f, rowRect.width - 104f, 18f), pair.Key.Kind == WorkshopItemKind.Card ? pair.Key.SpellTier.ToString() : pair.Key.Element.ToString(), tooltipSecondaryStyle);
                GUI.Label(new Rect(rowRect.xMax - 48f, rowRect.y + 12f, 40f, 18f), $"x{pair.Value}", tooltipPrimaryStyle);
                listY += 48f;
            }
        }

        private float CalculateTooltipBufferHeight(WorkshopNodeState node, System.Collections.Generic.KeyValuePair<WorkshopItemDefinition, int>[] bufferEntries)
        {
            if (node == null || bufferEntries.Length == 0)
            {
                return 62f;
            }

            return 24f + bufferEntries.Length * 48f;
        }

        private void DrawItemIcon(Rect rect, WorkshopItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Sprite sprite = GetItemIcon(item);
            if (DrawSprite(rect, sprite, Color.white))
            {
                return;
            }

            DrawFormulaIcon(rect, item.Element);
        }

        private bool IsPointerOverWorkshopUi(Vector2 mousePosition)
        {
            Rect topLeftRect = BuildThroughputPanelRect();
            Rect topRightRect = BuildControlPanelRect();
            Rect topCenterRect = BuildStatusPanelRect(topLeftRect, topRightRect);
            Rect metaRect = BuildLegacySigilStripRect(topLeftRect, topRightRect);
            Rect rightRailRect = BuildRightRailRect(metaRect);
            var paletteRect = new Rect(Margin, Screen.height - BottomDockHeight - Margin, Screen.width - Margin * 2f, BottomDockHeight);

            if (topLeftRect.Contains(mousePosition) || topCenterRect.Contains(mousePosition) || topRightRect.Contains(mousePosition) || metaRect.Contains(mousePosition) || rightRailRect.Contains(mousePosition) || paletteRect.Contains(mousePosition))
            {
                return true;
            }

            if (showRewards)
            {
                var rewardRect = GetRewardDrawerRect();
                if (rewardRect.Contains(mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static Rect BuildThroughputPanelRect()
        {
            return new Rect(Margin, Margin, ThroughputPanelWidth, ThroughputPanelHeight);
        }

        private static Rect BuildControlPanelRect()
        {
            return new Rect(Screen.width - ControlPanelWidth - Margin, Margin, ControlPanelWidth, ControlPanelHeight);
        }

        private static Rect BuildStatusPanelRect(Rect throughputRect, Rect controlRect)
        {
            float x = throughputRect.xMax + TopPanelGap;
            float width = Mathf.Max(240f, controlRect.xMin - x - Margin);
            return new Rect(x, Margin, width, StatusPanelHeight);
        }

        private static Rect BuildLegacySigilStripRect(Rect throughputRect, Rect controlRect)
        {
            float x = throughputRect.xMax + TopPanelGap;
            float width = Mathf.Max(240f, controlRect.xMin - x - Margin);
            return new Rect(x, throughputRect.y + LegacySigilStripOffsetY, width, LegacySigilStripHeight);
        }

        private static Rect BuildRightRailRect(Rect metaRect)
        {
            float y = metaRect.yMax + 12f;
            float height = Mathf.Max(220f, Screen.height - BottomDockHeight - y - Margin);
            return new Rect(
                Screen.width - RightRailWidth - Margin,
                y,
                RightRailWidth,
                height);
        }

        private static Rect GetFactoryViewportRect()
        {
            return new Rect(
                Margin,
                TopHudHeight + Margin,
                Screen.width - RightRailWidth - Margin * 3f,
                Screen.height - BottomDockHeight - TopHudHeight - Margin * 3f);
        }

        private static Rect GetRewardDrawerRect()
        {
            return new Rect(Screen.width - RightRailWidth - 336f, 104f, 320f, Mathf.Min(360f, Screen.height - BottomDockHeight - 138f));
        }

        private static Rect BuildGuideOverlayRect()
        {
            float width = Mathf.Clamp(Screen.width - 96f, 760f, 980f);
            float height = Mathf.Clamp(Screen.height - 88f, 520f, 700f);
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private static Rect BuildReturnToMenuPromptRect()
        {
            float width = Mathf.Min(460f, Screen.width - 64f);
            float height = 196f;
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private void DrawPaletteTabs(Rect rect)
        {
            string[] labels = { "Spirits", "Routing", "Core", "Fusion" };
            float gap = 8f;
            float tabWidth = Mathf.Min(148f, (rect.width - gap * (labels.Length - 1)) / labels.Length);

            for (int index = 0; index < labels.Length; index++)
            {
                Rect tabRect = new Rect(rect.x + index * (tabWidth + gap), rect.y, tabWidth, rect.height);
                bool selected = paletteTabIndex == index;
                bool isHover = IsInteractiveHover(tabRect, true, $"palette_tab_{index}");
                if ((selected ? tabActiveSprite : tabInactiveSprite) != null)
                {
                    DrawRect(new Rect(tabRect.x + 2f, tabRect.y + 3f, tabRect.width, tabRect.height), new Color(0f, 0f, 0f, 0.14f));
                    DrawSpriteCover(tabRect, selected ? tabActiveSprite : tabInactiveSprite, Color.white);
                    if (isHover)
                    {
                        DrawRect(new Rect(tabRect.x + 8f, tabRect.y + 6f, tabRect.width - 16f, tabRect.height - 12f), new Color(1f, 1f, 1f, 0.035f));
                    }
                }
                else
                {
                    Color accent = selected ? AtelierGold : HudStroke;
                    Color background = selected
                        ? new Color(0.18f, 0.16f, 0.09f, isHover ? 1f : 0.96f)
                        : isHover ? new Color(0.12f, 0.16f, 0.23f, 0.98f) : HudPanel;
                    Color outline = selected
                        ? new Color(accent.r, accent.g, accent.b, isHover ? 0.94f : 0.82f)
                        : new Color(accent.r, accent.g, accent.b, isHover ? 0.68f : 0.46f);
                    float accentAlpha = selected ? (isHover ? 1f : 0.9f) : (isHover ? 0.56f : 0.34f);
                    DrawRect(new Rect(tabRect.x + 2f, tabRect.y + 3f, tabRect.width, tabRect.height), new Color(0f, 0f, 0f, 0.16f));
                    DrawRect(tabRect, background);
                    DrawOutline(tabRect, outline);
                    DrawRect(new Rect(tabRect.x, tabRect.y, tabRect.width, 3f), new Color(accent.r, accent.g, accent.b, accentAlpha));
                }

                GUI.Label(tabRect, labels[index], tabButtonStyle);
                if (HandleInteractiveRect(tabRect, $"palette_tab_{index}"))
                {
                    paletteTabIndex = index;
                }
            }
        }

        private WorkshopNodeDefinition[] GetPaletteNodesForActiveTab()
        {
            var nodes = controller.PlaceableNodes
                .Where(node => node != null && !WorkshopNodeVariantUtility.IsMirrorVariant(node.Id))
                .ToArray();

            return paletteTabIndex switch
            {
                0 => nodes.Where(node => node.Category == WorkshopNodeCategory.Source).ToArray(),
                1 => nodes.Where(node =>
                        node.Id == "node.factory.conduit" ||
                        node.Id == WorkshopNodeVariantUtility.TurningConduitId ||
                        node.Id == "node.factory.spell_conduit" ||
                        node.Id == WorkshopNodeVariantUtility.TurningSpellConduitId)
                    .ToArray(),
                2 => nodes.Where(node =>
                        node.Id == "node.factory.element_fusion" ||
                        node.Id == "node.factory.element_shaping" ||
                        node.Id == "node.factory.deck_collector")
                    .ToArray(),
                3 => nodes.Where(node =>
                        node.Id == "node.factory.spell_fusion.basic" ||
                        node.Id == "node.factory.spell_fusion.intermediate" ||
                        node.Id == "node.factory.spell_fusion.advanced")
                    .ToArray(),
                _ => nodes
            };
        }

        private void DrawBlueprintCard(Rect rect, WorkshopNodeDefinition node)
        {
            if (node == null)
            {
                return;
            }

            var simulation = controller != null ? controller.Simulation : null;
            var unlocked = simulation != null && simulation.IsUnlocked(node);
            var mirrorNode = FindMirrorPaletteNode(node);
            var selectedNode = controller != null ? controller.SelectedPaletteNode : null;
            var selected = controller != null && (selectedNode == node || selectedNode == mirrorNode);
            var mirrorSelected = mirrorNode != null && selectedNode == mirrorNode;
            var accent = GetCategoryColor(node.Category, node.Tint);
            bool isHover = IsInteractiveHover(rect, unlocked, $"palette_node_{node.Id}");
            float lift = isHover ? ScalePaletteY(3f) : 0f;
            Rect drawRect = new Rect(rect.x, rect.y - lift, rect.width, rect.height);

            if (blueprintCardSprite != null)
            {
                DrawRect(new Rect(drawRect.x + 3f, drawRect.y + ScalePaletteY(4f), drawRect.width, drawRect.height), new Color(0f, 0f, 0f, 0.18f));
                DrawSprite(drawRect, blueprintCardSprite, Color.white);
                DrawRect(new Rect(drawRect.x + 10f, drawRect.y + ScalePaletteY(10f), drawRect.width - 20f, drawRect.height - ScalePaletteY(20f)), new Color(accent.r, accent.g, accent.b, selected ? 0.11f : isHover ? 0.07f : 0.03f));
            }
            else
            {
                Color cardFill = selected
                    ? new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f, 0.98f)
                    : isHover ? new Color(0.11f, 0.15f, 0.22f, 0.98f) : HudPanel;
                Color cardOutline = selected
                    ? new Color(0.99f, 0.86f, 0.5f, 0.95f)
                    : new Color(accent.r, accent.g, accent.b, isHover ? 0.8f : 0.58f);
                Color badgeFill = new Color(accent.r, accent.g, accent.b, selected || isHover ? 0.24f : 0.14f);
                DrawRect(new Rect(drawRect.x + 3f, drawRect.y + ScalePaletteY(4f), drawRect.width, drawRect.height), new Color(0f, 0f, 0f, 0.2f));
                DrawRect(drawRect, cardFill);
                DrawOutline(drawRect, cardOutline);
                DrawRect(new Rect(drawRect.x, drawRect.y, drawRect.width, ScalePaletteY(5f)), accent);
                DrawRect(new Rect(drawRect.x + 8f, drawRect.y + ScalePaletteY(10f), 30f, ScalePaletteY(34f)), badgeFill);
                DrawRect(new Rect(drawRect.x + 8f, drawRect.y + drawRect.height - ScalePaletteY(8f), drawRect.width - 16f, 1f), new Color(1f, 1f, 1f, 0.045f));
            }

            Event current = Event.current;
            if (unlocked &&
                mirrorNode != null &&
                current != null &&
                current.type == EventType.MouseDown &&
                current.button == 1 &&
                rect.Contains(current.mousePosition))
            {
                AudioManager.PlaySFX(SFXType.ButtonClick);
                controller.SetPaletteNode(mirrorNode);
                current.Use();
            }

            if (HandleInteractiveRect(rect, $"palette_node_{node.Id}", unlocked))
            {
                controller.SetPaletteNode(node);
            }

            var iconSprite = ResolveNodeSprite(node);
            var iconTint = ResolveNodeSpriteTint(node);
            if (iconSprite != null)
            {
                DrawSpritePreserveAspect(new Rect(drawRect.x + 26f, drawRect.y + ScalePaletteY(24f), 47f, ScalePaletteY(47f)), iconSprite, iconTint);
            }
            else
            {
                GUI.Label(new Rect(drawRect.x + 29f, drawRect.y + ScalePaletteY(27f), 40f, 40f), GetCategorySymbol(node.Category), iconStyle);
            }
            DrawNodeElementBadge(new Rect(drawRect.x + drawRect.width - 40f, drawRect.y + ScalePaletteY(11f), ScalePaletteY(24f), ScalePaletteY(24f)), node);
            GUI.Label(new Rect(drawRect.x + 95f, drawRect.y + ScalePaletteY(13f), drawRect.width - 164f, 18f), node.DisplayName, blueprintTitleStyle);
            GUI.Label(new Rect(drawRect.x + 95f, drawRect.y + ScalePaletteY(40f), drawRect.width - 116f, 16f), node.Category.ToString(), blueprintMetaStyle);
            GUI.Label(new Rect(drawRect.x + 95f, drawRect.y + ScalePaletteY(64f), drawRect.width - 116f, 16f), unlocked ? "Ready" : "Locked reward", blueprintMetaStyle);

            if (mirrorNode != null)
            {
                DrawCornerVariantStrip(new Rect(drawRect.x + 24f, drawRect.y + drawRect.height - ScalePaletteY(19f), drawRect.width - 48f, ScalePaletteY(18f)), node, mirrorNode, mirrorSelected, unlocked);
            }

            if (!unlocked)
            {
                DrawRect(drawRect, new Color(0f, 0f, 0f, 0.46f));
                GUI.Label(new Rect(drawRect.x, drawRect.y + drawRect.height * 0.5f - 10f, drawRect.width, 20f), "LOCKED", chipStyle);
            }
        }

        private void DrawCornerVariantStrip(Rect rect, WorkshopNodeDefinition primaryNode, WorkshopNodeDefinition mirrorNode, bool mirrorSelected, bool unlocked)
        {
            DrawRect(rect, new Color(0.045f, 0.065f, 0.095f, 0.94f));
            DrawOutline(rect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.56f));

            var leftPreview = new Rect(rect.x + 6f, rect.y + ScalePaletteY(2f), ScalePaletteY(14f), ScalePaletteY(14f));
            var rightPreview = new Rect(rect.x + 26f, rect.y + ScalePaletteY(2f), ScalePaletteY(14f), ScalePaletteY(14f));
            var defaultHighlight = new Rect(rect.x + 4f, rect.y + ScalePaletteY(1f), ScalePaletteY(18f), ScalePaletteY(16f));
            var mirrorHighlight = new Rect(rect.x + 24f, rect.y + ScalePaletteY(1f), ScalePaletteY(18f), ScalePaletteY(16f));
            if (mirrorSelected)
            {
                DrawRect(mirrorHighlight, new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.25f));
                DrawOutline(mirrorHighlight, new Color(0.98f, 0.82f, 0.48f, 0.9f));
            }
            else
            {
                DrawRect(defaultHighlight, new Color(ArcaneBlue.r, ArcaneBlue.g, ArcaneBlue.b, 0.22f));
                DrawOutline(defaultHighlight, new Color(0.76f, 0.92f, 1f, 0.82f));
            }

            DrawSprite(leftPreview, ResolveNodeSprite(primaryNode), ResolveNodeSpriteTint(primaryNode));
            DrawSprite(rightPreview, ResolveNodeSprite(mirrorNode), ResolveNodeSpriteTint(mirrorNode), true);
            GUI.Label(new Rect(rect.x + 50f, rect.y + ScalePaletteY(1f), 60f, 16f), mirrorSelected ? "Mirror" : "Default", blueprintModeStyle);
            GUI.Label(new Rect(rect.x + 108f, rect.y + ScalePaletteY(1f), rect.width - 114f, 16f), unlocked ? "LMB default  RMB mirror" : "Unlock to arm", blueprintModeStyle);
        }

        private void DrawMiniStat(Rect rect, string value, string label)
        {
            DrawRect(rect, new Color(0.07f, 0.095f, 0.135f, 0.78f));
            DrawOutline(rect, new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.42f));
            DrawRect(new Rect(rect.x + 4f, rect.y + 3f, rect.width - 8f, 1f), new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x, rect.y + 5f, rect.width, 16f), value, statValueStyle);
            GUI.Label(new Rect(rect.x, rect.y + 22f, rect.width, 16f), label, statLabelStyle);
        }

        private static Sprite ResolveNodeSprite(WorkshopNodeDefinition node)
        {
            if (node == null)
            {
                return null;
            }

            return node.NodeSprite != null
                ? node.NodeSprite
                : ArcaneArtCatalog.GetWorkshopNodeSprite(node.Id);
        }

        private static Color ResolveNodeSpriteTint(WorkshopNodeDefinition node)
        {
            return node == null
                ? Color.white
                : ArcaneArtCatalog.GetWorkshopNodeTint(node.Id);
        }

        private static float ScalePaletteY(float value)
        {
            return value * PaletteVerticalScale;
        }

        private void DrawChipWrap(float startX, float startY, float maxWidth, (string Text, Color Tint)[] items)
        {
            if (items.Length == 0)
            {
                GUI.Label(new Rect(startX, startY, maxWidth, 18f), "None", tinyLabelStyle);
                return;
            }

            var x = startX;
            var y = startY;
            foreach (var item in items)
            {
                var chipWidth = Mathf.Min(maxWidth, 22f + tinyLabelStyle.CalcSize(new GUIContent(item.Text)).x);
                if (x + chipWidth > startX + maxWidth)
                {
                    x = startX;
                    y += 26f;
                }

                DrawChip(new Rect(x, y, chipWidth, 20f), item.Text, item.Tint);
                x += chipWidth + 8f;
            }
        }

        private void DrawChip(Rect rect, string text, Color tint)
        {
            DrawRect(new Rect(rect.x + 1f, rect.y + 2f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.16f));
            DrawRect(rect, new Color(tint.r * 0.22f, tint.g * 0.22f, tint.b * 0.22f, 0.94f));
            DrawOutline(rect, new Color(tint.r * 0.95f, tint.g * 0.95f, tint.b * 0.95f, 0.82f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 2f, rect.width - 20f, rect.height - 4f), text, chipStyle);
        }

        private void DrawCompactList(Rect rect, (string Label, int Amount, Color Tint, WorkshopItemDefinition Item)[] items)
        {
            if (items.Length == 0)
            {
                GUI.Label(new Rect(rect.x, rect.y, rect.width, 18f), "None", tinyLabelStyle);
                return;
            }

            var visibleCount = Mathf.Min(items.Length, Mathf.Max(1, Mathf.FloorToInt(rect.height / 18f)));
            var hasOverflow = items.Length > visibleCount;
            var itemCount = hasOverflow ? Mathf.Max(0, visibleCount - 1) : visibleCount;
            for (var index = 0; index < itemCount; index++)
            {
                var item = items[index];
                var y = rect.y + index * 18f;
                if ((index & 1) == 0)
                {
                    DrawRect(new Rect(rect.x - 4f, y, rect.width + 4f, 17f), new Color(1f, 1f, 1f, 0.018f));
                }

                DrawItemIcon(new Rect(rect.x, y + 1f, 14f, 14f), item.Item);
                GUI.Label(new Rect(rect.x + 20f, y, rect.width - 76f, 18f), item.Label, compactRowLabelStyle);
                GUI.Label(new Rect(rect.x + rect.width - 42f, y, 40f, 18f), $"x{item.Amount}", tinyLabelStyle);
            }

            if (hasOverflow)
            {
                var overflowY = rect.y + (visibleCount - 1) * 18f;
                GUI.Label(new Rect(rect.x, overflowY, rect.width, 18f), $"+{items.Length - itemCount} more", tinyLabelStyle);
            }
        }

        private void DrawElementRecipeRow(Rect rect, WorkshopElementAttribute first, WorkshopElementAttribute second, WorkshopElementAttribute output)
        {
            DrawFormulaIcon(new Rect(rect.x, rect.y, 22f, 22f), first);
            GUI.Label(new Rect(rect.x + 28f, rect.y + 2f, 16f, 18f), "+", chipStyle);
            DrawFormulaIcon(new Rect(rect.x + 48f, rect.y, 22f, 22f), second);
            GUI.Label(new Rect(rect.x + 76f, rect.y + 2f, 24f, 18f), "->", chipStyle);
            DrawFormulaIcon(new Rect(rect.x + 104f, rect.y, 22f, 22f), output);
            GUI.Label(new Rect(rect.x + 134f, rect.y + 2f, rect.width - 134f, 18f), GetElementDisplayName(output), bodyStyle);
        }

        private void DrawFormulaIcon(Rect rect, WorkshopElementAttribute element)
        {
            if (!DrawSprite(rect, GetElementSprite(element), Color.white))
            {
                DrawRect(rect, GetElementColor(element));
                DrawOutline(rect, new Color(0.9f, 0.86f, 0.74f));
                GUI.Label(rect, GetElementDisplayName(element).Substring(0, 1), chipStyle);
            }
        }

        private void DrawNodeElementBadge(Rect rect, WorkshopNodeDefinition node)
        {
            if (TryGetNodeElement(node, out var element))
            {
                DrawCircleBadge(rect, element);
            }
        }

        private void DrawCircleBadge(Rect rect, WorkshopElementAttribute element)
        {
            DrawCircle(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.55f));
            DrawCircle(rect, new Color(0.06f, 0.08f, 0.11f, 0.96f));

            var iconRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
            Sprite elementSprite = GetElementSprite(element);
            if (elementSprite == null)
            {
                GUI.Label(iconRect, GetElementDisplayName(element).Substring(0, 1), chipStyle);
                return;
            }

            DrawSpritePreserveAspect(iconRect, elementSprite, Color.white);
        }

        private void DrawGuideRow(Rect rect, string key, string action)
        {
            float keyWidth = Mathf.Clamp(chipStyle.CalcSize(new GUIContent(key)).x + 24f, 58f, 124f);
            float actionWidth = rect.width - keyWidth - 12f;
            float actionHeight = CalculateTooltipTextHeight(bodyStyle, action, actionWidth, 22f);
            DrawChip(new Rect(rect.x, rect.y, keyWidth, 22f), key, new Color(0.88f, 0.74f, 0.33f));
            GUI.Label(new Rect(rect.x + keyWidth + 12f, rect.y, actionWidth, Mathf.Max(rect.height, actionHeight)), action, bodyStyle);
        }

        private void DrawRegionFrame(Rect rect, Sprite sprite, Color accent)
        {
            if (sprite == null)
            {
                DrawPanelFrame(rect, accent);
                return;
            }

            DrawRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
            DrawSprite(rect, sprite, Color.white);
            DrawRect(InsetRect(rect, 22f, 18f, 22f, 18f), new Color(accent.r, accent.g, accent.b, 0.025f));
        }

        private void DrawTallRegionFrame(Rect rect, Sprite sprite, Color accent)
        {
            if (sprite == null)
            {
                DrawPanelFrame(rect, accent);
                return;
            }

            DrawRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
            DrawSprite(rect, sprite, Color.white);
            DrawRect(InsetRect(rect, 24f, 24f, 24f, 24f), new Color(accent.r, accent.g, accent.b, 0.035f));
        }

        private static Rect InsetRect(Rect rect, float left, float top, float right, float bottom)
        {
            return new Rect(
                rect.x + left,
                rect.y + top,
                Mathf.Max(0f, rect.width - left - right),
                Mathf.Max(0f, rect.height - top - bottom));
        }

        private void DrawPanelFrame(Rect rect, Color accent)
        {
            if (panelMainSprite != null)
            {
                DrawRect(new Rect(rect.x + 5f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.22f));
                DrawSprite(rect, panelMainSprite, Color.white);
                DrawRect(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f), new Color(accent.r, accent.g, accent.b, 0.05f));
                DrawRect(new Rect(rect.x + 32f, rect.y + 18f, rect.width - 64f, 2f), new Color(accent.r, accent.g, accent.b, 0.3f));
                return;
            }

            DrawRect(new Rect(rect.x + 5f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.24f));
            DrawRect(rect, HudBackground);
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.68f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), accent);
            DrawRect(new Rect(rect.x + 7f, rect.y + 8f, rect.width - 14f, rect.height - 16f), new Color(1f, 1f, 1f, 0.012f));
            DrawRect(new Rect(rect.x + 1f, rect.y + 5f, rect.width - 2f, 1f), new Color(1f, 1f, 1f, 0.05f));
        }

        private void DrawTooltipFrame(Rect rect, Color accent)
        {
            if (tooltipFrameSprite != null)
            {
                DrawRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
                DrawSprite(rect, tooltipFrameSprite, Color.white);
                DrawRect(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 24f), new Color(accent.r, accent.g, accent.b, 0.04f));
                return;
            }

            DrawPanelFrame(rect, accent);
        }

        private void DrawHoverCardFrame(Rect rect, Color accent)
        {
            if (paletteDockSprite != null)
            {
                DrawRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
                DrawSprite(rect, paletteDockSprite, Color.white);
                DrawRect(InsetRect(rect, 22f, 18f, 22f, 18f), new Color(accent.r, accent.g, accent.b, 0.03f));
                return;
            }

            DrawPanelFrame(rect, accent);
        }

        private void DrawSubPanel(Rect rect, Color accent)
        {
            if (subPanelColumnSprite != null && rect.height >= rect.width * 0.82f)
            {
                DrawTallRegionFrame(rect, subPanelColumnSprite, accent);
                return;
            }

            if (panelSubSprite != null)
            {
                DrawRect(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.16f));
                DrawSprite(rect, panelSubSprite, Color.white);
                DrawRect(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, rect.height - 20f), new Color(accent.r, accent.g, accent.b, 0.045f));
                return;
            }

            DrawRect(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
            DrawRect(rect, HudPanelSoft);
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.42f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accent.r, accent.g, accent.b, 0.72f));
            DrawRect(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 1f), new Color(1f, 1f, 1f, 0.045f));
        }

        private bool DrawThemedButton(Rect rect, string label, Color accent, GUIStyle labelStyle, string interactionId, bool playClickSound = true)
        {
            bool isHover = IsInteractiveHover(rect, true, interactionId);
            bool useWideArtButton = buttonSprite != null && rect.width >= rect.height * 2.1f;
            if (useWideArtButton)
            {
                DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
                DrawSprite(rect, buttonSprite, Color.white);
                DrawRect(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), new Color(accent.r, accent.g, accent.b, isHover ? 0.14f : 0.08f));
                GUI.Label(rect, label, labelStyle);
                return HandleInteractiveRect(rect, interactionId, true, playClickSound);
            }

            if (buttonSmallSprite != null)
            {
                DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.16f));
                DrawSprite(rect, buttonSmallSprite, Color.white);
                DrawRect(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), new Color(accent.r, accent.g, accent.b, isHover ? 0.14f : 0.06f));
                GUI.Label(rect, label, labelStyle);
                return HandleInteractiveRect(rect, interactionId, true, playClickSound);
            }

            Color fillColor = isHover
                ? new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f, 0.99f)
                : new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.96f);
            Color outlineColor = isHover
                ? new Color(accent.r, accent.g, accent.b, 0.94f)
                : new Color(accent.r, accent.g, accent.b, 0.72f);
            Color topStripColor = new Color(accent.r, accent.g, accent.b, isHover ? 1f : 0.92f);
            DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.2f));
            DrawRect(rect, fillColor);
            DrawOutline(rect, outlineColor);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), topStripColor);
            GUI.Label(rect, label, labelStyle);
            return HandleInteractiveRect(rect, interactionId, true, playClickSound);
        }

        private bool HandleInteractiveRect(Rect rect, string interactionId, bool enabled = true, bool playClickSound = true)
        {
            if (!enabled)
            {
                return false;
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (playClickSound)
                    AudioManager.PlaySFX(SFXType.ButtonClick);
                return true;
            }

            return false;
        }

        private static bool IsInteractiveHover(Rect rect, bool enabled, string interactionId)
        {
            if (!enabled)
            {
                return false;
            }

            Event current = Event.current;
            if (current == null || !rect.Contains(current.mousePosition))
            {
                return false;
            }

            AudioManager.ReportUIHover($"workshop:{interactionId}");
            return true;
        }

        private void DrawRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private void DrawCircle(Rect rect, Color color)
        {
            if (circleTexture == null)
            {
                DrawRect(rect, color);
                return;
            }

            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawOutline(Rect rect, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private void DrawSpritePreserveAspect(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null)
            {
                return;
            }

            Vector2 size = sprite.rect.size;
            if (size.x <= 0.0001f || size.y <= 0.0001f)
            {
                DrawSprite(rect, sprite, tint);
                return;
            }

            float scale = Mathf.Min(rect.width / size.x, rect.height / size.y);
            float width = size.x * scale;
            float height = size.y * scale;
            Rect fitRect = new Rect(
                rect.x + (rect.width - width) * 0.5f,
                rect.y + (rect.height - height) * 0.5f,
                width,
                height);
            DrawSprite(fitRect, sprite, tint);
        }

        private void DrawSpriteCover(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect textureRect = sprite.textureRect;
            float sourceWidth = textureRect.width;
            float sourceHeight = textureRect.height;
            if (sourceWidth <= 0.0001f || sourceHeight <= 0.0001f)
            {
                DrawSprite(rect, sprite, tint);
                return;
            }

            float destAspect = rect.width / Mathf.Max(0.0001f, rect.height);
            float sourceAspect = sourceWidth / Mathf.Max(0.0001f, sourceHeight);
            float cropX = 0f;
            float cropY = 0f;
            float cropWidth = sourceWidth;
            float cropHeight = sourceHeight;

            if (destAspect > sourceAspect)
            {
                cropHeight = sourceWidth / destAspect;
                cropY = (sourceHeight - cropHeight) * 0.5f;
            }
            else
            {
                cropWidth = sourceHeight * destAspect;
                cropX = (sourceWidth - cropWidth) * 0.5f;
            }

            Rect uv = new Rect(
                (textureRect.x + cropX) / sprite.texture.width,
                (textureRect.y + cropY) / sprite.texture.height,
                cropWidth / sprite.texture.width,
                cropHeight / sprite.texture.height);

            Color previousColor = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.color = previousColor;
        }

        private void DrawNineSlice(Rect rect, Sprite sprite, float borderLeft, float borderRight, float borderTop, float borderBottom, Color tint)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect textureRect = sprite.textureRect;
            float sourceWidth = textureRect.width;
            float sourceHeight = textureRect.height;
            if (sourceWidth <= 0.0001f || sourceHeight <= 0.0001f)
            {
                DrawSprite(rect, sprite, tint);
                return;
            }

            float left = Mathf.Min(borderLeft, sourceWidth * 0.5f);
            float right = Mathf.Min(borderRight, sourceWidth * 0.5f);
            float top = Mathf.Min(borderTop, sourceHeight * 0.5f);
            float bottom = Mathf.Min(borderBottom, sourceHeight * 0.5f);

            if (rect.width < left + right && left + right > 0.0001f)
            {
                float scale = rect.width / (left + right);
                left *= scale;
                right *= scale;
            }

            if (rect.height < top + bottom && top + bottom > 0.0001f)
            {
                float scale = rect.height / (top + bottom);
                top *= scale;
                bottom *= scale;
            }

            float centerWidth = Mathf.Max(0f, rect.width - left - right);
            float centerHeight = Mathf.Max(0f, rect.height - top - bottom);

            float sourceCenterWidth = Mathf.Max(0f, sourceWidth - borderLeft - borderRight);
            float sourceCenterHeight = Mathf.Max(0f, sourceHeight - borderTop - borderBottom);

            DrawSlice(rect.x, rect.y, left, top, textureRect.x, textureRect.y, borderLeft, borderTop, sprite, tint);
            DrawSlice(rect.x + left, rect.y, centerWidth, top, textureRect.x + borderLeft, textureRect.y, sourceCenterWidth, borderTop, sprite, tint);
            DrawSlice(rect.x + rect.width - right, rect.y, right, top, textureRect.x + sourceWidth - borderRight, textureRect.y, borderRight, borderTop, sprite, tint);

            DrawSlice(rect.x, rect.y + top, left, centerHeight, textureRect.x, textureRect.y + borderTop, borderLeft, sourceCenterHeight, sprite, tint);
            DrawSlice(rect.x + left, rect.y + top, centerWidth, centerHeight, textureRect.x + borderLeft, textureRect.y + borderTop, sourceCenterWidth, sourceCenterHeight, sprite, tint);
            DrawSlice(rect.x + rect.width - right, rect.y + top, right, centerHeight, textureRect.x + sourceWidth - borderRight, textureRect.y + borderTop, borderRight, sourceCenterHeight, sprite, tint);

            DrawSlice(rect.x, rect.y + rect.height - bottom, left, bottom, textureRect.x, textureRect.y + sourceHeight - borderBottom, borderLeft, borderBottom, sprite, tint);
            DrawSlice(rect.x + left, rect.y + rect.height - bottom, centerWidth, bottom, textureRect.x + borderLeft, textureRect.y + sourceHeight - borderBottom, sourceCenterWidth, borderBottom, sprite, tint);
            DrawSlice(rect.x + rect.width - right, rect.y + rect.height - bottom, right, bottom, textureRect.x + sourceWidth - borderRight, textureRect.y + sourceHeight - borderBottom, borderRight, borderBottom, sprite, tint);
        }

        private void DrawSlice(float x, float y, float width, float height, float sourceX, float sourceY, float sourceWidth, float sourceHeight, Sprite sprite, Color tint)
        {
            if (width <= 0.0001f || height <= 0.0001f || sourceWidth <= 0.0001f || sourceHeight <= 0.0001f)
            {
                return;
            }

            Rect uv = new Rect(
                sourceX / sprite.texture.width,
                sourceY / sprite.texture.height,
                sourceWidth / sprite.texture.width,
                sourceHeight / sprite.texture.height);

            Color previousColor = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(new Rect(x, y, width, height), sprite.texture, uv, true);
            GUI.color = previousColor;
        }

        private WorkshopNodeDefinition FindMirrorPaletteNode(WorkshopNodeDefinition node)
        {
            if (node == null || controller == null || !WorkshopNodeVariantUtility.TryGetMirrorVariantId(node.Id, out string mirrorNodeId))
            {
                return null;
            }

            return controller.PlaceableNodes.FirstOrDefault(candidate => candidate != null && candidate.Id == mirrorNodeId);
        }

        private bool DrawSprite(Rect rect, Sprite sprite, Color tint)
        {
            return DrawSprite(rect, sprite, tint, false);
        }

        private bool DrawSprite(Rect rect, Sprite sprite, Color tint, bool flipX)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            var previousColor = GUI.color;
            GUI.color = tint;
            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);
            if (flipX)
            {
                uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
            }
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.color = previousColor;
            return true;
        }

        private void EnsureTheme()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            if (circleTexture == null)
            {
                circleTexture = CreateCircleTexture(32);
            }

            if (panelMainSprite == null)
            {
                panelMainSprite = ArcaneArtCatalog.GetWorkshopPanelMain();
                panelSubSprite = ArcaneArtCatalog.GetWorkshopPanelSub();
                statusBarSprite = ArcaneArtCatalog.GetWorkshopStatusBar();
                topLeftPanelSprite = ArcaneArtCatalog.GetWorkshopTopLeftPanel();
                rightRailPanelSprite = ArcaneArtCatalog.GetWorkshopRightRailPanel();
                paletteDockSprite = ArcaneArtCatalog.GetWorkshopPaletteDock();
                subPanelColumnSprite = ArcaneArtCatalog.GetWorkshopSubPanelColumn();
                ornateFrameSprite = ArcaneArtCatalog.GetWorkshopOrnateFrame();
                buttonSprite = ArcaneArtCatalog.GetWorkshopButton();
                buttonSmallSprite = ArcaneArtCatalog.GetWorkshopButtonSmall();
                tabActiveSprite = ArcaneArtCatalog.GetWorkshopTabActive();
                tabInactiveSprite = ArcaneArtCatalog.GetWorkshopTabInactive();
                blueprintCardSprite = ArcaneArtCatalog.GetWorkshopBlueprintCard();
                slotFrameSprite = ArcaneArtCatalog.GetWorkshopSlotFrame();
                tooltipFrameSprite = ArcaneArtCatalog.GetWorkshopTooltipFrame();
            }

            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = HudText }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.86f, 0.62f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.84f, 0.87f, 0.91f) }
            };

            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = HudMuted }
            };

            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = HudText }
            };

            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudMuted }
            };

            iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.9f, 0.68f) }
            };

            chipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            smallButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            tabButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = HudText }
            };

            cardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.76f, 0.81f, 0.88f) }
            };

            statusBarStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.96f, 0.94f, 0.88f) }
            };

            blueprintTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.12f, 0.085f, 0.045f) }
            };

            blueprintMetaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.18f, 0.135f, 0.08f) }
            };

            blueprintModeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.94f, 0.94f, 0.92f) }
            };

            tinyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = HudMuted }
            };

            centeredTinyLabelStyle = new GUIStyle(tinyLabelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            compactRowLabelStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 11,
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            tooltipPrimaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.94f, 0.93f, 0.89f) }
            };

            tooltipSecondaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.67f, 0.72f, 0.79f) }
            };

            tooltipEmptyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.67f, 0.72f, 0.79f) }
            };
        }

        private void ApplyCameraLayout()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        private static string ShortItemName(string displayName)
        {
            if (displayName.EndsWith(" Essence"))
            {
                return displayName.Replace(" Essence", string.Empty);
            }

            if (displayName.StartsWith("Arcane "))
            {
                return displayName.Replace("Arcane ", string.Empty);
            }

            return displayName;
        }

        private static Color GetCategoryColor(WorkshopNodeCategory category, Color fallback)
        {
            return category switch
            {
                WorkshopNodeCategory.Source => new Color(0.91f, 0.43f, 0.24f),
                WorkshopNodeCategory.Processor => new Color(0.66f, 0.46f, 0.95f),
                WorkshopNodeCategory.Crafter => new Color(0.29f, 0.74f, 0.96f),
                WorkshopNodeCategory.Storage => new Color(0.65f, 0.73f, 0.8f),
                _ => fallback
            };
        }

        private static string GetCategorySymbol(WorkshopNodeCategory category)
        {
            return category switch
            {
                WorkshopNodeCategory.Source => "✦",
                WorkshopNodeCategory.Processor => "◇",
                WorkshopNodeCategory.Crafter => "✧",
                WorkshopNodeCategory.Storage => "▣",
                _ => "•"
            };
        }

        private static bool TryGetNodeElement(WorkshopNodeDefinition node, out WorkshopElementAttribute element)
        {
            element = WorkshopElementAttribute.None;
            if (node == null || string.IsNullOrWhiteSpace(node.Id))
            {
                return false;
            }

            string id = node.Id.ToLowerInvariant();
            if (id.Contains(".fire"))
            {
                element = WorkshopElementAttribute.Fire;
            }
            else if (id.Contains(".water"))
            {
                element = WorkshopElementAttribute.Water;
            }
            else if (id.Contains(".wind"))
            {
                element = WorkshopElementAttribute.Wind;
            }
            else if (id.Contains(".earth"))
            {
                element = WorkshopElementAttribute.Earth;
            }
            else if (id.Contains(".ice"))
            {
                element = WorkshopElementAttribute.Ice;
            }
            else if (id.Contains(".thunder"))
            {
                element = WorkshopElementAttribute.Thunder;
            }
            else if (id.Contains(".light"))
            {
                element = WorkshopElementAttribute.Light;
            }
            else if (id.Contains(".dark"))
            {
                element = WorkshopElementAttribute.Dark;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static string GetElementDisplayName(WorkshopElementAttribute element)
        {
            return element switch
            {
                WorkshopElementAttribute.Fire => "Fire",
                WorkshopElementAttribute.Water => "Water",
                WorkshopElementAttribute.Wind => "Wind",
                WorkshopElementAttribute.Earth => "Earth",
                WorkshopElementAttribute.Ice => "Ice",
                WorkshopElementAttribute.Thunder => "Thunder",
                WorkshopElementAttribute.Light => "Light",
                WorkshopElementAttribute.Dark => "Dark",
                _ => "Neutral"
            };
        }

        private static Color GetElementColor(WorkshopElementAttribute element)
        {
            return element switch
            {
                WorkshopElementAttribute.Fire => new Color(0.95f, 0.36f, 0.2f),
                WorkshopElementAttribute.Water => new Color(0.26f, 0.62f, 0.97f),
                WorkshopElementAttribute.Wind => new Color(0.57f, 0.84f, 0.9f),
                WorkshopElementAttribute.Earth => new Color(0.63f, 0.47f, 0.28f),
                WorkshopElementAttribute.Ice => new Color(0.67f, 0.9f, 1f),
                WorkshopElementAttribute.Thunder => new Color(0.95f, 0.88f, 0.3f),
                WorkshopElementAttribute.Light => new Color(1f, 0.95f, 0.69f),
                WorkshopElementAttribute.Dark => new Color(0.45f, 0.4f, 0.62f),
                _ => new Color(0.65f, 0.73f, 0.8f)
            };
        }

        private static Sprite GetItemIcon(WorkshopItemDefinition item)
        {
            if (item == null)
            {
                return null;
            }

            return GetElementSprite(item.Element);
        }

        private static Sprite GetElementSprite(WorkshopElementAttribute element)
        {
            Sprite elementSprite = ArcaneArtCatalog.GetElementIcon(element);
            if (elementSprite != null)
            {
                return elementSprite;
            }

            return ArcaneArtCatalog.GetSpiritIcon(element);
        }

        private void DrawRewardIcon(Rect rect, WorkshopRewardDefinition reward)
        {
            Sprite icon = reward == null
                ? null
                : reward.RewardKind switch
                {
                    WorkshopRewardKind.UnlockNode => reward.TargetNode != null ? reward.TargetNode.NodeSprite : null,
                    WorkshopRewardKind.GrantItems => reward.GrantedItems != null && reward.GrantedItems.Length > 0 ? GetItemIcon(reward.GrantedItems[0].Item) : null,
                    WorkshopRewardKind.EfficiencyBoost => reward.TargetNode != null ? reward.TargetNode.NodeSprite : null,
                    _ => null
                };

            if (slotFrameSprite != null)
            {
                DrawSpritePreserveAspect(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f), slotFrameSprite, Color.white);
            }

            if (!DrawSprite(rect, icon, Color.white))
            {
                DrawRect(rect, new Color(0.17f, 0.19f, 0.24f, 0.96f));
                DrawOutline(rect, new Color(0.4f, 0.34f, 0.62f));
                GUI.Label(rect, reward != null ? "✦" : "•", iconStyle);
            }
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float radius = center - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((radius + 0.75f - distance) / 1.25f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private void DrawElementLegend(Rect rect)
        {
            WorkshopElementAttribute[] elements =
            {
                WorkshopElementAttribute.Fire,
                WorkshopElementAttribute.Water,
                WorkshopElementAttribute.Wind,
                WorkshopElementAttribute.Earth,
                WorkshopElementAttribute.Ice,
                WorkshopElementAttribute.Thunder,
                WorkshopElementAttribute.Light,
                WorkshopElementAttribute.Dark
            };

            float x = rect.x;
            foreach (WorkshopElementAttribute element in elements)
            {
                Sprite icon = GetElementSprite(element);
                if (DrawSprite(new Rect(x, rect.y, 16f, 16f), icon, Color.white))
                {
                    x += 22f;
                }
            }
        }
    }
}
