# EMOJ UI 贴图目录（按页签）

本目录下的 **PNG 即模组实际使用的贴图**（已从泰拉瑞亚 `Content` 解包并分类存放）。

重新导入原版贴图（Steam 默认路径）：

```powershell
powershell -ExecutionPolicy Bypass -File "Scripts/ImportVanillaUiAssets.ps1"
```

将 PNG 放到对应子目录即可覆盖；**未放置时自动回退泰拉瑞亚原版贴图**，不影响加载。

## 改图后仍显示原版？

1. 必须改 **子目录里对应文件名**（例如搜索图标 → `Common/Cursor_2.png`，不是根目录的 `Cursor_15.png`）。
2. **完全退出游戏** 后在 ModSources 里重新编译，生成新的 `EvenMoreOverpoweredJourney.tmod`。
3. 进游戏 **重载模组**；加载日志应出现 `[EMOJ UI] 模组贴图包探测 4/4`（若为 `0/4` 说明新 PNG 未打进包）。
4. 仅改 PNG 不编译时，游戏仍用旧 `.tmod` 或原版回退贴图。

## 目录结构

| 子目录 | 页签 | 说明 |
|--------|------|------|
| `Common/` | 共用 | 搜索框光标、关闭钮、详情分隔线、物品槽底 |
| `Shell/` | 主壳 | 标签图标、右下角缩放手柄 |
| `Buff/` | Buff | 分类折叠箭头、四脸表情表 |
| `ItemHub/` | 物品中枢 | 筛选/排序/视图/模组品牌图标 |
| `Bestiary/` | 图鉴 | 格子框、锁定图标、生态背景、表情表 |

## 从原版复制（Content 目录）

在 tModLoader 安装目录下，原版 UI 一般在：

`Steam/steamapps/common/tModLoader/Content/Images/UI/`

物品槽底：`Content/Images/Inventory_Back.png`

### Common

| 模组路径 | 原版源文件 |
|----------|------------|
| `Common/Cursor_2.png` | `UI/Cursor_2.png` |
| `Common/SearchCancel.png` | `UI/SearchCancel.png` |
| `Common/Divider.png` | `UI/Divider.png` |
| `Common/Inventory_Back.png` | `Images/Inventory_Back.png` |
| `Common/NPCHappiness.png` | `UI/NPCHappiness.png`（可选，与 Buff/Bestiary 共用） |

### Shell

| 模组路径 | 说明 |
|----------|------|
| `Shell/Handle.png` | 缩放手柄（原 `Assets/UI/Handle.png` 仍兼容） |
| `Shell/TabResearch.png` 等 | 四个主标签图标 |

### Buff

| 模组路径 | 原版源文件 |
|----------|------------|
| `Buff/ButtonPlay.png` | `UI/ButtonPlay.png` |
| `Buff/NPCHappiness.png` | `UI/NPCHappiness.png` |

### ItemHub

自定义图标；旧路径 `Assets/UI/ItemHubFilterButton.png` 等仍会自动尝试加载。

### Bestiary

| 模组路径 | 原版源文件 |
|----------|------------|
| `Bestiary/Slot_Back.png` | `UI/Bestiary/Slot_Back.png` |
| `Bestiary/Slot_Front.png` | `UI/Bestiary/Slot_Front.png` |
| `Bestiary/Slot_Overlay.png` | `UI/Bestiary/Slot_Overlay.png` |
| `Bestiary/Slot_Selection.png` | `UI/Bestiary/Slot_Selection.png` |
| `Bestiary/Icon_Locked.png` | `UI/Bestiary/Icon_Locked.png` |
| `Bestiary/Background_*.png` | `UI/Bestiary/Background_*.png`（生态底，按文件名放入本目录） |

## 代码入口

- 路径登记：`Shell/UI/Assets/EojUiAssetCatalog.cs`
- 加载缓存：`Shell/UI/Assets/EojUiTextureCache.cs`
- 调用：`EojUiTextures.{Tab}.属性名` 或 `EojUiTextures.ResolveVanillaUiPath(...)`
