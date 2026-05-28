开发工具脚本（不参与 tModLoader 编译）。

反编译参考副本已移至 ModSources 同级目录：
  EvenMoreOverpoweredJourney_decompiled\

蓝图内置模板导出（Phase 3.3）：
  dotnet run --project _tools/ExportBlueprintTemplates -- "<ModSources根目录>"
  输出：Data/Blueprint/Templates/<id>/{meta.json, structure.ebst, replace.bin}
