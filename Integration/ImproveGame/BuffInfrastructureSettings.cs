using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Integration.ImproveGame
{
    /// <summary>��Ч Buff ������ʩ������ ImproveGame������ʹ�� EMOJ ���á�</summary>
    public static class BuffInfrastructureSettings
    {
        public static void RefreshExternal() => ImproveGameIntegration.Refresh();

        /// <summary>��ģ��Ӧ���׵Ķ��� Buff ��λ����0 ��ʾ��ȫ���� ImproveGame �򲻿�����</summary>
        public static int GetOwnExtraBuffSlotsContribution()
        {
            RefreshExternal();
            if (ImproveGameIntegration.DelegatesExtraBuffSlots)
                return 0;

            int own = ModContent.GetInstance<OPJourneyConfig>().ExtraPlayerBuffSlots;
            return own < 0 ? 0 : own > 99 ? 99 : own;
        }

        /// <summary>�Ƿ�װ EMOJ �Դ��ġ��������� Buff��IL��ImproveGame �ѹ�ʱ��װ����</summary>
        public static bool UseOwnDeathBuffPreserve()
        {
            RefreshExternal();
            if (ImproveGameIntegration.DelegatesDeathBuffPreserve)
                return false;

            return ModContent.GetInstance<OPJourneyConfig>().PreserveBuffsOnDeath;
        }

        /// <summary>����������Ƿ��о��б����¹����й� Buff��</summary>
        public static bool ShouldReapplyBuffsOnRespawn()
        {
            RefreshExternal();
            if (ImproveGameIntegration.DelegatesDeathBuffPreserve)
                return true;

            return ModContent.GetInstance<OPJourneyConfig>().PreserveBuffsOnDeath;
        }

        /// <summary>��������ʱ�Ƿ񴥷��й� Buff �عң�ImproveGame �޴��ʼ�ն� EMOJ ���ã���</summary>
        public static bool ShouldReapplyBuffsOnWorldEnter() =>
            ModContent.GetInstance<OPJourneyConfig>().PreserveBuffsOnWorldEnter;
    }
}
