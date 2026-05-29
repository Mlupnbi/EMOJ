# Wiki × 日志 × 代码 — 家具套组对照审计

> **日志基线**：`2026-05-27_12-28-08`（QUICK 49 + Sets 3414，解析得 **1092** 条含 22 槽明细）  
> **解析工具**：[`Tools/ParseBlueprintLog.js`](../Tools/ParseBlueprintLog.js)、[`Tools/CompareWikiLog.js`](../Tools/CompareWikiLog.js)  
> **结构化数据**：[`audit_data/`](../Issues/audit_data/)（`batch_slots_index.json`、`wiki_compare_results.json`、`mod_prefix_scan.json`）

---

## 1. 批次汇总

| 批次 | Wiki 源 | 对照套组数 | 主要结论 |
|------|---------|------------|----------|
| A 原版 | [家具套装](https://terraria.wiki.gg/zh/wiki/家具套装) | Living Wood / Marble / Spider / Cactus | 生命木、蜘蛛 **22/22 全对**；大理石 **Dresser?Sofa 互换**；Bed 种子 **3163 整组 scramble** |
| B Calamity | [Furniture sets](https://calamitymod.wiki.gg/wiki/Furniture_sets) | Abyss / Cosmilite / Statigel + Silva / Profaned / Void / Monolith | 多数套组 **槽位内物品正确但进错槽**（shift/scramble）；Abyss 缺 Bed/Bathtub/Sink |
| C HJ | [家具](https://homewardjourney.wiki.gg/zh/wiki/家具) | Death / Nothingness | **ItemDeath\*** 部件本身匹配；**material=9 泛用木锚定** + 原版/生命木 placeholder 污染 |
| D Thorium | [Furniture sets](https://thoriummod.wiki.gg/wiki/Furniture_sets) | Marine（Deep Sea） | 日志前缀 `Marine*` / `RefinedMarine*`，非 `DeepSea*`；部分槽仍 scramble |
| E Spirit | [Spirit Reforged](https://spiritmod.wiki.gg/wiki/Spirit_Reforged/Furniture_sets) | Briar / Reach | **本次日志无 Spirit 前缀条目**（可能未加载或未进 Sets 队列） |
| F 附属 | CV / Fables / Spooky | Occult / Spooky | Spooky **多槽 scramble**；Occult 为 partial set + banner 等非标准件 |

### 1.1 日志指标（改后）

| 模式 | wiki 均分 | accuracy | slot_mismatch | lineage_miss | vanilla_leak |
|------|-----------|----------|---------------|--------------|--------------|
| QUICK | 17.91/22 | 1.00 | 0 | 0 | 0 |
| Sets | 12.50/22（计分 1058 项） | 0.97 | 0 | 297 | 0 |

**指标盲区（已在代码中修复，需重跑 batch 验证）**：

- `vanilla_leak` 对 `ItemLoader.GetItem==null` 的原版物（如 `Bed=224`）漏计
- `lineage_miss` 对 mod 种子 + 原版 pick 返回 false
- 未统计 `material=9` 对 mod 血统套的 **材料错锚** → 新增 `material_miss`

---

## 2. 批次 A — 原版

### 2.1 Living Wood（seed **829**）— 全匹配

| 指标 | 值 |
|------|-----|
| wiki 对照 | **22/22 match** |
| material | 9（Wood，wiki 正确） |
| 代码路径 | 生命木桥接 + 配方 Wood 材料正常 |

### 2.2 Marble（代表 seed **3154** / 回归 seed **3163**）

**Wiki 期望**（[Marble furniture](https://terraria.wiki.gg/wiki/Marble_furniture)）：`MarbleBlock` + 20 件 `Marble*` + `ToiletMarble`。

| 槽位 | seed=3154 日志 | wiki 期望 | 分类 |
|------|----------------|-----------|------|
| Dresser | Marble**Sofa** (3151) | MarbleDresser | **wrong_item**（与 Sofa 互换） |
| Sofa | Marble**Dresser** (3133) | MarbleSofa | **wrong_item** |
| Workbench | MarbleWorkBench (3157) | MarbleWorkBench | match（type 3157，与 3156 需游戏内再核） |
| 其余 19 槽 | 均正确 internal | — | match |

**seed=3163（大理石床，QUICK 回归）— 严重 placeholder scramble**：

| 错槽示例 | 日志 | 应为 |
|----------|------|------|
| Candelabra | MarbleLantern | MarbleCandelabra |
| Candle | MarbleColumn | MarbleCandle |
| Chair | MarbleDoor | MarbleChair |
| Chest | MarbleSink | MarbleChest |
| Door | MarbleLamp | MarbleDoor |
| Lamp | MarbleChair | MarbleLamp |
| Lantern | MarbleChest | MarbleLantern |
| Sink | 空 | MarbleSink |

**归因**：`FurniturePlaceholderPool` 占位一次性分配 + 大理石多件 `Marble*` 分数接近，Bed 作种子时候选排序与 Table 种子不同 → [`FurnitureSetRecognizer`](FurnitureSetRecognizer.cs) 选优 / [`FurnitureWikiSlotPlaceholder`](FurnitureWikiSlotPlaceholder.cs) 抢槽。

### 2.3 Spider（seed **3932**）— 全匹配 22/22

### 2.4 Cactus（seed **812**）— 已列 8 槽均 match（wiki 未要求满 22）

---

## 3. 批次 B — Calamity

> Partial sets（Banner / Monolith 装饰 / Ritual Candle 等）**不在 22 槽**，标记 `out_of_scope`。

### 3.1 Abyss（seed **6707**）

- **16/19** wiki 槽 match（Table=6707 `AbyssTable`）
- **empty_miss**：Bathtub、Bed、Sink（wiki 有但日志空 — 可能材料 `6709` 未扩全或 wiki 更新）

### 3.2 Cosmilite（seed **6806**）— 典型 scramble

| 槽位 | 实际 internal | 期望 |
|------|---------------|------|
| Candelabra | Cosmilite**Basin** | CosmiliteCandelabra |
| Table | Cosmilite**Bathtub** | CosmiliteTable |
| Dresser | Cosmilite**Bed** | CosmiliteDresser |
| Chandelier→…→Toilet | 整体前移 | 多件进错槽 |

**归因**：同套组 `Cosmilite*` 候选齐全，但 **槽位分类/占位顺序** 未按 wiki 槽锁定 → [`FurnitureSlotClassifier`](FurnitureSlotClassifier.cs) + placeholder 池。

### 3.3 Statigel（seed **7076**）

- seed 为 **StaticRefiner**（非 Table），仅 13/22 填格
- Lamp 槽 = `StaticRefiner`（功能站非家具灯）→ **wrong_item**
- 多槽 empty_miss

### 3.4 前缀扫描（Sets 实测）

| 套组 | seed | wiki_filled | 问题模式 |
|------|------|-------------|----------|
| Silva | 7055 | 18 | Dresser=SilvaBed, Sofa=SilvaBathtub, Table=SilvaDresser 等 **系统性错位** |
| Profaned | 6608 | 18 | 同 Cosmilite 模式 |
| Void | 6609 | 18 | Clock=VoidDresser, Table=VoidBathtub 等 |
| Monolith | ~54948 | 18+ | 部分槽 match，Bed/Bathtub 等仍缺 |

---

## 4. 批次 C — Homeward Journey

### 4.1 Death 套（seed **13843** Dresser / **13846** Piano）

**Wiki 登记部件**（`ItemDeath*`）：Bookcase … Toilet（15 件，Sink 在 Chest 槽）。

| 检查项 | seed=13843 | seed=13846 |
|--------|------------|------------|
| ItemDeath 部件 | **15/15 match** | 仅 Piano 为 Death，其余大量非 Death |
| material | **9 Wood** | **9 Wood** |
| Block/Wall | Wood / WoodWall | Wood / WoodWall |
| 原版污染 | Bed=224, Workbench=LivingWood | Bed=224, Chair=Wooden, Table=Wooden, Bookcase=354… |

**归因**：

1. [`ResolveModLineageMaterialBlock`](FurnitureSetMaterialRules.cs) 未解析 HJ 专用块 → `material=9`
2. [`FurniturePlaceholderPool`](FurniturePlaceholderPool.cs) 在 material=9 下注入原版木/生命木
3. 13846 以 Piano 为种子时 Death 套候选未完整进入池

### 4.2 Nothingness 套（seed **13904**）

- **15/15** `ItemNothingness*` match
- material=9；Platform/Sink/Workbench 空（wiki 可能无或未识别）
- 比 Death 套少原版床污染（Bed 空）

---

## 5. 批次 D–F — Thorium / Spirit / 附属

### 5.1 Thorium — Marine（Deep Sea）

- 日志 internal 前缀 **`Marine` / `RefinedMarine`**，非 wiki 表名 DeepSea
- seed 示例含 `MarineBookcase`、`RefinedMarineBlock`；DeepSea 期望表需按 **实际 internal** 维护
- `DepthChest` 等跨前缀混入 → lineage 问题

### 5.2 Spirit Reforged

- **`mod_prefix_scan`：Briar / Reach 均无 seed** → 本次环境未覆盖；wiki 对照标记 `wiki_gap` / `no_seed`

### 5.3 Calamity's Vanities — Occult（seed **7028**）

- partial set：含 `OccultLegionnaireBanner`、`RitualCandle` 等 **非标准 22 槽**
- Chair 槽 = Banner → 分类为 `non_set_seed` / `out_of_scope`

### 5.4 Spooky（seed **1816**）

| 错槽 | 日志 | 期望 |
|------|------|------|
| Clock | Spooky**Piano** | SpookyClock |
| Dresser | Spooky**Sofa** | SpookyDresser |
| Piano | Spooky**Dresser** | SpookyPiano |
| Sofa | Spooky**WorkBench** | SpookySofa |

**归因**：与 Calamity scramble 相同 — placeholder + 槽位证据不足。

### 5.5 Calamity Fables

- 前缀 `Fable` **无 seed** → wiki_gap 或未加载

---

## 6. 差异分类统计（Wiki 真值对照）

| 分类 | 数量级 | 典型套组 |
|------|--------|----------|
| **match** | Living Wood, Spider, HJ Death/Nothingness 部件 | 829, 3932, 13843 |
| **wrong_item** | 同套组进错槽 | Marble 3154, Cosmilite, Silva, Spooky |
| **empty_miss** | wiki 有、日志空 | Abyss Bed, Statigel 多数槽 |
| **material_wrong** | Block/Wall=Wood(9) on mod | 13846, 13843, 13904 |
| **vanilla_pollution** | mod 套组出现 Terraria 泛用件 | 13846（Bed/Chair/Table…） |
| **slot_mismatch**（内置指标） | 柱/工作台错槽 | QUICK 已归零；Marble 3163 为 **wrong_item 链** 非 classifier miss |
| **non_set_seed** | 雕像/锅/装饰食物 | 9932, 10110, 15324+ |
| **wiki_gap** | wiki 未登记 | Calamity/Spooky 新套组；Spirit 未加载 |
| **no_seed** | skip_no_mat 2356 或未进 Sets | 部分 Thorium/Fables |

---

## 7. 代码归因矩阵

| 现象 | 首要路径 | 次要路径 |
|------|----------|----------|
| Dresser/Sofa 互换 | `FurnitureSetRecognizer` 选优分数 | `FurnitureWikiSlotPlaceholder` 占位顺序 |
| 大理石 3163 scramble | placeholder-once + Bed 种子候选序 | `FurnitureCandidateExpander` |
| Calamity 整组前移 | 槽位证据 `Meets*PickEvidence` 过宽 | `FurnitureSlotClassifier` |
| HJ material=9 | `FurnitureSetMaterialRules.ResolveModLineageMaterialBlock` | `FurnitureBlueprintBatchTest.ResolveAutoMaterialBlock` |
| HJ 原版/生命木污染 | `FurniturePlaceholderPool` + material=9 | `IsForbiddenGenericMaterial` 未挡 placeholder |
| 指标误报 15/15 | `FurnitureSchemeAccuracy.IsVanillaLeak` | `IsLineageMiss` 对原版返回 false |

---

## 8. P0 / P1 修复候选

### P0（影响 wiki 真值）

1. **HJ / mod 血统 material 锚定**：13846/13843/13904 不得落 `material=9`（`FurnitureSetMaterialRules`）
2. **placeholder 池在 material 禁止泛用木时不注入原版木/生命木**（13846）
3. **同套组槽位锁定**：Cosmilite/Silva/Spooky 类 scramble — 选优时加强 **internal 槽位名对齐**（`FurnitureSlotNameRules` / dresser-sofa 区分）
4. **Bed 种子 scramble（3163）**：Marble 套 placeholder 按已占用 internal 去重

### P1

5. Dresser?Sofa 互换（3154）— 独立降分或成对校验
6. Calamity Statigel 代表 seed 应优先 Table 而非 StaticRefiner（`CollectRepresentativeSetSeeds` 过滤功能站）
7. `FurnitureSchemeAccuracy` 已加 `material_miss`、`TryWikiStyleMatch`、修正 vanilla/lineage — **需重跑 TEST_BLUEPRINT 验证**
8. FULL 跑批补 Sets 未覆盖单件

---

## 9. Wiki 回归 seed 清单

| 优先级 | seed | 套组 | 验证点 |
|--------|------|------|--------|
| P0 | 829 | Living Wood | 22/22 baseline |
| P0 | 3154 | Marble Table | Dresser/Sofa 不互换 |
| P0 | 3163 | Marble Bed | 无 scramble |
| P0 | 13846 | HJ Death Piano | material≠9，无 Bed=224 |
| P0 | 13843 | HJ Death Dresser | ItemDeath 全槽 + 材料 |
| P1 | 3932 | Spider | 22/22 |
| P1 | 6806 | Cosmilite | 无槽位前移 |
| P1 | 7055 | Silva | 无槽位前移 |
| P1 | 1816 | Spooky | Clock/Piano/Dresser 正确 |
| P1 | 6707 | Abyss | Bed/Bathtub/Sink 填充 |

---

## 10. 复现对照流程

```powershell
# 1. 游戏内：TEST_BLUEPRINT QUICK 或 Sets
# 2. 解析日志
node Tools/ParseBlueprintLog.js
# 3. Wiki 期望对照
node Tools/CompareWikiLog.js
# 4. 查看 Issues/audit_data/wiki_compare_results.json
```

---

*审计日期：2026-05-27；基于 post-P0-fix 日志会话 `12-28-08`。*
