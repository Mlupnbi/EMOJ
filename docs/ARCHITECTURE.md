# EvenMoreOverpoweredJourney — 源码架构

## 顶层目录

| 目录 | 命名空间根 | 职责 |
|------|------------|------|
| `Core/` | `EvenMoreOverpoweredJourney.Core.*` | 配置、本地化、日志、工具、模组元数据 |
| `Buffs/` | `EvenMoreOverpoweredJourney.Buffs.*` | 超级增益面板（玩家、系统、UI、内容 Buff） |
| `ItemHub/` | `EvenMoreOverpoweredJourney.ItemHub.*` | 物品中枢（目录、筛选、规则、UI） |
| `Research/` | `EvenMoreOverpoweredJourney.Research.*` | 旅途研究 / 配方分析 |
| `Shell/` | `EvenMoreOverpoweredJourney.Shell.*` | 主界面框架、标签页、共享 UI 组件 |
| `SuperAdmin/` | `EvenMoreOverpoweredJourney.SuperAdmin` | 调试聊天指令与会话状态 |
| `Integration/` | `EvenMoreOverpoweredJourney.Integration.*` | 第三方模组对接、会话导出、浏览器反射 |
| `Localization/` | — | hjson 本地化（tMod 约定路径） |
| `Data/BuffModSupport/` | — | 扫描器产出的 JSON/TSV（非编译） |

根目录仅保留：`EvenMoreOverpoweredJourney.cs`（Mod 入口）、`GlobalUsings.cs`、`build.txt`、图标。

## Buffs 子系统 (`Buffs/Systems/`)

| 子文件夹 | 说明 |
|----------|------|
| `Catalog/` | 列表、分类、稳定键、显示名、来源索引 |
| `Virtual/` | 虚拟 scratch 施加、分类器、安全名单 |
| `Managed/` | 真实栏位续期、进出世界、死亡保留、免疫 Hook |
| `Combat/` | 仆从 / 哨兵战斗召唤 |
| `Spawning/` | 宠物、坐骑、misc 槽实体类 Buff（避免与 Terraria `Entity` 冲突） |
| `SetBonus/` | 套装奖励与护甲解析 |
| `ModSupport/` | 模组/原版支持表、直写属性 |
| `FedState/` | 饱食度等兼容 |
| `Diagnostics/` | 全开跳过统计 |
| `Display/` | 表情刷屏防护等 |

## 命名约定

- **ModPlayer / ModSystem / ModBuff**：按 tMod 惯例，类名保留 `BuffResearchPlayer`、`EMOJAlphaBuff` 等。
- **UI 自定义槽**：`EmojItemSlot`（避免与原版 `UIItemSlot` 重名）。
- **配置**：`OPJourneyConfig` → `Core/Config/`。
- **全局 using**：见根目录 `GlobalUsings.cs`。

## 构建注意

- `Data/BuffModSupport/decompiled_cache/` 不得放在 ModSources 内；反编译输出在 `EvenMoreOverpoweredJourney_ModDev/decompiled_cache/`。
- `buildIgnore` 与 `.csproj` 已排除缓存中的 `.cs`。
