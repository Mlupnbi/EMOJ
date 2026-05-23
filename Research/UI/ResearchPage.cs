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
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI.Components;

namespace EvenMoreOverpoweredJourney.Research.UI
{
    /// <summary>研究页签（页签 1）：产物列表与配方逻辑对齐 0.1 超备份，仅保留现行配色与外壳布局。</summary>
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

        private bool isCardMode = true;
        private bool _triedApplyPendingQuickQuery;
        private List<int> currentProducts = new List<int>();

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
            currentProducts = RecipeAnalyzer.GetDeepCraftableProducts(item);
            UpdateProductList();
            recipesPanel.Clear();
            hintText?.SetText(item.IsAir ? EOPJText.UI("DragItemHint") : TruncateForHint(item.Name));
        }

        private void ResearchAllVisible()
        {
            EmojLog.Info(EmojLogChannel.Research, $"ResearchAllVisible count={currentProducts.Count}");
            if (currentProducts.Count == 0)
                return;

            List<string> researchedTags = new List<string>();
            bool anyResearched = false;
            foreach (int type in currentProducts)
            {
                if (!RecipeAnalyzer.IsResearched(type))
                {
                    CreativeUI.ResearchItem(type);
                    researchedTags.Add($"[i:{type}]");
                    anyResearched = true;
                }
            }

            if (anyResearched)
            {
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

            foreach (int type in currentProducts)
            {
                int maxStack = new Item(type).maxStack;
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), type, maxStack);
            }

            SoundEngine.PlaySound(SoundID.ResearchComplete);
        }

        private void UpdateProductList()
        {
            productList.Clear();
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
                    entry.Tint = ResearchProductTint.GreenResearchable;
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
                    entry.Tint = ResearchProductTint.GreenResearchable;
                    entry.OnLeftClick += (evt, el) => ShowRecipes(((UIProductEntryListView)el).ItemType);
                    productList.Add(entry);
                }
            }
        }

        private void ShowRecipes(int itemType)
        {
            recipesPanel.Clear();
            var recipes = RecipeAnalyzer.GetRecipesForItem(itemType);
            float panelWidth = GetInnerDimensions().Width - 60f;

            foreach (Recipe r in recipes)
                recipesPanel.Add(new UIResearchRecipeRow(r, panelWidth, false));
        }

        public override void Update(GameTime gameTime)
        {
            if (!_triedApplyPendingQuickQuery)
            {
                _triedApplyPendingQuickQuery = true;
                TryApplyPendingQuickQuery();
            }

            base.Update(gameTime);
        }
    }
}
