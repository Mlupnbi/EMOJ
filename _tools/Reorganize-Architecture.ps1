# EMOJ + Scanner architecture reorganization. Run from repo root or ModSources.
$ErrorActionPreference = "Stop"
$EmojRoot = "c:\Users\14451\Documents\My Games\Terraria\tModLoader\ModSources\EvenMoreOverpoweredJourney"
$ScannerRoot = "c:\Users\14451\Documents\My Games\Terraria\tModLoader\ModSources\EmojBuffModScanner\EmojBuffModScanner"

function Move-SetNamespace {
    param([string]$From, [string]$To, [string]$Namespace)
    $src = Join-Path $EmojRoot $From
    $dst = Join-Path $EmojRoot $To
    if (-not (Test-Path $src)) { Write-Warning "Skip missing: $From"; return }
    if ($src -eq $dst) { return }
    $dir = Split-Path $dst -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    if (Test-Path $dst) { Remove-Item $dst -Force }
    Move-Item -LiteralPath $src -Destination $dst -Force
    $content = Get-Content -LiteralPath $dst -Raw -Encoding UTF8
    $content = [regex]::Replace($content, 'namespace\s+[\w\.]+', "namespace $Namespace", 1)
    [System.IO.File]::WriteAllText($dst, $content, [System.Text.UTF8Encoding]::new($false))
}

# --- EMOJ moves: RelativePath, DestPath, Namespace ---
$emojMoves = @(
    @("Config\OPJourneyConfig.cs", "Core\Config\OPJourneyConfig.cs", "EvenMoreOverpoweredJourney.Core.Config"),
    @("Common\EOPJText.cs", "Core\Localization\EOPJText.cs", "EvenMoreOverpoweredJourney.Core.Localization"),
    @("Common\PinyinUtils.cs", "Core\Utilities\PinyinUtils.cs", "EvenMoreOverpoweredJourney.Core.Utilities"),
    @("Common\Utils\GameClipboard.cs", "Core\Utilities\GameClipboard.cs", "EvenMoreOverpoweredJourney.Core.Utilities"),
    @("Systems\EmojModMetaSystem.cs", "Core\Meta\EmojModMetaSystem.cs", "EvenMoreOverpoweredJourney.Core.Meta"),
    @("Systems\Logging\EmojLog.cs", "Core\Logging\EmojLog.cs", "EvenMoreOverpoweredJourney.Core.Logging"),
    @("Systems\Logging\EmojLogChannel.cs", "Core\Logging\EmojLogChannel.cs", "EvenMoreOverpoweredJourney.Core.Logging"),
    @("Systems\Logging\EmojLogDetail.cs", "Core\Logging\EmojLogDetail.cs", "EvenMoreOverpoweredJourney.Core.Logging"),
    @("Systems\Logging\EmojLogDiagnostics.cs", "Core\Logging\EmojLogDiagnostics.cs", "EvenMoreOverpoweredJourney.Core.Logging"),
    @("Systems\Logging\EmojLogSystem.cs", "Core\Logging\EmojLogSystem.cs", "EvenMoreOverpoweredJourney.Core.Logging"),
    @("Players\BuffResearchPlayer.cs", "Buffs\Players\BuffResearchPlayer.cs", "EvenMoreOverpoweredJourney.Buffs.Players"),
    @("Players\OPJourneyPlayer.cs", "Shell\Players\OPJourneyPlayer.cs", "EvenMoreOverpoweredJourney.Shell.Players"),
    @("Players\ItemHubPlayer.cs", "ItemHub\Players\ItemHubPlayer.cs", "EvenMoreOverpoweredJourney.ItemHub.Players"),
    @("Common\BuffUnlockGlobalBuff.cs", "Buffs\Globals\BuffUnlockGlobalBuff.cs", "EvenMoreOverpoweredJourney.Buffs.Globals"),
    @("Common\BuffUnlockGlobalItem.cs", "Buffs\Globals\BuffUnlockGlobalItem.cs", "EvenMoreOverpoweredJourney.Buffs.Globals"),
    @("Buffs\EMOJAlphaBuff.cs", "Buffs\Content\EMOJAlphaBuff.cs", "EvenMoreOverpoweredJourney.Buffs.Content"),
    @("Buffs\EMOJOmegaBuff.cs", "Buffs\Content\EMOJOmegaBuff.cs", "EvenMoreOverpoweredJourney.Buffs.Content"),
    @("BuffDebugUnlockSupport.cs", "Buffs\Debug\BuffDebugUnlockSupport.cs", "EvenMoreOverpoweredJourney.Buffs.Debug"),
    @("SuperAdminCommands.cs", "SuperAdmin\SuperAdminCommands.cs", "EvenMoreOverpoweredJourney.SuperAdmin"),
    @("Systems\OPJourneyUISystem.cs", "Shell\OPJourneyUISystem.cs", "EvenMoreOverpoweredJourney.Shell"),
    @("UI\OPJourneyUI.cs", "Shell\UI\OPJourneyUI.cs", "EvenMoreOverpoweredJourney.Shell.UI"),
    @("UI\UIGrid.cs", "Shell\UI\UIGrid.cs", "EvenMoreOverpoweredJourney.Shell.UI"),
    @("UI\BorderDrawUtil.cs", "Shell\UI\BorderDrawUtil.cs", "EvenMoreOverpoweredJourney.Shell.UI"),
    @("UI\Components\UIBaseComponents.cs", "Shell\UI\Components\UIBaseComponents.cs", "EvenMoreOverpoweredJourney.Shell.UI.Components"),
    @("UI\Components\UIFaceModeSelector.cs", "Shell\UI\Components\UIFaceModeSelector.cs", "EvenMoreOverpoweredJourney.Shell.UI.Components"),
    @("UI\Components\UIItemSlot.cs", "Shell\UI\Components\UIItemSlot.cs", "EvenMoreOverpoweredJourney.Shell.UI.Components"),
    @("UI\Pages\BuffPage.cs", "Buffs\UI\BuffPage.cs", "EvenMoreOverpoweredJourney.Buffs.UI"),
    @("UI\Pages\BuffSecondaryPanel.cs", "Buffs\UI\BuffSecondaryPanel.cs", "EvenMoreOverpoweredJourney.Buffs.UI"),
    @("UI\Components\UIBuffSlot.cs", "Buffs\UI\Components\UIBuffSlot.cs", "EvenMoreOverpoweredJourney.Buffs.UI.Components"),
    @("UI\Components\UIBuffSearchBar.cs", "Buffs\UI\Components\UIBuffSearchBar.cs", "EvenMoreOverpoweredJourney.Buffs.UI.Components"),
    @("UI\Components\BuffFilterIconButton.cs", "Buffs\UI\Components\BuffFilterIconButton.cs", "EvenMoreOverpoweredJourney.Buffs.UI.Components"),
    @("UI\Pages\ItemHubPage.cs", "ItemHub\UI\ItemHubPage.cs", "EvenMoreOverpoweredJourney.ItemHub.UI"),
    @("UI\Pages\ItemHubSecondaryPanel.cs", "ItemHub\UI\ItemHubSecondaryPanel.cs", "EvenMoreOverpoweredJourney.ItemHub.UI"),
    @("UI\Pages\ItemHubRareRangeStrip.cs", "ItemHub\UI\ItemHubRareRangeStrip.cs", "EvenMoreOverpoweredJourney.ItemHub.UI"),
    @("UI\Pages\ResearchPage.cs", "Research\UI\ResearchPage.cs", "EvenMoreOverpoweredJourney.Research.UI"),
    @("UI\Pages\ResearchPageUI.cs", "Research\UI\ResearchPageUI.cs", "EvenMoreOverpoweredJourney.Research.UI"),
    @("Research\ResearchFaceMode.cs", "Research\ResearchFaceMode.cs", "EvenMoreOverpoweredJourney.Research"),
    @("Research\RecipeAnalyzer.cs", "Research\RecipeAnalyzer.cs", "EvenMoreOverpoweredJourney.Research"),
    @("Research\RecipeEnvironmentHelper.cs", "Research\RecipeEnvironmentHelper.cs", "EvenMoreOverpoweredJourney.Research"),
    @("Integration\ImproveGameIntegration.cs", "Integration\ImproveGame\ImproveGameIntegration.cs", "EvenMoreOverpoweredJourney.Integration.ImproveGame"),
    @("Integration\BuffInfrastructureSettings.cs", "Integration\ImproveGame\BuffInfrastructureSettings.cs", "EvenMoreOverpoweredJourney.Integration.ImproveGame"),
    @("Systems\ImproveGameIntegrationSystem.cs", "Integration\ImproveGame\ImproveGameIntegrationSystem.cs", "EvenMoreOverpoweredJourney.Integration.ImproveGame"),
    @("Integration\SessionActiveBuffExporter.cs", "Integration\Session\SessionActiveBuffExporter.cs", "EvenMoreOverpoweredJourney.Integration.Session"),
    @("Integration\SessionBackpackExporter.cs", "Integration\Session\SessionBackpackExporter.cs", "EvenMoreOverpoweredJourney.Integration.Session"),
    @("Integration\SessionBrowserCatalogCompare.cs", "Integration\Session\SessionBrowserCatalogCompare.cs", "EvenMoreOverpoweredJourney.Integration.Session"),
    @("Integration\VanillaBuffCatalogExporter.cs", "Integration\Session\VanillaBuffCatalogExporter.cs", "EvenMoreOverpoweredJourney.Integration.Session"),
    @("Integration\ExternalBrowserCatalog.cs", "Integration\Browser\ExternalBrowserCatalog.cs", "EvenMoreOverpoweredJourney.Integration.Browser"),
    @("Integration\ExternalBrowserReflection.cs", "Integration\Browser\ExternalBrowserReflection.cs", "EvenMoreOverpoweredJourney.Integration.Browser"),
    # Catalog
    @("Systems\BuffListCatalog.cs", "Buffs\Systems\Catalog\BuffListCatalog.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffCategoryIndexSystem.cs", "Buffs\Systems\Catalog\BuffCategoryIndexSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffStableKey.cs", "Buffs\Systems\Catalog\BuffStableKey.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffDisplayNameHelper.cs", "Buffs\Systems\Catalog\BuffDisplayNameHelper.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffSourceIndexSystem.cs", "Buffs\Systems\Catalog\BuffSourceIndexSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffModCatalogSystem.cs", "Buffs\Systems\Catalog\BuffModCatalogSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffPlayerApplicability.cs", "Buffs\Systems\Catalog\BuffPlayerApplicability.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    @("Systems\BuffSecondaryFilterState.cs", "Buffs\Systems\Catalog\BuffSecondaryFilterState.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"),
    # Virtual
    @("Systems\BuffVirtualEffectSystem.cs", "Buffs\Systems\Virtual\BuffVirtualEffectSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Virtual"),
    @("Systems\BuffVirtualEffectClassifier.cs", "Buffs\Systems\Virtual\BuffVirtualEffectClassifier.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Virtual"),
    @("Systems\BuffVirtualEffectPhase.cs", "Buffs\Systems\Virtual\BuffVirtualEffectPhase.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Virtual"),
    @("Systems\BuffVirtualEffectSafety.cs", "Buffs\Systems\Virtual\BuffVirtualEffectSafety.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Virtual"),
    @("Systems\BuffVirtualEffectSummonGuard.cs", "Buffs\Systems\Virtual\BuffVirtualEffectSummonGuard.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Virtual"),
    # Managed
    @("Systems\BuffManagedReapplySystem.cs", "Buffs\Systems\Managed\BuffManagedReapplySystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Managed"),
    @("Systems\BuffManagedLifecycleSystem.cs", "Buffs\Systems\Managed\BuffManagedLifecycleSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Managed"),
    @("Systems\BuffManagedTimeRules.cs", "Buffs\Systems\Managed\BuffManagedTimeRules.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Managed"),
    @("Systems\BuffWorldTransitionCleanup.cs", "Buffs\Systems\Managed\BuffWorldTransitionCleanup.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Managed"),
    @("Systems\BuffPreserveOnDeathSystem.cs", "Buffs\Systems\Managed\BuffPreserveOnDeathSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Managed"),
    @("Systems\BuffImmunityHookSystem.cs", "Buffs\Systems\Managed\BuffImmunityHookSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Managed"),
    # Combat
    @("Systems\BuffCombatSummonSystem.cs", "Buffs\Systems\Combat\BuffCombatSummonSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Combat"),
    @("Systems\BuffCombatSummonClassifier.cs", "Buffs\Systems\Combat\BuffCombatSummonClassifier.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Combat"),
    @("Systems\BuffSummonProjectileHelper.cs", "Buffs\Systems\Combat\BuffSummonProjectileHelper.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Combat"),
    # Entity
    @("Systems\BuffEntityIndexSystem.cs", "Buffs\Systems\Entity\BuffEntityIndexSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Entity"),
    @("Systems\BuffMountCategorySystem.cs", "Buffs\Systems\Entity\BuffMountCategorySystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Entity"),
    @("Systems\BuffMiscEquipIndexSystem.cs", "Buffs\Systems\Entity\BuffMiscEquipIndexSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Entity"),
    # SetBonus
    @("Systems\SetBonusArmorResolver.cs", "Buffs\Systems\SetBonus\SetBonusArmorResolver.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus"),
    @("Systems\SetBonusHookSystem.cs", "Buffs\Systems\SetBonus\SetBonusHookSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus"),
    # ModSupport
    @("Systems\BuffModSupportSystem.cs", "Buffs\Systems\ModSupport\BuffModSupportSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport"),
    @("Systems\BuffModSupportLoader.cs", "Buffs\Systems\ModSupport\BuffModSupportLoader.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport"),
    @("Systems\BuffModProfileLoader.cs", "Buffs\Systems\ModSupport\BuffModProfileLoader.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport"),
    @("Systems\VanillaBuffCatalogSystem.cs", "Buffs\Systems\ModSupport\VanillaBuffCatalogSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport"),
    @("Systems\VanillaBuffStatRegistry.cs", "Buffs\Systems\ModSupport\VanillaBuffStatRegistry.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport"),
    @("Systems\VanillaBuffSupportMode.cs", "Buffs\Systems\ModSupport\VanillaBuffSupportMode.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport"),
    # FedState / Diagnostics / Display
    @("Systems\BuffFedStateCompat.cs", "Buffs\Systems\FedState\BuffFedStateCompat.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.FedState"),
    @("Systems\BuffFedStateHookSystem.cs", "Buffs\Systems\FedState\BuffFedStateHookSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.FedState"),
    @("Systems\BuffBulkSkipDiagnostics.cs", "Buffs\Systems\Diagnostics\BuffBulkSkipDiagnostics.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics"),
    @("Systems\BuffEmoteGuardSystem.cs", "Buffs\Systems\Display\BuffEmoteGuardSystem.cs", "EvenMoreOverpoweredJourney.Buffs.Systems.Display")
)

foreach ($m in $emojMoves) { Move-SetNamespace -From $m[0] -To $m[1] -Namespace $m[2] }

# Global usings
$globalUsings = @"
global using EvenMoreOverpoweredJourney.Core.Config;
global using EvenMoreOverpoweredJourney.Core.Localization;
global using EvenMoreOverpoweredJourney.Core.Logging;
global using EvenMoreOverpoweredJourney.Core.Utilities;
global using EvenMoreOverpoweredJourney.Buffs.Players;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Managed;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Combat;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Entity;
global using EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;
global using EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;
global using EvenMoreOverpoweredJourney.Buffs.Systems.FedState;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;
global using EvenMoreOverpoweredJourney.Buffs.Systems.Display;
global using EvenMoreOverpoweredJourney.Buffs.UI;
global using EvenMoreOverpoweredJourney.ItemHub.Catalog;
global using EvenMoreOverpoweredJourney.ItemHub.Filters;
global using EvenMoreOverpoweredJourney.ItemHub.Rules;
global using EvenMoreOverpoweredJourney.ItemHub.Data;
global using EvenMoreOverpoweredJourney.Integration.ImproveGame;
global using EvenMoreOverpoweredJourney.Integration.Session;
global using EvenMoreOverpoweredJourney.Integration.Browser;
"@
[System.IO.File]::WriteAllText((Join-Path $EmojRoot "GlobalUsings.cs"), $globalUsings.Trim() + "`n", [System.Text.UTF8Encoding]::new($false))
Remove-Item (Join-Path $EmojRoot "GlobalUsings.ItemHub.cs") -ErrorAction SilentlyContinue

# Namespace replacements (longest first)
$replacements = [ordered]@{
    "EvenMoreOverpoweredJourney.Systems.Logging" = "EvenMoreOverpoweredJourney.Core.Logging"
    "EvenMoreOverpoweredJourney.UI.Pages" = "EvenMoreOverpoweredJourney.Buffs.UI"
    "EvenMoreOverpoweredJourney.UI.Components" = "EvenMoreOverpoweredJourney.Buffs.UI.Components"
    "EvenMoreOverpoweredJourney.Common.Utils" = "EvenMoreOverpoweredJourney.Core.Utilities"
    "EvenMoreOverpoweredJourney.Common" = "EvenMoreOverpoweredJourney.Buffs.Globals"
    "EvenMoreOverpoweredJourney.Buffs" = "EvenMoreOverpoweredJourney.Buffs.Content"
    "EvenMoreOverpoweredJourney.Integration" = "EvenMoreOverpoweredJourney.Integration.Session"
    "EvenMoreOverpoweredJourney.Systems" = "EvenMoreOverpoweredJourney.Buffs.Systems.Catalog"
}
# Fix bad replacements - do targeted replaces only in using/namespace contexts via explicit list
$nsMap = @{
    "EvenMoreOverpoweredJourney.Systems.Logging" = "EvenMoreOverpoweredJourney.Core.Logging"
    "using EvenMoreOverpoweredJourney.Systems;" = "using EvenMoreOverpoweredJourney.Buffs.Systems.Catalog;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.Virtual;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.Managed;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.Combat;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.Entity;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.SetBonus;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.ModSupport;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.FedState;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.Diagnostics;`nusing EvenMoreOverpoweredJourney.Buffs.Systems.Display;"
    "using EvenMoreOverpoweredJourney.UI.Pages;" = "using EvenMoreOverpoweredJourney.Buffs.UI;`nusing EvenMoreOverpoweredJourney.ItemHub.UI;`nusing EvenMoreOverpoweredJourney.Research.UI;`nusing EvenMoreOverpoweredJourney.Shell.UI;"
    "using EvenMoreOverpoweredJourney.UI.Components;" = "using EvenMoreOverpoweredJourney.Buffs.UI.Components;`nusing EvenMoreOverpoweredJourney.Shell.UI.Components;"
    "using EvenMoreOverpoweredJourney.UI;" = "using EvenMoreOverpoweredJourney.Shell.UI;"
    "using EvenMoreOverpoweredJourney.Common;" = "using EvenMoreOverpoweredJourney.Buffs.Globals;"
    "using EvenMoreOverpoweredJourney.Integration;" = "using EvenMoreOverpoweredJourney.Integration.ImproveGame;`nusing EvenMoreOverpoweredJourney.Integration.Session;`nusing EvenMoreOverpoweredJourney.Integration.Browser;"
    "using EvenMoreOverpoweredJourney.Config;" = "using EvenMoreOverpoweredJourney.Core.Config;"
    "using EvenMoreOverpoweredJourney.Buffs;" = "using EvenMoreOverpoweredJourney.Buffs.Content;"
    "namespace EvenMoreOverpoweredJourney.Buffs`n" = "namespace EvenMoreOverpoweredJourney.Buffs.Content`n"
}
Get-ChildItem $EmojRoot -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\|_tools\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    foreach ($k in $nsMap.Keys) { $t = $t.Replace($k, $nsMap[$k]) }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t, [System.Text.UTF8Encoding]::new($false)) }
}

Write-Host "EMOJ file moves done."
