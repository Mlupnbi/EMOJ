# EMOJ Buff 扫描结果 vs overrides.json

生成时间：2026-05-20（本地）

## 输入

- 扫描 JSON（8 个）：`CalamityMod_buff_scan.json`, `CalValEX_buff_scan.json`, `EvenMoreOverpoweredJourney_buff_scan.json`, `FargosMutantMod_buff_scan.json`, `FargosSoulsDLC_buff_scan.json`, `FargosSoulsMod_buff_scan.json`, `ImproveGame_buff_scan.json`, `Spooky_buff_scan.json`
- 对照文件：`Data/BuffModSupport/overrides.json`
- StableKey：沿用扫描器 `stableKey` 字段，格式 **`ModName/BuffClassName`**（与游戏内 BuffId 无关，便于版本变化后仍可对上类名）

## 1）与 overrides 对比 — StableKey

| 指标 | 数量 |
|------|-----:|
| 全部扫描中不重复的 StableKey（∪scans） | 657 |
| overrides 中条目总数 | 44 |
| **新增**：出现在扫描中，但 overrides 里还没有 | 632 |
| **删除**：`source=scanner`、所属模组本次也扫过，但该 key 已不在 ∪scans 中 | 0 |

完整「新增」列表见同目录 **`diff_new_stablekeys.tsv`**（632 行）。

## 2）High + Stat → 合并进 overrides

| 指标 | 数量 |
|------|-----:|
| 扫描中 Confidence=High 且 SuggestedPhase=Stat 的不重复 StableKey | 25 |
| 已在 overrides 中 | 25 |
| 尚缺、需要写入的 | **0** |

**结论：无需修改 `overrides.json`。** 当前所有 High+Stat 项均已存在且为 `phase=Stat`。

## 3）Mixed / Unknown — 仅列清单，未写入

| Phase | 数量 |
|-------|-----:|
| Mixed | 4 |
| Unknown | 355 |

的人工审阅清单见 **`manual_review_mixed_unknown.tsv`**（未自动写入 overrides）。

## 4）关于 BuffId

运行时 BuffId 会随加载顺序变化；数据侧请始终用 **`ModName/BuffClassName`** 形式的 StableKey。本次扫描与 overrides 均使用该格式。

---

## 附带修复

- `CalamityMod_buff_scan.json` 首行 JSON 花括号已修正（此前为损坏的 BOM/乱码，可能影响工具解析）。
