using System.Linq;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    /// <summary>物品名：装饰物排除、材料名+部件名吻合加分。</summary>
    public static class FurnitureNameSignals
    {
        public static bool IsDecorativeMark(string nameLower)
        {
            if (string.IsNullOrEmpty(nameLower))
                return false;

            if (nameLower.Contains("印记") || nameLower.Contains("徽章") || nameLower.Contains("纪念")
                || nameLower.Contains("旗帜") || nameLower.Contains("奖杯") || nameLower.Contains("祭坛")
                || nameLower.Contains("雕像") || nameLower.Contains("挂画") || nameLower.Contains("画像"))
                return true;

            if (nameLower.Contains("画") && !nameLower.Contains("画像") && !nameLower.Contains("书架"))
                return true;

            if (nameLower.Contains("海报") || nameLower.Contains("poster") || nameLower.Contains("banner")
                || nameLower.Contains("壁画") || nameLower.Contains("painting"))
                return true;

            if (nameLower.Contains("sigil") || nameLower.Contains("banner") || nameLower.Contains("trophy")
                || nameLower.Contains("altar") || nameLower.Contains("plaque") || nameLower.Contains("emblem")
                || nameLower.Contains("relic"))
                return true;

            if (nameLower.Contains("柱") || nameLower.Contains("pillar") || nameLower.Contains("column"))
                return true;

            return false;
        }

        public static bool IsDecorativeMark(int itemType)
        {
            if (itemType <= ItemID.None)
                return false;

            Item item = new Item();
            item.SetDefaults(itemType);
            return IsDecorativeMark((item.Name ?? "").ToLowerInvariant());
        }

        public static bool HasSlotPartKeyword(string nameLower, FurnitureSlotKind slot)
        {
            if (string.IsNullOrEmpty(nameLower))
                return false;

            return slot switch
            {
                FurnitureSlotKind.Chair => nameLower.Contains("椅") || nameLower.Contains("chair"),
                FurnitureSlotKind.Table => (nameLower.Contains("桌") && !nameLower.Contains("书桌"))
                    || nameLower.Contains("table") || nameLower.Contains("desk"),
                FurnitureSlotKind.Bed => nameLower.Contains("床") || nameLower.Contains("bed") || nameLower.Contains("睡眠"),
                FurnitureSlotKind.Sofa => nameLower.Contains("沙发") || nameLower.Contains("sofa") || nameLower.Contains("长凳"),
                FurnitureSlotKind.Door => nameLower.Contains("门") || nameLower.Contains("door"),
                FurnitureSlotKind.Wall => nameLower.Contains("墙") || nameLower.Contains("wall"),
                FurnitureSlotKind.Chest => nameLower.Contains("箱") || nameLower.Contains("chest"),
                FurnitureSlotKind.Lamp => nameLower.Contains("灯") && !nameLower.Contains("烛"),
                FurnitureSlotKind.Bookcase => nameLower.Contains("书架") || nameLower.Contains("书柜") || nameLower.Contains("bookcase"),
                FurnitureSlotKind.Sink => nameLower.Contains("水槽") || nameLower.Contains("sink"),
                FurnitureSlotKind.Toilet => nameLower.Contains("马桶") || nameLower.Contains("toilet"),
                FurnitureSlotKind.Bathtub => nameLower.Contains("浴缸") || nameLower.Contains("bathtub"),
                FurnitureSlotKind.Piano => nameLower.Contains("钢琴") || nameLower.Contains("键盘") || nameLower.Contains("piano"),
                FurnitureSlotKind.Workbench => nameLower.Contains("工作台") || nameLower.Contains("制作站")
                    || nameLower.Contains("workbench") || nameLower.Contains("work bench"),
                _ => false
            };
        }

        /// <summary>显示名含材料词 + 目标槽部件词时加分（如「紫杉木椅」「干木墙」）；显示名与 StyleKey 不一致时用族匹配兜底。</summary>
        public static int ScoreMaterialPartName(int itemType, int materialBlock, FurnitureSlotKind slot, int seedType = ItemID.None)
        {
            if (itemType <= ItemID.None || materialBlock <= ItemID.None || IsDecorativeMark(itemType))
                return 0;

            Item mat = new Item();
            mat.SetDefaults(materialBlock);
            Item prod = new Item();
            prod.SetDefaults(itemType);

            string prodLower = (prod.Name ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(prodLower) || !HasSlotPartKeyword(prodLower, slot))
                return 0;

            if (seedType > ItemID.None)
            {
                string seedMoniker = FurnitureSetLineageScoring.ExtractSeedLineageMoniker(seedType).ToLowerInvariant();
                if (seedMoniker.Length >= 2 && prodLower.Contains(seedMoniker))
                    return FurnitureSlotScoring.MaterialPartNameStrong;

                string matToken = NormalizeMaterialDisplayName(mat.Name);
                if (!string.IsNullOrEmpty(matToken)
                    && seedMoniker.Length > matToken.Length
                    && seedMoniker.StartsWith(matToken, System.StringComparison.OrdinalIgnoreCase)
                    && prodLower.Contains(matToken)
                    && !prodLower.Contains(seedMoniker.Substring(matToken.Length).Trim()))
                    return FurnitureSetLineageScoring.MaterialOnlyPartial;
            }

            string matTokenFallback = NormalizeMaterialDisplayName(mat.Name);
            if (!string.IsNullOrEmpty(matTokenFallback) && prodLower.Contains(matTokenFallback))
                return FurnitureSlotScoring.MaterialPartNameStrong;

            string matKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
            string prodKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
            if (string.IsNullOrWhiteSpace(matKey) || string.IsNullOrWhiteSpace(prodKey))
                return 0;

            if (FurnitureStyleSignature.StyleKeyFuzzyMatch(matKey, prodKey)
                || FurnitureMaterialKeyNormalizer.SameMaterialFamily(matKey, prodKey))
                return FurnitureSlotScoring.MaterialPartNameStrong;

            return 0;
        }

        public static string NormalizeMaterialDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "";

            string s = displayName.Trim().ToLowerInvariant();
            string[] strip =
            {
                "块", "砖", "板", "墙", "平台", "家具", "材料", "物品",
                "block", "brick", "wall", "platform", "furniture", "plank"
            };

            foreach (string suffix in strip)
            {
                if (s.EndsWith(suffix) && s.Length > suffix.Length + 1)
                    s = s.Substring(0, s.Length - suffix.Length);
            }

            return s.Trim();
        }

        public static bool MeetsChairPickEvidence(int itemType, int materialBlock, FurnitureRecognizeContext ctx)
        {
            if (FurnitureNameSignals.IsDecorativeMark(itemType))
                return false;

            if (ScoreMaterialPartName(itemType, materialBlock, FurnitureSlotKind.Chair) > 0)
                return true;

            if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, FurnitureSlotKind.Chair) > 0)
                return true;

            return FurnitureCandidateFilter.ScoreFootprintBonus(itemType, FurnitureSlotKind.Chair)
                   >= FurnitureSlotScoring.FootprintPerfect;
        }

        public static bool MeetsBedPickEvidence(int itemType, int materialBlock, int seedType)
        {
            if (IsDecorativeMark(itemType))
                return false;

            if (ScoreMaterialPartName(itemType, materialBlock, FurnitureSlotKind.Bed, seedType) > 0)
                return true;

            if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, FurnitureSlotKind.Bed) > 0)
                return true;

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return false;
            return item.createTile == TileID.Beds;
        }

        public static bool MeetsBathtubPickEvidence(int itemType, int materialBlock, int seedType)
        {
            if (IsDecorativeMark(itemType))
                return false;

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return false;

            string name = (item.Name ?? "").ToLowerInvariant();
            if (name.Contains("浴缸") || name.Contains("bathtub"))
                return true;

            if (seedType > ItemID.None)
            {
                string moniker = FurnitureSetRecognizer.ExtractDisplayLineageMoniker(seedType).ToLowerInvariant();
                if (moniker.Length >= 2 && name.Contains(moniker) && name.Contains("浴"))
                    return true;
            }

            if (ScoreMaterialPartName(itemType, materialBlock, FurnitureSlotKind.Bathtub, seedType) > 0)
                return true;

            if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, FurnitureSlotKind.Bathtub) > 0)
                return true;

            if (item.createTile == TileID.Bathtubs)
                return FurnitureBathtubRules.SharesSetWithMaterial(itemType, materialBlock, seedType);

            return false;
        }

        public static bool MeetsWorkbenchPickEvidence(int itemType, int materialBlock, int seedType)
        {
            if (IsDecorativeMark(itemType))
                return false;

            if (ScoreMaterialPartName(itemType, materialBlock, FurnitureSlotKind.Workbench, seedType) > 0)
                return true;

            if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, FurnitureSlotKind.Workbench) > 0)
                return true;

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return false;

            return item.createTile == TileID.WorkBenches;
        }

        public static bool MeetsSinkPickEvidence(int itemType, int materialBlock, int seedType)
        {
            if (IsDecorativeMark(itemType))
                return false;

            if (ScoreMaterialPartName(itemType, materialBlock, FurnitureSlotKind.Sink, seedType) > 0)
                return true;

            if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, FurnitureSlotKind.Sink) > 0)
                return true;

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return false;

            return item.createTile == TileID.Sinks;
        }

        public static bool MeetsTablePickEvidence(int itemType, int materialBlock, int seedType)
        {
            if (IsDecorativeMark(itemType))
                return false;

            if (ScoreMaterialPartName(itemType, materialBlock, FurnitureSlotKind.Table, seedType) > 0)
                return true;

            if (FurnitureRecipeSlotSignals.ScoreNameBonus(itemType, FurnitureSlotKind.Table) > 0)
                return true;

            Item item = new Item();
            if (!FurnitureItemDefaults.TrySetDefaults(item, itemType))
                return false;

            if (FurnitureTileSafety.RoomNeedsCountsAsTable(item.createTile))
                return true;

            return false;
        }
    }
}
