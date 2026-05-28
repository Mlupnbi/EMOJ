using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>
    /// 配方弱信号：仅用于「除主材料外还有辅料的」槽位（床/灯/书架等）。
    /// 椅子、桌子等单材料合成槽位不启用。
    /// </summary>
    public static class FurnitureRecipeSlotSignals
    {
        private static readonly FurnitureSlotKind[] WeakSignalSlots =
        {
            FurnitureSlotKind.Bed,
            FurnitureSlotKind.Sofa,
            FurnitureSlotKind.Lamp,
            FurnitureSlotKind.Candle,
            FurnitureSlotKind.Lantern,
            FurnitureSlotKind.Chandelier,
            FurnitureSlotKind.Candelabra,
            FurnitureSlotKind.Bookcase,
            FurnitureSlotKind.Piano,
            FurnitureSlotKind.Bathtub,
            FurnitureSlotKind.Sink,
            FurnitureSlotKind.Toilet,
            FurnitureSlotKind.Dresser,
        };

        public static bool SlotUsesRecipeWeakSignal(FurnitureSlotKind slot)
        {
            for (int i = 0; i < WeakSignalSlots.Length; i++)
            {
                if (WeakSignalSlots[i] == slot)
                    return true;
            }

            return false;
        }

        public static int ScoreProductForSlot(int productType, FurnitureSlotKind slot, int materialBlock = ItemID.None) =>
            ComputeRecipeScore(productType, slot, materialBlock);

        internal static int ComputeRecipeScore(int productType, FurnitureSlotKind slot, int materialBlock)
        {
            if (productType <= ItemID.None || !SlotUsesRecipeWeakSignal(slot))
                return 0;

            int best = 0;
            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))
            {
                if (!RecipeHasAuxiliaryIngredient(recipe, materialBlock))
                    continue;

                best = System.Math.Max(best, ScoreRecipeForSlot(recipe, slot));
            }

            return best;
        }

        public static int ScoreRecipeForSlot(Recipe recipe, FurnitureSlotKind slot)
        {
            if (recipe?.requiredItem == null || !SlotUsesRecipeWeakSignal(slot))
                return 0;

            int score = 0;
            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;

                score += ScoreIngredientForSlot(req.type, slot);

                int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);
                if (gid >= 0 && gid < RecipeGroup.recipeGroups.Count)
                {
                    RecipeGroup group = RecipeGroup.recipeGroups[gid];
                    if (group?.ValidItems != null)
                    {
                        foreach (int t in FurnitureRecipeGroupSampling.Sample(group.ValidItems, maxItems: 12))
                            score += ScoreIngredientForSlot(t, slot) / 2;
                    }
                }
            }

            return score;
        }

        public static bool TryInferSlotFromRecipe(int productType, out FurnitureSlotKind kind, out int score)
        {
            kind = FurnitureSlotKind.None;
            score = 0;
            if (productType <= ItemID.None)
                return false;

            int secondScore = 0;

            foreach (FurnitureSlotKind slot in FurnitureWikiSlots.RecognitionOrder)
            {
                if (slot is FurnitureSlotKind.Block or FurnitureSlotKind.Wall or FurnitureSlotKind.Platform)
                    continue;

                int s = ScoreProductForSlot(productType, slot);
                if (s <= score)
                {
                    if (s > 0 && s > secondScore && s < score)
                        secondScore = s;
                    continue;
                }

                if (score > secondScore)
                    secondScore = score;

                score = s;
                kind = slot;
            }

            if (score < FurnitureSlotScoring.MinClassifyRecipeScore
                || score - secondScore < FurnitureSlotScoring.ClassifyRecipeMargin)
            {
                kind = FurnitureSlotKind.None;
                return false;
            }

            return true;
        }

        /// <summary>物品名与目标槽的吻合度（仅加分）。</summary>
        public static int ScoreNameBonus(int productType, FurnitureSlotKind slot)
        {
            if (productType <= ItemID.None)
                return 0;

            Item item = new Item();
            item.SetDefaults(productType);
            string name = (item.Name ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(name))
                return 0;

            return slot switch
            {
                FurnitureSlotKind.Bed => ScoreNameBed(name),
                FurnitureSlotKind.Sofa => ScoreNameSofa(name),
                FurnitureSlotKind.Chair => ScoreNameChair(name),
                FurnitureSlotKind.Lamp => ScoreNameLamp(name),
                FurnitureSlotKind.Candle => ScoreNameCandle(name),
                FurnitureSlotKind.Piano => ScoreNamePiano(name),
                FurnitureSlotKind.Bookcase => ScoreNameBookcase(name),
                FurnitureSlotKind.Bathtub => ScoreNameBathtub(name),
                FurnitureSlotKind.Sink => ScoreNameSink(name),
                FurnitureSlotKind.Toilet => ScoreNameToilet(name),
                FurnitureSlotKind.Table => ScoreNameTable(name),
                FurnitureSlotKind.Dresser => ScoreNameDresser(name),
                FurnitureSlotKind.Chandelier => NameContains(name, "吊灯", "枝形", "chandelier")
                    ? FurnitureSlotScoring.NameStrong : 0,
                FurnitureSlotKind.Lantern => NameContains(name, "灯笼", "lantern") && !name.Contains("吊灯")
                    ? FurnitureSlotScoring.NameMedium : 0,
                FurnitureSlotKind.Candelabra => NameContains(name, "烛台", "candelabra")
                    ? FurnitureSlotScoring.NameMedium : 0,
                FurnitureSlotKind.Door => NameContains(name, "门", "door") && !name.Contains("桌面")
                    ? FurnitureSlotScoring.NameMedium : 0,
                FurnitureSlotKind.Chest => NameContains(name, "箱", "匣", "chest")
                    ? FurnitureSlotScoring.NameMedium : 0,
                FurnitureSlotKind.Clock => NameContains(name, "钟", "clock")
                    ? FurnitureSlotScoring.NameMedium : 0,
                FurnitureSlotKind.Wall => ScoreNameWall(name),
                FurnitureSlotKind.Platform => NameContains(name, "平台", "platform")
                    ? FurnitureSlotScoring.NameMedium : 0,
                FurnitureSlotKind.Workbench => ScoreNameWorkbench(name),
                _ => 0
            };
        }

        private static int ScoreNameWall(string name)
        {
            if (name.Contains("fence") || name.Contains("栅栏") || name.Contains("藤架")
                || name.Contains("trellis") || name.Contains("pergola") || name.Contains("arbor"))
                return 0;

            if (name.Contains("墙") || name.Contains("wall"))
                return FurnitureSlotScoring.NameStrong;

            return 0;
        }

        private static int ScoreNameBed(string name)
        {
            if (name.Contains("睡眠") || name.Contains("睡舱") || name.Contains("sleep") || name.Contains("pod"))
                return FurnitureSlotScoring.NameStrong;
            if (name.Contains("床") && !name.Contains("床头柜"))
                return FurnitureSlotScoring.NameStrong;
            return 0;
        }

        private static int ScoreNameSofa(string name)
        {
            if (name.Contains("沙发") || name.Contains("躺椅"))
                return FurnitureSlotScoring.NameStrong;
            if (name.Contains("长凳") || name.Contains("长椅") || name.Contains("sofa") || name.Contains("couch"))
                return FurnitureSlotScoring.NameStrong;
            if (name.Contains("bench") && !name.Contains("work"))
                return FurnitureSlotScoring.NameMedium;
            return 0;
        }

        private static int ScoreNameChair(string name)
        {
            if (FurnitureNameSignals.IsDecorativeMark(name))
                return 0;
            if (name.EndsWith("椅") || name.Contains("chair"))
                return FurnitureSlotScoring.NameStrong;
            return 0;
        }

        private static int ScoreNameLamp(string name)
        {
            if (name.Contains("台灯") || name.Contains("落地灯") || name.Contains("desk lamp") || name.Contains("table lamp"))
                return FurnitureSlotScoring.NameStrong;
            if (name.Contains("灯") && !name.Contains("烛") && !name.Contains("吊灯") && !name.Contains("灯笼"))
                return FurnitureSlotScoring.NameWeak;
            return 0;
        }

        private static int ScoreNameCandle(string name)
        {
            if (name.Contains("烛") || name.Contains("candle"))
                return FurnitureSlotScoring.NameMedium;
            if (name.Contains("火把") || name.Contains("torch"))
                return FurnitureSlotScoring.NameWeak;
            return 0;
        }

        private static int ScoreNamePiano(string name)
        {
            if (name.Contains("钢琴") || name.Contains("piano"))
                return FurnitureSlotScoring.NameStrong;
            if (name.Contains("键盘") || name.Contains("keyboard"))
                return FurnitureSlotScoring.NameStrong;
            return 0;
        }

        private static int ScoreNameBookcase(string name) =>
            NameContains(name, "书架", "书柜", "书橱", "bookcase") ? FurnitureSlotScoring.NameStrong : 0;

        private static int ScoreNameBathtub(string name)
        {
            if (name.Contains("浴缸"))
                return FurnitureSlotScoring.NameStrong;
            if (name.Contains("浴") && !name.Contains("书"))
                return FurnitureSlotScoring.NameMedium;
            return 0;
        }

        private static int ScoreNameSink(string name) =>
            NameContains(name, "水槽", "洗手", "水池", "sink") ? FurnitureSlotScoring.NameMedium : 0;

        private static int ScoreNameToilet(string name) =>
            NameContains(name, "马桶", "toilet") ? FurnitureSlotScoring.NameStrong : 0;

        private static int ScoreNameTable(string name) =>
            NameContains(name, "桌", "table") && !name.Contains("梳妆") && !name.Contains("床头柜") && !name.Contains("台灯")
                ? FurnitureSlotScoring.NameMedium
                : 0;

        private static int ScoreNameDresser(string name) =>
            NameContains(name, "梳妆", "衣柜", "衣橱", "dresser") ? FurnitureSlotScoring.NameMedium : 0;

        private static int ScoreNameWorkbench(string name)
        {
            if (name.Contains("长凳") || name.Contains("长椅")
                || (name.Contains("bench") && !name.Contains("work")))
                return 0;

            if (NameContains(name, "工作台", "制作站", "workbench", "work bench"))
                return FurnitureSlotScoring.NameMedium;

            return 0;
        }

        private static int ScoreIngredientForSlot(int ingredientType, FurnitureSlotKind slot)
        {
            if (ingredientType <= ItemID.None)
                return 0;

            return slot switch
            {
                FurnitureSlotKind.Bed when ingredientType == ItemID.Silk => 2_400,
                FurnitureSlotKind.Sofa when ingredientType == ItemID.Silk => 2_000,
                FurnitureSlotKind.Bookcase when ingredientType == ItemID.Book => 2_400,
                FurnitureSlotKind.Piano when ingredientType == ItemID.Book => 1_200,
                FurnitureSlotKind.Piano when ingredientType == ItemID.Bone => 1_600,
                FurnitureSlotKind.Piano when ingredientType == ItemID.CrystalShard => 600,
                FurnitureSlotKind.Bathtub when ingredientType == ItemID.Glass => 1_400,
                FurnitureSlotKind.Bathtub when ingredientType == ItemID.HallowedBar => 800,
                FurnitureSlotKind.Sink when ingredientType == ItemID.Glass => 1_200,
                FurnitureSlotKind.Sink when ingredientType == ItemID.SilverBar => 600,
                FurnitureSlotKind.Toilet when ingredientType == ItemID.Silk => 800,
                FurnitureSlotKind.Dresser when ingredientType == ItemID.Silk => 600,
                FurnitureSlotKind.Candle when IsTorchIngredient(ingredientType) => 2_200,
                FurnitureSlotKind.Lamp when IsTorchIngredient(ingredientType) => 1_800,
                FurnitureSlotKind.Lantern when IsTorchIngredient(ingredientType) => 1_000,
                FurnitureSlotKind.Chandelier when IsTorchIngredient(ingredientType) => 900,
                FurnitureSlotKind.Candelabra when IsTorchIngredient(ingredientType) => 1_100,
                _ => 0
            };
        }

        /// <summary>除主材料/木组外是否还有其它参与合成的原料。</summary>
        public static bool RecipeHasAuxiliaryIngredient(Recipe recipe, int primaryMaterialBlock)
        {
            if (recipe?.requiredItem == null)
                return false;

            bool hasAux = false;
            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir)
                    continue;

                if (primaryMaterialBlock > ItemID.None && req.type == primaryMaterialBlock)
                    continue;

                int gid = RecipeAnalyzer.GetAcceptedGroupId(recipe, i);
                if (gid == RecipeGroupID.Wood)
                    continue;

                if (IsWoodLikeItem(req.type))
                    continue;

                if (IsGenericCraftingMetalBar(req.type))
                    continue;

                if (ScoreIngredientForSlot(req.type, FurnitureSlotKind.Bed) > 0
                    || ScoreIngredientForSlot(req.type, FurnitureSlotKind.Bookcase) > 0
                    || IsTorchIngredient(req.type)
                    || req.type == ItemID.Glass
                    || req.type == ItemID.Bone
                    || req.type == ItemID.CrystalShard)
                {
                    hasAux = true;
                    continue;
                }

                if (gid >= 0 && gid != RecipeGroupID.Wood)
                {
                    hasAux = true;
                    continue;
                }

                if (IsWoodLikeItem(req.type))
                    continue;

                if (primaryMaterialBlock > ItemID.None && req.type == primaryMaterialBlock)
                    continue;

                hasAux = true;
            }

            return hasAux;
        }

        private static bool IsGenericCraftingMetalBar(int type) =>
            type is ItemID.IronBar or ItemID.LeadBar or ItemID.SilverBar or ItemID.TungstenBar
                or ItemID.GoldBar or ItemID.PlatinumBar or ItemID.CopperBar or ItemID.TinBar;

        private static bool IsWoodLikeItem(int type)
        {
            if (type <= ItemID.None)
                return false;

            if (RecipeGroup.recipeGroups != null
                && RecipeGroupID.Wood < RecipeGroup.recipeGroups.Count
                && RecipeGroup.recipeGroups.TryGetValue(RecipeGroupID.Wood, out RecipeGroup woodGroup)
                && woodGroup.ContainsItem(type))
                return true;

            if (!FurnitureRecognitionCaches.TryGetProbe(type, out Item probe))
                return false;

            if (!FurnitureTileSafety.IsPhysicallySolidTile(probe.createTile))
                return false;

            return FurnitureBuildingBlockRules.IsPlainMaterialBrick(probe);
        }

        private static bool IsTorchIngredient(int type)
        {
            if (type == ItemID.Torch)
                return true;

            Item probe = new Item();
            probe.SetDefaults(type);
            string n = (probe.Name ?? "").ToLowerInvariant();
            return n.Contains("火把") || n.Contains("torch");
        }

        private static bool NameContains(string name, params string[] parts)
        {
            foreach (string p in parts)
            {
                if (!string.IsNullOrEmpty(p) && name.Contains(p))
                    return true;
            }

            return false;
        }
    }
}
