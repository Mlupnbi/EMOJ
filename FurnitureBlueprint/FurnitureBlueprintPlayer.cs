using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.FurnitureBlueprint.Placement;

namespace EvenMoreOverpoweredJourney.FurnitureBlueprint
{
    public enum FurnitureBlueprintMode : byte
    {
        Auto = 0,
        Manual = 1,
        Free = 2
    }

    public sealed class FurnitureBlueprintPlayer : ModPlayer
    {
        public bool ConsumeMaterialsOnPlace { get; set; } = true;
        public BlueprintPlacementMode PlacementMode { get; set; } = BlueprintPlacementMode.Strict;
        public string ActiveTemplateId { get; set; } = "npc_room_a";
        public string SelectedLibrarySchemeId { get; set; } = "";

        /// <summary>两步识别：家具种子待确认的材料方块 type。</summary>
        public int PendingSeedType { get; set; } = ItemID.None;

        public int PendingMaterialBlock { get; set; } = ItemID.None;

        public bool AwaitingMaterialConfirm { get; set; }

        public int QueuedRecognizeSeed { get; private set; } = ItemID.None;

        public int QueuedRecognizeBlock { get; private set; } = ItemID.None;

        public bool RecognitionBusy { get; private set; }

        public bool SeedProbeBusy { get; private set; }

        public int QueuedSeedProbeType { get; private set; } = ItemID.None;

        public bool NeedsSeedProbeUiApply { get; set; }

        public bool SeedProbeOpenMaterialPicker { get; private set; }

        public IReadOnlyList<int> SeedProbeMaterialCandidates { get; private set; }

        private FurnitureRecognitionJob _activeRecognition;

        public bool NeedsBlueprintUiRefresh { get; set; }

        /// <summary>识别完成且尚未保存为自定义套组时，主窗提示保存。</summary>
        public bool RecognitionAwaitingSave { get; set; }

        /// <summary>最近一次完整识别结果（供详情窗「从识别覆盖」，与当前手改内容分离）。</summary>
        public FurnitureScheme LastRecognitionSnapshot { get; private set; }

        public bool HasRecognitionOverlaySource =>
            LastRecognitionSnapshot != null && CountFilledSlots(LastRecognitionSnapshot) > 0;

        /// <summary>进入世界后下一帧清空家具页 22 槽显示（持久化数据已在 OnWorldLoad 丢弃）。</summary>
        public bool ClearWorkspaceUiOnNextTick { get; set; }

        /// <summary>当前编辑/放置用的混合方案（22 槽，可来自多套方案分部位合并或手拖）。</summary>
        public FurnitureScheme ActiveScheme { get; private set; } = new FurnitureScheme();

        /// <summary>主窗识别区 22 格展示用（点击已保存套组不覆盖）。</summary>
        public FurnitureScheme QueryResultScheme { get; private set; } = new FurnitureScheme();

        /// <summary>界面与幽灵预览用的套组快照（与右侧 22 槽显示一致，不必等于已套用到放置的 ActiveScheme）。</summary>
        public FurnitureScheme PreviewScheme { get; private set; } = new FurnitureScheme();

        /// <summary>自动识别缓存（按种子 type）。</summary>
        public readonly Dictionary<int, FurnitureScheme> AutoSchemesBySeed = new();

        public readonly Dictionary<string, FurnitureScheme> CustomSchemes = new();

        public void QueueRecognition(int seedType, int anchorBlock)
        {
            if (seedType <= ItemID.None || anchorBlock <= ItemID.None)
            {
                CancelQueuedRecognition();
                return;
            }

            QueuedRecognizeSeed = seedType;
            QueuedRecognizeBlock = anchorBlock;
        }

        public void QueueSeedProbe(int seedType)
        {
            if (seedType <= ItemID.None)
            {
                CancelQueuedSeedProbe();
                return;
            }

            QueuedSeedProbeType = seedType;
            PendingSeedType = seedType;
            AwaitingMaterialConfirm = false;
        }

        public void CancelQueuedSeedProbe()
        {
            QueuedSeedProbeType = ItemID.None;
            SeedProbeBusy = false;
            SeedProbeOpenMaterialPicker = false;
            SeedProbeMaterialCandidates = null;
        }

        public void CancelQueuedRecognition()
        {
            QueuedRecognizeSeed = ItemID.None;
            QueuedRecognizeBlock = ItemID.None;
            CancelQueuedSeedProbe();
            _activeRecognition = null;
            RecognitionBusy = false;
        }

        public override void PostUpdate()
        {
            if (_activeRecognition != null)
            {
                RecognitionBusy = true;
                try
                {
                    if (FurnitureRecognitionRunner.Tick(_activeRecognition))
                        CompleteRecognitionJob(_activeRecognition);
                }
                catch (Exception ex)
                {
                    FurnitureBlueprintDiagnostics.LogRecognizeFailure(
                        _activeRecognition.SeedType, _activeRecognition.AnchorBlock, ex, "tick");
                    FurnitureBlueprintDiagnostics.LogRealtimeHint();
                    _activeRecognition = null;
                    RecognitionBusy = false;
                }

                return;
            }

            if (QueuedSeedProbeType > ItemID.None)
            {
                if (!BlueprintSubsystemGuard.CanStartSeedProbe)
                    return;

                int probeSeed = QueuedSeedProbeType;
                QueuedSeedProbeType = ItemID.None;
                SeedProbeBusy = true;
                RecognitionBusy = true;
                try
                {
                    ApplySeedWorkspaceResolution(FurnitureSeedWorkspaceResolver.Resolve(probeSeed));
                    NeedsSeedProbeUiApply = true;
                }
                catch (Exception ex)
                {
                    FurnitureBlueprintLog.Warn($"seed probe failed seed={probeSeed}: {ex.Message}");
                }
                finally
                {
                    SeedProbeBusy = false;
                    RecognitionBusy = false;
                }

                return;
            }

            if (QueuedRecognizeSeed <= ItemID.None || QueuedRecognizeBlock <= ItemID.None)
                return;

            if (!BlueprintSubsystemGuard.CanStartRecognition)
                return;

            int seed = QueuedRecognizeSeed;
            int block = QueuedRecognizeBlock;
            CancelQueuedRecognition();

            RecognitionBusy = true;
            try
            {
                FurnitureBlueprintDiagnostics.LogRecognizePhase("start", seed, block);
                _activeRecognition = FurnitureSetRecognizer.BeginRecognition(seed, block);
                if (_activeRecognition.IsComplete)
                    CompleteRecognitionJob(_activeRecognition);
            }
            catch (Exception ex)
            {
                FurnitureBlueprintDiagnostics.LogRecognizeFailure(seed, block, ex, "start");
                FurnitureBlueprintDiagnostics.LogRealtimeHint();
                RecognitionBusy = false;
            }
        }

        private void ApplySeedWorkspaceResolution(FurnitureSeedWorkspaceResolution resolution)
        {
            if (resolution.SeedType <= ItemID.None)
                return;

            PendingSeedType = resolution.SeedType;
            PendingMaterialBlock = resolution.MaterialBlock;
            SeedProbeMaterialCandidates = resolution.MaterialCandidates;
            SeedProbeOpenMaterialPicker = resolution.OpenMaterialPicker;

            if (resolution.AppliedFromCache && resolution.CachedScheme != null)
            {
                int block = resolution.CachedMaterialBlock > ItemID.None
                    ? resolution.CachedMaterialBlock
                    : resolution.MaterialBlock;
                FurnitureScheme hit = resolution.CachedScheme.Clone();
                hit.SeedType = resolution.SeedType;
                ApplyRecognitionToActive(hit, rememberAsOverlaySource: true);
                SetQueryResultScheme(hit);
                PendingMaterialBlock = block;
                NeedsBlueprintUiRefresh = true;
                FurnitureBlueprintUiBridge.NotifySchemeApplied();
                return;
            }

            if (resolution.MaterialBlock > ItemID.None)
                QueueRecognition(resolution.SeedType, resolution.MaterialBlock);
        }

        private void CompleteRecognitionJob(FurnitureRecognitionJob job)
        {
            if (job == null)
                return;

            var logger = ModContent.GetInstance<EvenMoreOverpoweredJourney>().Logger;
            FurnitureScheme scheme = job.Scheme.Clone();
            scheme.SeedType = job.SeedType;
            ApplyRecognitionToActive(scheme, rememberAsOverlaySource: true);
            SetQueryResultScheme(scheme);
            int resolvedMaterial = job.MaterialBlock > ItemID.None
                ? job.MaterialBlock
                : scheme.AnchorMaterialType;
            PendingMaterialBlock = resolvedMaterial > ItemID.None ? resolvedMaterial : job.AnchorBlock;

            int cacheMaterial = scheme.AnchorMaterialType > ItemID.None ? scheme.AnchorMaterialType : PendingMaterialBlock;
            if (cacheMaterial > ItemID.None
                && FurnitureGenericWoodLineageRules.IsSchemeCacheable(scheme, job.SeedType, cacheMaterial))
                FurnitureSetCacheSystem.RegisterScheme(scheme, job.SeedType, cacheMaterial);

            NeedsBlueprintUiRefresh = true;
            RecognitionAwaitingSave = CountFilledSlots(ActiveScheme) > 0;
            FurnitureBlueprintUiBridge.NotifySchemeApplied();
            logger.Info(
                $"[Blueprint] recognize done seed={job.SeedType} block={PendingMaterialBlock} filled={CountFilledSlots(ActiveScheme)}/{FurnitureWikiSlots.TotalCount}");

            _activeRecognition = null;
            RecognitionBusy = false;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["fb_consume"] = ConsumeMaterialsOnPlace;
            tag["fb_place_mode"] = (byte)PlacementMode;
            tag["fb_template"] = ActiveTemplateId ?? "";
            tag["fb_sel"] = SelectedLibrarySchemeId ?? "";
            tag["fb_active"] = ActiveScheme.ToTag("active");

            var autoList = new List<TagCompound>();
            foreach (var pair in AutoSchemesBySeed)
            {
                var entry = pair.Value.ToTag(FurnitureSchemeLibrary.AutoIdForSeed(pair.Key));
                entry["seedKey"] = pair.Key;
                autoList.Add(entry);
            }
            tag["fb_auto"] = autoList;

            var list = new List<TagCompound>();
            foreach (var pair in CustomSchemes)
                list.Add(pair.Value.ToTag(pair.Key));
            tag["fb_custom"] = list;
        }

        public override void LoadData(TagCompound tag)
        {
            ConsumeMaterialsOnPlace = !tag.ContainsKey("fb_consume") || tag.GetBool("fb_consume");
            PlacementMode = tag.ContainsKey("fb_place_mode")
                && Enum.IsDefined(typeof(BlueprintPlacementMode), tag.GetByte("fb_place_mode"))
                ? (BlueprintPlacementMode)tag.GetByte("fb_place_mode")
                : BlueprintPlacementMode.Strict;
            ActiveTemplateId = tag.GetString("fb_template");
            if (string.IsNullOrEmpty(ActiveTemplateId))
                ActiveTemplateId = "npc_room_a";
            ActiveTemplateId = BuiltinBlueprintTemplates.MigrateLegacyTemplateId(ActiveTemplateId);
            BuiltinBlueprintTemplates.EnsureValidActiveTemplate(this);
            SelectedLibrarySchemeId = tag.GetString("fb_sel") ?? "";

            ActiveScheme = tag.ContainsKey("fb_active")
                ? FurnitureScheme.FromTag(tag.GetCompound("fb_active"))
                : new FurnitureScheme();
            QueryResultScheme = ActiveScheme.Clone();

            AutoSchemesBySeed.Clear();
            if (tag.ContainsKey("fb_auto"))
            {
                foreach (TagCompound entry in tag.GetList<TagCompound>("fb_auto"))
                {
                    int seed = entry.ContainsKey("seedKey") ? entry.GetInt("seedKey") : entry.GetInt("seed");
                    if (seed <= ItemID.None)
                        continue;
                    AutoSchemesBySeed[seed] = FurnitureScheme.FromTag(entry);
                }
            }

            CustomSchemes.Clear();
            if (tag.ContainsKey("fb_custom"))
            {
                foreach (TagCompound entry in tag.GetList<TagCompound>("fb_custom"))
                {
                    string id = entry.GetString("id");
                    if (string.IsNullOrEmpty(id))
                        continue;
                    CustomSchemes[id] = FurnitureScheme.FromTag(entry);
                }
            }
        }

        public void RegisterAutoScheme(FurnitureScheme scheme)
        {
            if (scheme == null || scheme.SeedType <= ItemID.None)
                return;
            AutoSchemesBySeed[scheme.SeedType] = scheme.Clone();
            SelectedLibrarySchemeId = FurnitureSchemeLibrary.AutoIdForSeed(scheme.SeedType);
            FurnitureBlueprintLog.Info(
                $"auto scheme registered seed={scheme.SeedType} name={scheme.DisplayName} filled={CountFilledSlots(scheme)}/{FurnitureWikiSlots.TotalCount}");
        }

        /// <summary>种子变化时仅更新当前工作区，不写入方案库。</summary>
        public void ApplyRecognitionToActive(FurnitureScheme scheme, bool rememberAsOverlaySource = false)
        {
            if (scheme == null)
                return;
            ActiveScheme = scheme.Clone();
            ActiveScheme.IsAutoGenerated = false;
            SyncPreviewFromActive();
            if (rememberAsOverlaySource)
                RememberRecognitionSnapshot(ActiveScheme);
        }

        public void RememberRecognitionSnapshot(FurnitureScheme scheme)
        {
            LastRecognitionSnapshot = scheme?.Clone();
        }

        public void SetQueryResultScheme(FurnitureScheme scheme)
        {
            QueryResultScheme = scheme?.Clone() ?? new FurnitureScheme();
        }

        public void ClearQueryResultScheme()
        {
            QueryResultScheme = new FurnitureScheme();
        }

        /// <summary>用已缓存的识别快照覆盖 ActiveScheme（不触发重新识别）。</summary>
        public bool TryApplyStoredRecognitionOverlay()
        {
            if (!HasRecognitionOverlaySource)
                return false;

            ApplyRecognitionToActive(LastRecognitionSnapshot.Clone());
            NeedsBlueprintUiRefresh = true;
            return true;
        }

        public void ApplyEntireScheme(FurnitureScheme scheme)
        {
            if (scheme == null)
                return;
            ActiveScheme = scheme.Clone();
            ActiveScheme.IsAutoGenerated = false;
            RecognitionAwaitingSave = false;
            SetQueryResultScheme(ActiveScheme);
            SyncPreviewFromActive();
            FurnitureBlueprintLog.Info($"apply entire scheme name={ActiveScheme.DisplayName} filled={CountFilledSlots(ActiveScheme)}");
        }

        public void ApplyEntireSchemeById(string libraryId)
        {
            if (!FurnitureSchemeLibrary.TryGetScheme(this, libraryId, out FurnitureScheme scheme))
            {
                FurnitureBlueprintLog.Warn($"apply entire failed unknown id={libraryId}");
                return;
            }
            SelectedLibrarySchemeId = libraryId;
            ApplyEntireScheme(scheme);
        }

        public void SaveCustomScheme(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
                id = System.Guid.NewGuid().ToString("N");
            FurnitureScheme copy = ActiveScheme.Clone();
            copy.DisplayName = displayName ?? copy.DisplayName;
            copy.IsAutoGenerated = false;
            int cover = copy.ResolveCoverItemType();
            if (cover > ItemID.None)
                copy.IconItemType = cover;
            if (copy.AnchorMaterialType <= ItemID.None && PendingMaterialBlock > ItemID.None)
                copy.AnchorMaterialType = PendingMaterialBlock;
            CustomSchemes[id] = copy;
            SelectedLibrarySchemeId = id;
            RecognitionAwaitingSave = false;
            int filled = 0;
            foreach (FurnitureSlotKind kind in FurnitureWikiSlots.RecognitionOrder)
            {
                if (copy.GetSlot(kind) > ItemID.None)
                    filled++;
            }

            FurnitureBlueprintLog.Info(
                $"custom scheme saved id={id} name={copy.DisplayName} seed={copy.SeedType} material={copy.AnchorMaterialType} filled={filled}/22");
        }

        public bool DeleteCustomScheme(string id)
        {
            if (string.IsNullOrEmpty(id) || !CustomSchemes.Remove(id))
                return false;
            if (SelectedLibrarySchemeId == id)
                SelectedLibrarySchemeId = "";
            RecognitionAwaitingSave = false;
            FurnitureBlueprintLog.Info($"custom scheme deleted id={id}");
            return true;
        }

        public bool RenameCustomScheme(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id) || !CustomSchemes.TryGetValue(id, out FurnitureScheme scheme))
                return false;
            scheme.DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? scheme.DisplayName
                : displayName.Trim();
            FurnitureBlueprintLog.Info($"custom scheme renamed id={id} name={scheme.DisplayName}");
            return true;
        }

        /// <summary>仅内存草稿，写入库需经详情窗保存。</summary>
        public FurnitureScheme CreateEmptySchemeDraft()
        {
            string name = FurnitureSchemeNaming.AllocateUniqueDisplayName(this);
            var scheme = new FurnitureScheme
            {
                DisplayName = name,
                IsAutoGenerated = false
            };
            int cover = scheme.ResolveCoverItemType();
            if (cover > ItemID.None)
                scheme.IconItemType = cover;

            SelectedLibrarySchemeId = "";
            RecognitionAwaitingSave = false;
            ApplyEntireScheme(scheme);
            FurnitureBlueprintLog.Info($"empty scheme draft started name={name}");
            return scheme;
        }

        public void LoadCustomScheme(string id)
        {
            if (CustomSchemes.TryGetValue(id, out FurnitureScheme scheme))
            {
                SelectedLibrarySchemeId = id;
                ApplyEntireScheme(scheme);
            }
        }

        public void ApplyTemplateDefaults(BlueprintLayout template)
        {
            if (template == null)
                return;
            ActiveTemplateId = template.Id;
            FurnitureBlueprintLog.Info($"template selected id={template.Id}");
            if (!Main.gameMenu && !Main.dedServ && ActiveScheme != null)
                BlueprintLayoutPreviewCache.RequestRebuild(template, ActiveScheme);
        }

        public void SyncPreviewScheme(FurnitureScheme fromUiSlots)
        {
            PreviewScheme = fromUiSlots?.Clone() ?? new FurnitureScheme();
        }

        /// <summary>放置器与幽灵预览均与 ActiveScheme 一致；预览贴图由 PostUpdateWorld 在空闲帧重建。</summary>
        public void SyncPreviewFromActive()
        {
            SyncPreviewScheme(ActiveScheme);
            if (Main.gameMenu || Main.dedServ)
                return;

            BlueprintLayout layout = BuiltinBlueprintTemplates.ResolveActiveLayout(this);
            if (layout != null && ActiveScheme != null)
                BlueprintLayoutPreviewCache.RequestRebuild(layout, ActiveScheme);
        }

        public void ClearWorkspaceForWorldEntry()
        {
            ActiveScheme = new FurnitureScheme();
            QueryResultScheme = new FurnitureScheme();
            PreviewScheme = new FurnitureScheme();
            LastRecognitionSnapshot = null;
            AutoSchemesBySeed.Clear();
            PendingSeedType = ItemID.None;
            PendingMaterialBlock = ItemID.None;
            AwaitingMaterialConfirm = false;
            SelectedLibrarySchemeId = "";
            CancelQueuedRecognition();
            _activeRecognition = null;
            RecognitionBusy = false;
            NeedsBlueprintUiRefresh = true;
            ClearWorkspaceUiOnNextTick = true;
        }

        private static int CountFilledSlots(FurnitureScheme scheme)
        {
            int n = 0;
            for (int i = 0; i < FurnitureSlotKinds.Count; i++)
            {
                if (scheme.SlotItemTypes[i] > ItemID.None)
                    n++;
            }
            return n;
        }
    }
}
