using System.Collections.Generic;

using Terraria;

using Terraria.ID;

using EvenMoreOverpoweredJourney.Research;



namespace EvenMoreOverpoweredJourney.FurnitureBlueprint

{

    /// <summary>产物→配方：索引未命中时回退扫描 Main.recipe（模组加载顺序常导致索引漏项）。</summary>

    public static class FurnitureRecipeLookup

    {

        public static IEnumerable<Recipe> GetRecipesCreating(int productType)

        {

            if (productType <= ItemID.None)

                yield break;



            bool any = false;

            foreach (Recipe recipe in RecipeAnalyzer.GetRecipesForItem(productType))

            {

                any = true;

                yield return recipe;

            }



            if (any)

                yield break;



            foreach (Recipe recipe in Main.recipe)

            {

                if (recipe?.createItem == null || recipe.createItem.IsAir)

                    continue;

                if (recipe.createItem.type == productType)

                    yield return recipe;

            }

        }

    }

}


