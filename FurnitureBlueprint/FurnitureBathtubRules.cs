using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    internal static class FurnitureBathtubRules
    {
        public static bool SharesSetWithMaterial(int itemType, int materialBlock, int seedType)
        {
            if (itemType <= ItemID.None)
                return false;

            if (!FurnitureRecognitionCaches.TryGetProbe(itemType, out Item item))
                return false;

            if (item.createTile != TileID.Bathtubs)
                return false;

            if (materialBlock > ItemID.None)
            {
                if (FurnitureRecipeSetLinker.ProductUsesExactMaterial(itemType, materialBlock))
                {
                    if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))
                    {
                        string blockKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
                        string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
                        if (FurnitureSlotScoring.PassesModLineageWoodProductLink(
                                itemType, seedType, materialBlock, blockKey, productKey))
                            return true;
                    }
                    else
                        return true;
                }

                string blockKey2 = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
                string productKey2 = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
                if (FurnitureStyleSignature.StyleKeyFuzzyMatch(blockKey2, productKey2)
                    || FurnitureMaterialKeyNormalizer.StyleKeysMatch(blockKey2, productKey2)
                    || FurnitureMaterialKeyNormalizer.SameMaterialFamily(blockKey2, productKey2))
                    return true;

                if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))
                    return false;

                ModItem matMi = ItemLoader.GetItem(materialBlock);
                ModItem prodMi = ItemLoader.GetItem(itemType);
                if (matMi != null && prodMi != null && matMi.Mod.Name == prodMi.Mod.Name)
                    return true;
            }

            if (seedType > ItemID.None)
            {
                if (FurnitureSetLineageScoring.ScoreSeedLineage(itemType, seedType, materialBlock)
                    >= FurnitureSetLineageScoring.LineageStrong)
                    return true;

                if (RecipeAnalyzer.IsHighFanoutMaterial(materialBlock))
                {
                    string blockKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(materialBlock);
                    string productKey = FurnitureSetRecognizer.ExtractStyleKeyPublic(itemType);
                    if (FurnitureSlotScoring.PassesModLineageWoodProductLink(
                            itemType, seedType, materialBlock, blockKey, productKey))
                        return true;

                    return false;
                }

                ModItem seedMi = ItemLoader.GetItem(seedType);
                ModItem prodMi = ItemLoader.GetItem(itemType);
                if (seedMi != null && prodMi != null && seedMi.Mod.Name == prodMi.Mod.Name)
                    return true;
            }

            return false;
        }
    }
}
