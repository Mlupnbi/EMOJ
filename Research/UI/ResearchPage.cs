using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Research;
using EvenMoreOverpoweredJourney.Research.Crafting;
using EvenMoreOverpoweredJourney.Research.Players;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.Research.UI
{
    /// <summary>研究页签：旅途四脸筛选 + RecipeBrowser 式嵌套可研究判定。</summary>
    public class ResearchPage : UIElement
    {
        private static readonly string[] FaceTipKeys =
        {
            "FaceTipYellow",
            "FaceTipGreen",
            "FaceTipBlue",
            "FaceTipPurpleNonJourney"
        };

        private EmojItemSlot inputSlot;
        private UIText hintText;
        private UIText emptyHintText;
        private UIFaceModeSelector faceSelector;
        private UIList productList;
        private UIList recipesPanel;
        private UIPanel modeBtnP;
        private UIPanel researchBtnP;
        private UIPanel giveBtnP;
        private UIText modeBtnT;
        private UIText researchBtnT;
        private UIText giveBtnT;

        private bool isCardMode = true;
        private bool _triedApplyPendingQuickQuery;
        private ResearchFaceMode _activeFace = ResearchFaceMode.Green;
        private int _lastEnvironmentSignature = int.MinValue;
        private bool _lastShimmerEncountered;
        private int _craftingRefreshCooldown;
        private int _greenTintUiRefreshCooldown;
        private bool _pendingCraftingRefresh;
        private RecipeBrowserNestedCraft.GreenFaceQuerySession _greenQuerySession;
        private List<int> currentProducts = new List<int>();
        private HashSet<int> _greenFaceImmediateProducts = new HashSet<int>();
        private Dictionary<int, bool> _purpleCanCraft;

        public ResearchPage()
        {
            Width.Set(0, 1f);
            Height.Set(0, 1f);

            const float slotSize = 52f;
            const float faceHeight = 22f;
            const float faceMarginRight = 4f;
            const float hintFrameOverlap = 4f;
            const float hintExtraOffsetPx = 15f;
            const float hintTextScale = 1.4f;
            float queryRowWidthMargin = -(OPJourneyShellMetrics.ContentInsetLeft + OPJourneyShellMetrics.ScrollSafeMarginRight);

            UIElement queryRow = new UIElement();
            queryRow.Left.Set(OPJourneyShellMetrics.ContentInsetLeft, 0);
            queryRow.Top.Set(0, 0);
            queryRow.Width.Set(queryRowWidthMargin, 1f);
            queryRow.Height.Set(slotSize, 0);

            inputSlot = new EmojItemSlot();
            inputSlot.Left.Set(0, 0);
            inputSlot.Top.Set(0, 0);
            inputSlot.OnItemChanged += OnInputItemChanged;
            queryRow.Append(inputSlot);

            hintText = new UIText(EOPJText.UI("DragItemHint"), hintTextScale);
            hintText.TextOriginX = 0f;
            float hintLeft = slotSize - hintFrameOverlap + hintExtraOffsetPx;
            hintText.Left.Set(hintLeft, 0);
            hintText.Top.Set(0, 0);
            hintText.VAlign = 0.5f;
            hintText.Width.Set(220f, 0);
            hintText.IsWrapped = false;
            hintText.IgnoresMouseInteraction = true;
            queryRow.Append(hintText);

            faceSelector = new UIFaceModeSelector(faceHeight);
            faceSelector.Top.Set((slotSize - faceHeight) * 0.5f, 0);
            faceSelector.HAlign = 1f;
            faceSelector.Left.Set(-faceMarginRight, 0f);
            faceSelector.ActiveFace = _activeFace;
            faceSelector.CanInteract = CanSelectFace;
            faceSelector.OnFaceSelected = OnFaceSelected;
            queryRow.Append(faceSelector);
            Append(queryRow);

            UIText secProd = new UIText(EOPJText.UI("SectionProducts"), 0.72f);
            secProd.Left.Set(10, 0);
            secProd.Top.Set(slotSize + 6f, 0);
            secProd.TextColor = Color.LightGray;
            Append(secProd);

            productList = new UIList();
            productList.Left.Set(0, 0f);
            productList.Width.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            float lastListWidth = 0;
            productList.OnUpdate += delegate (UIElement element)
            {
                float currentWidth = element.GetInnerDimensions().Width;
                if (lastListWidth != 0 && Math.Abs(lastListWidth - currentWidth) > 1f)
                    UpdateProductList();
                lastListWidth = currentWidth;
            };
            Append(productList);

            var scrollbar = new UIScrollbar();
            scrollbar.Left.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            float productTop = slotSize + 26f;
            productList.Top.Set(productTop, 0f);
            float productBottomPad = 130f + OPJourneyShellMetrics.ContentBottomSafeMargin + OPJourneyShellMetrics.ContentLayoutBottomInset;
            productList.Height.Set(-productBottomPad, 0.66f);
            scrollbar.Top.Set(productTop, 0f);
            scrollbar.Height.Set(-productBottomPad, 0.66f);
            productList.SetScrollbar(scrollbar);
            Append(scrollbar);

            emptyHintText = new UIText("", 0.72f);
            emptyHintText.Left.Set(10, 0);
            emptyHintText.Top.Set(productTop + 4f, 0);
            emptyHintText.Width.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            emptyHintText.TextColor = Color.Gray;
            emptyHintText.IsWrapped = true;
            emptyHintText.IgnoresMouseInteraction = true;
            Append(emptyHintText);

            recipesPanel = new UIList();
            recipesPanel.Top.Set(25, 0.66f);
            recipesPanel.Left.Set(0, 0f);
            recipesPanel.Width.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            Append(recipesPanel);

            var recScroll = new UIScrollbar();
            recScroll.Left.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            float recipeBottomPad = 25f + OPJourneyShellMetrics.ContentBottomSafeMargin + OPJourneyShellMetrics.ContentLayoutBottomInset;
            recipesPanel.Height.Set(-recipeBottomPad, 0.34f);
            recScroll.Top.Set(25, 0.66f);
            recScroll.Height.Set(-recipeBottomPad, 0.34f);
            recipesPanel.SetScrollbar(recScroll);
            Append(recScroll);

            UIText secRec = new UIText(EOPJText.UI("SectionRecipes"), 0.72f);
            secRec.Left.Set(10, 0);
            secRec.Top.Set(6, 0.66f);
            secRec.TextColor = Color.LightGray;
            Append(secRec);

            UIElement bottomStrip = new UIElement();
            bottomStrip.Left.Set(OPJourneyShellMetrics.ContentInsetLeft, 0);
            bottomStrip.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
            bottomStrip.Height.Set(OPJourneyShellMetrics.ResearchBottomStripHeight, 0f);
            bottomStrip.Top.Set(-38, 0.66f);
            bottomStrip.VAlign = 0f;
            Append(bottomStrip);

            modeBtnP = StripButton(60, 35, 0, 2, OPJourneyUiColors.ButtonBackground, EOPJText.UI("ViewCard"), out modeBtnT);
            modeBtnP.OnLeftClick += ToggleViewMode;
            bottomStrip.Append(modeBtnP);

            researchBtnP = StripButton(90, 35, 68, 2, OPJourneyUiColors.ButtonActionSuccess, EOPJText.UI("ResearchAll"), out researchBtnT);
            researchBtnP.OnLeftClick += (_, __) => ResearchAllVisible();
            bottomStrip.Append(researchBtnP);

            giveBtnP = StripButton(90, 35, 164, 2, OPJourneyUiColors.ButtonActionWarm, EOPJText.UI("GiveAll"), out giveBtnT);
            giveBtnP.OnLeftClick += (_, __) => GiveAllProducts();
            bottomStrip.Append(giveBtnP);

            UpdateProductList();
        }

        private static bool CanSelectFace(ResearchFaceMode face)
        {
            if (RecipeAnalyzer.IsJourneyWorld)
                return face != ResearchFaceMode.Purple;
            return face == ResearchFaceMode.Purple;
        }

        private void OnFaceSelected(ResearchFaceMode face)
        {
            if (!CanSelectFace(face))
                return;

            _activeFace = face;
            faceSelector.ActiveFace = face;
            CancelGreenQuery();
            RebuildProducts(inputSlot.item);
            UpdateProductList();
            recipesPanel.Clear();
            ProcessGreenQuery();
        }

        private static string TruncateForHint(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            const int max = 26;
            if (name.Length <= max)
                return name;
            return name.Substring(0, max - 1) + "\u2026";
        }

        public void OnShellResized()
        {
            Recalculate();
            UpdateProductList();
        }

        private static UIPanel StripButton(float w, float h, float left, float top, Color bg, string text, out UIText label)
        {
            var p = new UIPanel();
            p.SetPadding(0);
            p.Left.Set(left, 0f);
            p.Top.Set(top, 0f);
            p.Width.Set(w, 0f);
            p.Height.Set(h, 0f);
            p.BackgroundColor = bg;
            p.BorderColor = new Color(55, 55, 85);
            label = new UIText(text, 0.85f);
            label.HAlign = label.VAlign = 0.5f;
            label.IgnoresMouseInteraction = true;
            p.Append(label);
            return p;
        }

        private void TryApplyPendingQuickQuery()
        {
            if (Main.dedServ || Main.gameMenu)
                return;
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;
            OPJourneyPlayer plr = player.GetModPlayer<OPJourneyPlayer>();
            int t = plr.PendingResearchQueryType;
            if (t <= ItemID.None)
                return;
            if (t >= ItemLoader.ItemCount || !ContentSamples.ItemsByType.ContainsKey(t))
            {
                plr.PendingResearchQueryType = 0;
                return;
            }
            plr.PendingResearchQueryType = 0;
            inputSlot.item.SetDefaults(t);
            OnInputItemChanged(inputSlot.item);
        }

        private void ToggleViewMode(UIMouseEvent evt, UIElement listeningElement)
        {
            isCardMode = !isCardMode;
            modeBtnT.SetText(isCardMode ? EOPJText.UI("ViewCard") : EOPJText.UI("ViewList"));
            UpdateProductList();
        }

        private void OnInputItemChanged(Item item)
        {
            if (item.IsAir)
            {
                _activeFace = ResearchFaceMode.Green;
            }
            else if (RecipeAnalyzer.IsJourneyWorld)
            {
                _activeFace = RecipeAnalyzer.GetDefaultFaceForJourneySeed(item);
            }
            else
            {
                _activeFace = ResearchFaceMode.Purple;
            }

            faceSelector.ActiveFace = _activeFace;
            CancelGreenQuery();
            RebuildProducts(item);
            UpdateProductList();
            recipesPanel.Clear();
            hintText?.SetText(item.IsAir ? EOPJText.UI("DragItemHint") : TruncateForHint(item.Name));
            _lastEnvironmentSignature = ResearchCraftingPlayer.GetEnvironmentSignature();
            _lastShimmerEncountered = ResearchCraftingPlayer.HasEncounteredShimmer;
        }

        private void CancelGreenQuery()
        {
            _greenQuerySession = null;
            _pendingCraftingRefresh = false;
            _greenFaceImmediateProducts.Clear();
        }


        private void StartGreenQuery(int seedType)
        {
            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();
            _greenQuerySession = RecipeBrowserNestedCraft.BeginGreenFaceQuery(seedType);
            _greenFaceImmediateProducts.Clear();
            currentProducts.Clear();
            emptyHintText?.SetText(EOPJText.UI("GreenFaceQuerying"));
            UpdateProductList();
        }

        private void ProcessGreenQuery()
        {
            if (_greenQuerySession == null)
                return;

            if (!_greenQuerySession.Complete)
            {
                RecipeBrowserNestedCraft.StepGreenFaceQuery(_greenQuerySession);
                if (_greenQuerySession.ListReady)
                    SyncGreenFaceListFromSession(force: true);
                if (!_greenQuerySession.Complete)
                {
                    SyncGreenFaceTintsFromSession();
                    return;
                }
            }

            if (_greenQuerySession.ListReady)
                SyncGreenFaceListFromSession(force: true);
            SyncGreenFaceTintsFromSession(force: true);
            _greenQuerySession = null;
            emptyHintText?.SetText(string.Empty);
            UpdateProductList();

            if (_pendingCraftingRefresh && inputSlot.item != null && !inputSlot.item.IsAir
                && _activeFace == ResearchFaceMode.Green)
            {
                _pendingCraftingRefresh = false;
                RefreshGreenFaceJourneyTints();
            }
        }

        private void SyncGreenFaceListFromSession(bool force = false)
        {
            if (_greenQuerySession == null || !_greenQuerySession.ListReady || _greenQuerySession.Results == null)
                return;

            if (!force && currentProducts.Count == _greenQuerySession.Results.Count)
            {
                bool same = true;
                for (int i = 0; i < currentProducts.Count; i++)
                {
                    if (currentProducts[i] != _greenQuerySession.Results[i])
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                    return;
            }

            currentProducts.Clear();
            currentProducts.AddRange(_greenQuerySession.Results);
            emptyHintText?.SetText(string.Empty);
            UpdateProductList();
        }

        private void SyncGreenFaceTintsFromSession(bool force = false)
        {
            if (_greenQuerySession?.ImmediateCraftProducts == null)
                return;

            if (!force && _greenFaceImmediateProducts.SetEquals(_greenQuerySession.ImmediateCraftProducts))
                return;

            if (!force)
            {
                if (_greenTintUiRefreshCooldown > 0)
                {
                    _greenTintUiRefreshCooldown--;
                    return;
                }

                _greenTintUiRefreshCooldown = 8;
            }

            _greenFaceImmediateProducts.Clear();
            foreach (int type in _greenQuerySession.ImmediateCraftProducts)
                _greenFaceImmediateProducts.Add(type);
            UpdateProductList();
        }

        private void RebuildProducts(Item item)
        {
            _purpleCanCraft = null;
            _greenFaceImmediateProducts.Clear();
            currentProducts.Clear();
            if (item == null || item.IsAir)
                return;

            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();

            int seedType = item.type;
            try
            {
                if (RecipeAnalyzer.IsJourneyWorld)
                {
                    switch (_activeFace)
                    {
                        case ResearchFaceMode.Yellow:
                            currentProducts = RecipeAnalyzer.FilterJourneyYellow(
                                seedType,
                                RecipeAnalyzer.GetAllProductTypesUsingMaterial(seedType));
                            break;
                        case ResearchFaceMode.Green:
                            if (!RecipeAnalyzer.IsFullyResearched(seedType))
                            {
                                CancelGreenQuery();
                                break;
                            }

                            StartGreenQuery(seedType);
                            return;
                        case ResearchFaceMode.Blue:
                            CancelGreenQuery();
                            currentProducts = RecipeAnalyzer.FilterJourneyBlue(seedType);
                            break;
                    }
                }
                else if (_activeFace == ResearchFaceMode.Purple)
                {
                    currentProducts = RecipeAnalyzer.FilterAdventurePurple(seedType, out _purpleCanCraft);
                }
                else
                {
                    currentProducts = RecipeAnalyzer.GetAllProductTypesUsingMaterial(seedType);
                }
            }
            catch (System.Exception ex)
            {
                CancelGreenQuery();
                LogRebuildProductsFailure(ex, item, seedType);
            }
        }

        private void LogRebuildProductsFailure(Exception ex, Item item, int seedType)
        {
            string itemName = item != null && !item.IsAir ? item.Name : "?";
            string context =
                $"RebuildProducts failed face={_activeFace} item={itemName}(id={seedType}) " +
                $"journey={RecipeAnalyzer.IsJourneyWorld} researched={RecipeAnalyzer.IsFullyResearched(seedType)} " +
                $"greenQueryActive={_greenQuerySession != null}";

            EmojLog.Warn(EmojLogChannel.Research, context);
            EmojLog.Error(EmojLogChannel.Research, context, ex);
            EmojLog.WarnFull(EmojLogChannel.Research, context);
            if (ex != null)
            {
                EmojLog.WarnFull(EmojLogChannel.Research, ex.ToString());
                if (ex.InnerException != null)
                    EmojLog.WarnFull(EmojLogChannel.Research, "InnerException: " + ex.InnerException);
            }

            ModContent.GetInstance<EvenMoreOverpoweredJourney>().Logger.Warn(context);
            if (ex != null)
                ModContent.GetInstance<EvenMoreOverpoweredJourney>().Logger.Warn(ex.ToString());
        }

        private void MaybeRefreshForCraftingDiscovery()
        {
            if (inputSlot.item.IsAir || _activeFace != ResearchFaceMode.Green)
                return;

            if (_craftingRefreshCooldown > 0)
            {
                _craftingRefreshCooldown--;
                return;
            }

            int signature = ResearchCraftingPlayer.GetEnvironmentSignature();
            bool shimmer = ResearchCraftingPlayer.HasEncounteredShimmer;
            if (signature == _lastEnvironmentSignature && shimmer == _lastShimmerEncountered)
                return;

            _lastEnvironmentSignature = signature;
            _lastShimmerEncountered = shimmer;
            _craftingRefreshCooldown = 120;

            if (_greenQuerySession != null && !_greenQuerySession.Complete)
            {
                _pendingCraftingRefresh = true;
                return;
            }

            RefreshGreenFaceJourneyTints();
        }

        /// <summary>环境/台子变化时只刷新绿黄着色，不重跑列表（列表入列规则与背包无关）。</summary>
        private void RefreshGreenFaceJourneyTints()
        {
            if (inputSlot.item.IsAir || _activeFace != ResearchFaceMode.Green || currentProducts.Count == 0)
                return;

            int seedType = inputSlot.item.type;
            if (!RecipeAnalyzer.IsFullyResearched(seedType))
                return;

            Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();

            var nextGreen = new HashSet<int>();
            RecipeBrowserNestedCraft.RefreshJourneyReadyTints(currentProducts, seedType, nextGreen);

            if (nextGreen.SetEquals(_greenFaceImmediateProducts))
                return;

            _greenFaceImmediateProducts.Clear();
            foreach (int type in nextGreen)
                _greenFaceImmediateProducts.Add(type);
            UpdateProductList();
        }

        private ResearchProductTint GetProductTint(int type)
        {
            switch (_activeFace)
            {
                case ResearchFaceMode.Yellow:
                    return ResearchProductTint.BlueResearched;
                case ResearchFaceMode.Green:
                    return _greenFaceImmediateProducts.Contains(type)
                        ? ResearchProductTint.GreenResearchable
                        : ResearchProductTint.GreenMultiStep;
                case ResearchFaceMode.Blue:
                    return ResearchProductTint.RedUnresearched;
                case ResearchFaceMode.Purple:
                    if (_purpleCanCraft != null
                        && _purpleCanCraft.TryGetValue(type, out bool canCraft))
                    {
                        return canCraft
                            ? ResearchProductTint.PurpleCraftable
                            : ResearchProductTint.PurpleCannotCraft;
                    }
                    return ResearchProductTint.None;
                default:
                    return ResearchProductTint.None;
            }
        }

        private bool IsGreenFaceImmediateForBulkAction(int productType, int seedType)
        {
            if (_activeFace != ResearchFaceMode.Green || seedType <= ItemID.None)
                return true;

            return RecipeBrowserNestedCraft.IsProductImmediatelyCraftable(productType, seedType);
        }

        private void ResearchAllVisible()
        {
            EmojLog.Info(EmojLogChannel.Research, $"ResearchAllVisible count={currentProducts.Count}");
            if (currentProducts.Count == 0)
                return;

            int seedType = inputSlot.item.IsAir ? ItemID.None : inputSlot.item.type;
            if (_activeFace == ResearchFaceMode.Green && seedType > ItemID.None)
                Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();

            List<string> researchedTags = new List<string>();
            bool anyResearched = false;
            int skippedYellow = 0;
            foreach (int type in currentProducts)
            {
                if (!IsGreenFaceImmediateForBulkAction(type, seedType))
                {
                    skippedYellow++;
                    continue;
                }

                if (!RecipeAnalyzer.IsResearched(type))
                {
                    CreativeUI.ResearchItem(type);
                    researchedTags.Add($"[i:{type}]");
                    anyResearched = true;
                }
            }

            if (skippedYellow > 0)
                EmojLog.Info(EmojLogChannel.Research, $"ResearchAllVisible skipped journeyYellow={skippedYellow}");

            if (anyResearched)
            {
                RecipeBrowserNestedCraft.InvalidateGreenFaceResultCache();
                SoundEngine.PlaySound(SoundID.ResearchComplete);
                Main.NewText(EOPJText.UIFormat("ResearchDoneLine", string.Join(" ", researchedTags)), Color.LightGreen);
            }

            OnInputItemChanged(inputSlot.item);
        }

        private void GiveAllProducts()
        {
            if (currentProducts.Count == 0)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            int seedType = inputSlot.item.IsAir ? ItemID.None : inputSlot.item.type;
            if (_activeFace == ResearchFaceMode.Green && seedType > ItemID.None)
                player.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();

            foreach (int type in currentProducts)
            {
                if (!IsGreenFaceImmediateForBulkAction(type, seedType))
                    continue;

                int maxStack = new Item(type).maxStack;
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), type, maxStack);
            }

            SoundEngine.PlaySound(SoundID.ResearchComplete);
        }

        private void UpdateProductList()
        {
            productList.Clear();
            bool greenNeedsResearchedSeed = !inputSlot.item.IsAir
                && RecipeAnalyzer.IsJourneyWorld
                && _activeFace == ResearchFaceMode.Green
                && !RecipeAnalyzer.IsFullyResearched(inputSlot.item.type);
            bool showGreenEmptyHint = !inputSlot.item.IsAir
                && RecipeAnalyzer.IsJourneyWorld
                && _activeFace == ResearchFaceMode.Green
                && _greenQuerySession == null
                && !greenNeedsResearchedSeed
                && currentProducts.Count == 0;
            emptyHintText?.SetText(
                _greenQuerySession != null && currentProducts.Count == 0 ? EOPJText.UI("GreenFaceQuerying")
                : greenNeedsResearchedSeed ? EOPJText.UI("GreenFaceNeedResearchedSeed")
                : showGreenEmptyHint ? EOPJText.UI("GreenFaceNoProducts")
                : string.Empty);

            if (isCardMode)
            {
                int cardsPerRow = Math.Max(1, (int)(productList.GetInnerDimensions().Width / 46f));
                UIElement currentRow = null;
                for (int i = 0; i < currentProducts.Count; i++)
                {
                    if (i % cardsPerRow == 0)
                    {
                        currentRow = new UIElement();
                        currentRow.Width.Set(0, 1f);
                        currentRow.Height.Set(46, 0);
                        productList.Add(currentRow);
                    }

                    var entry = new UIProductEntryCardView(currentProducts[i]);
                    entry.Tint = GetProductTint(currentProducts[i]);
                    entry.Left.Set((i % cardsPerRow) * 46, 0);
                    entry.OnLeftClick += (evt, el) => ShowRecipes(((UIProductEntryCardView)el).ItemType);
                    currentRow.Append(entry);
                }
            }
            else
            {
                foreach (int type in currentProducts)
                {
                    var entry = new UIProductEntryListView(type);
                    entry.Tint = GetProductTint(type);
                    entry.OnLeftClick += (evt, el) => ShowRecipes(((UIProductEntryListView)el).ItemType);
                    productList.Add(entry);
                }
            }
        }

        private void ShowRecipes(int itemType)
        {
            recipesPanel.Clear();
            float panelWidth = GetInnerDimensions().Width - 60f;
            List<Recipe> recipes;
            bool tintMaterials;

            if (RecipeAnalyzer.IsJourneyWorld
                && _activeFace == ResearchFaceMode.Green
                && !inputSlot.item.IsAir)
            {
                Main.LocalPlayer?.GetModPlayer<ResearchCraftingPlayer>()?.RefreshEnvironmentForResearchQuery();
                recipes = RecipeBrowserNestedCraft.GetQualifyingRecipesForGreenFace(itemType, inputSlot.item.type);
                tintMaterials = true;
            }
            else
            {
                recipes = RecipeAnalyzer.GetRecipesForItem(itemType);
                tintMaterials = RecipeAnalyzer.IsJourneyWorld && _activeFace == ResearchFaceMode.Blue;
            }

            foreach (Recipe recipe in recipes)
                recipesPanel.Add(new UIResearchRecipeRow(recipe, panelWidth, tintMaterials));
        }

        private void UpdateFaceTooltip()
        {
            if (faceSelector == null || !faceSelector.IsMouseHovering)
                return;

            ResearchFaceMode face = faceSelector.GetFaceUnderMouse();
            if (!CanSelectFace(face))
            {
                Main.instance.MouseText(
                    RecipeAnalyzer.IsJourneyWorld
                        ? EOPJText.UI("FaceTipPurpleJourney")
                        : EOPJText.UI("FaceTipPurpleNeedJourney"));
                return;
            }

            if ((int)face >= 0 && (int)face < FaceTipKeys.Length)
                Main.instance.MouseText(EOPJText.UI(FaceTipKeys[(int)face]));
        }

        public override void Update(GameTime gameTime)
        {
            if (!_triedApplyPendingQuickQuery)
            {
                _triedApplyPendingQuickQuery = true;
                TryApplyPendingQuickQuery();
            }

            MaybeRefreshForCraftingDiscovery();
            ProcessGreenQuery();
            UpdateFaceTooltip();
            base.Update(gameTime);
        }
    }
}
