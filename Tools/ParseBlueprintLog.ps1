# Parses EMOJ simple.log batch-test output into JSON index.
param(
    [string]$LogPath = "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\Logs\EMOJ\2026-05-27_12-28-08\simple.log",
    [string]$OutDir = "$PSScriptRoot\..\Issues\audit_data"
)

$slotOrder = @(
    'Block','Wall','Bathtub','Bed','Bookcase','Candelabra','Candle','Chandelier',
    'Chair','Chest','Clock','Door','Dresser','Lamp','Lantern','Piano','Platform',
    'Sink','Sofa','Table','Toilet','Workbench'
)

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$lines = Get-Content -LiteralPath $LogPath -Encoding UTF8
$entries = @{}
$currentSeed = $null
$currentMeta = $null
$inSlots = $false

$seedPattern = 'batch-test seed=(\d+)\s+name=(.+?)\s+material=(\d+)\s+wiki=(\d+)/22\s+accuracy=(\d+)/(\d+)\s+mismatch=(\d+)\s+lineage_miss=(\d+)\s+vanilla_leak=(\d+)'
$slotPattern = '^\s+(Block|Wall|Bathtub|Bed|Bookcase|Candelabra|Candle|Chandelier|Chair|Chest|Clock|Door|Dresser|Lamp|Lantern|Piano|Platform|Sink|Sofa|Table|Toilet|Workbench)=(\d+)\|([^|]*)\|(.*)$'

function Flush-Entry {
    param($Meta, $Seed)
    if ($null -eq $Meta) { return }
    if ($Meta.slots.Count -ge 20) {
        $entries["$Seed"] = $Meta
    }
}

foreach ($raw in $lines) {
    # Strip log prefix: [time] [LEVEL] [Blueprint] ...
    $line = $raw
    if ($line -match '\[Blueprint\]\s*(.+)$') {
        $line = $Matches[1]
    }

    if ($line -match $seedPattern) {
        Flush-Entry $currentMeta $currentSeed
        $currentSeed = [int]$Matches[1]
        $name = $Matches[2] -replace '\s+(incomplete|candidates=\d+).*$', ''
        $currentMeta = [ordered]@{
            seed = $currentSeed
            name = $name.Trim()
            material = [int]$Matches[3]
            wiki_filled = [int]$Matches[4]
            accuracy = [int]$Matches[5]
            accuracy_denom = [int]$Matches[6]
            slot_mismatch = [int]$Matches[7]
            lineage_miss = [int]$Matches[8]
            vanilla_leak = [int]$Matches[9]
            incomplete = ($raw -match 'incomplete')
            slots = @{}
        }
        $inSlots = $false
        continue
    }

    if ($line -match 'batch-test slots seed=(\d+)') {
        if ([int]$Matches[1] -eq $currentSeed) { $inSlots = $true }
        continue
    }

    if ($inSlots -and $currentMeta -and $line -match $slotPattern) {
        $slot = $Matches[1]
        $currentMeta.slots[$slot] = [ordered]@{
            type = [int]$Matches[2]
            internal = $Matches[3]
            display = $Matches[4]
        }
        continue
    }

    if ($inSlots -and $currentMeta -and $line -notmatch $slotPattern -and $line -notmatch 'batch-test slots') {
        Flush-Entry $currentMeta $currentSeed
        $inSlots = $false
    }
}

Flush-Entry $currentMeta $currentSeed

$jsonPath = Join-Path $OutDir 'batch_slots_index.json'
$entries | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$all = @($entries.Values)
$scored = @($all | Where-Object { $_.accuracy_denom -gt 0 })
$summary = [ordered]@{
    total_seeds = $all.Count
    scored = $scored.Count
    avg_wiki = if ($all.Count) { [math]::Round(($all | Measure-Object -Property wiki_filled -Average).Average, 2) } else { 0 }
    total_lineage_miss = ($scored | Measure-Object -Property lineage_miss -Sum).Sum
    high_lineage = @($scored | Where-Object { $_.lineage_miss -ge 5 } | Sort-Object lineage_miss -Descending | Select-Object -First 30 seed, name, material, wiki_filled, lineage_miss, accuracy, accuracy_denom)
    quick_seeds_present = @($all | Where-Object { $_.seed -in @(829,828,812,917,3163,3932,13846,13843,13904) } | Select-Object seed, name, material, wiki_filled, lineage_miss, accuracy, accuracy_denom)
}

$summaryPath = Join-Path $OutDir 'batch_summary.json'
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "Parsed $($all.Count) seeds -> $jsonPath"
