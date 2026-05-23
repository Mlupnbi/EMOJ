using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvenMoreOverpoweredJourney.Research.Players;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
  /// <summary>
  /// 制作环境：身边/背包（SeenTiles、adjTile、携带物）或对应物品已旅途研究满（ResearchedTiles / ResearchedEnvironment），满足其一即可。
  /// </summary>
  internal static class ResearchCraftEnvironment
  {
    internal const int GraveyardTombstoneTile = 85;
    internal const int SnowBiomeScoreRequired = 1500;

    internal static bool IsRecipeEnvironmentUnlocked(Recipe recipe)
    {
      if (recipe == null)
        return false;

      if (!RequiredTilesUnlocked(recipe))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsWater(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Water))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsLava(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Lava))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsHoney(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Honey))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsGraveyard(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Graveyard))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsSnowBiome(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Snow))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsAlchemyTable(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.AlchemyTable))
        return false;

      return true;
    }

    internal static bool RequiredTilesUnlocked(Recipe recipe)
    {
      if (recipe.requiredTile == null || recipe.requiredTile.Count == 0)
        return true;

      bool anyStation = false;
      foreach (int tileId in recipe.requiredTile)
      {
        if (tileId < 0)
          continue;

        anyStation = true;
        if (ResearchCraftingPlayer.IsCraftingEnvironmentUnlocked(tileId))
          return true;
      }

      // 仅含 -1 等占位符 → 视为徒手/无台子要求
      return !anyStation;
    }

    internal static bool ItemProvidesCraftingTile(int itemType, int targetTile)
    {
      if (itemType <= ItemID.None || targetTile <= 0)
        return false;

      if (!ContentSamples.ItemsByType.TryGetValue(itemType, out Item item))
        return false;

      if (PortableCraftEnvironmentRegistry.TryGetTiles(itemType, out int[] portableTiles))
      {
        foreach (int portable in portableTiles)
        {
          foreach (int expanded in CraftingStationAdjacency.Expand(portable))
          {
            if (expanded == targetTile)
              return true;
          }
        }
      }

      if (item.createTile >= 0)
      {
        foreach (int expanded in CraftingStationAdjacency.Expand(item.createTile))
        {
          if (expanded == targetTile)
            return true;
        }
      }

      return false;
    }

    internal static CraftEnvironmentFlags CollectInventoryEnvironmentFlags(Player player)
    {
      if (player == null)
        return CraftEnvironmentFlags.None;

      CraftEnvironmentFlags flags = CraftEnvironmentFlags.None;

      void considerItem(int itemType)
      {
        if (itemType <= ItemID.None)
          return;

        flags |= FlagsFromItemType(itemType);

        if (PortableCraftEnvironmentRegistry.TryGetTiles(itemType, out int[] portableTiles))
        {
          foreach (int tile in portableTiles)
            flags |= FlagsFromTile(tile);
        }

        if (ContentSamples.ItemsByType.TryGetValue(itemType, out Item item) && item.createTile >= 0)
          flags |= FlagsFromTile(item.createTile);
      }

      for (int i = 0; i < player.inventory.Length; i++)
        considerItem(player.inventory[i].type);

      void scanBank(Item[] bank)
      {
        if (bank == null)
          return;
        foreach (Item it in bank)
        {
          if (it != null && !it.IsAir)
            considerItem(it.type);
        }
      }

      scanBank(player.bank.item);
      scanBank(player.bank2.item);
      scanBank(player.bank3.item);
      scanBank(player.bank4.item);

      return flags;
    }

    internal static CraftEnvironmentFlags ApplyItemToResearchedEnvironment(int itemType, bool[] researchedTiles)
    {
      if (itemType <= ItemID.None || !ContentSamples.ItemsByType.TryGetValue(itemType, out Item item))
        return CraftEnvironmentFlags.None;

      CraftEnvironmentFlags flags = CraftEnvironmentFlags.None;

      if (PortableCraftEnvironmentRegistry.TryGetTiles(itemType, out int[] portableTiles))
      {
        foreach (int tile in portableTiles)
        {
          CraftingStationAdjacency.MarkExpanded(researchedTiles, tile);
          flags |= FlagsFromTile(tile);
        }
      }

      if (item.createTile >= 0)
      {
        CraftingStationAdjacency.MarkExpanded(researchedTiles, item.createTile);
        flags |= FlagsFromTile(item.createTile);
      }

      flags |= FlagsFromItemType(itemType);

      if (item.createTile == GraveyardTombstoneTile)
        flags |= CraftEnvironmentFlags.Graveyard;

      if (item.createTile >= 0 && TileID.Sets.SnowBiome[item.createTile] > 0)
        flags |= CraftEnvironmentFlags.Snow;

      return flags;
    }

    internal static CraftEnvironmentFlags FlagsFromItemType(int itemType)
    {
      CraftEnvironmentFlags flags = CraftEnvironmentFlags.None;

      switch (itemType)
      {
        case ItemID.WaterBucket:
        case ItemID.BottomlessBucket:
          flags |= CraftEnvironmentFlags.Water;
          break;
        case ItemID.LavaBucket:
        case ItemID.BottomlessLavaBucket:
          flags |= CraftEnvironmentFlags.Lava;
          break;
        case ItemID.HoneyBucket:
          flags |= CraftEnvironmentFlags.Honey;
          break;
      }

      return flags;
    }

    internal static CraftEnvironmentFlags FlagsFromTile(int tileType)
    {
      if (tileType < 0)
        return CraftEnvironmentFlags.None;

      CraftEnvironmentFlags flags = CraftEnvironmentFlags.None;

      if (tileType < TileID.Sets.CountsAsWaterSource.Length && TileID.Sets.CountsAsWaterSource[tileType])
        flags |= CraftEnvironmentFlags.Water;
      if (tileType < TileID.Sets.CountsAsLavaSource.Length && TileID.Sets.CountsAsLavaSource[tileType])
        flags |= CraftEnvironmentFlags.Lava;
      if (tileType < TileID.Sets.CountsAsHoneySource.Length && TileID.Sets.CountsAsHoneySource[tileType])
        flags |= CraftEnvironmentFlags.Honey;
      if (tileType == GraveyardTombstoneTile)
        flags |= CraftEnvironmentFlags.Graveyard;
      if (tileType < TileID.Sets.SnowBiome.Length && TileID.Sets.SnowBiome[tileType] > 0)
        flags |= CraftEnvironmentFlags.Snow;
      if (tileType == TileID.AlchemyTable)
        flags |= CraftEnvironmentFlags.AlchemyTable;

      switch (tileType)
      {
        case 355:
          flags |= CraftEnvironmentFlags.AlchemyTable;
          break;
      }

      return flags;
    }

        internal static CraftEnvironmentFlags CaptureSeenFromPlayer(Player player)
        {
            if (player == null)
                return CraftEnvironmentFlags.None;

            CraftEnvironmentFlags flags = CraftEnvironmentFlags.None;

            if (player.adjWater)
                flags |= CraftEnvironmentFlags.Water;
            if (player.adjLava)
                flags |= CraftEnvironmentFlags.Lava;
            if (player.adjHoney)
                flags |= CraftEnvironmentFlags.Honey;
            if (player.ZoneGraveyard)
                flags |= CraftEnvironmentFlags.Graveyard;
            if (player.ZoneSnow)
                flags |= CraftEnvironmentFlags.Snow;
            if (player.alchemyTable)
                flags |= CraftEnvironmentFlags.AlchemyTable;

            return flags;
        }

    internal static int BuildEnvironmentSignature(
      CraftEnvironmentFlags seen,
      CraftEnvironmentFlags researched,
      bool[] seenTiles,
      bool[] researchedTiles)
    {
      unchecked
      {
        int hash = ((int)seen * 397) ^ (int)researched;
        hash = hash * 397 ^ CountTrue(seenTiles);
        hash = hash * 397 ^ CountTrue(researchedTiles);
        return hash;
      }
    }

    private static int CountTrue(bool[] flags)
    {
      if (flags == null)
        return 0;

      int count = 0;
      for (int i = 0; i < flags.Length; i++)
      {
        if (flags[i])
          count++;
      }

      return count;
    }
  }
}
