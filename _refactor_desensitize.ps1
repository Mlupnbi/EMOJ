# Desensitization + structure refactor (string-literal safe)
$ErrorActionPreference = "Stop"
$root = "c:\Users\14451\Documents\My Games\Terraria\tModLoader\ModSources\EvenMoreOverpoweredJourney"
$desktop = [Environment]::GetFolderPath("Desktop")
$date = Get-Date -Format "yyyyMMdd"
$version = (Get-Content (Join-Path $root "build.txt") -Raw | Select-String "(?m)^version:\s*(.+)$").Matches.Groups[1].Value.Trim()

$idReplacements = [ordered]@{
    "ItemHubItemChecklist" = "HubCollectibleRules"
    "DragonLensBrowserReflection" = "ExternalBrowserReflection"
    "SuperAdminDragonLensCompare" = "SessionBrowserCatalogCompare"
    "DragonLensItemCatalog" = "ExternalBrowserCatalog"
    "SuperAdminBackpackExporter" = "SessionBackpackExporter"
    "ItemHubClassificationIndex" = "HubClassificationIndex"
    "ItemHubCreativeGroupRules" = "HubCreativeGroupRules"
    "ItemHubMaterialResearchBridge" = "HubMaterialResearchBridge"
    "ItemHubSecondaryFilterState" = "HubSecondaryFilterState"
    "ItemHubModBrandTextures" = "HubModBrandTextures"
    "ItemHubStationTileIndex" = "HubStationTileIndex"
    "ItemHubTooltipGlobal" = "HubTooltipGlobal"
    "ItemHubDisplayQuery" = "HubDisplayQuery"
    "ItemHubTagPredicates" = "HubTagPredicates"
    "ItemHubModFilters" = "HubModFilters"
    "ItemHubTagPreviewIds" = "HubTagPreviewIds"
    "ItemHubRecipeClosure" = "HubRecipeClosure"
    "ItemHubModAbbrev" = "HubModAbbrev"
    "ItemHubRegistry" = "HubRegistry"
    "ItemHubIcCategories" = "HubCategoryDefinitions"
    "ItemHubCatalog" = "HubCatalog"
    "ItemHubExtData" = "HubExtData"
    "ItemHubSearch" = "HubSearchQuery"
}

$commentReplacements = [ordered]@{
    "JavidPack/ItemChecklist" = "reference collectible-checklist mod"
    "https://github.com/JavidPack/ItemChecklist" = "reference collectible-checklist repository"
    "ItemChecklistPlayer" = "CollectibleChecklistPlayer"
    "ItemChecklistUI" = "CollectibleChecklistUI"
    "ItemChecklist" = "CollectibleChecklist"
    "DragonLens" = "external item browser"
    "dragon lens" = "external item browser"
    "ItemBrowser" = "item browser UI"
    "Recipe Browser" = "recipe browser"
    "CheatSheet" = "cheat sheet mod"
    "MagicStorage" = "storage mod"
    "RecipeBrowser" = "recipe browser mod"
    "JavidPack" = "reference author"
}

$fileRenames = @{
    "DragonLensBrowserReflection.cs" = "Integration\ExternalBrowserReflection.cs"
    "DragonLensItemCatalog.cs" = "Integration\ExternalBrowserCatalog.cs"
    "SuperAdminDragonLensCompare.cs" = "Integration\SessionBrowserCatalogCompare.cs"
    "SuperAdminBackpackExporter.cs" = "Integration\SessionBackpackExporter.cs"
}

$moves = @(
    @{ From = "GameClipboard.cs"; To = "Common\Utils\GameClipboard.cs" }
    @{ From = "ItemHub\ItemHubRegistry.cs"; To = "ItemHub\Catalog\ItemHubRegistry.cs" }
    @{ From = "ItemHub\ItemHubCatalog.cs"; To = "ItemHub\Catalog\ItemHubCatalog.cs" }
    @{ From = "ItemHub\ItemHubClassificationIndex.cs"; To = "ItemHub\Catalog\HubClassificationIndex.cs" }
    @{ From = "ItemHub\ItemHubCreativeGroupRules.cs"; To = "ItemHub\Catalog\HubCreativeGroupRules.cs" }
    @{ From = "ItemHub\ItemHubModFilters.cs"; To = "ItemHub\Filters\ItemHubModFilters.cs" }
    @{ From = "ItemHub\ItemHubSearch.cs"; To = "ItemHub\Filters\ItemHubSearchQuery.cs" }
    @{ From = "ItemHub\ItemHubDisplayQuery.cs"; To = "ItemHub\Filters\ItemHubDisplayQuery.cs" }
    @{ From = "ItemHub\ItemHubTileRules.cs"; To = "ItemHub\Filters\ItemHubTileRules.cs" }
    @{ From = "ItemHub\ItemHubTagPredicates.cs"; To = "ItemHub\Filters\ItemHubTagPredicates.cs" }
    @{ From = "ItemHub\ItemHubSecondaryFilterState.cs"; To = "ItemHub\Filters\HubSecondaryFilterState.cs" }
    @{ From = "ItemHub\ItemHubItemChecklist.cs"; To = "ItemHub\Rules\HubCollectibleRules.cs" }
    @{ From = "ItemHub\ItemHubIcCategories.cs"; To = "ItemHub\Rules\HubCategoryDefinitions.cs" }
    @{ From = "ItemHub\ItemHubTagPreviewIds.cs"; To = "ItemHub\Rules\ItemHubTagPreviewIds.cs" }
    @{ From = "ItemHub\ItemHubExtData.cs"; To = "ItemHub\Rules\HubExtData.cs" }
    @{ From = "ItemHub\ItemHubRecipeClosure.cs"; To = "ItemHub\Rules\HubRecipeClosure.cs" }
    @{ From = "ItemHub\ItemHubMaterialResearchBridge.cs"; To = "ItemHub\Data\HubMaterialResearchBridge.cs" }
    @{ From = "ItemHub\ItemHubModAbbrev.cs"; To = "ItemHub\Data\HubModAbbrev.cs" }
    @{ From = "ItemHub\ItemHubModBrandTextures.cs"; To = "ItemHub\Data\HubModBrandTextures.cs" }
    @{ From = "ItemHub\ItemHubStationTileIndex.cs"; To = "ItemHub\Data\HubStationTileIndex.cs" }
    @{ From = "ItemHub\ItemHubTooltipGlobal.cs"; To = "ItemHub\Data\HubTooltipGlobal.cs" }
)

function Protect-StringLiterals([string]$text) {
    $list = New-Object System.Collections.Generic.List[string]
    $pattern = '@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"'
    $rx = [regex]::new($pattern)
    $protected = $rx.Replace($text, {
        param($m)
        $idx = $list.Count
        [void]$list.Add($m.Value)
        return "__STR_${idx}__"
    })
    return @{ Text = $protected; Literals = $list }
}

function Restore-StringLiterals([string]$text, $literals) {
    for ($i = 0; $i -lt $literals.Count; $i++) {
        $text = $text.Replace("__STR_${i}__", $literals[$i])
    }
    return $text
}

function Apply-IdReplacements([string]$text, $map) {
    foreach ($k in ($map.Keys | Sort-Object { $_.Length } -Descending)) {
        $text = $text.Replace($k, $map[$k])
    }
    return $text
}

function Update-NamespaceForPath([string]$content, [string]$fullPath) {
    if ($fullPath -match '\\ItemHub\\Catalog\\') {
        $content = $content -replace 'namespace EvenMoreOverpoweredJourney\.ItemHub\b', 'namespace EvenMoreOverpoweredJourney.ItemHub.Catalog'
    }
    elseif ($fullPath -match '\\ItemHub\\Filters\\') {
        $content = $content -replace 'namespace EvenMoreOverpoweredJourney\.ItemHub\b', 'namespace EvenMoreOverpoweredJourney.ItemHub.Filters'
    }
    elseif ($fullPath -match '\\ItemHub\\Rules\\') {
        $content = $content -replace 'namespace EvenMoreOverpoweredJourney\.ItemHub\b', 'namespace EvenMoreOverpoweredJourney.ItemHub.Rules'
    }
    elseif ($fullPath -match '\\ItemHub\\Data\\') {
        $content = $content -replace 'namespace EvenMoreOverpoweredJourney\.ItemHub\b', 'namespace EvenMoreOverpoweredJourney.ItemHub.Data'
    }
    elseif ($fullPath -match '\\Integration\\') {
        $content = $content -replace 'namespace EvenMoreOverpoweredJourney\b(?!\.Integration)', 'namespace EvenMoreOverpoweredJourney.Integration'
    }
    elseif ($fullPath -match '\\Common\\Utils\\') {
        $content = $content -replace 'namespace EvenMoreOverpoweredJourney\b(?!\.Common)', 'namespace EvenMoreOverpoweredJourney.Common.Utils'
    }
    return $content
}

$dirs = @("Common\Utils", "Integration", "ItemHub\Catalog", "ItemHub\Filters", "ItemHub\Rules", "ItemHub\Data")
foreach ($d in $dirs) { New-Item -ItemType Directory -Path (Join-Path $root $d) -Force | Out-Null }

$log = [System.Collections.Generic.List[string]]::new()
$log.Add("# Desensitization & Structure Refactor Log")
$log.Add("Version: $version | Date: $date")
$log.Add("")

foreach ($entry in $fileRenames.GetEnumerator()) {
    $src = Join-Path $root $entry.Key
    $dst = Join-Path $root $entry.Value
    if (Test-Path $src) {
        New-Item -ItemType Directory -Path (Split-Path $dst -Parent) -Force | Out-Null
        Move-Item -Path $src -Destination $dst -Force
        $log.Add("RENAME: $($entry.Key) -> $($entry.Value)")
    }
}

foreach ($m in $moves) {
    $src = Join-Path $root $m.From
    $dst = Join-Path $root $m.To
    if (Test-Path $src) {
        New-Item -ItemType Directory -Path (Split-Path $dst -Parent) -Force | Out-Null
        Move-Item -Path $src -Destination $dst -Force
        $log.Add("MOVE: $($m.From) -> $($m.To)")
    }
}

$csFiles = Get-ChildItem -Path $root -Filter "*.cs" -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|\.vs)\\' -and $_.Name -ne '_refactor_desensitize.ps1'
}

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content) { continue }
    $original = $content
    $p = Protect-StringLiterals $content
    $content = Apply-IdReplacements $p.Text $idReplacements
  foreach ($k in ($commentReplacements.Keys | Sort-Object { $_.Length } -Descending)) {
        $content = $content.Replace($k, $commentReplacements[$k])
    }
    $content = Restore-StringLiterals $content $p.Literals
    $content = Update-NamespaceForPath $content $file.FullName
    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        $log.Add("EDIT: $($file.FullName.Substring($root.Length + 1))")
    }
}

# Global usings file for cross-ItemHub references
$globalUsingsPath = Join-Path $root "GlobalUsings.ItemHub.cs"
$globalUsings = @"
global using EvenMoreOverpoweredJourney.ItemHub.Catalog;
global using EvenMoreOverpoweredJourney.ItemHub.Filters;
global using EvenMoreOverpoweredJourney.ItemHub.Rules;
global using EvenMoreOverpoweredJourney.ItemHub.Data;
global using EvenMoreOverpoweredJourney.Integration;
global using EvenMoreOverpoweredJourney.Common.Utils;
"@
if (-not (Test-Path $globalUsingsPath)) {
    Set-Content -Path $globalUsingsPath -Value $globalUsings.Trim() -Encoding UTF8
    $log.Add("ADD: GlobalUsings.ItemHub.cs")
}

$docPath = Join-Path $desktop "EMOJ_desensitize_structure_v${version}_${date}"
New-Item -ItemType Directory -Path $docPath -Force | Out-Null
$log | Set-Content (Join-Path $docPath "REFACTOR_LOG.txt") -Encoding UTF8

Get-ChildItem -Path $root -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|\.vs)\\'
} | ForEach-Object { $_.FullName.Substring($root.Length + 1) } | Sort-Object |
    Set-Content (Join-Path $docPath "PROJECT_TREE.txt") -Encoding UTF8

$readme = @"
# EvenMoreOverpoweredJourney 脱敏与结构整理

- **版本**: $version
- **日期**: $date
- **源路径**: ``$root``

## 脱敏映射（C# 标识符）

| 原标识符 | 新标识符 |
|----------|----------|
| ItemHubItemChecklist | HubCollectibleRules |
| DragonLensBrowserReflection | ExternalBrowserReflection |
| DragonLensItemCatalog | ExternalBrowserCatalog |
| SuperAdminDragonLensCompare | SessionBrowserCatalogCompare |
| SuperAdminBackpackExporter | SessionBackpackExporter |
| ItemHubCatalog / Registry / … | HubCatalog / HubRegistry / … |

## 白名单（未改）

- ``Issues/CHANGELOG.md``、``description.txt``、``description_workshop.txt``
- 双引号字符串中的模组 ID 与反射类型名（如 ``TryGetMod("DragonLens")``、``"DragonLens.Content...."``）
- 聊天指令字面量 ``DEBUG_COMPAREDRAGONLENS``

## 目录结构

- ``Common/Utils/`` — GameClipboard
- ``Integration/`` — 外部物品浏览器反射与对比
- ``ItemHub/Catalog/`` — 目录与注册表
- ``ItemHub/Filters/`` — 筛选、搜索、显示查询
- ``ItemHub/Rules/`` — 可收集规则、分类、扩展数据
- ``ItemHub/Data/`` — 材质桥接、模组纹理、Tooltip

详见 ``REFACTOR_LOG.txt`` 与 ``PROJECT_TREE.txt``。
"@
Set-Content (Join-Path $docPath "README.md") -Value $readme.Trim() -Encoding UTF8

Write-Host "Done. Documentation: $docPath"
