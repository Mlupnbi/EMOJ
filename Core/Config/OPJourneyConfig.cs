using System.ComponentModel;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using EvenMoreOverpoweredJourney.Research.Crafting;

namespace EvenMoreOverpoweredJourney.Core.Config
{
    public class OPJourneyConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(LosslessGiveAmountKind.MaxStack)]
        [DrawTicks]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.PurpleLosslessGiveAmount.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.PurpleLosslessGiveAmount.Tooltip")]
        public LosslessGiveAmountKind PurpleLosslessGiveAmount { get; set; }

        [DefaultValue(ItemHubUnlockRequirementKind.JourneyHalf)]
        [DrawTicks]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ItemHubUnlockRequirement.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ItemHubUnlockRequirement.Tooltip")]
        public ItemHubUnlockRequirementKind ItemHubUnlockRequirement { get; set; }

        /// <summary>�״ΰ�װĬ��Ϊ <see cref="ModLogModeKind.Off"/>��</summary>
        [DefaultValue(ModLogModeKind.Off)]
        [DrawTicks]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ModLogMode.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ModLogMode.Tooltip")]
        public ModLogModeKind ModLogMode { get; set; } = ModLogModeKind.Off;

        /// <summary>������Ч��ʽ��Ĭ�� BuffsPlus ��ʵ��λ�������������� scratch Ϊȫ�������Ե��������ܡ�</summary>
        [DefaultValue(VirtualBuffApplyModeKind.BuffsPlusRealBar)]
        [DrawTicks]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.VirtualBuffApplyMode.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.VirtualBuffApplyMode.Tooltip")]
        public VirtualBuffApplyModeKind VirtualBuffApplyMode { get; set; } = VirtualBuffApplyModeKind.BuffsPlusRealBar;

        /// <summary>ƽ��ģʽ�£�ս��/��Ч��ÿ�����֡����ʩ��һ�֣�?2�C6����</summary>
        [DefaultValue(3)]
        [Range(2, 6)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.CombatVisualUpdateInterval.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.CombatVisualUpdateInterval.Tooltip")]
        public int CombatVisualUpdateInterval { get; set; } = 3;

        /// <summary>ƽ��ģʽ�£����Զӷּ�֡��ѯ��һ�֣�1=ÿ֡ȫ�㣬3��ʡ 2/3 CPU����</summary>
        [DefaultValue(3)]
        [Range(1, 6)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.StatUpdateSpreadFrames.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.StatUpdateSpreadFrames.Tooltip")]
        public int StatUpdateSpreadFrames { get; set; } = 3;

        [Header("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.BuffInfrastructureHeader")]
        /// <summary>���� Buff ����0�C99�����Ѽ��� ImproveGame ���䡸���� BUFF ����&gt;0 ʱ������Ч���� ImproveGame Ϊ׼��</summary>
        [DefaultValue(0)]
        [Range(0, 99)]
        [Increment(11)]
        [ReloadRequired]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ExtraPlayerBuffSlots.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ExtraPlayerBuffSlots.Tooltip")]
        public int ExtraPlayerBuffSlots { get; set; }

        /// <summary>����/�����������¹����о��б��е� Buff��ImproveGame �ѿ��������������桹ʱ�ɶԷ�����λ����������Ի��ڸ�����ع��й��б��?</summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.PreserveBuffsOnDeath.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.PreserveBuffsOnDeath.Tooltip")]
        public bool PreserveBuffsOnDeath { get; set; } = true;

        /// <summary>��������ʱ���о��б����¹����й� Buff��</summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.PreserveBuffsOnWorldEnter.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.PreserveBuffsOnWorldEnter.Tooltip")]
        public bool PreserveBuffsOnWorldEnter { get; set; } = true;

        /// <summary>���й��б��е�ԭ�泣������ҩˮʹ��ֱд��ֵ������ Buff.Update���� VanillaBuffStatRegistry����</summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.UseVanillaSyntheticStats.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.UseVanillaSyntheticStats.Tooltip")]
        public bool EnableVanillaSyntheticStats { get; set; } = true;

        [Header("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ResearchHeader")]

        /// <summary>????????????????????????????????????????????????</summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.GreenFaceExtendedNestedSearch.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.GreenFaceExtendedNestedSearch.Tooltip")]
        public bool GreenFaceExtendedNestedSearch { get; set; } = true;

        [Header("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.BestiaryHeader")]

        /// <summary>
        /// ???????????????????????????
        /// ImproveGame ????????????????????????????????
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.BestiaryUseVanillaKillCountForProgressiveDisclosure.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.BestiaryUseVanillaKillCountForProgressiveDisclosure.Tooltip")]
        public bool BestiaryUseVanillaKillCountForProgressiveDisclosure { get; set; } = true;

        /// <summary>��ȫ�������ʱ��������? <see cref="BuffBulkSkipDiagnostics.SkipReason.UnsafeVirtual"/> �����δ����/���ֶ��ȹ���Լ������</summary>
        [DefaultValue(false)]
        [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ForceBulkEnableUnsafeVirtual.Label")]
        [TooltipKey("$Mods.EvenMoreOverpoweredJourney.Configs.OPJourneyConfig.ForceBulkEnableUnsafeVirtual.Tooltip")]
        public bool ForceBulkEnableUnsafeVirtual { get; set; }

        public enum VirtualBuffApplyModeKind
        {
            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.VirtualBuffApplyModeKind.BuffsPlusRealBar.Label")]
            BuffsPlusRealBar,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.VirtualBuffApplyModeKind.BalancedVirtualScratch.Label")]
            BalancedVirtualScratch,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.VirtualBuffApplyModeKind.UnifiedVirtualEveryFrame.Label")]
            UnifiedVirtualEveryFrame
        }

        public enum ModLogModeKind
        {
            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ModLogModeKind.Off.Label")]
            Off,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ModLogModeKind.Simplified.Label")]
            Simplified,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ModLogModeKind.Full.Label")]
            Full
        }

        public enum LosslessGiveAmountKind
        {
            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.LosslessGiveAmountKind.Five.Label")]
            Five = 5,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.LosslessGiveAmountKind.Ten.Label")]
            Ten = 10,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.LosslessGiveAmountKind.Fifty.Label")]
            Fifty = 50,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.LosslessGiveAmountKind.MaxStack.Label")]
            MaxStack = 0
        }

        /// <summary>��Ʒ���ࣺ����;�������о������ж�������ȡ�����ż�������;�԰�����/��귢�֣���?</summary>
        public enum ItemHubUnlockRequirementKind
        {
            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ItemHubUnlockRequirementKind.Once.Label")]
            Once = 0,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ItemHubUnlockRequirementKind.Five.Label")]
            Five = 1,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ItemHubUnlockRequirementKind.JourneyHalf.Label")]
            JourneyHalf = 2,

            [LabelKey("$Mods.EvenMoreOverpoweredJourney.Configs.ItemHubUnlockRequirementKind.JourneyFull.Label")]
            JourneyFull = 3
        }

        public override void OnChanged()
        {
            ImproveGameIntegration.Refresh();
            EmojLog.RefreshFromConfig();
            RecipeBrowserNestedCraft.InvalidateCaches();

            if (Main.LocalPlayer?.active == true)
                Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>()?.NotifyBuffRuntimeStateChanged();
        }

        public static bool UseGreenFaceExtendedNestedSearch() =>
            ModContent.GetInstance<OPJourneyConfig>().GreenFaceExtendedNestedSearch;

        /// <summary>�Ƿ��� PreUpdate �� scratch ���� ModBuff.Update������/��˸���򣩡�</summary>
        public static bool UseVirtualScratchApply()
        {
            var mode = ModContent.GetInstance<OPJourneyConfig>().VirtualBuffApplyMode;
            return mode == VirtualBuffApplyModeKind.BalancedVirtualScratch ||
                   mode == VirtualBuffApplyModeKind.UnifiedVirtualEveryFrame;
        }

        public static bool UseBalancedVirtualQueues() =>
            ModContent.GetInstance<OPJourneyConfig>().VirtualBuffApplyMode == VirtualBuffApplyModeKind.BalancedVirtualScratch;

        public static bool UseVanillaSyntheticStats() =>
            ModContent.GetInstance<OPJourneyConfig>().EnableVanillaSyntheticStats;

        public static bool AllowBulkEnableUnsafeVirtual() =>
            ModContent.GetInstance<OPJourneyConfig>().ForceBulkEnableUnsafeVirtual;

        public static int GetCombatVisualIntervalFrames()
        {
            int interval = ModContent.GetInstance<OPJourneyConfig>().CombatVisualUpdateInterval;
            return interval < 2 ? 2 : interval > 6 ? 6 : interval;
        }

        public static int GetStatUpdateSpreadFrames()
        {
            if (!UseBalancedVirtualQueues())
                return 1;

            int spread = ModContent.GetInstance<OPJourneyConfig>().StatUpdateSpreadFrames;
            return spread < 1 ? 1 : spread > 6 ? 6 : spread;
        }

        public static int GetPurpleGiveCount(int itemType)
        {
            var c = ModContent.GetInstance<OPJourneyConfig>();
            Item probe = new Item();
            probe.SetDefaults(itemType);
            if (c.PurpleLosslessGiveAmount == LosslessGiveAmountKind.MaxStack || probe.maxStack <= 1)
                return probe.maxStack <= 1 ? 1 : probe.maxStack;
            return (int)c.PurpleLosslessGiveAmount;
        }
    }
}
