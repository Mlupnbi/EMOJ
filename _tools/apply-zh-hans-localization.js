/**
 * Apply zh-Hans translations to hjson (ASCII source only; output UTF-8).
 * Run: node _tools/apply-zh-hans-localization.js
 */
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..", "Localization");
const zhPath = path.join(root, "zh-Hans_Mods.EvenMoreOverpoweredJourney.hjson");

/** key -> zh value (without outer quotes; script adds quoting when needed) */
const T = {
  // --- Configs ---
  "PurpleLosslessGiveAmount.Label": "\u8d85\u7ea7\u5236\u9020\u7814\u7a76 - \u65e0\u635f\u5408\u6210\u6b21\u6570",
  "PurpleLosslessGiveAmount.Tooltip":
    "\u975e\u65c5\u9014\u6a21\u5f0f\u4e0b\u7d2b\u8138\u65e0\u635f\u5408\u6210\u6bcf\u6b21\u8d60\u4e88\u7684\u7269\u54c1\u6570\u91cf\u3002",
  "MaxStack.Label": "\u6700\u5927\u5806\u53e0",
  "ItemHubUnlockRequirement.Label": "\u8d85\u7ea7\u7269\u54c1\u7814\u7a76 - \u89e3\u9501\u6761\u4ef6",
  "ItemHubUnlockRequirement.Tooltip":
    "\u65c5\u9014\u6a21\u5f0f\u4e2d\u9700\u591a\u5c11\u6b21\u83b7\u5f97/\u7814\u7a76\u8fdb\u5ea6\u624d\u80fd\u5728\u4e2d\u67a2\u9886\u53d6\u3002\u975e\u5806\u53e0\u88c5\u5907\u8ba1\u4e3a\u83b7\u5f971\u6b21\u3002\u300c5\u6b21\u300d\u9009\u9879\u5c06\u5728\u540e\u7eed\u901a\u8fc7\u94a9\u5b50\u6309\u5806\u53e0\u8ba1\u6570\u3002",
  "ModLogMode.Label": "EMOJ \u65e5\u5fd7\u6a21\u5f0f",
  "ModLogMode.Tooltip":
    "\u9ed8\u8ba4\u5173\u95ed\u3002\u65e5\u5fd7\u5199\u5165 Documents/.../tModLoader/Logs/EMOJ/\u65f6\u95f4\u6233/\n\u7b80\u5316\uff1a\u5355\u4e00 simple.log\uff0c\u5f00\u9500\u4f4e\uff0c\u53ef\u5e38\u5f00\n\u5b8c\u6574\uff1asimple.log + \u5206\u9891\u9053\u65e5\u5fd7 + manifest-full.txt\uff0c\u7528\u4e8e\u6df1\u5ea6\u6392\u9519",
  "Off.Label": "\u5173",
  "Simplified.Label": "\u7b80\u5316\uff08\u53ef\u5e38\u5f00\uff09",
  "Full.Label": "\u5b8c\u6574\uff08\u6392\u9519\u7528\uff09",
  "VirtualBuffApplyMode.Label": "\u865a\u62df\u589e\u76ca\u5e94\u7528\u6a21\u5f0f",
  "VirtualBuffApplyMode.Tooltip":
    "\u5e73\u8861\uff08\u63a8\u8350\uff09\uff1a\u5c5e\u6027\u589e\u76ca\u6bcf\u5e27\u5237\u65b0\uff0c\u6218\u6597/\u89c6\u89c9\u589e\u76ca\u6309\u95f4\u9694\u5237\u65b0\n\u6bcf\u5e27\u7edf\u4e00\uff1a\u65e7\u884c\u4e3a\uff0c\u6240\u6709\u865a\u62df\u589e\u76ca\u6bcf\u5e27\u5237\u65b0\uff0c\u8d1f\u8f7d\u6700\u9ad8\n\u66f4\u6539\u540e\u8bf7\u91cd\u8fdb\u4e16\u754c\u6216\u91cd\u65b0\u5207\u6362\u589e\u76ca",
  "BuffsPlusRealBar.Label": "\u771f\u5b9e\u589e\u76ca\u680f\uff08\u6d41\u7545\uff0c\u63a8\u8350\uff09",
  "BalancedVirtualScratch.Label": "\u865a\u62df\u7f13\u51b2 - \u5e73\u8861\uff08\u5168\u5c5e\u6027\uff0c\u8f83\u5361\uff09",
  "UnifiedVirtualEveryFrame.Label": "\u865a\u62df\u7f13\u51b2 - \u6bcf\u5e27\uff08\u6700\u5361\uff09",
  "CombatVisualUpdateInterval.Label": "\u89c6\u89c9\u6548\u679c\u95f4\u9694\uff08\u5e27\uff09\uff08\u4ec5\u5e73\u8861\u6a21\u5f0f\uff09",
  "CombatVisualUpdateInterval.Tooltip":
    "\u4ec5\u5f71\u54cd\u6218\u6597/\u89c6\u89c9\u961f\u5217\u3002\u5927\u591a\u6570\u5361\u987f\u6765\u81ea\u5c5e\u6027\u961f\u5217\uff0c\u8bf7\u4f18\u5148\u8c03\u4e0b\u65b9\u5c5e\u6027\u5206\u6563\u3002",
  "StatUpdateSpreadFrames.Label": "\u5c5e\u6027\u961f\u5217\u5206\u6563\uff08\u5e27\uff09\uff08\u4ec5\u5e73\u8861\u6a21\u5f0f\uff09",
  "StatUpdateSpreadFrames.Tooltip":
    "\u591a\u5c11\u5e27\u5185\u8f6e\u6362\u6240\u6709\u5c5e\u6027\u589e\u76ca\uff081=\u6bcf\u5e27\uff0c3\u2248\u51cf\u5c11 2/3 \u5c5e\u6027\u8d1f\u8f7d\uff09\u3002\u8fd9\u662f\u5e73\u8861\u6a21\u5f0f\u4e3b\u8981\u5e27\u7387\u63d0\u5347\u624b\u6bb5\u3002",
  "BuffInfrastructureHeader": "\u589e\u76ca\u680f\u4f4d\u4e0e\u6301\u4e45\u5316\uff08\u8de8\u6a21\u7ec4\uff09",
  "ExtraPlayerBuffSlots.Label": "\u989d\u5916\u589e\u76ca\u680f\u4f4d",
  "ExtraPlayerBuffSlots.Tooltip":
    "0 = \u65e0\u989d\u5916\u4f4d\uff08\u539f\u7248\u7ea6 22\uff09\n\u82e5\u5df2\u52a0\u8f7d ImproveGame \u4e14\u5176\u300c\u989d\u5916 BUFF \u680f\u4f4d\u300d> 0\uff0c\u672c\u9879\u88ab\u5ffd\u7565\uff08\u5e38\u4e3a 99\uff1b\u6539\u540e\u8bf7\u91cd\u8fdb\u4e16\u754c\uff09\n\u5426\u5219\u7531 EMOJ \u5728\u6b64\u63d0\u4f9b\u989d\u5916\u680f\u4f4d",
  "PreserveBuffsOnDeath.Label": "\u6b7b\u4ea1/\u91cd\u751f\u4fdd\u7559\u589e\u76ca",
  "PreserveBuffsOnDeath.Tooltip":
    "\u5f00\uff1a\u5c3d\u53ef\u80fd\u4fdd\u7559\u975e\u51cf\u76ca\u680f\u589e\u76ca\uff0c\u91cd\u751f\u540e\u91cd\u65b0\u5e94\u7528\u7ba1\u7406\u5217\u8868\n\u82e5 ImproveGame \u5f00\u542f\u6b7b\u4ea1\u4fdd\u7559\uff0c\u6e05\u9664\u7531\u5176\u5904\u7406\uff1bEMOJ \u4ecd\u5728\u91cd\u751f\u65f6\u91cd\u65b0\u5e94\u7528\u7ba1\u7406\u5217\u8868",
  "PreserveBuffsOnWorldEnter.Label": "\u8fdb\u4e16\u754c\u65f6\u91cd\u65b0\u5e94\u7528\u7ba1\u7406\u589e\u76ca",
  "PreserveBuffsOnWorldEnter.Tooltip":
    "\u5f00\uff1a\u6bcf\u6b21\u8fdb\u4e16\u754c\u91cd\u65b0\u5e94\u7528\u8d85\u7ea7\u589e\u76ca\u9762\u677f\u4e2d\u52fe\u9009\u7684\u589e\u76ca\n\u5173\uff1a\u8fdb\u4e16\u754c\u4e0d\u81ea\u52a8\u5e94\u7528\uff08\u52fe\u9009\u4ecd\u4f1a\u4fdd\u5b58\uff0c\u9700\u624b\u52a8\u5207\u6362\uff09\n\u4e0d\u53d7 ImproveGame \u63a7\u5236",
  "UseVanillaSyntheticStats.Label": "\u539f\u7248\u5c5e\u6027\u836f\u6c34\uff08\u76f4\u63a5\u5e94\u7528\uff09",
  "UseVanillaSyntheticStats.Tooltip":
    "\u5bf9 VanillaBuffStatRegistry \u4e2d\u5217\u51fa\u7684\u5e38\u89c1\u589e\u76ca\uff08\u94c1\u76ae\u3001\u56de\u8840\u3001\u8fc5\u6377\u7b49\uff09\uff1a\u4e0d\u8d70 Buff.Update \u76f4\u63a5\u52a0\u5c5e\u6027\uff0c\u8282\u7701 CPU\n\u89c1 Data/BuffModSupport/vanilla_stats_manifest.json",
  "BestiaryHeader": "\u8d85\u7ea7\u751f\u7269\u56fe\u9274",
  "BestiaryUseVanillaKillCountForProgressiveDisclosure.Label":
    "\u6e10\u8fdb\u89e3\u9501\u4f7f\u7528\u539f\u7248\u51fb\u6740\u8ba1\u6570",
  "BestiaryUseVanillaKillCountForProgressiveDisclosure.Tooltip":
    "\u5f71\u54cd\u56fe\u9274\u8868\u60c5 2\u20134 \u7684\u8be6\u60c5\u5f00\u653e\u3002\n\u5f00\uff1a\u4e0e\u539f\u7248\u56fe\u9274\u51fb\u6740\u8981\u6c42\u4e00\u81f4\u3002\n\u5173\uff1a\u4ec5\u300c\u5df2\u89c1/\u672a\u89c1\u300d\uff0c\u65e0\u51fb\u6740\u9636\u6bb5\u3002\n\u82e5 ImproveGame \u5f00\u542f\u56fe\u9274\u5feb\u901f\u89e3\u9501\uff0c\u51fb\u67401\u6b21\u89c6\u4e3a\u5b8c\u5168\u89e3\u9501\uff08\u8986\u76d6\u672c\u9879\uff09\u3002",
  "ForceBulkEnableUnsafeVirtual.Label": "\u6279\u91cf\u542f\u7528\u5305\u542b\u4e0d\u5b89\u5168\u865a\u62df\u589e\u76ca",
  "ForceBulkEnableUnsafeVirtual.Tooltip":
    "\u5173\uff08\u9ed8\u8ba4\uff09\uff1a\u300c\u5168\u90e8\u542f\u7528\u300d\u8df3\u8fc7\u5730\u72f1\u3001\u4e2d\u6bd2\u7b49\u6807\u8bb0\u4e3a\u865a\u62df\u4e0d\u5b89\u5168\u7684\u589e\u76ca\uff0c\u9700\u624b\u52a8\u5207\u6362\u3002\n\u5f00\uff1a\u4e5f\u53ef\u6279\u91cf\u542f\u7528\uff08\u4ecd\u53d7\u9501\u5b9a/\u624b\u52a8\u5b9e\u4f53\u89c4\u5219\u7ea6\u675f\uff09\u3002",
  "ItemHubUnlockRequirementKind.Once.Label": "\u83b7\u5f97\u4e00\u6b21",
  "ItemHubUnlockRequirementKind.Five.Label": "\u83b7\u5f97\u4e94\u6b21\uff08\u53ef\u5806\u53e0\uff09",
  "ItemHubUnlockRequirementKind.JourneyHalf.Label": "\u65c5\u9014\u7814\u7a76\u8fbe\u534a\u6570",
  "ItemHubUnlockRequirementKind.JourneyFull.Label": "\u65c5\u9014\u7814\u7a76\u5b8c\u6210",
  "ModLogModeKind.Off.Label": "\u5173",
  "ModLogModeKind.Simplified.Label": "\u7b80\u5316\uff08\u53ef\u5e38\u5f00\uff09",
  "ModLogModeKind.Full.Label": "\u5b8c\u6574\uff08\u6392\u9519\u7528\uff09",
  "ModLogMode.Off.Label": "\u5173",
  "ModLogMode.Simplified.Label": "\u7b80\u5316\uff08\u53ef\u5e38\u5f00\uff09",
  "ModLogMode.Full.Label": "\u5b8c\u6574\uff08\u6392\u9519\u7528\uff09",
  "BuffsPlusRealBar.Label": "\u6d41\u7545\uff08\u63a8\u8350\uff09",
  "BalancedVirtualScratch.Label": "\u5b9e\u9a8c - \u6279\u5904\u7406\uff08\u6613\u5361\uff09",
  "UnifiedVirtualEveryFrame.Label": "\u5b9e\u9a8c - \u6bcf\u5e27\uff08\u6700\u5361\uff09",
  "OPJourneyConfig.DisplayName": "\u6e38\u620f\u8bbe\u7f6e",
  DisplayName: "\u66f4\u8d85\u6a21\u7684\u65c5\u9014",

  "OpenResearchPanel.DisplayName": "\u6253\u5f00\u8d85\u7ea7\u5236\u9020\u7814\u7a76\u9762\u677f",
  "OpenBuffPanel.DisplayName": "\u6253\u5f00\u8d85\u7ea7\u589e\u76ca\u9762\u677f",
  "OpenItemHubPanel.DisplayName": "\u6253\u5f00\u8d85\u7ea7\u7269\u54c1\u7814\u7a76\u9762\u677f",
  "OpenBestiaryPanel.DisplayName": "\u6253\u5f00\u8d85\u7ea7\u751f\u7269\u56fe\u9274",
  "QuickItemQuery.DisplayName": "\u7269\u54c1\u5feb\u901f\u67e5\u8be2",

  ListJoiner: "\u3001",
  EnvRequiredLabel: "\u6240\u9700\u73af\u5883\uff1a",
  TabResearch: "\u7814\u7a76",
  TabBuff: "\u589e\u76ca",
  TabStorage: "\u8d85\u7ea7\u7269\u54c1\u4e2d\u67a2",
  TabBestiary: "\u8d85\u7ea7\u751f\u7269\u56fe\u9274",
  TabHoverResearch: "\u8d85\u7ea7\u5236\u9020\u7814\u7a76",
  TabHoverBuff: "\u8d85\u7ea7\u589e\u76ca",
  TabHoverStorage: "\u8d85\u7ea7\u7269\u54c1\u7814\u7a76",
  TabHoverBestiary: "\u8d85\u7ea7\u751f\u7269\u56fe\u9274",
  SectionQuery: "\u67e5\u8be2\u7269\u54c1",
  SectionProducts: "\u4ea7\u7269\u5217\u8868",
  SectionRecipes: "\u5408\u6210\u8def\u5f84",
  DragItemHint: "\u62d6\u5165\u7269\u54c1\u67e5\u8be2",
  ViewCard: "\u5361\u7247",
  ViewList: "\u5217\u8868",
  ResearchAll: "\u4e00\u952e\u7814\u7a76",
  GiveAll: "\u83b7\u53d6\u5168\u90e8",
  ResearchDoneLine: "\u672c\u6b21\u5df2\u7814\u7a76\uff0c{0}",
  BestiaryViewCard: "\u5361\u7247",

  NearWater: "\u4e34\u8fd1\u6c34\u4f53",
  NearLava: "\u4e34\u8fd1\u7194\u5ca9",
  NearHoney: "\u4e34\u8fd1\u8702\u871c",
  UnknownStation: "\u672a\u77e5\u5236\u4f5c\u53f0",
  BestiaryTitle: "\u8d85\u7ea7\u751f\u7269\u56fe\u9274",
  BestiaryFilterJoiner: " | ",
  BestiaryNoActiveFilters: "\u5168\u90e8\u751f\u7269",
  BestiaryFilterBtn: "\u7b5b\u9009",
  BestiaryViewGroup: "\u5206\u7ec4",
  BestiaryCatalogLoading: "\u6b63\u5728\u52a0\u8f7d\u56fe\u9274\u76ee\u5f55\u2026",
  BestiarySearchHint: "\u641c\u7d22\u751f\u7269\u540d/\u5185\u90e8ID/\u62fc\u97f3\u2026",
  BestiarySec_Mod: "\u6a21\u7ec4",
  BestiarySec_Vanilla: "\u7fa4\u7cfb",
  BestiarySec_ModPick: "\u6309\u6a21\u7ec4\u7b5b\u9009",
  BestiaryFilterReset: "\u91cd\u7f6e",
  BestiaryFilterEmpty: "\uff08\u672a\u52a0\u8f7d\u539f\u7248\u7b5b\u9009\uff0c\u8bf7\u8fdb\u5165\u4e16\u754c\u540e\u91cd\u8bd5\uff09",
  BestiaryDetailBack: "\u8fd4\u56de",
  BestiaryDetailModFmt: "\u6a21\u7ec4\uff1a{0}",
  BestiaryDetailNetIdFmt: "NPC ID\uff1a{0}",
  BestiaryDetailUnlockFmt: "\u89e3\u9501\u72b6\u6001\uff1a{0}",
  BestiaryDetailPlaceholder: "\u5b8c\u6574\u56fe\u9274\u5b57\u6bb5\u5c06\u5728\u540e\u7eed\u7248\u672c\u8865\u5168\uff08\u5f53\u524d\u4e3a\u8be6\u60c5\u5916\u58f3\uff09\u3002",
  BestiaryDetailEmpty: "\u5c1a\u672a\u53d1\u73b0\uff0c\u65e0\u8be6\u60c5\u3002",
  BestiaryDetailUnregistered: "\u4e0d\u5728\u539f\u7248\u56fe\u9274\u6570\u636e\u5e93\u4e2d\u3002",
  BestiaryDetailField_Name: "\u540d\u79f0",
  BestiaryDetailField_Mod: "\u6a21\u7ec4",
  BestiaryDetailField_Status: "\u72b6\u6001",
  BestiaryDetailField_Debug: "\u8c03\u8bd5",
  BestiaryDetailField_Flavor: "\u63cf\u8ff0",
  BestiaryDetailField_Kills: "\u51fb\u6740",
  BestiaryDetailField_Drops: "\u6389\u843d",
  BestiaryDetailField_Stats: "\u6570\u503c",
  BestiaryDetailField_Spawn: "\u5237\u65b0",
  BestiaryDetailField_NoNumbers: "\uff08\u5f53\u524d\u8868\u60c5\u6a21\u5f0f\u9690\u85cf\u6570\u503c\uff09",
  BestiaryGroupPhotoWip: "\u5408\u5f71\u5e03\u5c40\uff08\u7b2c\u4e8c\u9636\u6bb5\uff09",
  BestiaryBand_TownNpc: "\u57ce\u9547 NPC",
  BestiaryBand_Critter: "\u5c0f\u52a8\u7269",
  BestiaryBand_Event: "\u4e8b\u4ef6",
  BestiaryBand_NormalEnemy: "\u654c\u4eba",
  BestiaryBand_MiniBoss: "\u5c0f\u5934\u76ee",
  BestiaryBand_Boss: "Boss",
  BestiaryBand_Other: "\u5176\u4ed6",
  BestiaryBand_Unregistered: "\u672a\u767b\u5f55",
  BestiaryPendingDiscoveryFmt: "\u8fd8\u6709 {0} \u79cd\u751f\u7269\u5f85\u53d1\u73b0",
  BestiaryStatsLine: "\u539f\u7248\u56fe\u9274 {0} | \u76ee\u5f55 {1} | \u53ef\u89c1 {2} | \u672a\u767b\u5f55 {3} | \u89c6\u56fe {4}",
  BestiaryFaceTip_AllVisible: "\u5168\u90e8\u53ef\u89c1",
  BestiaryFaceTip_ProgressivePlus: "\u6e10\u8fdb\u89e3\u9501 +",
  BestiaryFaceTip_ProgressiveMinus: "\u4ec5\u5df2\u53d1\u73b0",
  BestiaryFaceTip_UnlockedOnly: "\u4ec5\u672a\u89e3\u9501",
  BestiaryCollectionStats: "\u5df2\u627e\u5230 {0} | \u672a\u627e\u5230 {1} ({2:0.#}%)",
  BestiaryCollectionSummary: "\u5df2\u627e\u5230 {0} ({1:0.#}%) | \u672a\u627e\u5230 {2} | \u603b\u8ba1 {3}",
  BestiaryFaceSummary_DiscoveredOnly: "\u5df2\u627e\u5230 {0} | \u603b\u8ba1 {1}",
  BestiaryFaceSummary_UnlockedOnly: "\u672a\u53d1\u73b0 {0} | \u603b\u8ba1 {1}",
  BestiaryCardSourceModFmt: "\u6765\u6e90\uff1a{0}",
  BestiaryCardSourceVanilla: "\u6765\u6e90\uff1a\u539f\u7248\u751f\u7269",
  BestiaryModTipVanilla: "\u539f\u7248\u751f\u7269",

  // --- UI: Research extras ---
  ResearchSearchHint: "\u6309\u540d\u79f0/\u5185\u90e8ID/\u62fc\u97f3\u9996\u5b57\u6bcd\u641c\u7d22\u2026",
  NoRecipePath: "\u5f53\u524d\u7269\u54c1\u65e0\u5408\u6210\u8def\u5f84",
  GreenFaceNoProducts:
    "\u6b64\u5904\u65e0\u4ecd\u9700\u65c5\u9014\u7814\u7a76\u7684\u4ea7\u7269\uff0c\u6216\u5d4c\u5957\u5408\u6210\u88ab\u963b\u65ad\uff08\u8bf7\u9760\u8fd1\u5df2\u6446\u653e\u7684\u5236\u4f5c\u53f0\uff1b\u89e6\u6478\u5fae\u5149\u524d\u9002\u7528\u5fae\u5149\u89c4\u5219\uff09",
  NoProductsForFilter: "\u6ca1\u6709\u7b26\u5408\u5f53\u524d\u7b5b\u9009\u7684\u4ea7\u7269",
  LosslessCraft: "\u65e0\u635f\u5408\u6210",
  GetThisItem: "\u83b7\u53d6\u6b64\u7269",
  WarehouseWip: "\u4ed3\u5e93\u9875\u7b7e\u5c1a\u672a\u5c31\u7eea\u3002",
  FaceTipPurpleNonJourney: "\u4ee5\u6b64\u6750\u6599\u5408\u6210\u7684\u4ea7\u7269",
  FaceTipPurpleNeedJourney: "\u8bf7\u5728\u65c5\u9014\u6a21\u5f0f\u4e0b\u542f\u7528\u6b64\u6a21\u5f0f",
  FaceTipYellow: "\u5df2\u7814\u7a76\u3001\u4ee5\u6b64\u6750\u6599\u5408\u6210\u7684\u4ea7\u7269",
  FaceTipGreen:
    "\u4f7f\u7528\u6b64\u6750\u6599\u4e14\u5d4c\u5957\u94fe\u6750\u6599\u5747\u5df2\u7814\u7a76\u3001\u914d\u65b9\u53ef\u7528\u4e14\u5df2\u89c1\u5236\u4f5c\u53f0\u7684\u4ea7\u7269\uff1b\u89e6\u6478\u5fae\u5149\u524d\u9690\u85cf\u5fae\u5149\u4ea7\u51fa\u4e0e\u95f4\u63a5\u4f9d\u8d56",
  FaceTipBlue: "\u672a\u7814\u7a76\u3001\u4ee5\u6b64\u6750\u6599\u5408\u6210\u7684\u4ea7\u7269",
  FaceTipPurpleJourney: "\u8bf7\u5728\u975e\u65c5\u9014\u6a21\u5f0f\u4e0b\u542f\u7528\u6b64\u6a21\u5f0f",

  // --- UI: Buff ---
  BuffUnlockedTitle: "\u5df2\u89e3\u9501\u589e\u76ca",
  BuffControlsHint: "\u5de6\u952e\u5207\u6362 - \u53f3\u952e\u7981\u7528 - \u4e2d\u952e\u56fa\u5b9a\u5230\u589e\u76ca\u680f",
  BuffWarning: "\u5806\u53e0\u8fc7\u591a\u589e\u76ca\u53ef\u80fd\u5bfc\u81f4\u4e0d\u7a33\u5b9a\uff1b\u90e8\u5206\u6a21\u7ec4\u589e\u76ca\u53ef\u80fd\u65e0\u6548\u3002",
  BuffSearchHint: "\u641c\u7d22\u589e\u76ca\u540d/\u5185\u90e8ID/\u62fc\u97f3\u2026",
  BuffStats: "\u5df2\u89e3\u9501\uff1a{0}/{1} | \u6fc0\u6d3b\uff1a{2} | \u7981\u7528\uff1a{3}",
  BuffCat_Positive: "\u589e\u76ca",
  BuffCat_PositivePotionFood: "\u836f\u6c34\u4e0e\u98df\u7269",
  BuffCat_PositiveEquipment: "\u88c5\u5907\uff08\u9970\u54c1/\u90e8\u4ef6\uff09",
  BuffCat_PositiveSetBonus: "\u5957\u88c5\u6548\u679c",
  BuffCat_PositiveEnvironment: "\u73af\u5883",
  BuffBulkEnableSummary:
    "\u5df2\u542f\u7528 {0}\uff1b\u8df3\u8fc7 {1}\uff08\u9501\u5b9a {3}/\u624b\u52a8\u5b9e\u4f53 {4}/\u4ec5\u624b\u52a8 {5}/\u4e0d\u5b89\u5168\u865a\u62df {6}\uff09\uff1b{2} \u4e2a\u5957\u88c5\u8bf7\u5728\u5957\u88c5\u533a\u5207\u6362",
  BuffBulkEnableSummaryUnsafeAllowed:
    "\u5df2\u542f\u7528 {0}\uff08\u914d\u7f6e\u5141\u8bb8\u4e0d\u5b89\u5168\u865a\u62df\uff09\uff1b\u8df3\u8fc7 {1}\uff08\u9501\u5b9a {3}/\u624b\u52a8\u5b9e\u4f53 {4}/\u4ec5\u624b\u52a8 {5}\uff09\uff1b{2} \u4e2a\u5957\u88c5\u8bf7\u5728\u5957\u88c5\u533a\u5207\u6362",
  BuffBulkSetBonusNoBulk: "\u5957\u88c5\u6548\u679c\u4e0d\u53ef\u6279\u91cf\u542f\u7528\uff0c\u8bf7\u9010\u4e2a\u70b9\u51fb\u56fe\u6807",
  BuffBulkMiscExclusiveNoBulk: "\u5ba0\u7269\u3001\u5149\u5ba0\u3001\u5750\u9a91\u3001\u77ff\u8f66\u5404\u5360\u4e00\u4f4d\uff0c\u6bcf\u533a\u4ec5\u53ef\u542f\u7528\u4e00\u4e2a",
  SetBonusCircuitBroken: "\u5957\u88c5\u6548\u679c\u8fc7\u591a\uff0c\u5df2\u6682\u505c\u81ea\u52a8\u5e94\u7528\u3002\u8bf7\u5173\u95ed\u90e8\u5206\u5957\u88c5\u5e76\u91cd\u8fdb\u4e16\u754c",
  BuffCat_Negative: "\u51cf\u76ca",
  BuffFilterTitle: "\u589e\u76ca\u6a21\u7ec4\u7b5b\u9009",
  BuffFilterModPick: "\u6309\u6a21\u7ec4\u7b5b\u9009",
  BuffFilterModHoverFmt: "\u6765\u81ea\u6a21\u7ec4 {0} \u7684\u589e\u76ca",
  BuffFilterReset: "\u91cd\u7f6e",
  BuffCat_Other: "\u5176\u4ed6",
  BuffCat_Mount: "\u5750\u9a91",
  BuffCat_Minecart: "\u77ff\u8f66",
  BuffCat_Pet: "\u5ba0\u7269",
  BuffCat_LightPet: "\u5149\u5ba0",
  BuffCat_Minion: "\u5f1f\u5b50",
  BuffCat_Sentry: "\u557e\u5854",
  BuffCat_CombatSummon: "\u4ec6\u4ece",
  BuffHoverMinionSlots: "\u6b64\u5f1f\u5b50\u5360\u7528 {0} \u4e2a\u5f1f\u5b50\u69fd\uff08\u6700\u591a {1} \u4e2a\uff09\u3002",
  BuffCat_Disabled: "\u5df2\u7981\u7528",
  BuffBtnEnableAll: "\u5168\u90e8\u542f\u7528",
  BuffBtnClearAll: "\u5168\u90e8\u6e05\u9664",
  BuffBtnDisableAllDebuffs: "\u5168\u90e8\u7981\u7528",
  BuffBtnRestoreAll: "\u5168\u90e8\u6062\u590d",
  BuffHoverLeft: "[c/00FF00:\u5de6\u952e\uff1a\u6c38\u4e45\u5207\u6362]",
  BuffHoverRight: "[c/FF5555:\u53f3\u952e\uff1a\u7981\u7528\u6b64\u589e\u76ca]",
  BuffHoverMiddle: "[c/FFAA00:\u4e2d\u952e\uff1a\u56fa\u5b9a/\u53d6\u6d88\u56fa\u5b9a\u5230\u589e\u76ca\u680f]",
  BuffHoverDisabled: "[c/FF0000:\u5df2\u7981\u7528]",
  BuffHoverPinned: "[c/FFAA00:\u5df2\u56fa\u5b9a\u5230\u589e\u76ca\u680f]",
  BuffHoverSource: "\u6765\u6e90\uff1a{0}",
  BuffHoverMiscEquip: "[c/FFAA00:\u901a\u8fc7\u6742\u9879\u69fd\u4f4d\u7ef4\u6301\uff1b\u6bcf\u533a\u4e00\u4e2a\uff0c\u5173\u95ed\u65f6\u6062\u590d\u539f\u69fd\u4f4d]",
  BuffHoverVirtual: "[c/88CCFF:\u865a\u62df\u6548\u679c\uff1b\u226420 \u4e2a\u805a\u5408\u4e3a Alpha \u56fe\u6807]",
  BuffHoverPinAdvice: "\u82e5\u6548\u679c\u4e0d\u751f\u6548\uff0c\u53ef\u5c1d\u8bd5\u4e2d\u952e\u56fa\u5b9a\u5230\u589e\u76ca\u680f",

  // --- RecipeEnv ---
  SnowBiome: "\u96ea\u5730\u7fa4\u7cfb",
  Graveyard: "\u5893\u5730",
  ZenithWorld: "\u5929\u9876\u79cd\u5b50\u4e16\u754c",
  CorruptionWorld: "\u8150\u5316\u4e16\u754c",
  CrimsonWorld: "\u8840\u816b\u4e16\u754c",
};

// ItemHub + remaining UI: auto suffix map
const itemHubSuffix = {
  ItemHubTitle: "\u7269\u54c1\u7814\u7a76\u4e2d\u67a2",
  ItemHubTitleHint: "\u9886\u53d6\u4e00\u6b21\uff0c\u7ec8\u8eab\u6548\u679c\uff0c\u514d\u8d39\u5237\u65b0",
  ItemHubProgressTitle: "\u7814\u7a76\u8fdb\u5ea6",
  ItemHubMajor_Mod: "\u5929\u6027",
  ItemHubMajor_Categories: "\u5206\u7c7b",
  ItemHubSearchHint: "\u641c\u7d22\u7269\u54c1\u540d/\u5185\u90e8ID/\u62fc\u97f3\u2026",
  ItemHubResetFilters: "\u91cd\u7f6e",
  ItemHubFilterLabel: "\u7b5b\u9009",
  ItemHubFilterOpen: "\u7b5b\u9009",
  ItemHubFilterTitle: "\u7269\u54c1\u7b5b\u9009",
  ItemHubFilterClose: "\u5173\u95ed",
  ItemHubModVanillaShort: "\u539f",
  ItemHubModTipVanilla: "\u539f\u7248\u7269\u54c1",
  ItemHubModTipModFmt: "\u6765\u81ea\u6a21\u7ec4\u300c{0}\u300d\u7684\u7269\u54c1",
  ItemHubChainActivate: "\u542f\u7528",
  ItemHubChainActivateDesc: "\u542f\u7528\u540e\u663e\u793a\u8be5\u7269\u54c1\u7684\u4e0a\u6e38\u6750\u6599\u4e0e\u4e0b\u6e38\u4ea7\u7269\u3002",
  ItemHubChainSlotHint: "\u62d6\u5165\u7269\u54c1\uff0c\u6216\u542f\u7528\u540e\u6309 [{0}] \u70b9\u51fb\u7269\u54c1\u67e5\u8be2\u3002",
  ItemHubChainKeyUnbound: "\u672a\u7ed1\u5b9a",
  ItemHubActiveStripRemove: "\u70b9\u51fb\u79fb\u9664\u6b64\u7b5b\u9009",
  ItemHubNotOwnedSuffix: "\u4ece\u672a\u83b7\u5f97",
  ItemHubLockedTip: "\u9700\u5148\u83b7\u5f97\u8be5\u7269\u54c1\u624d\u80fd\u5728\u6b64\u9886\u53d6",
  ItemHubLockedShort: "\uff08\u672a\u89e3\u9501\uff09",
  ItemHubSec_ModPick: "\u6309\u6a21\u7ec4\u7b5b\u9009",
  ItemHubSec_Chain: "\u5408\u6210\u4f9d\u8d56\u94fe",
  ItemHubSec_Rare: "\u6309\u7a00\u6709\u5ea6",
  ItemHubCatAll: "\u5168\u90e8",
  ItemHubModAll: "\u5168\u90e8\u6a21\u7ec4",
  ItemHubModVanilla: "\u539f\u7248",
  ItemHubSortName: "\u540d\u79f0",
  ItemHubSortId: "ID",
  ItemHubSortValue: "\u4ef7\u503c",
  ItemHubSortRare: "\u7a00\u6709\u5ea6",
  ItemHubSortDamage: "\u4f24\u5bb3",
  ItemHubSortDefense: "\u9632\u5fa1",
  ItemHubSortDirHint: "\u70b9\u51fb\u5207\u6362\u5347\u964d\u5e8f",
  ItemHubCategory: "\u5206\u7c7b",
  ItemHubMod: "\u6a21\u7ec4",
  ItemHubSort: "\u6392\u5e8f",
  ItemHubCatMelee: "\u8fd1\u6218",
  ItemHubCatRanged: "\u8fdc\u7a0b",
  ItemHubCatMagic: "\u9b54\u6cd5",
  ItemHubCatSummon: "\u53ec\u5524",
  ItemHubCatAccessory: "\u9970\u54c1",
  ItemHubCatConsumable: "\u6d88\u80d7",
  ItemHubCatPlaceable: "\u53ef\u6446\u653e",
  ItemHubCatOther: "\u5176\u4ed6",
  ItemHubFilterApply: "\u5e94\u7528\u5e76\u5173\u95ed",
  ItemHubFilterClearAll: "\u5168\u90e8\u6e05\u9664",
  ItemHubFilterPick: "\u9550",
  ItemHubFilterAxe: "\u65a7",
  ItemHubFilterHammer: "\u9524",
  ItemHubFilterFish: "\u9c7c\u7aff",
  ItemHubProgressFmt: "{0}/{1} | {2}%",
  ItemHubRareSliderFmt: "{0} \u2192 {1}",
  ItemHubActiveRareHoverFmt: "\u7a00\u6709\u5ea6 {0}~{1}",
};

Object.assign(T, itemHubSuffix);

// Icon labels ItemHubIc_*
const ic = {
  Melee: "\u8fd1\u6218\u6b66\u5668",
  Yoyo: "\u745c\u4f3d",
  Magic: "\u9b54\u6cd5\u6b66\u5668",
  Ranged: "\u8fdc\u7a0b\u6b66\u5668",
  Throwing: "\u6295\u63b7\u6b66\u5668",
  Summon: "\u53ec\u5524\u6b66\u5668",
  Sentry: "\u557e\u5854",
  Pickaxe: "\u9550",
  Axe: "\u65a7",
  Hammer: "\u9524",
  Head: "\u5934\u76d4",
  Body: "\u8eab\u7532",
  Legs: "\u817f\u7532",
  VanityArmor: "\u65f6\u88c5\u62a4\u7532",
  NonVanityArmor: "\u975e\u65f6\u88c5\u62a4\u7532",
  Tiles: "\u53ef\u6446\u653e\u65b9\u5757",
  Containers: "\u5bb9\u5668",
  Wiring: "\u7535\u7ebf",
  Statues: "\u96d5\u50cf",
  Doors: "\u95e8",
  Chairs: "\u6905\u5b50",
  Tables: "\u684c\u5b50",
  LightSources: "\u5149\u6e90",
  Torches: "\u706b\u628a",
  Walls: "\u5899",
  Accessories: "\u9970\u54c1",
  Wings: "\u7fc5\u8180",
  Ammo: "\u5f39\u836f",
  Potions: "\u836f\u6c34",
  Health: "\u751f\u547d\u836f\u6c34",
  Mana: "\u9b54\u529b\u836f\u6c34",
  BuffPotion: "\u589e\u76ca\u836f\u6c34",
  Expert: "\u4e13\u5bb6\u7269\u54c1",
  Pets: "\u5ba0\u7269",
  LightPets: "\u5149\u5ba0",
  Mounts: "\u5750\u9a91",
  Carts: "\u77ff\u8f66",
  Hooks: "\u94a9\u722a",
  Dyes: "\u67d3\u6599",
  HairDyes: "\u53d1\u578b\u67d3\u6599",
  BossSummon: "Boss \u53ec\u5524\u7269",
  Consumables: "\u6d88\u80d7\u54c1",
  CapturedNpc: "\u6355\u83b7 NPC",
  FishingPole: "\u9c7c\u7aff",
  Bait: "\u9c7c\u9975",
  QuestFish: "\u4efb\u52a1\u9c7c",
  Extractinator: "\u63d0\u53d6\u673a",
  Materials: "\u6750\u6599",
  Other: "\u5176\u4ed6",
};
for (const [k, v] of Object.entries(ic)) T["ItemHubIc_" + k] = v;

Object.assign(T, {
  "ForceBulkEnableUnsafeVirtual.Tooltip":
    "\u5173\uff08\u9ed8\u8ba4\uff09\uff1a\u300c\u5168\u90e8\u542f\u7528\u300d\u8df3\u8fc7\u5730\u72f1\u3001\u4e2d\u6bd2\u7b49\u6807\u8bb0\u4e3a\u865a\u62df\u4e0d\u5b89\u5168\u7684\u589e\u76ca\uff0c\u9700\u624b\u52a8\u5207\u6362\u3002\n\u5f00\uff1a\u4e5f\u53ef\u6279\u91cf\u542f\u7528\uff08\u4ecd\u53d7\u9501\u5b9a/\u624b\u52a8\u5b9e\u4f53\u89c4\u5219\u7ea6\u675f\uff09\u3002",
  ItemHubSec_Tile: "\u6309\u65b9\u5757\u7c7b\u578b",
  ItemHubSec_Weapon: "\u6309\u6b66\u5668",
  ItemHubSec_Equip: "\u6309\u88c5\u5907\u69fd\u4f4d",
  ItemHubSec_Vanity: "\u65f6\u88c5 / \u5750\u9a91 / \u5ba0\u7269",
  ItemHubSec_ConBuff: "\u836f\u6c34\u4e0e\u98df\u7269",
  ItemHubSec_ConBio: "\u5c0f\u52a8\u7269\u4e0e Boss \u7269\u54c1",
  ItemHubSec_Misc: "\u5176\u4ed6\u7b5b\u9009",
  ItemHubTip_Block: "\u56fa\u4f53\u65b9\u5757\uff1a\u53ef\u6446\u653e\u7684\u5b9e\u4f53\u65b9\u5757\uff08\u975e\u6846\u67b6\u91cd\u8981\u7269\uff09",
  ItemHubTip_Light: "\u7167\u660e\uff1a\u706b\u628a\u7c7b\u65b9\u5757\u3001\u53d1\u5149\u65b9\u5757\u3001\u7b71\u706b\uff1b\u53d1\u5149\u5899\uff1b\u624b\u6301/\u6d88\u80d7\u5149\u6e90",
  ItemHubTip_Wall: "\u5899\u4f53\uff1a\u53ef\u6446\u653e\u5899\u7684\u7269\u54c1",
  ItemHubTip_Furniture: "\u5bb6\u5177\uff1a\u6846\u67b6\u91cd\u8981\u65b9\u5757\u4e14\u4e0d\u5728\u5236\u4f5c\u53f0\u7d22\u5f15\u4e2d\uff1b\u65e0\u6446\u653e\u65b9\u5757\u7684\u7ed8\u753b\u7c7b",
  ItemHubTip_Station: "\u5236\u4f5c\u53f0\uff1a\u5728\u5df2\u52a0\u8f7d\u914d\u65b9\u4e2d\u4f5c\u4e3a requiredTile \u7684\u65b9\u5757\u7c7b\u578b",
  ItemHubTip_Chest: "\u5bb9\u5668\uff1a\u7bb1\u5b50\u3001\u8863\u67dc\u7b49\u5bb9\u5668\u65b9\u5757",
  ItemHubTip_StatWire: "\u7535\u7ebf\u4e0e\u96d5\u50cf\uff1a\u7535\u7ebf\u5206\u7ec4\u3001\u96d5\u50cf\u5217\u8868\u3001\u673a\u68b0\u7269\u54c1\u3001\u65d7\u5e1c",
  ItemHubTip_Melee: "\u8fd1\u6218\u6b66\u5668\uff08\u4e0d\u542b\u94bb/\u65a7/\u9524\u5de5\u5177\uff09",
  ItemHubTip_Magic: "\u9b54\u6cd5\u6b66\u5668",
  ItemHubTip_Ranged: "\u8fdc\u7a0b\u6b66\u5668",
  ItemHubTip_Ammo: "\u5f39\u836f\uff08\u6216\u53ef\u5f53\u5f39\u836f\u4f7f\u7528\u7684\u7269\u54c1\uff09",
  ItemHubTip_Summon: "\u5f1f\u5b50\u53ec\u5524\u6b66\u5668\uff08\u4e0d\u542b\u97ad\u4e0e\u557e\u5854\uff09",
  ItemHubTip_Whip: "\u97ad\u5b50",
  ItemHubTip_Thrown: "\u6295\u63b7\u6b66\u5668\uff08\u542b\u6295\u63b7\u7c7b\u4e0e\u8fdc\u7a0b\u6d88\u80d7\u6b66\u5668\uff09",
  ItemHubTip_Sentry: "\u557e\u5854",
  ItemHubTip_Yoyo: "\u745c\u4f3d",
  ItemHubTip_Pick: "\u94bb\u529b",
  ItemHubTip_Axe: "\u65a7\u529b",
  ItemHubTip_Hammer: "\u9524\u529b",
  ItemHubTip_Armor: "\u62a4\u7532\u69fd\u4f4d\u88c5\u5907",
  ItemHubTip_Acc: "\u9970\u54c1\uff08\u4e0d\u542b\u7fc5\u8180\uff09",
  ItemHubTip_Wing: "\u7fc5\u8180",
  ItemHubTip_Grapple: "\u94a9\u722a",
  ItemHubTip_Fashion: "\u65f6\u88c5",
  ItemHubTip_Mount: "\u5750\u9a91",
  ItemHubTip_Pet: "\u5ba0\u7269",
  ItemHubTip_LightPet: "\u5149\u5ba0",
  ItemHubTip_Heal: "\u751f\u547d\u836f\u6c34 / \u98df\u7269\u56de\u590d",
  ItemHubTip_ManaPot: "\u9b54\u529b\u836f\u6c34",
  ItemHubTip_BuffPot: "\u529f\u80fd\u589e\u76ca\u836f\u6c34",
  ItemHubTip_Food: "\u98df\u7269",
  ItemHubTip_Spawn: "\u5c0f\u52a8\u7269 / \u74f6\u88c5",
  ItemHubTip_BossSummon: "Boss \u53ec\u5524\u7269",
  ItemHubTip_GoodieBag: "\u793c\u888b\u7b49",
  ItemHubTip_OtherCons: "\u5176\u4ed6\u6d88\u80d7\uff08\u542f\u53d1\u5f0f\u5269\u4f59\uff09",
  ItemHubTip_Expert: "\u4ec5\u4e13\u5bb6",
  ItemHubTip_Master: "\u4ec5\u5927\u5e08",
  ItemHubTip_Dye: "\u67d3\u6599 / \u53d1\u578b\u67d3\u6599",
  ItemHubTip_Extract: "\u63d0\u53d6\u673a\u76f8\u5173",
  ItemHubTip_Fish: "\u9493\u9c7c\uff08\u6746\u3001\u9975\u3001\u4efb\u52a1\u9c7c\u7b49\uff09",
  ItemHubTip_Material: "\u6750\u6599\uff08\u6807\u8bb0\u4e3a\u6750\u6599\u7684\u7269\u54c1\uff09",
  ItemHubTip_OtherMisc: "\u5176\u4ed6\u6742\u9879",
  ItemHubTip_Debug: "\u8c03\u8bd5 / \u5e9f\u5f03\uff08\u4ec5\u5728\u6b64\u7b5b\u9009\u4e0b\u53ef\u89c1\uff1b\u4e0d\u53ef\u9886\u53d6\uff09",
  ItemHubTip_RareFmt: "\u7a00\u6709\u5ea6 {0}",
  ItemHubChainToggle: "\u7b5b\u9009\u6b64\u7269\u54c1\u7684\u914d\u65b9\u7f51\u7edc",
  ItemHubChainKeyHint: "\u9875\u7b7e3 + \u672c\u9762\u677f + \u94fe\u6761\u5f00\uff1a\u5bf9\u7269\u54c1\u6309 {0} \u8bbe\u4e3a\u79cd\u5b50\uff08\u4e0d\u6253\u5f00\u7814\u7a76\u9875\uff09\u3002",
  ItemHubChainHint: "\u94fe\u5f0f\u7b5b\u9009\uff1a\u5217\u8868\u4e2d\u6bcf\u6761\u89c4\u5219\u90fd\u5fc5\u987b\u6ee1\u8db3\u3002\u5de6\u4fa7\u9009\u7c7b\u578b\uff0c\u4e2d\u95f4\u7f16\u8f91\uff0c\u70b9\u300c\u52a0\u5165\u94fe\u6761\u300d\u3002",
  ItemHubChainListTitle: "\u5f53\u524d\u94fe\u6761",
  ItemHubChainAddRule: "\u52a0\u5165\u94fe\u6761",
  ItemHubChainOneClickHint: "\u5728\u4e0b\u65b9\u70b9\u300c\u52a0\u5165\u94fe\u6761\u300d\uff08\u8fd1\u6218 / \u8fdc\u7a0b\u7b49\uff09\u3002",
  ItemHubChainRarityHint: "\u7a00\u6709\u5ea6\u8303\u56f4\uff08\u5305\u542b\u7aef\u70b9\uff09\u3002",
  ItemHubChainRarityMin: "\u6700\u4f4e",
  ItemHubChainRarityMax: "\u6700\u9ad8",
  ItemHubChainValueHint: "\u4e0a = \u6700\u4f4e\u4ef7\u503c\uff0c\u4e0b = \u6700\u9ad8\u4ef7\u503c\uff08\u6574\u6570\uff09\u3002",
  ItemHubChainNameHint: "\u5339\u914d\u663e\u793a\u540d\u6216\u5185\u90e8\u540d\uff08\u5b50\u4e32\u3001\u5c0f\u5199\uff09\u3002",
  ItemHubChainActiveFmt: "\u94fe\u6761\u00b7{0}",
  ItemHubChainKind_Mod: "\u6a21\u7ec4",
  ItemHubChainKind_Rarity: "\u7a00\u6709\u5ea6",
  ItemHubChainKind_Tile: "\u65b9\u5757",
  ItemHubChainKind_Tools: "\u5de5\u5177",
  ItemHubChainKind_Value: "\u4ef7\u503c",
  ItemHubChainKind_Name: "\u540d\u79f0",
  ItemHubChainKind_DmgMelee: "\u8fd1\u6218",
  ItemHubChainKind_DmgRanged: "\u8fdc\u7a0b",
  ItemHubChainKind_DmgMagic: "\u9b54\u6cd5",
  ItemHubChainKind_DmgSummon: "\u53ec\u5524",
  ItemHubChainKind_Acc: "\u9970\u54c1",
  ItemHubChainKind_Cons: "\u6d88\u80d7",
  ItemHubChainKind_Place: "\u53ef\u6446\u653e",
  ItemHubRuleEmpty: "?",
  ItemHubRuleModsFmt: "\u6a21\u7ec4 ({0})",
  ItemHubRuleRarityFmt: "\u7a00\u6709\u5ea6 {0}\u2013{1}",
  ItemHubRuleValueFmt: "\u4ef7\u503c {0}\u2013{1}",
  ItemHubRuleTileFmt: "\u65b9\u5757 {0}",
  ItemHubRuleToolsFmt: "\u5de5\u5177 {0}",
  ItemHubRuleNameFmt: "\u540d\u79f0\u5305\u542b\u300c{0}\u300d",
  ItemHubRuleDmgMelee: "\u8fd1\u6218\u6b66\u5668",
  ItemHubRuleDmgRanged: "\u8fdc\u7a0b\u6b66\u5668",
  ItemHubRuleDmgMagic: "\u9b54\u6cd5\u6b66\u5668",
  ItemHubRuleDmgSummon: "\u53ec\u5524\u6b66\u5668",
  ItemHubRuleAcc: "\u9970\u54c1",
  ItemHubRuleCons: "\u6d88\u80d7\u54c1",
  ItemHubRulePlace: "\u53ef\u6446\u653e",
  ItemHubFilterModHint: "\u6a21\u7ec4\uff08\u591a\u9009\uff1b\u7a7a = \u5168\u90e8\uff09",
  ItemHubFilterTileHint: "\u53ef\u6446\u653e\u5173\u952e\u8bcd\uff08\u5339\u914d\u7269\u54c1\u540d\uff0c\u7528\u5176 createTile\uff09",
  ItemHubFilterApplyTile: "\u5e94\u7528\u65b9\u5757\u7b5b\u9009",
  ItemHubFilterClearTile: "\u6e05\u9664\u65b9\u5757\u7b5b\u9009",
  ItemHubFilterToolsHint: "\u5de5\u5177\uff08\u52fe\u9009\u4efb\u4e00\u9879\u5373\u53ef\uff1b\u7269\u54c1\u987b\u5339\u914d\u5176\u4e00\uff09",
});

// Buffs section
T["EMOJAlphaBuff.DisplayName"] = "\u805a\u5408\u589e\u76ca";
T["EMOJAlphaBuff.Description"] = "\u7ec4\u5408\u6240\u6709\u5df2\u542f\u7528\u7684\u6b63\u9762\u589e\u76ca";
T["EMOJOmegaBuff.DisplayName"] = "\u805a\u5408\u51cf\u76ca";
T["EMOJOmegaBuff.Description"] = "\u7ec4\u5408\u6240\u6709\u5df2\u542f\u7528\u7684\u51cf\u76ca";

function lookupZh(pathStack, key) {
  const candidates = [key];
  for (let i = 0; i < pathStack.length; i++) {
    candidates.push(pathStack.slice(i).concat(key).join("."));
  }
  for (const c of candidates) {
    if (T[c]) return T[c];
  }
  return null;
}

function needsQuotes(v) {
  return /[:#{}[\],]|^\s|\s$/.test(v) || v.includes('"');
}

function formatValue(v, indent) {
  if (v.includes("\n")) {
    return "'''\n" + v.split("\n").map((l) => indent + "\t" + l).join("\n") + "\n" + indent + "'''";
  }
  if (needsQuotes(v)) return '"' + v.replace(/\\/g, "\\\\").replace(/"/g, '\\"') + '"';
  return v;
}

function skipTooltipBlock(lines, i, indent) {
  let j = i + 1;
  if (j < lines.length && /^\s*'''\s*$/.test(lines[j])) {
    j++;
    while (j < lines.length && !/^\s*'''\s*$/.test(lines[j])) j++;
    if (j < lines.length) j++;
  }
  return j - 1;
}

const enPath = path.join(root, "en-US_Mods.EvenMoreOverpoweredJourney.hjson");
const lines = fs.readFileSync(enPath, "utf8").split(/\r?\n/);
let replaced = 0;
const pathStack = [];
const out = [];
for (let i = 0; i < lines.length; i++) {
  const line = lines[i];
  const open = line.match(/^(\s*)([A-Za-z0-9_]+):\s*\{\s*$/);
  if (open) {
    pathStack.push(open[2]);
    out.push(line);
    continue;
  }
  if (/^\s*\}\s*$/.test(line)) {
    pathStack.pop();
    out.push(line);
    continue;
  }
  const m = line.match(/^(\s*)([A-Za-z0-9_.]+):\s*(.*)$/);
  if (!m) {
    out.push(line);
    continue;
  }
  const indent = m[1];
  const key = m[2];
  const rest = m[3].trim();
  const zh = lookupZh(pathStack, key);

  if (key === "Tooltip") {
    if (zh) {
      if (zh.includes("\n")) {
        out.push(indent + "Tooltip:");
        out.push(indent + "\t'''");
        for (const tl of zh.split("\n")) out.push(indent + "\t\t" + tl);
        out.push(indent + "\t'''");
      } else {
        out.push(indent + "Tooltip: " + formatValue(zh, indent));
      }
      replaced++;
      i = skipTooltipBlock(lines, i, indent);
      continue;
    }
    out.push(line);
    if (rest === "" || rest === "'''") i = skipTooltipBlock(lines, i, indent);
    continue;
  }

  if (!zh) {
    out.push(line);
    continue;
  }
  out.push(indent + key + ": " + formatValue(zh, indent));
  replaced++;
}

fs.writeFileSync(zhPath, out.join("\n"), "utf8");
console.log("Rebuilt", zhPath, "from en-US with", replaced, "translated keys");

let cjk = 0;
let q = 0;
for (const ch of out.join("\n")) {
  if (ch === "?") q++;
  if (/[\u4e00-\u9fff]/.test(ch)) cjk++;
}
console.log("CJK chars:", cjk, "question marks:", q);
