# build-mod.ps1
# RimAI 모드 빌드 스크립트 (Windows PowerShell)

param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Deploy,
    [string]$RimWorldPath = ""
)

$ErrorActionPreference = "Stop"

# 색상 출력 함수
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }
function Write-Warn { param($msg) Write-Host $msg -ForegroundColor Yellow }

Write-Info "=========================================="
Write-Info "  RimAI Mod Build Script"
Write-Info "=========================================="

# 프로젝트 루트 찾기
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$SourceDir = Join-Path $ProjectRoot "Source\RimAI"
$AssembliesDir = Join-Path $ProjectRoot "Assemblies"

Write-Info "Project Root: $ProjectRoot"
Write-Info "Configuration: $Configuration"

# Clean 옵션
if ($Clean) {
    Write-Info "`nCleaning build outputs..."

    $binDir = Join-Path $SourceDir "bin"
    $objDir = Join-Path $SourceDir "obj"

    if (Test-Path $binDir) { Remove-Item -Recurse -Force $binDir }
    if (Test-Path $objDir) { Remove-Item -Recurse -Force $objDir }
    if (Test-Path $AssembliesDir) { Remove-Item -Recurse -Force $AssembliesDir }

    Write-Success "Clean completed."
}

# Assemblies 디렉토리 생성
if (!(Test-Path $AssembliesDir)) {
    New-Item -ItemType Directory -Path $AssembliesDir | Out-Null
}

# 빌드 실행
Write-Info "`nBuilding RimAI..."
Push-Location $SourceDir

try {
    dotnet build -c $Configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    Write-Success "Build succeeded!"
}
finally {
    Pop-Location
}

# DLL 복사
Write-Info "`nCopying DLL to Assemblies..."
$dllSource = Join-Path $SourceDir "bin\$Configuration\net472\RimAI.dll"
$dllDest = Join-Path $AssembliesDir "RimAI.dll"

if (Test-Path $dllSource) {
    Copy-Item $dllSource $dllDest -Force
    Write-Success "DLL copied: $dllDest"
} else {
    Write-Warn "Warning: DLL not found at $dllSource"
}

# Deploy 옵션 (RimWorld Mods 폴더로 복사)
if ($Deploy) {
    Write-Info "`nDeploying to RimWorld Mods folder..."

    # RimWorld 경로 자동 탐지
    if ([string]::IsNullOrEmpty($RimWorldPath)) {
        $possiblePaths = @(
            "C:\Program Files (x86)\Steam\steamapps\common\RimWorld",
            "C:\Program Files\Steam\steamapps\common\RimWorld",
            "$env:USERPROFILE\Games\RimWorld"
        )

        foreach ($path in $possiblePaths) {
            if (Test-Path $path) {
                $RimWorldPath = $path
                break
            }
        }
    }

    if ([string]::IsNullOrEmpty($RimWorldPath)) {
        Write-Warn "Warning: RimWorld path not found. Specify with -RimWorldPath"
    } else {
        $modsPath = Join-Path $RimWorldPath "Mods\RimAI"

        # 기존 모드 폴더 삭제 후 복사
        if (Test-Path $modsPath) {
            Remove-Item -Recurse -Force $modsPath
        }

        # 필요한 폴더만 복사 (소스 제외)
        $foldersToC = @("About", "Assemblies", "Defs", "Languages", "Patches", "Textures")
        New-Item -ItemType Directory -Path $modsPath | Out-Null

        foreach ($folder in $foldersToC) {
            $srcFolder = Join-Path $ProjectRoot $folder
            if (Test-Path $srcFolder) {
                Copy-Item -Recurse $srcFolder $modsPath
            }
        }

        Write-Success "Deployed to: $modsPath"
    }
}

# 빌드 정보 출력
Write-Info "`n=========================================="
Write-Info "  Build Summary"
Write-Info "=========================================="

if (Test-Path $dllDest) {
    $dllInfo = Get-Item $dllDest
    Write-Info "DLL: $($dllInfo.FullName)"
    Write-Info "Size: $([math]::Round($dllInfo.Length / 1KB, 2)) KB"
    Write-Info "Modified: $($dllInfo.LastWriteTime)"
}

Write-Success "`nBuild completed successfully!"
