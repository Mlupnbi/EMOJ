using System.Collections.Generic;
using System.Linq;
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

      if (!RecipeConditionsMetWhenNoStations(recipe))
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

    /// <summary>旅途绿格着色：台子仅献祭目录研究满（<see cref="JourneyStationSacrifice"/>）；液体/群系仍为见过或研究满。</summary>
    internal static bool IsRecipeJourneyGreenEnvironmentUnlocked(Recipe recipe)
    {
      if (recipe == null)
        return false;

      if (!RequiredTilesUnlockedForJourneyGreen(recipe))
        return false;

      if (!RecipeJourneyGreenConditionsOk(recipe))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsWaterIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Water))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsLavaIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Lava))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsHoneyIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Honey))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsGraveyardIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Graveyard))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsSnowBiomeIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Snow))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsAlchemyTable(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.AlchemyTable))
        return false;

      return true;
    }

    private static bool RecipeJourneyGreenConditionsOk(Recipe recipe)
    {
      if (recipe?.Conditions == null || recipe.Conditions.Count == 0)
        return true;

      Player player = Main.LocalPlayer;
      if (player == null || !player.active)
        return false;

      foreach (Condition condition in recipe.Conditions)
      {
        if (condition == null)
          continue;

        if (RecipeEnvironmentHelper.ConditionRequiresGraveyard(condition)
            || RecipeEnvironmentHelper.ConditionRequiresSnowBiome(condition)
            || RecipeEnvironmentHelper.ConditionRequiresWater(condition)
            || RecipeEnvironmentHelper.ConditionRequiresLava(condition)
            || RecipeEnvironmentHelper.ConditionRequiresHoney(condition))
        {
          continue;
        }

        if (RecipeEnvironmentHelper.TryGetConditionTileIds(condition, out int[] conditionTiles)
            && conditionTiles != null
            && conditionTiles.Length > 0)
        {
          if (!AnyJourneyGreenStationUnlocked(conditionTiles))
            return false;
          continue;
        }

        if (RecipeEnvironmentHelper.ConditionLooksLikeCraftingStation(condition))
          return false;
      }

      return true;
    }

    internal static string DescribeJourneyGreenEnvironmentFailure(Recipe recipe)
    {
      if (recipe == null)
        return "nullRecipe";

      if (!RequiredTilesUnlockedForJourneyGreen(recipe))
      {
        var missing = new List<string>();
        if (recipe.requiredTile != null)
        {
          foreach (int tileId in recipe.requiredTile)
          {
            if (tileId < 0)
              continue;
            if (!ResearchCraftingPlayer.IsCraftingStationUnlockedForJourneyGreen(tileId))
              missing.Add($"tile{tileId}");
          }
        }

        return missing.Count > 0 ? "stations:" + string.Join(",", missing) : "stations:none";
      }

      if (!RecipeJourneyGreenConditionsOk(recipe))
        return DescribeJourneyGreenConditionFailure(recipe);

      if (RecipeEnvironmentHelper.RecipeNeedsWaterIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Water))
        return "water";

      if (RecipeEnvironmentHelper.RecipeNeedsLavaIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Lava))
        return "lava";

      if (RecipeEnvironmentHelper.RecipeNeedsHoneyIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Honey))
        return "honey";

      if (RecipeEnvironmentHelper.RecipeNeedsGraveyardIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Graveyard))
        return "graveyard";

      if (RecipeEnvironmentHelper.RecipeNeedsSnowBiomeIncludingConditions(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Snow))
        return "snow";

      if (RecipeEnvironmentHelper.RecipeNeedsAlchemyTable(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.AlchemyTable))
        return "alchemy";

      return "unknown";
    }

    /// <summary>部分 Mod 把合成台写在 Condition 里而不写 requiredTile。</summary>
    private static bool RecipeConditionsMetWhenNoStations(Recipe recipe)
    {
      if (recipe.requiredTile != null && recipe.requiredTile.Any(t => t >= 0))
        return true;
      if (recipe.Conditions == null || recipe.Conditions.Count == 0)
        return true;

      Player player = Main.LocalPlayer;
      if (player == null || !player.active)
        return false;

      foreach (Condition condition in recipe.Conditions)
      {
        if (condition == null)
          continue;
        try
        {
          if (!condition.IsMet())
            return false;
        }
        catch
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>RB 嵌套入列：台子须已见过（对齐 RecipePath 对 seenTiles 的裁剪，不含「仅研究满」）。</summary>
    internal static bool IsRecipeNestedListEnvironmentOk(Recipe recipe)
    {
      if (recipe == null)
        return false;

      if (!RequiredTilesSeen(recipe))
        return false;

      if (!RecipeConditionsMetWhenNoStations(recipe))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsWater(recipe)
          && !IsEnvironmentSeen(CraftEnvironmentFlags.Water))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsLava(recipe)
          && !IsEnvironmentSeen(CraftEnvironmentFlags.Lava))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsHoney(recipe)
          && !IsEnvironmentSeen(CraftEnvironmentFlags.Honey))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsGraveyard(recipe)
          && !IsEnvironmentSeen(CraftEnvironmentFlags.Graveyard))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsSnowBiome(recipe)
          && !IsEnvironmentSeen(CraftEnvironmentFlags.Snow))
        return false;

      if (RecipeEnvironmentHelper.RecipeNeedsAlchemyTable(recipe)
          && !IsEnvironmentSeen(CraftEnvironmentFlags.AlchemyTable))
        return false;

      return true;
    }

  /// <summary>已遇见的环境（不含 ResearchedEnvironment），用于嵌套入列。</summary>
    internal static bool IsEnvironmentSeen(CraftEnvironmentFlags flag)
    {
      if (flag == CraftEnvironmentFlags.None)
        return true;

      Player player = Main.LocalPlayer;
      if (player != null && player.active)
      {
        CraftEnvironmentFlags live = CaptureSeenFromPlayer(player);
        if ((live & flag) == flag)
          return true;
      }

      if ((ResearchCraftingPlayer.SeenEnvironment & flag) == flag)
        return true;

      switch (flag)
      {
        case CraftEnvironmentFlags.Water:
        case CraftEnvironmentFlags.Lava:
        case CraftEnvironmentFlags.Honey:
        {
          CraftEnvironmentFlags fluids = ResearchCraftingPlayer.SeenEnvironment & (CraftEnvironmentFlags.Water | CraftEnvironmentFlags.Lava | CraftEnvironmentFlags.Honey);
          if (player != null && player.active)
            fluids |= CollectInventoryEnvironmentFlags(player);
          return (fluids & flag) == flag;
        }
        case CraftEnvironmentFlags.AlchemyTable:
          return ResearchCraftingPlayer.IsCraftingStationSeen(TileID.AlchemyTable);
        default:
          return false;
      }
    }

    internal static bool RequiredTilesSeen(Recipe recipe)
    {
      if (recipe.requiredTile == null || recipe.requiredTile.Count == 0)
        return true;

      bool anyStation = false;
      foreach (int tileId in recipe.requiredTile)
      {
        if (tileId < 0)
          continue;

        anyStation = true;
        if (ResearchCraftingPlayer.IsCraftingStationSeen(tileId))
          return true;

        Player player = Main.LocalPlayer;
        if (player?.adjTile != null
            && tileId < player.adjTile.Length
            && player.adjTile[tileId])
          return true;

        if (player != null && player.active && ResearchCraftingPlayer.DebugCarriesTile(player, tileId))
          return true;
      }

      return !anyStation;
    }

    internal static string DescribeEnvironmentFailure(Recipe recipe)
    {
      if (recipe == null)
        return "nullRecipe";

      if (!RequiredTilesUnlocked(recipe))
      {
        var missing = new List<string>();
        if (recipe.requiredTile != null)
        {
          foreach (int tileId in recipe.requiredTile)
          {
            if (tileId < 0)
              continue;
            if (!ResearchCraftingPlayer.IsCraftingEnvironmentUnlocked(tileId))
              missing.Add($"tile{tileId}");
          }
        }

        return missing.Count > 0 ? "stations:" + string.Join(",", missing) : "stations:none";
      }

      if (!RecipeConditionsMetWhenNoStations(recipe))
        return DescribeFailedConditions(recipe);

      if (RecipeEnvironmentHelper.RecipeNeedsWater(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Water))
        return "water";

      if (RecipeEnvironmentHelper.RecipeNeedsLava(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Lava))
        return "lava";

      if (RecipeEnvironmentHelper.RecipeNeedsHoney(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Honey))
        return "honey";

      if (RecipeEnvironmentHelper.RecipeNeedsGraveyard(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Graveyard))
        return "graveyard";

      if (RecipeEnvironmentHelper.RecipeNeedsSnowBiome(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.Snow))
        return "snow";

      if (RecipeEnvironmentHelper.RecipeNeedsAlchemyTable(recipe)
          && !ResearchCraftingPlayer.IsEnvironmentUnlocked(CraftEnvironmentFlags.AlchemyTable))
        return "alchemy";

      return "unknown";
    }

    private static string DescribeFailedConditions(Recipe recipe)
    {
      if (recipe.Conditions == null)
        return "conditions";

      foreach (Condition condition in recipe.Conditions)
      {
        if (condition == null)
          continue;
        try
        {
          if (!condition.IsMet())
            return "condition:" + (condition.Description?.Value ?? "?");
        }
        catch (System.Exception ex)
        {
          return "conditionEx:" + ex.Message;
        }
      }

      return "conditions";
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

    private static bool RequiredTilesUnlockedForJourneyGreen(Recipe recipe)
    {
      if (recipe.requiredTile == null || recipe.requiredTile.Count == 0)
        return true;

      bool anyStation = false;
      foreach (int tileId in recipe.requiredTile)
      {
        if (tileId < 0)
          continue;

        anyStation = true;
        if (ResearchCraftingPlayer.IsCraftingStationUnlockedForJourneyGreen(tileId))
          return true;
      }

      return !anyStation;
    }

    private static bool AnyJourneyGreenStationUnlocked(int[] tileIds)
    {
      if (tileIds == null || tileIds.Length == 0)
        return true;

      foreach (int tileId in tileIds)
      {
        if (tileId < 0)
          continue;
        if (ResearchCraftingPlayer.IsCraftingStationUnlockedForJourneyGreen(tileId))
          return true;
      }

      return false;
    }

    private static string DescribeJourneyGreenConditionFailure(Recipe recipe)
    {
      if (recipe?.Conditions == null)
        return "conditions";

      foreach (Condition condition in recipe.Conditions)
      {
        if (condition == null)
          continue;

        if (RecipeEnvironmentHelper.ConditionRequiresGraveyard(condition)
            || RecipeEnvironmentHelper.ConditionRequiresSnowBiome(condition)
            || RecipeEnvironmentHelper.ConditionRequiresWater(condition)
            || RecipeEnvironmentHelper.ConditionRequiresLava(condition)
            || RecipeEnvironmentHelper.ConditionRequiresHoney(condition))
        {
          continue;
        }

        if (RecipeEnvironmentHelper.TryGetConditionTileIds(condition, out int[] conditionTiles)
            && conditionTiles != null
            && conditionTiles.Length > 0
            && !AnyJourneyGreenStationUnlocked(conditionTiles))
        {
          return "conditionStation:" + string.Join(",", conditionTiles);
        }

        if (RecipeEnvironmentHelper.ConditionLooksLikeCraftingStation(condition))
          return "conditionStation:unresolved";
      }

      return "conditions";
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
          if (CraftingStationAdjacency.ProvidesStation(portable, targetTile))
            return true;
        }
      }

      if (item.createTile >= 0)
      {
        if (CraftingStationAdjacency.ProvidesStation(item.createTile, targetTile))
          return true;
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
            flags |= FlagsFromFluidTile(tile);
        }
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
          MarkResearchedTileExact(researchedTiles, tile);
          flags |= FlagsFromFluidTile(tile);
        }
      }

      if (item.createTile >= 0)
      {
        MarkResearchedTileExact(researchedTiles, item.createTile);
        flags |= FlagsFromFluidTile(item.createTile);
      }

      flags |= FlagsFromItemType(itemType);

      // 规则 2：研究满对应放置物才解锁墓园/雪原等（背包携带不算）
      if (item.createTile == GraveyardTombstoneTile)
        flags |= CraftEnvironmentFlags.Graveyard;

      if (item.createTile >= 0 && item.createTile < TileID.Sets.SnowBiome.Length && TileID.Sets.SnowBiome[item.createTile] > 0)
        flags |= CraftEnvironmentFlags.Snow;

      if (item.createTile == TileID.AlchemyTable || item.createTile == 355)
        flags |= CraftEnvironmentFlags.AlchemyTable;

      return flags;
    }

    private static void MarkResearchedTileExact(bool[] researchedTiles, int tileType)
    {
      if (researchedTiles == null || tileType < 0 || tileType >= researchedTiles.Length)
        return;

      researchedTiles[tileType] = true;
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

    /// <summary>附近方块扫描：只认液体源，不把墓碑/雪块当成「已在墓园/雪原」。</summary>
    internal static CraftEnvironmentFlags FlagsFromFluidTile(int tileType)
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
