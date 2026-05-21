namespace EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport
{
    /// <summary>EMOJ ��ԭ�� Buff ��֧�ַ�ʽ��ȫ���ɽ�������壻ʵ��·����ͬ����</summary>
    public enum VanillaBuffSupportMode
    {
        /// <summary>����ֱд������ Buff.Update���� VanillaBuffStatRegistry����</summary>
        SyntheticStat,

        /// <summary>������ʵ��������ʳ��ҩˮ�Ӿ���������Ⱦ�ȣ���</summary>
        PhysicalMechanic,

        /// <summary>���棺��ʵ����/�����߼�����������ֱд��</summary>
        DebuffPhysical,

        /// <summary>��ͨ���棺Ĭ����ʵ AddBuff ·����v0.4.60 ��ʵ��λģʽ����</summary>
        StandardPhysical
    }
}
