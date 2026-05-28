# Issues 文档维护约定

1. **每次**针对本模组的交互式开发（含用户在本对话中提出的修改）结束后，在 `Issues/CHANGELOG.md` 顶部追加一条 **vX.Y.Z** 小节（默认仅递增 **补丁号** Z，例如 0.4.3→0.4.4），除非用户明确要求改次版本或主版本。
2. 小节内用简短条目列出：**问题或需求**、**涉及文件/模块**、**状态**（已解决 / 部分解决 / 已知限制）。
3. 与 Recipe Browser / Item Checklist 对齐的详细对照，以 **`Issues/RB对照清单.txt`**（UTF-8）为准；**当前实现的逐条判定**见 **`Issues/ITEMLOGIC.md`**。桌面 `RB对照清单.txt` 可同步进仓库。编码说明见 **`RB对照清单_编码说明.txt`**。
4. **家具蓝图页签**的功能说明与完整赋分机制见 **`Issues/FURNITURE_BLUEPRINT.md`**（含 UI 交互、识别流水线、分类顺序、全部分值表与选优门槛）。
5. **家具蓝图重构**（参考 ImproveGame / StructureHelper 的技术细节、文件格式、UI 规范）见 **`Issues/BLUEPRINT_REFERENCE.md`**。参考源码放在 **`tModLoader/ReferenceMods/`**（ModSources 外，见文档 §0），**不得**放在 `ModSources/EvenMoreOverpoweredJourney/` 内。
6. **家具蓝图分步实现路线图**（Phase 0–5、二级窗体策略、文件树）见 **`Issues/BLUEPRINT_IMPLEMENTATION.md`**。
7. 不在此文件夹存放编译输出；`build.txt` 与 `.csproj` 中的版本号应与 CHANGELOG 最新条保持一致。
