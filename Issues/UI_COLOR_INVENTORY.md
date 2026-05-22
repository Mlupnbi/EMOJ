# UI 颜色清单（青色系 · 换色指南）

## 一、你要改的只有两个文件

| 文件 | 作用 |
|------|------|
| **`Shell/UI/OPJourneyUiPalette.cs`** | 四级青色阶梯 **Tier0–Tier3**（改 RGB 只改这里） |
| **`Shell/UI/OPJourneyUiColors.cs`** | 各 UI 元件用哪个档位、透明度（窗体/按钮/描边等） |

研究页产物 **蓝/绿/红边** 故意保留语义色，不在青色系里。

---

## 二、四级青色阶梯（你指定的色值）

| 档位 | RGB | 明度 | 建议用途 |
|------|-----|------|----------|
| **Tier0** 最亮 | 127, 255, 212 | 最高 | 选中描边、高亮框、拖拽手柄、稀有度标记 |
| **Tier1** 亮 | 118, 238, 198 | 高 | 打开态按钮、搜索框边框、图标淡 tint |
| **Tier2** 中 | 102, 205, 170 | 中 | **主窗底板基色**（×70% 透明） |
| **Tier3** 暗 | 69, 139, 116 | 低 | 左侧页签、普通按钮底、搜索框底 |

### 自动映射规则（`OPJourneyUiPalette.MapByLuminance`）

把**旧 UI 颜色**按亮度自动对应到某一档：

- 亮度 ≥ 72% → Tier0（原金色描边 255,220,120 等）
- 亮度 ≥ 52% → Tier1（原淡紫白、青色描边等）
- 亮度 ≥ 32% → Tier2（原中灰绿、主窗色等）
- 更暗 → Tier3（原 40,40,60 深蓝灰按钮等）

---

## 三、令牌表（`OPJourneyUiColors` → 游戏里是什么）

### 窗体

| 令牌 | 实际颜色 | 游戏位置 |
|------|----------|----------|
| `MainPanelBackground` | Tier2 × **70%** | 主窗、图鉴筛选窗、图鉴详情窗、增益/物品二级窗 |
| `PanelBorder` | 由旧边框色映射 | 所有上述窗体描边 |
| `GridBackdrop` | Tier2 × 88% | 图鉴卡片网格背景 |

### 左侧壳页签（4 个图标）

| 令牌 | 档位 | 游戏位置 |
|------|------|----------|
| `TabBackground` / `TabInactiveBackground` | Tier3 | 未选中页签底 |
| `TabActiveBackground` | Tier2 | 选中页签底 |
| `TabActiveBorder` | Tier0 | 选中页签金边 → 现为最亮青 |
| `TabInactiveBorder` | 映射暗青 | 未选中描边 |

### 图鉴筛选窗 · 左侧外挂按钮

| 令牌 | 档位 | 按钮 |
|------|------|------|
| `SecondaryTabOnBackground` | Tier2 | 模组 / 群系（选中） |
| `SecondaryTabOffBackground` | Tier3 | 未选中 |
| `SecondaryTabOnBorder` | Tier0 | 选中描边 |
| `DangerBackground` / `DangerBorder` | **红色** | 重置（语义色，未改青） |

### 按钮 / 搜索

| 令牌 | 档位 | 游戏位置 |
|------|------|----------|
| `ButtonBackground` | Tier3 | 物品中心排序区、筛选面板 |
| `ButtonBackgroundOpen` | Tier2 | 筛选面板打开态 |
| `ButtonBorderOpen` | Tier0 | 打开态描边 |
| `SearchBarBackground` | Tier3 × 92% | 图鉴/增益搜索条 |
| `SearchBarBorder` | Tier1 | 搜索条边框 |

### 描边高亮

| 令牌 | 档位 | 游戏位置 |
|------|------|----------|
| `AccentGoldOutline` | Tier0 | 筛选钮、模组/群系格选中 |
| `AccentCyanOutline` | Tier1 | 已选模组 chip |

### 关闭钮

| 令牌 | 说明 |
|------|------|
| `CloseButtonFill` | **红色**（主窗 + 详情窗，详情已回滚为 0.5 倍小红 X） |
| `CloseButtonMark` | 深红 × 字 |

### 文字

| 令牌 | 说明 |
|------|------|
| `TextPrimary` | 白色（正文） |
| `TextMuted` | Tier0 淡化（统计行） |
| `TextHint` | Tier1 淡化（占位、搜索提示） |

---

## 四、已自动接线的源文件（不必逐文件找色）

以下文件中的**主题色**已改为 `OPJourneyUiColors.*`：

- `Shell/UI/OPJourneyUI.cs` — 主窗
- `Shell/UI/Components/UIBaseComponents.cs` — 页签、关闭钮
- `Bestiary/UI/BestiaryPage.cs` — 网格、统计文字
- `Bestiary/UI/BestiarySecondaryPanel.cs` — 筛选窗 + 外挂按钮
- `Bestiary/UI/BestiaryDetailSecondaryPanel.cs` — 详情窗底
- `Bestiary/UI/Components/*` — 筛选钮、脸 fallback
- `Buffs/UI/BuffSecondaryPanel.cs`、`UIBuffSearchBar.cs`
- `ItemHub/UI/ItemHubPage.cs`、`ItemHubSecondaryPanel.cs`（底板）

### 仍为字面量、可后续迁移

| 区域 | 文件 | 说明 |
|------|------|------|
| 研究产物 rim | `Research/UI/ResearchPageUI.cs` | 蓝/绿/红语义 |
| 研究底部按钮 | `Research/UI/ResearchPage.cs` | 绿/黄功能钮 |
| Buff 页批量按钮 | `Buffs/UI/BuffPage.cs` | 绿/黄/红 |
| 卡片剪影 tint | `Bestiary/UI/BestiaryVanillaSlotRenderer.cs` | 深蓝灰 |

---

## 五、改色示例

**只想整体换青色系：**

1. 改 `OPJourneyUiPalette.cs` 里 Tier0–Tier3 四个 RGB。
2. 进游戏看主窗、页签、筛选窗是否满意。

**只想主窗更透：**

- 改 `MainPanelBackground` 的 `0.7f` → `0.6f` 等。

**想恢复金色页签描边：**

- 把 `TabActiveBorder` 改回 `new Color(255, 210, 90)`（不要用 Tier0）。

---

*维护：新增 UI 时用 `OPJourneyUiColors`，勿写裸 `new Color(102,205,170)`。*
