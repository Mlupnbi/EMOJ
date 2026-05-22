using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Research;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Buffs.UI.Components;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.Research.UI
{
    public class ResearchPage : UIElement
    {
        private EmojItemSlot inputSlot;
        private UIText hintText;
        private UIList productList;
        private UIList recipesPanel;
        private UIPanel modeBtnP;
        private UIPanel researchBtnP;
        private UIPanel giveBtnP;
        private UIText modeBtnT;
        private UIText researchBtnT;
        private UIText giveBtnT;
        private UIFaceModeSelector faceSelector;

        private bool isCardMode = true;
        private bool _triedApplyPendingQuickQuery;

        private ResearchFaceMode activeFace;
        private List<int> displayedProducts = new List<int>();
        private Dictionary<int, bool> purpleCraftable = new Dictionary<int, bool>();
        private int? highlightedProductType;

        public ResearchPage()
        {
            Width.Set(0, 1f);
            Height.Set(0, 1f);

            const float slotSize = 52f;
            const float hintFrameOverlap = 4f;
            const float hintExtraOffsetPx = 15f;
            const float hintTextScale = 1.4f;

            UIElement queryRow = new UIElement();
            queryRow.Left.Set(OPJourneyShellMetrics.ContentInsetLeft, 0);
            queryRow.Top.Set(0, 0);
            queryRow.Width.Set(OPJourneyShellMetrics.ChromeWidth, 0);
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
            hintText.Width.Set(280f, 0);
            hintText.IsWrapped = false;
            hintText.IgnoresMouseInteraction = true;
            queryRow.Append(hintText);
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
                    RebuildProductListOnly();
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

            recipesPanel = new UIList();
            recipesPanel.Top.Set(25, 0.66f);
            recipesPanel.Left.Set(0, 0f);
            recipesPanel.Width.Set(-OPJourneyShellMetrics.ScrollSafeMarginRight, 1f);
            recipesPanel.Height.Set(-25, 0.34f);
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

            modeBtnP = StripButton(60, 35, 0, 2, OPJourneyUiColors.ButtonBackground, isCardMode ? EOPJText.UI("ViewCard") : EOPJText.UI("ViewList"), out modeBtnT);
            modeBtnP.OnLeftClick += ToggleViewMode;
            bottomStrip.Append(modeBtnP);

            researchBtnP = StripButton(90, 35, 68, 2, OPJourneyUiColors.ButtonActionSuccess, EOPJText.UI("ResearchAll"), out researchBtnT);
            researchBtnP.OnLeftClick += (_, __) => OnPrimaryAction();
            bottomStrip.Append(researchBtnP);

            giveBtnP = StripButton(90, 35, 164, 2, OPJourneyUiColors.ButtonActionWarm, EOPJText.UI("GiveAll"), out giveBtnT);
            giveBtnP.OnLeftClick += (_, __) => OnSecondaryGiveAll();
            bottomStrip.Append(giveBtnP);

            faceSelector = new UIFaceModeSelector(18f);
            faceSelector.Top.Set(10, 0f);
            faceSelector.Left.Set(262, 0f);
            faceSelector.OnFaceSelected += OnFaceSelected;
            bottomStrip.Append(faceSelector);

            activeFace = ResearchFaceMode.Green;
            ApplyWorldModeDefaults();
            RefreshFaceInteractionRules();
            RefreshButtonChrome();
            RebuildProductListOnly();
            ShowDefaultRecipeOrClear();
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
            RebuildProductListOnly();
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

        /// <summary>????????????? UI ??????????У??????ε???????????δ??????????</summary>
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

        private void SetStripGrayed(UIPanel p, bool gray)
        {
            p.IgnoresMouseInteraction = gray;
        }

        private void ApplyWorldModeDefaults()
        {
            if (RecipeAnalyzer.IsJourneyWorld)
            {
                activeFace = inputSlot.item.IsAir
                    ? ResearchFaceMode.Green
                    : RecipeAnalyzer.GetDefaultFaceForJourneySeed(inputSlot.item);
            }
            else
                activeFace = ResearchFaceMode.Purple;
            faceSelector.ActiveFace = activeFace;
        }

        private void RefreshFaceInteractionRules()
        {
            bool journey = RecipeAnalyzer.IsJourneyWorld;
            faceSelector.CanInteract = face =>
            {
                if (journey)
                    return face != ResearchFaceMode.Purple;
                return face == ResearchFaceMode.Purple;
            };
        }

        private void OnFaceSelected(ResearchFaceMode mode)
        {
            activeFace = mode;
            highlightedProductType = null;
            RebuildProductListOnly();
            RefreshButtonChrome();
            ShowDefaultRecipeOrClear();
        }

        private void ToggleViewMode(UIMouseEvent evt, UIElement listeningElement)
        {
            isCardMode = !isCardMode;
            modeBtnT.SetText(isCardMode ? EOPJText.UI("ViewCard") : EOPJText.UI("ViewList"));
            RebuildProductListOnly();
        }

        private void OnInputItemChanged(Item item)
        {
            highlightedProductType = null;
            ApplyWorldModeDefaults();
            RefreshFaceInteractionRules();
            RebuildProductListOnly();
            hintText?.SetText(item.IsAir ? EOPJText.UI("DragItemHint") : TruncateForHint(item.Name));
            RefreshButtonChrome();
            ShowDefaultRecipeOrClear();
        }

        private void RecomputeDisplayedProducts()
        {
            displayedProducts.Clear();
            purpleCraftable.Clear();
            if (inputSlot.item.IsAir)
                return;

            int seed = inputSlot.item.type;
            bool journey = RecipeAnalyzer.IsJourneyWorld;

            if (!journey)
            {
                displayedProducts = RecipeAnalyzer.FilterAdventurePurple(seed, out purpleCraftable);
            }
            else
            {
                switch (activeFace)
                {
                    case ResearchFaceMode.Yellow:
                        displayedProducts = RecipeAnalyzer.FilterJourneyYellow(seed, RecipeAnalyzer.GetAllProductTypesUsingMaterial(seed));
                        break;
                    case ResearchFaceMode.Green:
                        displayedProducts = RecipeAnalyzer.FilterJourneyGreen(inputSlot.item);
                        break;
                    case ResearchFaceMode.Blue:
                        displayedProducts = RecipeAnalyzer.FilterJourneyBlue(seed);
                        break;
                    default:
                        displayedProducts = new List<int>();
                        break;
                }
            }

        }

        private void RebuildProductListOnly()
        {
            RecomputeDisplayedProducts();
            productList.Clear();
            if (isCardMode)
            {
                int cardsPerRow = Math.Max(1, (int)(productList.GetInnerDimensions().Width / 46f));
                UIElement currentRow = null;
                for (int i = 0; i < displayedProducts.Count; i++)
                {
                    if (i % cardsPerRow == 0)
                    {
                        currentRow = new UIElement();
                        currentRow.Width.Set(0, 1f);
                        currentRow.Height.Set(46, 0);
                        productList.Add(currentRow);
                    }
                    int t = displayedProducts[i];
                    var entry = new UIProductEntryCardView(t);
                    entry.Highlighted = highlightedProductType == t;
                    entry.Tint = GetTintForProduct(t);
                    entry.Left.Set((i % cardsPerRow) * 46, 0);
                    entry.OnLeftClick += (evt, el) => OnProductClicked(((UIProductEntryCardView)el).ItemType);
                    currentRow.Append(entry);
                }
            }
            else
            {
                foreach (int type in displayedProducts)
                {
                    var entry = new UIProductEntryListView(type);
                    entry.Highlighted = highlightedProductType == type;
                    entry.Tint = GetTintForProduct(type);
                    entry.OnLeftClick += (evt, el) => OnProductClicked(((UIProductEntryListView)el).ItemType);
                    productList.Add(entry);
                }
            }
        }

        private ResearchProductTint GetTintForProduct(int type)
        {
            bool journey = RecipeAnalyzer.IsJourneyWorld;
            if (!journey)
            {
                if (purpleCraftable.TryGetValue(type, out bool ok) && ok)
                    return ResearchProductTint.PurpleCraftable;
                return ResearchProductTint.PurpleCannotCraft;
            }
            return activeFace switch
            {
                ResearchFaceMode.Yellow => ResearchProductTint.BlueResearched,
                ResearchFaceMode.Green => ResearchProductTint.GreenResearchable,
                ResearchFaceMode.Blue => ResearchProductTint.RedUnresearched,
                _ => ResearchProductTint.None
            };
        }

        private void OnProductClicked(int itemType)
        {
            if (highlightedProductType == itemType)
            {
                highlightedProductType = null;
                ShowDefaultRecipeOrClear();
            }
            else
            {
                highlightedProductType = itemType;
                ShowRecipes(itemType);
            }
            RebuildProductListOnly();
        }

        private void ShowDefaultRecipeOrClear()
        {
            recipesPanel.Clear();
            if (inputSlot.item.IsAir)
                return;
            if (highlightedProductType.HasValue)
                return;

            int t = inputSlot.item.type;
            var list = RecipeAnalyzer.GetRecipesForItem(t);
            if (list.Count == 0)
            {
                AppendNoRecipePathMessage();
                return;
            }
            float panelWidth = GetInnerDimensions().Width - 60f;
            bool blueMat = RecipeAnalyzer.IsJourneyWorld && activeFace == ResearchFaceMode.Blue;
            foreach (Recipe r in list)
                recipesPanel.Add(new UIResearchRecipeRow(r, panelWidth, blueMat));
        }

        private void ShowRecipes(int itemType)
        {
            recipesPanel.Clear();
            int seed = inputSlot.item.IsAir ? ItemID.None : inputSlot.item.type;
            var list = RecipeAnalyzer.IsJourneyWorld && activeFace == ResearchFaceMode.Green
                ? RecipeAnalyzer.GetGreenFaceQualifyingRecipes(itemType, seed)
                : RecipeAnalyzer.GetRecipesForItem(itemType);
            if (list.Count == 0)
            {
                AppendNoRecipePathMessage();
                return;
            }
            float panelWidth = GetInnerDimensions().Width - 60f;
            bool blueMat = RecipeAnalyzer.IsJourneyWorld && activeFace == ResearchFaceMode.Blue;
            foreach (Recipe r in list)
                recipesPanel.Add(new UIResearchRecipeRow(r, panelWidth, blueMat));
        }

        private void AppendNoRecipePathMessage()
        {
            var t = new UIText(EOPJText.UI("NoRecipePath"), 0.88f);
            t.Width.Set(0, 1f);
            t.Height.Set(28, 0);
            t.TextColor = Color.Gray;
            t.PaddingTop = 6;
            recipesPanel.Add(t);
        }

        private bool PrimaryGrayed;
        private bool SecondaryGrayed;

        private void RefreshButtonChrome()
        {
            bool journey = RecipeAnalyzer.IsJourneyWorld;
            PrimaryGrayed = false;
            SecondaryGrayed = false;

            if (!journey)
            {
                researchBtnT.SetText(EOPJText.UI("LosslessCraft"));
                researchBtnP.BackgroundColor = new Color(80, 120, 180);
                giveBtnT.SetText(EOPJText.UI("GiveAll"));
                giveBtnP.BackgroundColor = new Color(180, 150, 40);
                SetStripGrayed(researchBtnP, false);
                SetStripGrayed(giveBtnP, false);
                return;
            }

            giveBtnT.SetText(EOPJText.UI("GiveAll"));
            giveBtnP.BackgroundColor = new Color(180, 150, 40);

            switch (activeFace)
            {
                case ResearchFaceMode.Yellow:
                    researchBtnT.SetText(EOPJText.UI("GetThisItem"));
                    researchBtnP.BackgroundColor = new Color(180, 150, 40);
                    break;
                case ResearchFaceMode.Green:
                    researchBtnT.SetText(EOPJText.UI("ResearchAll"));
                    researchBtnP.BackgroundColor = new Color(60, 100, 60);
                    break;
                case ResearchFaceMode.Blue:
                    researchBtnT.SetText(EOPJText.UI("ResearchAll"));
                    researchBtnP.BackgroundColor = new Color(90, 90, 90);
                    giveBtnP.BackgroundColor = new Color(90, 90, 90);
                    PrimaryGrayed = true;
                    SecondaryGrayed = true;
                    break;
                default:
                    researchBtnT.SetText(EOPJText.UI("ResearchAll"));
                    researchBtnP.BackgroundColor = new Color(60, 100, 60);
                    break;
            }

            SetStripGrayed(researchBtnP, PrimaryGrayed);
            SetStripGrayed(giveBtnP, SecondaryGrayed);
        }

        private void OnPrimaryAction()
        {
            if (PrimaryGrayed) return;
            bool journey = RecipeAnalyzer.IsJourneyWorld;
            if (!journey)
            {
                PurpleLosslessGive();
                return;
            }
            if (activeFace == ResearchFaceMode.Green)
                ResearchAllVisible();
            else if (activeFace == ResearchFaceMode.Yellow)
                YellowGiveSelectedOrQuery();
        }

        private void OnSecondaryGiveAll()
        {
            if (SecondaryGrayed) return;
            GiveAllProducts();
        }

        private void YellowGiveSelectedOrQuery()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;
            int type = highlightedProductType ?? (inputSlot.item.IsAir ? 0 : inputSlot.item.type);
            if (type <= 0)
                return;
            int amt = new Item(type).maxStack;
            player.QuickSpawnItem(player.GetSource_GiftOrReward(), type, amt);
            SoundEngine.PlaySound(SoundID.ResearchComplete);
        }

        private void PurpleLosslessGive()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || inputSlot.item.IsAir) return;
            int seed = inputSlot.item.type;
            int count;
            if (highlightedProductType.HasValue &&
                RecipeAnalyzer.PlayerCanCraftAnyRecipeForProduct(highlightedProductType.Value, seed, player))
            {
                count = OPJourneyConfig.GetPurpleGiveCount(highlightedProductType.Value);
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), highlightedProductType.Value, count);
            }
            else
            {
                count = OPJourneyConfig.GetPurpleGiveCount(seed);
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), seed, count);
            }
            SoundEngine.PlaySound(SoundID.ResearchComplete);
        }

        private void ResearchAllVisible()
        {
            EmojLog.Info(EmojLogChannel.Research, $"ResearchAllVisible count={displayedProducts.Count}");
            if (displayedProducts.Count == 0) return;
            List<string> researchedTags = new List<string>();
            bool any = false;
            foreach (int type in displayedProducts)
            {
                if (!RecipeAnalyzer.IsFullyResearched(type))
                {
                    CreativeUI.ResearchItem(type);
                    researchedTags.Add($"[i:{type}]");
                    any = true;
                }
            }
            if (any)
            {
                SoundEngine.PlaySound(SoundID.ResearchComplete);
                Main.NewText(EOPJText.UIFormat("ResearchDoneLine", string.Join(" ", researchedTags)), Color.LightGreen);
            }
            OnInputItemChanged(inputSlot.item);
        }

        private void GiveAllProducts()
        {
            IEnumerable<int> source = displayedProducts;
            if (!RecipeAnalyzer.IsJourneyWorld && activeFace == ResearchFaceMode.Purple)
                source = displayedProducts.Where(t => purpleCraftable.TryGetValue(t, out bool ok) && ok);
            List<int> toGive = source.ToList();
            if (toGive.Count == 0)
                return;
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;
            foreach (int type in toGive)
            {
                int maxStack = new Item(type).maxStack;
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), type, maxStack);
            }
            SoundEngine.PlaySound(SoundID.ResearchComplete);
        }

        public override void Update(GameTime gameTime)
        {
            if (!_triedApplyPendingQuickQuery)
            {
                _triedApplyPendingQuickQuery = true;
                TryApplyPendingQuickQuery();
            }
            base.Update(gameTime);

            if (faceSelector != null && faceSelector.IsMouseHovering && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.mouseInterface = true;
                ResearchFaceMode hover = faceSelector.GetFaceUnderMouse();
                string tip = GetFaceTooltip(hover);
                Main.instance.MouseText(tip);
            }
        }

        private string GetFaceTooltip(ResearchFaceMode face)
        {
            bool journey = RecipeAnalyzer.IsJourneyWorld;
            if (!journey)
            {
                if (face == ResearchFaceMode.Purple)
                    return EOPJText.UI("FaceTipPurpleNonJourney");
                return EOPJText.UI("FaceTipPurpleNeedJourney");
            }
            return face switch
            {
                ResearchFaceMode.Yellow => EOPJText.UI("FaceTipYellow"),
                ResearchFaceMode.Green => EOPJText.UI("FaceTipGreen"),
                ResearchFaceMode.Blue => EOPJText.UI("FaceTipBlue"),
                ResearchFaceMode.Purple => EOPJText.UI("FaceTipPurpleJourney"),
                _ => ""
            };
        }
    }
}
