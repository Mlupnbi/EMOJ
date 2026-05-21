using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using EvenMoreOverpoweredJourney.Core.Logging;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus
{
    /// <summary>��װ������ Buff ���� UpdateArmorSets �ж���αװװ����ͬ֡�ڰ�����ȥ�ز����� Mod ��װ���������</summary>
    public sealed class SetBonusHookSystem : ModSystem
    {
        private delegate void AfterApply(Player player);

        private const int MaxModSetUpdatesPerFrame = 5;
        private const int MaxArmorSetCallsPerFrame = 24;

        /// <summary>ָ�� Hook �ڲ��������� <see cref="PatchComplexSetBonusBuffs"/>����</summary>
        private static On_Player.orig_UpdateArmorSets _innerUpdateArmorSets;

        private static bool _applyingVirtualSetBonus;
        private static readonly HashSet<string> AfterApplyArmorKeysDone = new HashSet<string>();
        private static int _modSetRotor;
        private static uint _lastArmorSetBudgetFrame;
        private static int _armorSetCallsThisFrame;
        private static bool _circuitBroken;
        private static uint _circuitBreakNotifiedFrame;

        private sealed class SetBonusDefinition
        {
            public readonly string[] BuffNames;
            public readonly string HeadItemName;
            public readonly string BodyItemName;
            public readonly string LegsItemName;
            public readonly AfterApply AfterApply;

            public SetBonusDefinition(string[] buffNames, string headItemName, string bodyItemName, string legsItemName, AfterApply afterApply = null)
            {
                BuffNames = buffNames;
                HeadItemName = headItemName;
                BodyItemName = bodyItemName;
                LegsItemName = legsItemName;
                AfterApply = afterApply;
            }

            public string ArmorKey => HeadItemName + "|" + BodyItemName + "|" + LegsItemName;
        }

        private static readonly SetBonusDefinition[] SupportedSetBonuses =
        {
            new SetBonusDefinition(
                new[] { "SolarShield1", "SolarShield2", "SolarShield3" },
                "SolarFlareHelmet", "SolarFlareBreastplate", "SolarFlareLeggings",
                player =>
                {
                    player.solarShields = 3;
                    player.solarCounter = 180;
                }),
            new SetBonusDefinition(
                new[] { "BeetleEndurance1", "BeetleEndurance2", "BeetleEndurance3" },
                "BeetleHelmet", "BeetleShell", "BeetleLeggings",
                player => player.beetleOrbs = 3),
            new SetBonusDefinition(
                new[] { "BeetleMight1", "BeetleMight2", "BeetleMight3" },
                "BeetleHelmet", "BeetleScaleMail", "BeetleLeggings",
                player => player.beetleOrbs = 3),
            new SetBonusDefinition(new[] { "StardustGuardianMinion" }, "StardustHelmet", "StardustPlate", "StardustLeggings"),
            new SetBonusDefinition(new[] { "VortexStealth" }, "VortexHelmet", "VortexBreastplate", "VortexLeggings"),
            new SetBonusDefinition(
                new[]
                {
                    "NebulaUpLife1", "NebulaUpLife2", "NebulaUpLife3",
                    "NebulaUpMana1", "NebulaUpMana2", "NebulaUpMana3",
                    "NebulaUpDmg1", "NebulaUpDmg2", "NebulaUpDmg3"
                },
                "NebulaHelmet", "NebulaBreastplate", "NebulaLeggings"),
            new SetBonusDefinition(new[] { "ShroomiteStealth" }, "ShroomiteHeadgear", "ShroomiteBreastplate", "ShroomiteLeggings"),
            new SetBonusDefinition(new[] { "ShroomiteStealth" }, "ShroomiteMask", "ShroomiteBreastplate", "ShroomiteLeggings"),
            new SetBonusDefinition(new[] { "ShroomiteStealth" }, "ShroomiteHelmet", "ShroomiteBreastplate", "ShroomiteLeggings"),
            new SetBonusDefinition(new[] { "HolyProtection" }, "HallowedHelmet", "HallowedPlateMail", "HallowedGreaves"),
            new SetBonusDefinition(new[] { "HolyProtection" }, "HallowedHeadgear", "HallowedPlateMail", "HallowedGreaves"),
            new SetBonusDefinition(new[] { "HolyProtection" }, "HallowedMask", "HallowedPlateMail", "HallowedGreaves"),
            new SetBonusDefinition(new[] { "HolyProtection" }, "HallowedHood", "HallowedPlateMail", "HallowedGreaves"),
            new SetBonusDefinition(new[] { "TitaniumStorm" }, "TitaniumHelmet", "TitaniumBreastplate", "TitaniumLeggings"),
            new SetBonusDefinition(new[] { "TitaniumStorm" }, "TitaniumHeadgear", "TitaniumBreastplate", "TitaniumLeggings"),
            new SetBonusDefinition(new[] { "TitaniumStorm" }, "TitaniumMask", "TitaniumBreastplate", "TitaniumLeggings"),
            new SetBonusDefinition(new[] { "ShadowDodge" }, "TitaniumHelmet", "TitaniumBreastplate", "TitaniumLeggings"),
            new SetBonusDefinition(new[] { "ShadowDodge" }, "TitaniumHeadgear", "TitaniumBreastplate", "TitaniumLeggings"),
            new SetBonusDefinition(new[] { "ShadowDodge" }, "TitaniumMask", "TitaniumBreastplate", "TitaniumLeggings")
        };

        public override void Load()
        {
            On_Player.UpdateArmorSets += CaptureInnerUpdateArmorSets;
            On_Player.UpdateArmorSets += PatchComplexSetBonusBuffs;
        }

        public override void Unload()
        {
            On_Player.UpdateArmorSets -= PatchComplexSetBonusBuffs;
            On_Player.UpdateArmorSets -= CaptureInnerUpdateArmorSets;
            ResetRuntimeState();
        }

        public static void ResetRuntimeState()
        {
            _innerUpdateArmorSets = null;
            _applyingVirtualSetBonus = false;
            AfterApplyArmorKeysDone.Clear();
            _modSetRotor = 0;
            _lastArmorSetBudgetFrame = 0;
            _armorSetCallsThisFrame = 0;
            _circuitBroken = false;
            _circuitBreakNotifiedFrame = 0;
        }

        private static void CaptureInnerUpdateArmorSets(On_Player.orig_UpdateArmorSets orig, Player player, int i)
        {
            _innerUpdateArmorSets ??= orig;
            orig(player, i);
        }

        private static void PatchComplexSetBonusBuffs(On_Player.orig_UpdateArmorSets orig, Player player, int i)
        {
            if (_applyingVirtualSetBonus)
            {
                orig(player, i);
                return;
            }

            orig(player, i);

            if (player.whoAmI != Main.myPlayer)
                return;

            BuffResearchPlayer modPlayer = player.GetModPlayer<BuffResearchPlayer>();
            if (!modPlayer.HasAnyKnownSetBonusBuffActive())
            {
                AfterApplyArmorKeysDone.Clear();
                if (_circuitBroken)
                    ResetRuntimeState();

                return;
            }

            if (_circuitBroken)
                return;

            if (_innerUpdateArmorSets == null)
                return;

            BeginArmorSetBudgetFrame();

            int oldHead = player.head;
            int oldBody = player.body;
            int oldLegs = player.legs;

            _applyingVirtualSetBonus = true;
            try
            {
                ApplyVanillaVirtualSets(player, i, modPlayer);
                ApplyModVirtualSets(player, modPlayer);
            }
            finally
            {
                _applyingVirtualSetBonus = false;
                player.head = oldHead;
                player.body = oldBody;
                player.legs = oldLegs;
            }

            if (_armorSetCallsThisFrame > MaxArmorSetCallsPerFrame)
                TripCircuitBreaker();
        }

        private static void BeginArmorSetBudgetFrame()
        {
            uint frame = (uint)Main.GameUpdateCount;
            if (frame == _lastArmorSetBudgetFrame)
                return;

            _lastArmorSetBudgetFrame = frame;
            _armorSetCallsThisFrame = 0;
        }

        private static bool TryConsumeArmorSetCallBudget()
        {
            _armorSetCallsThisFrame++;
            return _armorSetCallsThisFrame <= MaxArmorSetCallsPerFrame;
        }

        private static void TripCircuitBreaker()
        {
            if (_circuitBroken)
                return;

            _circuitBroken = true;
            EmojLog.Warn(EmojLogChannel.SetBonus,
                $"circuit broken after {_armorSetCallsThisFrame} UpdateArmorSets calls in one frame");

            if (_circuitBreakNotifiedFrame == (uint)Main.GameUpdateCount)
                return;

            _circuitBreakNotifiedFrame = (uint)Main.GameUpdateCount;
            if (Main.netMode != NetmodeID.Server)
                Main.NewText(EOPJText.UI("SetBonusCircuitBroken"), Color.OrangeRed);
        }

        private static void ApplyVanillaVirtualSets(Player player, int i, BuffResearchPlayer modPlayer)
        {
            var seenArmor = new HashSet<string>();
            foreach (SetBonusDefinition definition in SupportedSetBonuses)
            {
                if (!modPlayer.HasAnyActiveBuffByName(definition.BuffNames))
                    continue;

                if (!seenArmor.Add(definition.ArmorKey))
                    continue;

                if (!TryConsumeArmorSetCallBudget())
                {
                    TripCircuitBreaker();
                    return;
                }

                if (!TryApplyArmorVisualSlots(player, definition))
                    continue;

                _innerUpdateArmorSets(player, i);
                TryRunAfterApplyOnce(player, definition);
            }
        }

        private static void ApplyModVirtualSets(Player player, BuffResearchPlayer modPlayer)
        {
            var modDefs = new List<SetBonusArmorResolver.ArmorSetBuffDefinition>();
            foreach (SetBonusArmorResolver.ArmorSetBuffDefinition definition in SetBonusArmorResolver.GetDefinitionsForActiveBuffs(modPlayer.ActiveBuffs))
                modDefs.Add(definition);

            if (modDefs.Count == 0)
                return;

            int batch = Math.Min(MaxModSetUpdatesPerFrame, modDefs.Count);
            for (int n = 0; n < batch; n++)
            {
                if (!TryConsumeArmorSetCallBudget())
                {
                    TripCircuitBreaker();
                    return;
                }

                int index = (_modSetRotor + n) % modDefs.Count;
                ApplyModArmorSet(player, modDefs[index]);
            }

            _modSetRotor = (_modSetRotor + batch) % modDefs.Count;
        }

        private static void TryRunAfterApplyOnce(Player player, SetBonusDefinition definition)
        {
            if (definition.AfterApply == null)
                return;

            if (!AfterApplyArmorKeysDone.Add(definition.ArmorKey))
                return;

            definition.AfterApply(player);
        }

        public static bool IsVanillaHardcodedSetBonusBuff(int buffId)
        {
            if (buffId <= 0 || buffId >= BuffLoader.BuffCount)
                return false;

            if (buffId == BuffID.SolarShield1 || buffId == BuffID.SolarShield2 || buffId == BuffID.SolarShield3)
                return true;

            if (buffId == BuffID.BeetleEndurance1 || buffId == BuffID.BeetleEndurance2 || buffId == BuffID.BeetleEndurance3)
                return true;

            if (buffId == BuffID.BeetleMight1 || buffId == BuffID.BeetleMight2 || buffId == BuffID.BeetleMight3)
                return true;

            string buffName = buffId < BuffID.Count ? BuffID.Search.GetName(buffId) : null;
            if (string.IsNullOrEmpty(buffName))
                return false;

            foreach (SetBonusDefinition definition in SupportedSetBonuses)
            {
                foreach (string known in definition.BuffNames)
                {
                    if (string.Equals(buffName, known, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        public static bool IsSupportedSetBonusBuff(int buffId) =>
            IsVanillaHardcodedSetBonusBuff(buffId) || SetBonusArmorResolver.HasDefinition(buffId);

        private static void ApplyModArmorSet(Player player, SetBonusArmorResolver.ArmorSetBuffDefinition definition)
        {
            Item oldHeadItem = player.armor[0].Clone();
            Item oldBodyItem = player.armor[1].Clone();
            Item oldLegItem = player.armor[2].Clone();

            try
            {
                player.armor[0] = CreateItem(definition.HeadItemType);
                player.armor[1] = CreateItem(definition.BodyItemType);
                player.armor[2] = CreateItem(definition.LegItemType);
                player.head = definition.HeadSlot;
                player.body = definition.BodySlot;
                player.legs = definition.LegSlot;

                ItemLoader.UpdateArmorSet(player, player.armor[0], player.armor[1], player.armor[2]);
            }
            finally
            {
                player.armor[0] = oldHeadItem;
                player.armor[1] = oldBodyItem;
                player.armor[2] = oldLegItem;
            }
        }

        private static Item CreateItem(int itemType)
        {
            var item = new Item();
            if (itemType > ItemID.None)
                item.SetDefaults(itemType);

            return item;
        }

        private static bool TryApplyArmorVisualSlots(Player player, SetBonusDefinition definition)
        {
            if (!TryGetEquipSlots(definition.HeadItemName, out int headSlot, out _, out _) ||
                !TryGetEquipSlots(definition.BodyItemName, out _, out int bodySlot, out _) ||
                !TryGetEquipSlots(definition.LegsItemName, out _, out _, out int legSlot))
                return false;

            player.head = headSlot;
            player.body = bodySlot;
            player.legs = legSlot;
            return true;
        }

        private static bool TryGetEquipSlots(string itemName, out int headSlot, out int bodySlot, out int legSlot)
        {
            headSlot = -1;
            bodySlot = -1;
            legSlot = -1;

            int itemId = GetItemId(itemName);
            if (itemId <= ItemID.None)
                return false;

            Item item = new Item();
            item.SetDefaults(itemId);
            headSlot = item.headSlot;
            bodySlot = item.bodySlot;
            legSlot = item.legSlot;
            return headSlot >= 0 || bodySlot >= 0 || legSlot >= 0;
        }

        private static int GetItemId(string name)
        {
            System.Reflection.FieldInfo field = typeof(ItemID).GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field == null)
                return ItemID.None;

            if (field.FieldType == typeof(short))
                return (short)field.GetValue(null);

            if (field.FieldType == typeof(int))
                return (int)field.GetValue(null);

            return ItemID.None;
        }
    }
}
