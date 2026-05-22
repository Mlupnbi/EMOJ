using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
using EvenMoreOverpoweredJourney.Buffs.Systems.Spawning;
using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
using EvenMoreOverpoweredJourney.Buffs.Systems.Display;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI.Components;
using EvenMoreOverpoweredJourney.Shell.UI.Components;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI
{
    public static class BuffCategories
    {
        public const string Positive = "Positive";
        public const string PositivePotionFood = "PositivePotionFood";
        public const string PositiveEquipment = "PositiveEquipment";
        public const string PositiveSetBonus = "PositiveSetBonus";
        public const string PositiveEnvironment = "PositiveEnvironment";
        public const string Negative = "Negative";
        public const string Mount = "Mount";
        public const string Minecart = "Minecart";
        public const string Pet = "Pet";
        public const string LightPet = "LightPet";
        public const string Minion = "Minion";
        public const string Sentry = "Sentry";
        /// <summary>????? UI ????????? Buff + ?? Buff ?????????</summary>
        public const string CombatSummon = "CombatSummon";
        public const string Disabled = "Disabled";

        /// <summary>????????????? ??20 ???????????????????</summary>
        public static bool IsVirtualizablePositiveSubCategory(string subCategory) =>
            subCategory == PositivePotionFood ||
            subCategory == PositiveEquipment ||
            subCategory == PositiveSetBonus ||
            subCategory == PositiveEnvironment;
    }

    public class BuffPage : UIElement
    {
        private readonly OPJourneyUI _shell;

        private static readonly HashSet<string> AlwaysVisibleCategories = new HashSet<string>
        {
            BuffCategories.Mount,
            BuffCategories.Minecart,
            BuffCategories.Pet,
            BuffCategories.LightPet,
            BuffCategories.CombatSummon
        };

        private static readonly string[] CategoryDisplayOrder =
        {
            BuffCategories.PositivePotionFood,
            BuffCategories.PositiveEquipment,
            BuffCategories.PositiveSetBonus,
            BuffCategories.PositiveEnvironment,
            BuffCategories.CombatSummon,
            BuffCategories.Negative,
            BuffCategories.Mount,
            BuffCategories.Minecart,
            BuffCategories.Pet,
            BuffCategories.LightPet,
            BuffCategories.Disabled
        };

        private static readonly HashSet<string> ExpandedByDefaultOnNewWorld = new HashSet<string>
        {
            BuffCategories.PositivePotionFood,
            BuffCategories.PositiveEquipment,
            BuffCategories.PositiveEnvironment
        };

        private UIText buffHeaderText;
        private BuffFilterIconButton buffFilterBtn;
        private UIElement buffHeaderRow;
        private UIText buffWarningText;
        private UIText buffStatsText;
        private UIList buffAreaList;
        private UIScrollbar buffAreaScrollbar;
        private UIBuffSearchBar searchBar;
        private readonly HashSet<string> collapsedCategories = new HashSet<string>();

        private const float ContentInsetLeft = 10f;
        private const float HeaderGapPx = 15f;
        private const float HeaderTitleScale = 1.4f;
        private const float CategoryTitleScale = 1.1f;
        private const float CategoryFoldButtonSize = 18f;
        private const float CategoryFoldButtonGap = 6f;
        private const float HeaderRowTop = 5f;
        private const float WarningLineH = 20f;
        private const float StatsLineH = 24f;
        private const float SearchBlockH = 40f;

        private int lastUnlockedCount = -1;
        private int lastActiveCount = -1;
        private bool lastDebugUnlockAllBuffs;
        private string searchText = "";
        private float lastWidth = -1f;

        public BuffPage(OPJourneyUI shell)
        {
            _shell = shell;
            Width.Set(0, 1f);
            Height.Set(0, 1f);

            buffHeaderRow = new UIElement();
            buffHeaderRow.Top.Set(HeaderRowTop, 0);
            buffHeaderRow.Left.Set(ContentInsetLeft, 0);
            buffHeaderRow.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            Append(buffHeaderRow);

            buffHeaderText = new UIText(EOPJText.UI("BuffUnlockedTitle"), HeaderTitleScale);
            buffHeaderText.TextOriginX = 0f;
            buffHeaderText.TextOriginY = 0f;
            buffHeaderText.Left.Set(0, 0);
            buffHeaderText.Top.Set(0, 0);
            buffHeaderText.IgnoresMouseInteraction = true;
            buffHeaderRow.Append(buffHeaderText);

            if (_shell != null)
            {
                buffFilterBtn = new BuffFilterIconButton(_shell, () =>
                {
                    bool open = !(_shell.BuffSecondaryPanel?.IsOpen ?? false);
                    _shell.BuffSecondaryPanel?.SetOpen(open);
                });
                buffFilterBtn.IgnoresMouseInteraction = false;
                buffHeaderRow.Append(buffFilterBtn);
            }

            buffWarningText = new UIText(EOPJText.UI("BuffWarning"), 0.6f);
            buffWarningText.Left.Set(ContentInsetLeft, 0);
            buffWarningText.TextColor = Color.Yellow;
            buffWarningText.IgnoresMouseInteraction = true;
            Append(buffWarningText);

            buffStatsText = new UIText("", 0.6f);
            buffStatsText.Left.Set(ContentInsetLeft, 0);
            buffStatsText.TextColor = Color.Gray;
            buffStatsText.IgnoresMouseInteraction = true;
            Append(buffStatsText);

            searchBar = new UIBuffSearchBar();
            searchBar.SearchHint = EOPJText.UI("BuffSearchHint");
            searchBar.Left.Set(ContentInsetLeft, 0);
            searchBar.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            searchBar.Height.Set(30, 0);
            searchBar.OnTextChanged += text =>
            {
                searchText = text;
                PopulateBuffAreas();
            };
            Append(searchBar);

            buffAreaList = new UIList();
            buffAreaList.Left.Set(ContentInsetLeft, 0);
            buffAreaList.Width.Set(-(ContentInsetLeft + OPJourneyShellMetrics.ScrollSafeMarginRight), 1f);
            Append(buffAreaList);

            buffAreaScrollbar = new UIScrollbar();
            buffAreaScrollbar.Left.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            buffAreaList.SetScrollbar(buffAreaScrollbar);
            Append(buffAreaScrollbar);

            LayoutBuffHeaderRow();

            PopulateBuffAreas();
            UpdateBuffStats();
        }

        private void PopulateBuffAreas()
        {
            if (buffAreaList == null) return;
            buffAreaList.Clear();

            string[] categories = CategoryDisplayOrder;
            var categoriesWithButtons = new HashSet<string>
            {
                BuffCategories.PositivePotionFood,
                BuffCategories.PositiveEquipment,
                BuffCategories.PositiveSetBonus,
                BuffCategories.PositiveEnvironment,
                BuffCategories.Negative,
                BuffCategories.Disabled
            };

            var categoriesExtraButtonOffsetX = new HashSet<string>
            {
                BuffCategories.PositivePotionFood,
                BuffCategories.PositiveEquipment,
                BuffCategories.PositiveSetBonus,
                BuffCategories.PositiveEnvironment
            };

            float iconSize = 32f;
            float spacing = 4f;
            float totalPerIcon = iconSize + spacing;

            float listWidth = buffAreaList.GetInnerDimensions().Width;
            if (listWidth <= 0f)
                listWidth = 560f;

            foreach (string cat in categories)
            {
                var buffIds = GetBuffsByCategory(cat);
                if (buffIds.Count == 0 && cat != BuffCategories.Disabled && !AlwaysVisibleCategories.Contains(cat))
                    continue;

                var categoryPanel = new UIElement();
                categoryPanel.Width.Set(0, 1f);

                string categoryTitleText = EOPJText.UI("BuffCat_" + cat);
                UIText catTitle = new UIText(categoryTitleText, CategoryTitleScale);
                catTitle.Top.Set(2, 0);
                catTitle.Left.Set(5, 0);
                categoryPanel.Append(catTitle);

                Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(categoryTitleText) * CategoryTitleScale;
                bool collapsed = collapsedCategories.Contains(cat);
                var foldButton = new UIBuffCategoryFoldButton(() => !collapsedCategories.Contains(cat));
                foldButton.Left.Set(5f + titleSize.X + CategoryFoldButtonGap, 0);
                foldButton.Top.Set(4f, 0);
                foldButton.Width.Set(CategoryFoldButtonSize, 0);
                foldButton.Height.Set(CategoryFoldButtonSize, 0);
                foldButton.OnLeftClick += (evt, el) =>
                {
                    if (!collapsedCategories.Add(cat))
                        collapsedCategories.Remove(cat);

                    PopulateBuffAreas();
                };
                categoryPanel.Append(foldButton);

                float currentTop = 35f;

                if (collapsed)
                {
                    categoryPanel.Height.Set(currentTop, 0);
                    buffAreaList.Add(categoryPanel);
                    continue;
                }

                if (categoriesWithButtons.Contains(cat))
                {
                    float buttonTop = -2f;
                    float buttonLeftStart = categoriesExtraButtonOffsetX.Contains(cat) ? 145f : 85f;
                    float buttonWidth = 75f;
                    float buttonSpacing = 80f;

                    if (cat == BuffCategories.Disabled)
                    {
                        const float disabledRestoreExtraLeft = 20f;
                        var releaseBtn = new UIPanel();
                        releaseBtn.Width.Set(buttonWidth, 0);
                        releaseBtn.Height.Set(26, 0);
                        releaseBtn.Top.Set(buttonTop, 0);
                        releaseBtn.Left.Set(buttonLeftStart + disabledRestoreExtraLeft, 0);
                        releaseBtn.BackgroundColor = OPJourneyUiColors.ButtonActionWarm;
                        var releaseText = new UIText(EOPJText.UI("BuffBtnRestoreAll"), 0.6f);
                        releaseText.HAlign = releaseText.VAlign = 0.5f;
                        releaseBtn.Append(releaseText);
                        releaseBtn.OnLeftClick += (evt, el) =>
                        {
                            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
                            foreach (int id in GetBuffsByCategory(BuffCategories.Disabled)) player.DisabledBuffs.Remove(id);
                            PopulateBuffAreas();
                            UpdateBuffStats();
                        };
                        categoryPanel.Append(releaseBtn);
                    }
                    else
                    {
                        string catCopy = cat;
                        float clearLeft = buttonLeftStart;

                        if (cat != BuffCategories.Negative && cat != BuffCategories.PositiveSetBonus)
                        {
                            var enableAllBtn = new UIPanel();
                            enableAllBtn.Width.Set(buttonWidth, 0);
                            enableAllBtn.Height.Set(26, 0);
                            enableAllBtn.Top.Set(buttonTop, 0);
                            enableAllBtn.Left.Set(buttonLeftStart, 0);
                            enableAllBtn.BackgroundColor = OPJourneyUiColors.ButtonActionSuccess;
                            var enableText = new UIText(EOPJText.UI("BuffBtnEnableAll"), 0.6f);
                            enableText.HAlign = enableText.VAlign = 0.5f;
                            enableAllBtn.Append(enableText);
                            enableAllBtn.OnLeftClick += (evt, el) => ToggleAllInCategory(catCopy, true);
                            categoryPanel.Append(enableAllBtn);
                            clearLeft = buttonLeftStart + buttonSpacing;
                        }

                        var clearBtn = new UIPanel();
                        clearBtn.Width.Set(buttonWidth, 0);
                        clearBtn.Height.Set(26, 0);
                        clearBtn.Top.Set(buttonTop, 0);
                        clearBtn.Left.Set(clearLeft, 0);
                        clearBtn.BackgroundColor = OPJourneyUiColors.DangerBackground;
                        var clearText = new UIText(EOPJText.UI("BuffBtnClearAll"), 0.6f);
                        clearText.HAlign = clearText.VAlign = 0.5f;
                        clearBtn.Append(clearText);
                        clearBtn.OnLeftClick += (evt, el) => ToggleAllInCategory(catCopy, false);
                        categoryPanel.Append(clearBtn);

                        if (cat == BuffCategories.Negative)
                        {
                            var disableAllBtn = new UIPanel();
                            disableAllBtn.Width.Set(buttonWidth, 0);
                            disableAllBtn.Height.Set(26, 0);
                            disableAllBtn.Top.Set(buttonTop, 0);
                            disableAllBtn.Left.Set(clearLeft + buttonSpacing, 0);
                            disableAllBtn.BackgroundColor = OPJourneyUiColors.ButtonActionWarm;
                            var disText = new UIText(EOPJText.UI("BuffBtnDisableAllDebuffs"), 0.6f);
                            disText.HAlign = disText.VAlign = 0.5f;
                            disableAllBtn.Append(disText);
                            disableAllBtn.OnLeftClick += (evt, el) =>
                            {
                                var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
                                var debuffIds = GetBuffsByCategory(BuffCategories.Negative).Where(id => player.IsBuffUnlocked(id));
                                foreach (int id in debuffIds)
                                {
                                    player.ActiveBuffs.Remove(id);
                                    player.DisabledBuffs.Add(id);
                                    BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, id);
                                }
                                PopulateBuffAreas();
                                UpdateBuffStats();
                            };
                            categoryPanel.Append(disableAllBtn);
                        }
                    }
                }

                int perRow = Math.Max(1, (int)(listWidth / totalPerIcon));
                UIElement currentRow = null;
                int count = 0;
                for (int i = 0; i < buffIds.Count; i++)
                {
                    if (i % perRow == 0)
                    {
                        currentRow = new UIElement();
                        currentRow.Width.Set(0, 1f);
                        currentRow.Height.Set(iconSize, 0);
                        currentRow.Top.Set(currentTop + (i / perRow) * (iconSize + spacing), 0);
                        categoryPanel.Append(currentRow);
                        count++;
                    }

                    var slot = new UIBuffSlot(buffIds[i]);
                    slot.Left.Set((i % perRow) * totalPerIcon, 0);
                    currentRow.Append(slot);
                }

                float totalHeight = currentTop + count * (iconSize + spacing) + 10;
                categoryPanel.Height.Set(totalHeight, 0);
                buffAreaList.Add(categoryPanel);
            }
        }

        private void ToggleAllInCategory(string category, bool enable)
        {
            if (enable && category == BuffCategories.Negative)
                return;

            if (enable && category == BuffCategories.PositiveSetBonus)
            {
                Main.NewText(EOPJText.UI("BuffBulkSetBonusNoBulk"), new Color(255, 200, 100));
                return;
            }

            if (enable && BlocksCombatSummonBulkEnable(category))
                return;

            if (enable && BlocksMiscExclusiveBulkEnable(category))
            {
                Main.NewText(EOPJText.UI("BuffBulkMiscExclusiveNoBulk"), new Color(255, 200, 100));
                return;
            }

            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
            var buffIds = GetBuffsByCategory(category);
            if (enable)
                buffIds = buffIds.Where(id => !player.DisabledBuffs.Contains(id)).ToList();

            if (enable && IsExclusiveSingleSelectCategory(category))
            {
                foreach (int id in player.ActiveBuffs.Where(i => GetBuffCategory(i) == category).ToList())
                {
                    player.ActiveBuffs.Remove(id);
                    BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, id);
                }
            }

            int enabledCount = 0;
            int skippedCount = 0;
            int skippedSetBonusCount = 0;
            var skipLog = enable ? new List<BuffBulkSkipDiagnostics.SkipEntry>() : null;

            foreach (int id in buffIds)
            {
                BuffBulkSkipDiagnostics.SkipReason? skipReason =
                    BuffBulkSkipDiagnostics.TryGetSkipReason(id, category, enable, player);

                if (skipReason.HasValue)
                {
                    if (enable)
                    {
                        if (skipReason.Value == BuffBulkSkipDiagnostics.SkipReason.SetBonusSection)
                            skippedSetBonusCount++;
                        else
                            skippedCount++;

                        skipLog?.Add(new BuffBulkSkipDiagnostics.SkipEntry(id, skipReason.Value));
                    }

                    continue;
                }

                if (enable)
                {
                    if (BuffSourceIndexSystem.IsKnownSetBonusBuff(id))
                        SetBonusArmorResolver.TryResolve(id, forceRetry: true);

                    player.TryGrantPermanentUnlock(id);
                    if (!player.ActiveBuffs.Contains(id))
                        player.ActiveBuffs.Add(id);

                    enabledCount++;
                }
                else
                {
                    player.ActiveBuffs.Remove(id);
                    BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, id);
                }
            }

            EmojLogDiagnostics.LogBulkToggle(category, enable, enabledCount, skippedCount, skippedSetBonusCount);
            if (EmojLog.IsFullMode && skipLog != null && skipLog.Count > 0)
                BuffBulkSkipDiagnostics.LogSkippedList(category, enable, skipLog);

            if (enable && (skippedCount > 0 || skippedSetBonusCount > 0))
            {
                var breakdown = BuffBulkSkipDiagnostics.SkipCountBreakdown.FromEntries(skipLog);
                string summaryKey = OPJourneyConfig.AllowBulkEnableUnsafeVirtual()
                    ? "BuffBulkEnableSummaryUnsafeAllowed"
                    : "BuffBulkEnableSummary";
                Main.NewText(
                    EOPJText.UIFormat(
                        summaryKey,
                        enabledCount,
                        skippedCount,
                        skippedSetBonusCount,
                        breakdown.NotUnlocked,
                        breakdown.ManualEntity,
                        breakdown.ManualOnly,
                        breakdown.UnsafeVirtual),
                    new Color(180, 220, 255));
            }

            player.NotifyBuffRuntimeStateChanged();

            if (enable)
            {
                BuffVirtualEffectSystem.RebuildVirtualQueue(player, force: true);
                player.ApplyMiscEquipBuffsFromUi();
            }
            else if (category == BuffCategories.CombatSummon)
            {
                BuffCombatSummonSystem.ClearCategoryEntities(Main.LocalPlayer, BuffCategories.Minion);
                BuffCombatSummonSystem.ClearCategoryEntities(Main.LocalPlayer, BuffCategories.Sentry);
                player.SetTrackedCombatSummonBuff(BuffCategories.Minion, 0);
                player.SetTrackedCombatSummonBuff(BuffCategories.Sentry, 0);
            }

            PopulateBuffAreas();
            UpdateBuffStats();
            lastUnlockedCount = player.UnlockedBuffs.Count;
            lastActiveCount = player.ActiveBuffs.Count;
        }

        private void UpdateBuffStats()
        {
            if (buffStatsText == null) return;
            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
            int unlocked = player.UnlockedBuffs.Count;
            int total = BuffListCatalog.ListableBuffCount;
            int active = player.ActiveBuffs.Count;
            int disabled = player.DisabledBuffs.Count;
            buffStatsText.SetText(EOPJText.UIFormat("BuffStats", unlocked, total, active, disabled));
        }

        public static string GetBuffCategoryStatic(int buffId) =>
            BuffCategoryIndexSystem.GetCategory(buffId);

        public static string GetBuffCategory(int buffId) => GetBuffCategoryStatic(buffId);

        public static bool IsExclusiveMiscCategory(string category) =>
            category == BuffCategories.Pet || category == BuffCategories.LightPet ||
            category == BuffCategories.Mount || category == BuffCategories.Minecart;

        public static bool IsExclusiveCombatSummonCategory(string category) =>
            category == BuffCategories.Minion || category == BuffCategories.Sentry;

        public static bool BlocksCombatSummonBulkEnable(string category) =>
            category == BuffCategories.CombatSummon || IsExclusiveCombatSummonCategory(category);

        public static bool BlocksMiscExclusiveBulkEnable(string category) =>
            IsExclusiveMiscCategory(category);

        public static bool IsExclusiveSingleSelectCategory(string category) =>
            IsExclusiveMiscCategory(category) || IsExclusiveCombatSummonCategory(category);

        public void ApplyDefaultCollapsedCategoriesForNewWorld()
        {
            collapsedCategories.Clear();
            foreach (string category in CategoryDisplayOrder)
            {
                if (!ExpandedByDefaultOnNewWorld.Contains(category))
                    collapsedCategories.Add(category);
            }
        }

        private List<int> GetBuffsByCategory(string category)
        {
            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
            var list = new List<int>();

            if (category == BuffCategories.Disabled)
            {
                var disabled = new List<int>(player.DisabledBuffs);
                disabled.Sort((a, b) => a.CompareTo(b));
                list = disabled;
            }
            else if (category == BuffCategories.Mount)
            {
                foreach (int id in BuffMountCategorySystem.MountBuffIds)
                    if (id > 0 && id < BuffLoader.BuffCount && BuffListCatalog.IsListable(id))
                        list.Add(id);
            }
            else if (category == BuffCategories.Minecart)
            {
                foreach (int id in BuffMountCategorySystem.MinecartBuffIds)
                    if (id > 0 && id < BuffLoader.BuffCount && BuffListCatalog.IsListable(id))
                        list.Add(id);
            }
            else if (category == BuffCategories.Pet)
            {
                foreach (int id in BuffCategoryIndexSystem.PetBuffIds)
                    if (id > 0 && id < BuffLoader.BuffCount && BuffListCatalog.IsListable(id))
                        list.Add(id);
            }
            else if (category == BuffCategories.LightPet)
            {
                foreach (int id in BuffCategoryIndexSystem.LightPetBuffIds)
                    if (id > 0 && id < BuffLoader.BuffCount && BuffListCatalog.IsListable(id))
                        list.Add(id);
            }
            else if (category == BuffCategories.CombatSummon)
            {
                foreach (int id in BuffCategoryIndexSystem.MinionBuffIds)
                    if (id > 0 && id < BuffLoader.BuffCount && BuffListCatalog.IsListable(id))
                        list.Add(id);

                foreach (int id in BuffCategoryIndexSystem.SentryBuffIds)
                    if (id > 0 && id < BuffLoader.BuffCount && BuffListCatalog.IsListable(id) && !list.Contains(id))
                        list.Add(id);
            }
            else if (category == BuffCategories.PositivePotionFood ||
                     category == BuffCategories.PositiveEquipment ||
                     category == BuffCategories.PositiveSetBonus ||
                     category == BuffCategories.PositiveEnvironment)
            {
                for (int k = 1; k < BuffLoader.BuffCount; k++)
                {
                    if (GetBuffCategoryStatic(k) != BuffCategories.Positive)
                        continue;

                    if (BuffSourceIndexSystem.GetPositiveSubCategory(k) != category)
                        continue;

                    list.Add(k);
                }
            }
            else if (category == BuffCategories.Negative || category == BuffCategories.Positive)
            {
                for (int k = 1; k < BuffLoader.BuffCount; k++)
                {
                    if (GetBuffCategory(k) != category || !BuffListCatalog.IsListable(k))
                        continue;

                    list.Add(k);
                }
            }
            else
            {
                for (int k = 1; k < BuffLoader.BuffCount; k++)
                {
                    if (GetBuffCategory(k) != category || !BuffListCatalog.IsListable(k))
                        continue;

                    list.Add(k);
                }
            }

            BuffSecondaryFilterState filter = _shell?.BuffSecondary ?? OPJourneyUI.Instance?.BuffSecondary;
            if (filter != null && filter.ActiveModKeys.Count > 0)
                list = list.Where(id => filter.AllowsBuff(id)).ToList();

            if (!string.IsNullOrEmpty(searchText))
            {
                string search = searchText.ToLower().Trim();
                list = list.Where(id =>
                {
                    string locName = BuffDisplayNameHelper.GetDisplayName(id).ToLower();
                    string intName = id < BuffID.Count ? BuffID.Search.GetName(id)?.ToLower() ?? "" : ModContent.GetModBuff(id)?.Name?.ToLower() ?? "";
                    string pinyinInitials = PinyinUtils.GetPinyinInitials(locName);
                    return locName.Contains(search) || intName.Contains(search) || pinyinInitials.Contains(search);
                }).ToList();
            }

            list.Sort((a, b) =>
            {
                bool aUnl = player.IsBuffUnlocked(a);
                bool bUnl = player.IsBuffUnlocked(b);
                if (aUnl == bUnl) return a.CompareTo(b);
                return aUnl ? -1 : 1;
            });
            return list;
        }

        public void OnShellResized()
        {
            Recalculate();
            lastWidth = -1f;
        }

        public void OnModFiltersChanged()
        {
            PopulateBuffAreas();
            UpdateBuffStats();
        }

        private void LayoutBuffHeaderRow()
        {
            if (buffHeaderRow == null || buffHeaderText == null)
                return;

            string title = EOPJText.UI("BuffUnlockedTitle");
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * HeaderTitleScale;
            float titleLeft = 0f;
            float xAfterTitle = titleLeft + titleSize.X + HeaderGapPx;
            float filterSize = BuffFilterIconButton.OuterSize;
            float rowH = Math.Max(titleSize.Y, filterSize);

            buffHeaderRow.Height.Set(rowH, 0);
            buffHeaderText.Left.Set(titleLeft, 0);
            buffHeaderText.Top.Set(0, 0);
            buffHeaderText.VAlign = 0.5f;

            if (buffFilterBtn != null)
            {
                buffFilterBtn.Left.Set(xAfterTitle, 0);
                buffFilterBtn.Top.Set(0, 0);
                buffFilterBtn.VAlign = 0.5f;
                xAfterTitle += filterSize + HeaderGapPx;
            }

            float currentY = HeaderRowTop + rowH + 4f;

            if (buffWarningText != null)
            {
                buffWarningText.Top.Set(currentY, 0);
                currentY += WarningLineH;
            }

            if (buffStatsText != null)
            {
                buffStatsText.Top.Set(currentY, 0);
                currentY += StatsLineH;
            }

            if (searchBar != null)
            {
                searchBar.Top.Set(currentY, 0);
                currentY += SearchBlockH;
            }

            if (buffAreaList != null)
                ShellUiScrollLayout.ApplyVerticalRange(buffAreaList, buffAreaScrollbar, currentY);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            LayoutBuffHeaderRow();
            if (searchBar != null && searchBar.Focused) Main.CurrentInputTextTakerOverride = searchBar;

            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
            bool debugUnlockAll = SuperAdminSession.DebugUnlockAllBuffs;
            float currentWidth = buffAreaList.GetInnerDimensions().Width;

            if (player.NeedsBuffUiCollapseForNewWorld)
            {
                ApplyDefaultCollapsedCategoriesForNewWorld();
                player.NeedsBuffUiCollapseForNewWorld = false;
                PopulateBuffAreas();
                UpdateBuffStats();
            }

            if (player.UnlockedBuffs.Count != lastUnlockedCount ||
                player.ActiveBuffs.Count != lastActiveCount ||
                debugUnlockAll != lastDebugUnlockAllBuffs ||
                Math.Abs(currentWidth - lastWidth) > 1f)
            {
                lastUnlockedCount = player.UnlockedBuffs.Count;
                lastActiveCount = player.ActiveBuffs.Count;
                lastDebugUnlockAllBuffs = debugUnlockAll;
                lastWidth = currentWidth;
                PopulateBuffAreas();
                UpdateBuffStats();
            }
        }

        private sealed class UIBuffCategoryFoldButton : UIElement
        {
            private static Texture2D expandedTexture;

            private readonly Func<bool> isExpanded;

            public UIBuffCategoryFoldButton(Func<bool> isExpanded)
            {
                this.isExpanded = isExpanded;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (IsMouseHovering)
                    Main.LocalPlayer.mouseInterface = true;

                Texture2D texture = isExpanded() ? GetExpandedTexture() : GetCollapsedTexture();
                CalculatedStyle dims = GetDimensions();
                Color color = IsMouseHovering ? Color.White : Color.White * 0.75f;
                spriteBatch.Draw(texture, dims.ToRectangle(), color);
            }

            private static Texture2D GetCollapsedTexture()
            {
                global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextureCache.WarmTab(
                    global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTab.Buff);
                return global::EvenMoreOverpoweredJourney.Shell.UI.Assets.EojUiTextures.Buff.ButtonPlay;
            }

            private static Texture2D GetExpandedTexture()
            {
                Texture2D source = GetCollapsedTexture();
                if (expandedTexture != null && !expandedTexture.IsDisposed &&
                    expandedTexture.Width == source.Height && expandedTexture.Height == source.Width)
                    return expandedTexture;

                var sourceData = new Color[source.Width * source.Height];
                var rotatedData = new Color[source.Width * source.Height];
                source.GetData(sourceData);

                int rotatedWidth = source.Height;
                for (int y = 0; y < source.Height; y++)
                {
                    for (int x = 0; x < source.Width; x++)
                    {
                        int destX = source.Height - 1 - y;
                        int destY = x;
                        rotatedData[destY * rotatedWidth + destX] = sourceData[y * source.Width + x];
                    }
                }

                expandedTexture = new Texture2D(Main.graphics.GraphicsDevice, source.Height, source.Width);
                expandedTexture.SetData(rotatedData);
                return expandedTexture;
            }
        }
    }
}
