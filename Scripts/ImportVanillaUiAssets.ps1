# 从 Steam 版 Terraria Content 将原版 UI 的 .xnb 批量解包为 PNG，写入模组 Assets/UI/{页签}/
param(
    [string]$TerrariaContent = "D:\SteamLibrary\steamapps\common\Terraria\Content",
    [string]$ModRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
$ModUi = Join-Path (Resolve-Path $ModRoot) "Assets\UI"
$xnbZip = Join-Path $env:TEMP "xnbcli.zip"
$xnbDir = Join-Path $env:TEMP "xnbcli"
$xnbExe = Join-Path $xnbDir "xnbcli.exe"

if (-not (Test-Path $xnbExe)) {
    Write-Host "Downloading xnbcli..."
    Invoke-WebRequest -Uri "https://github.com/LeonBlade/xnbcli/releases/download/v1.0.7/xnbcli-windows-x64.zip" -OutFile $xnbZip -UseBasicParsing
    Expand-Archive -Path $xnbZip -DestinationPath $xnbDir -Force
}

$ui = Join-Path $TerrariaContent "Images\UI"
$images = Join-Path $TerrariaContent "Images"
$bestiary = Join-Path $ui "Bestiary"

$work = Join-Path $env:TEMP "eoj-xnb-batch"
$packed = Join-Path $work "packed"
$out = Join-Path $work "out"
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packed, $out | Out-Null

$sources = @(
    (Join-Path $ui "Cursor_2.xnb"),
    (Join-Path $ui "SearchCancel.xnb"),
    (Join-Path $ui "Divider.xnb"),
    (Join-Path $images "Inventory_Back.xnb"),
    (Join-Path $ui "ButtonPlay.xnb"),
    (Join-Path $ui "NPCHappiness.xnb")
)
if (Test-Path $bestiary) {
    $sources += Get-ChildItem $bestiary -Filter "*.xnb" -File | ForEach-Object { $_.FullName }
}
foreach ($s in $sources) {
    if (Test-Path $s) { Copy-Item $s $packed -Force }
}

Write-Host "Unpacking $($sources.Count) xnb files (one batch)..."
& $xnbExe unpack $packed $out

$map = @{
    "Cursor_2"           = "Common\Cursor_2.png"
    "SearchCancel"       = "Common\SearchCancel.png"
    "Divider"            = "Common\Divider.png"
    "Inventory_Back"     = "Common\Inventory_Back.png"
    "ButtonPlay"         = "Buff\ButtonPlay.png"
    "NPCHappiness"       = "Buff\NPCHappiness.png"
    "Slot_Back"          = "Bestiary\Slot_Back.png"
    "Slot_Front"         = "Bestiary\Slot_Front.png"
    "Slot_Overlay"       = "Bestiary\Slot_Overlay.png"
    "Slot_Selection"     = "Bestiary\Slot_Selection.png"
    "Icon_Locked"        = "Bestiary\Icon_Locked.png"
    "Icon_Tags_Shadow"   = "Bestiary\Icon_Tags_Shadow.png"
}

$ok = 0
Get-ChildItem $out -Filter "*.png" -File | ForEach-Object {
    $name = $_.BaseName
    if ($map.ContainsKey($name)) {
        $dest = Join-Path $ModUi $map[$name]
    }
    elseif ($name -like "Biome_*" -or $name -like "Button_*" -or $name -like "Stat_*" -or $name -like "Portrait_*" -or $name -like "Icon_*") {
        $dest = Join-Path $ModUi "Bestiary\$name.png"
    }
    else {
        $dest = Join-Path $ModUi "Bestiary\$name.png"
    }
    $dir = Split-Path $dest -Parent
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Copy-Item $_.FullName $dest -Force
    Write-Host "OK $dest"
    $ok++
    if ($name -eq "NPCHappiness") {
        Copy-Item $_.FullName (Join-Path $ModUi "Bestiary\NPCHappiness.png") -Force
    }
}

$mirrors = @(
    @{ Src = "Handle.png"; Sub = "Shell\Handle.png" },
    @{ Src = "TabResearch.png"; Sub = "Shell\TabResearch.png" },
    @{ Src = "TabBuff.png"; Sub = "Shell\TabBuff.png" },
    @{ Src = "TabStorage.png"; Sub = "Shell\TabStorage.png" },
    @{ Src = "TabBestiary.png"; Sub = "Shell\TabBestiary.png" },
    @{ Src = "ItemHubFilterButton.png"; Sub = "ItemHub\FilterButton.png" },
    @{ Src = "ItemHubSortOrderAsc.png"; Sub = "ItemHub\SortOrderAsc.png" },
    @{ Src = "ItemHubSortOrderDesc.png"; Sub = "ItemHub\SortOrderDesc.png" },
    @{ Src = "ItemHubViewCard.png"; Sub = "ItemHub\ViewCard.png" },
    @{ Src = "ItemHubViewList.png"; Sub = "ItemHub\ViewList.png" },
    @{ Src = "ModBrandVanilla.png"; Sub = "ItemHub\ModBrandVanilla.png" },
    @{ Src = "ModBrandTModLoader.png"; Sub = "ItemHub\ModBrandTModLoader.png" }
)
foreach ($m in $mirrors) {
    $src = Join-Path $ModUi $m.Src
    if (-not (Test-Path $src)) { continue }
    $dst = Join-Path $ModUi $m.Sub
    New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent) | Out-Null
    Copy-Item $src $dst -Force
    Write-Host "Mirror $dst"
}

Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Finished. PNG count: $ok -> $ModUi"
Write-Host "Docs: Scripts\EojUiAssets-README.md (do NOT put .md under Assets\)"
