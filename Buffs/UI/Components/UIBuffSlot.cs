using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
using EvenMoreOverpoweredJourney.Buffs.Systems.Spawning;
using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
using EvenMoreOverpoweredJourney.Buffs.Systems.Display;
using EvenMoreOverpoweredJourney.Core.Logging;
using EvenMoreOverpoweredJourney.Buffs.UI;
using EvenMoreOverpoweredJourney.ItemHub.UI;
using EvenMoreOverpoweredJourney.Research.UI;
using EvenMoreOverpoweredJourney.Shell.UI;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.Buffs.UI.Components
{
    public class UIBuffSlot : UIElement
    {
        public int buffId;

        public UIBuffSlot(int buffId)
        {
            this.buffId = buffId;
            Width.Set(32, 0);
            Height.Set(32, 0);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();

            if (player.DisabledBuffs.Contains(buffId)) return;

            if (player.IsBuffUnlocked(buffId))
            {
                string category = BuffPage.GetBuffCategory(buffId);
                if (player.ActiveBuffs.Contains(buffId))
                {
                    EmojLogDiagnostics.LogBuffToggle("disable", buffId, player);
                    player.ActiveBuffs.Remove(buffId);
                    player.NotifyBuffRuntimeStateChanged();
                    if (BuffPage.IsExclusiveCombatSummonCategory(category))
                        BuffCombatSummonSystem.OnExclusiveBuffDisabled(Main.LocalPlayer, player, category);
                    else if (BuffVirtualEffectSystem.UsesVirtualEffect(buffId, player))
                        player.ApplyMiscEquipBuffsFromUi();
                    else
                        BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, buffId);
                }
                else
                {
                    if (BuffPage.IsExclusiveSingleSelectCategory(category))
                    {
                        var toRemove = new System.Collections.Generic.List<int>();
                        foreach (int id in player.ActiveBuffs)
                        {
                            if (id != buffId && BuffPage.GetBuffCategory(id) == category)
                                toRemove.Add(id);
                        }

                        foreach (int id in toRemove)
                        {
                            player.ActiveBuffs.Remove(id);
                            BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, id);
                        }

                        if (BuffPage.IsExclusiveCombatSummonCategory(category))
                            BuffCombatSummonSystem.ClearCategoryEntities(Main.LocalPlayer, category);
                    }

                    if (BuffEntityIndexSystem.RequiresManualEntityManagement(buffId))
                        return;

                    if (!BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId) &&
                        BuffVirtualEffectSystem.UsesVirtualEffect(buffId, player) &&
                        !BuffVirtualEffectSafety.IsSafeForVirtualApply(buffId) &&
                        !BuffVirtualEffectSafety.IsManualOnlyBulkEnable(buffId))
                        return;

                    if (BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                        SetBonusArmorResolver.TryResolve(buffId, forceRetry: true);

                    player.TryGrantPermanentUnlock(buffId);
                    player.ActiveBuffs.Add(buffId);
                    player.NotifyBuffRuntimeStateChanged();
                    EmojLogDiagnostics.LogBuffToggle("enable", buffId, player);

                    if (BuffPage.IsExclusiveCombatSummonCategory(category))
                        BuffCombatSummonSystem.OnExclusiveBuffEnabled(Main.LocalPlayer, player, buffId, category);
                    else if (BuffVirtualEffectSystem.UsesVirtualEffect(buffId, player))
                    {
                        BuffVirtualEffectSystem.RebuildVirtualQueue(player, force: true);
                        BuffVirtualEffectSystem.ApplyBuffImmediately(Main.LocalPlayer, player, buffId);
                        player.ApplyMiscEquipBuffsFromUi();
                    }
                    else if (BuffResearchPlayer.IsMiscEquipBuff(buffId) ||
                             BuffSourceIndexSystem.IsKnownSetBonusBuff(buffId))
                        player.ApplyMiscEquipBuffsFromUi();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        public override void MiddleMouseDown(UIMouseEvent evt)
        {
            base.MiddleMouseDown(evt);
            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
            if (!player.IsBuffUnlocked(buffId))
                return;

            player.TogglePinnedPhysicalBuff(buffId);
            EmojLog.Info(EmojLogChannel.Buff,
                $"pin toggle buff={buffId} pinned={player.PinnedPhysicalBuffs.Contains(buffId)}");
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void RightMouseDown(UIMouseEvent evt)
        {
            base.RightMouseDown(evt);
            var player = Main.LocalPlayer.GetModPlayer<BuffResearchPlayer>();
            if (!player.IsBuffUnlocked(buffId))
                return;

            if (player.DisabledBuffs.Contains(buffId))
            {
                EmojLog.Info(EmojLogChannel.Buff, $"banlist remove buff={buffId}");
                player.DisabledBuffs.Remove(buffId);
                if (buffId > 0 && buffId < Main.LocalPlayer.buffImmune.Length)
                    Main.LocalPlayer.buffImmune[buffId] = false;
            }
            else
            {
                EmojLog.Info(EmojLogChannel.Buff, $"banlist add buff={buffId}");
                player.DisabledBuffs.Add(buffId);
                player.ActiveBuffs.Remove(buffId);
                player.PinnedPhysicalBuffs.Remove(buffId);
                BuffResearchPlayer.ClearManagedBuff(Main.LocalPlayer, buffId);
                if (buffId > 0 && buffId < Main.LocalPlayer.buffImmune.Length)
                    Main.LocalPlayer.buffImmune[buffId] = true;
            }

            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dims = GetDimensions();
            Player pl = Main.LocalPlayer;
            if (pl == null)
                return;
            var player = pl.GetModPlayer<BuffResearchPlayer>();

            bool unlocked = player.IsBuffUnlocked(buffId);
            bool active = player.ActiveBuffs.Contains(buffId);
            bool disabled = player.DisabledBuffs.Contains(buffId);
            bool pinned = player.PinnedPhysicalBuffs.Contains(buffId);
            bool virtualEligible = unlocked && BuffVirtualEffectSystem.UsesVirtualEffect(buffId, player);

            Texture2D icon = Terraria.GameContent.TextureAssets.Buff[buffId].Value;
            Vector2 pos = dims.Position();

            Color drawColor = unlocked ? Color.White : Color.Black * 0.5f;
            // ���ý�����ʾ��ͼ�걣����������
            spriteBatch.Draw(icon, pos, drawColor);

            if (!unlocked)
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dims.ToRectangle(), Color.Black * 0.6f);
            else if (!active)
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dims.ToRectangle(), Color.Black * 0.3f);

            if (active && unlocked)
                BorderDrawUtil.DrawRectOutline(spriteBatch, dims.ToRectangle(), Color.LimeGreen, 2);

            if (disabled && unlocked)
                BorderDrawUtil.DrawRectOutline(spriteBatch, dims.ToRectangle(), Color.Red, 2);

            if (pinned && unlocked && !disabled)
                BorderDrawUtil.DrawRectOutline(spriteBatch, dims.ToRectangle(), Color.Gold, 2);

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                string name = BuffDisplayNameHelper.GetDisplayName(buffId);
                string desc = BuffDisplayNameHelper.GetDescription(buffId);
                string categoryKey = BuffPage.GetBuffCategory(buffId);
                string categoryText = EOPJText.UI("BuffCat_" + categoryKey);
                string engName = buffId < BuffID.Count ? BuffID.Search.GetName(buffId) : ModContent.GetModBuff(buffId)?.Name ?? "Unknown";

                string leftClickTip = unlocked ? "\n" + EOPJText.UI("BuffHoverLeft") : "";
                string rightClickTip = unlocked ? "\n" + EOPJText.UI("BuffHoverRight") : "";
                string middleClickTip = unlocked ? "\n" + EOPJText.UI("BuffHoverMiddle") : "";
                string disabledStatus = disabled ? "\n" + EOPJText.UI("BuffHoverDisabled") : "";
                string pinnedStatus = pinned ? "\n" + EOPJText.UI("BuffHoverPinned") : "";
                string virtualTip = virtualEligible ? "\n" + EOPJText.UI("BuffHoverVirtual") + "\n" + EOPJText.UI("BuffHoverPinAdvice") : "";
                string toggleTip = leftClickTip + rightClickTip + middleClickTip + disabledStatus + pinnedStatus + virtualTip;
                string modSource = ModContent.GetModBuff(buffId)?.Mod?.Name ?? "Terraria";

                string slotTip = "";
                if (BuffPage.GetBuffCategory(buffId) == BuffCategories.Minion &&
                    BuffCombatSummonSystem.TryGetMinionSlotCost(buffId, out float slotCost))
                {
                    string costText = slotCost.ToString(slotCost % 1f == 0f ? "0" : "0.##");
                    int displayMaxMinions = System.Math.Min(Main.LocalPlayer.maxMinions, BuffVirtualEffectSummonGuard.AbsoluteMaxMinionSlots);
                    slotTip = "\n" + EOPJText.UIFormat("BuffHoverMinionSlots", costText, displayMaxMinions);
                }

                string hoverText = $"{name}  [c/AAAAAA:{categoryText}]  [c/888888:{engName}]\n{desc}{slotTip}{toggleTip}\n\n{EOPJText.UIFormat("BuffHoverSource", modSource)}";
                Main.instance.MouseText(hoverText);
            }
        }
    }
}
