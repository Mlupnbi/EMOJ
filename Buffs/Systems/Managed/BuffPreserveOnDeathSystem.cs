using System;
using System.Reflection;
using EvenMoreOverpoweredJourney.Integration.ImproveGame;
using EvenMoreOverpoweredJourney.Integration.Session;
using EvenMoreOverpoweredJourney.Integration.Browser;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Managed
{
    /// <summary>δ��װ ImproveGame�������������桹ʱ����ѡ�������������б���� debuff ��λ Buff������ ImproveGame �߼�����</summary>
    public sealed class BuffPreserveOnDeathSystem : ModSystem
    {
        public override void Load()
        {
            IL_Player.UpdateDead += KeepBuffOnUpdateDead;
        }

        public override void Unload()
        {
            IL_Player.UpdateDead -= KeepBuffOnUpdateDead;
        }

        private static void KeepBuffOnUpdateDead(ILContext il)
        {
            if (!BuffInfrastructureSettings.UseOwnDeathBuffPreserve())
                return;

            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                    MoveType.After,
                    i => i.MatchLdsfld<Main>(nameof(Main.persistentBuff)),
                    i => i.Match(OpCodes.Ldarg_0),
                    i => i.MatchLdfld<Player>(nameof(Player.buffType)),
                    i => i.Match(OpCodes.Ldloc_2),
                    i => i.Match(OpCodes.Ldelem_I4),
                    i => i.Match(OpCodes.Ldelem_U1)))
                return;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld,
                typeof(Player).GetField(nameof(Player.buffType),
                    BindingFlags.Instance | BindingFlags.Public));
            c.Emit(OpCodes.Ldloc_2);
            c.Emit(OpCodes.Ldelem_I4);
            c.EmitDelegate<Func<bool, int, bool>>((returnValue, buffType) =>
            {
                if (buffType <= 0)
                    return returnValue;

                return !Main.debuff[buffType] && !Main.buffNoSave[buffType] && !Main.lightPet[buffType] &&
                       !Main.vanityPet[buffType];
            });
        }
    }
}
