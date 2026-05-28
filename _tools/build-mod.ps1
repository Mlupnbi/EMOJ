# 模组打包前检查 .tmod 是否被占用（TML003 常见原因）
param(
    [string]$ModRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$CompileOnly
)

$tmod = Join-Path $env:LOCALAPPDATA "..\.." 
$tmod = [IO.Path]::GetFullPath((Join-Path $ModRoot "..\..\Mods\EvenMoreOverpoweredJourney.tmod"))
# ModSources/EvenMoreOverpoweredJourney -> tModLoader/Mods
$tmod = [IO.Path]::GetFullPath("C:\Users\14451\Documents\My Games\Terraria\tModLoader\Mods\EvenMoreOverpoweredJourney.tmod")

function Test-TmodWritable {
    if (-not (Test-Path $tmod)) { return $true }
    try {
        $fs = [IO.File]::Open($tmod, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        $fs.Close()
        return $true
    } catch {
        return $false
    }
}

function Get-TmodLoaderDotnetProcesses {
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*tModLoader*' }
}

function Invoke-CompileOnly {
    param([string]$ProjectPath)
    dotnet build $ProjectPath -p:BuildMod=false -p:TargetFramework=net8.0 -p:LangVersion=12.0
}

Write-Host "Checking: $tmod"
$locked = -not (Test-TmodWritable)
if ($locked) {
    Write-Host ""
    Write-Host "[BLOCKED] EvenMoreOverpoweredJourney.tmod is locked (TML003)." -ForegroundColor Red
    Write-Host "C# 编译本身通常没问题；失败发生在写入 .tmod 打包阶段。" -ForegroundColor DarkYellow
    Write-Host ""
    Write-Host "Fix options:" -ForegroundColor Yellow
    Write-Host "  1. 游戏开着时：在 tML 里 Mod Sources -> Build + Reload（不要用 Cursor/dotnet build）"
    Write-Host "  2. 要在外部打包：Steam 库 -> 右键 tModLoader -> 停止（不要只关窗口）"
    Write-Host "  3. 游戏内先禁用本模组 -> 完全退出 -> 再 dotnet build"
    Write-Host "  4. 仅检查语法：_tools/build-mod.ps1 -CompileOnly（不生成 .tmod）"
    Write-Host ""
    $procs = Get-TmodLoaderDotnetProcesses
    if ($procs) {
        Write-Host "检测到 tModLoader 仍在运行（会占用 .tmod）：" -ForegroundColor Yellow
        $procs | ForEach-Object { Write-Host "  PID $($_.ProcessId): $($_.CommandLine)" }
    } else {
        Write-Host "未找到 tModLoader 进程，但文件仍被锁定；可尝试重启 Steam。" -ForegroundColor Yellow
    }
    Write-Host ""
    if ($CompileOnly) {
        Write-Host "CompileOnly: 跳过 .tmod 打包，仅编译 DLL..." -ForegroundColor Cyan
        Invoke-CompileOnly (Join-Path $ModRoot "EvenMoreOverpoweredJourney.csproj")
        exit $LASTEXITCODE
    }
    exit 1
}

if ($CompileOnly) {
    Write-Host "CompileOnly: 跳过 .tmod 打包..." -ForegroundColor Cyan
    Invoke-CompileOnly (Join-Path $ModRoot "EvenMoreOverpoweredJourney.csproj")
    exit $LASTEXITCODE
}

Write-Host "tmod writable. Building..." -ForegroundColor Green
dotnet build (Join-Path $ModRoot "EvenMoreOverpoweredJourney.csproj")
exit $LASTEXITCODE
