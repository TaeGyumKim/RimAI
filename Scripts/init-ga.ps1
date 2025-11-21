#!/usr/bin/env pwsh
# RimAI GA Initialization Script - PowerShell
# Usage: .\init-ga.ps1 -PopulationSize 20

param(
    [int]$PopulationSize = 20,
    [int]$Generation = 0
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ExperimentDir = Join-Path $RootDir "ga_experiments\generation_$Generation"
$GATrainerExe = Join-Path $RootDir "GATrainer\bin\Debug\net472\GATrainer.exe"

Write-Host "=== RimAI GA Initialization ===" -ForegroundColor Cyan
Write-Host "Generation: $Generation" -ForegroundColor Yellow
Write-Host "Population Size: $PopulationSize" -ForegroundColor Yellow
Write-Host ""

# Validate GATrainer
if (-not (Test-Path $GATrainerExe)) {
    Write-Error "GATrainer.exe not found at: $GATrainerExe"
    Write-Host "Building GATrainer..." -ForegroundColor Yellow

    Push-Location (Join-Path $RootDir "GATrainer")
    try {
        dotnet build
    } finally {
        Pop-Location
    }

    if (-not (Test-Path $GATrainerExe)) {
        Write-Error "Failed to build GATrainer"
        exit 1
    }
}

# Create experiment directory
Write-Host "[1/2] Creating experiment directory..." -ForegroundColor Green

$genomesDir = Join-Path $ExperimentDir "genomes"
$runsDir = Join-Path $ExperimentDir "runs"

if (Test-Path $ExperimentDir) {
    Write-Warning "Experiment directory already exists: $ExperimentDir"
    Write-Host "Do you want to overwrite? (y/N): " -NoNewline -ForegroundColor Yellow
    $response = Read-Host

    if ($response -ne "y" -and $response -ne "Y") {
        Write-Host "Aborted." -ForegroundColor Red
        exit 1
    }

    Remove-Item -Recurse -Force $ExperimentDir
}

New-Item -ItemType Directory -Force -Path $genomesDir | Out-Null
New-Item -ItemType Directory -Force -Path $runsDir | Out-Null

Write-Host "Created: $ExperimentDir" -ForegroundColor White
Write-Host ""

# Generate initial population
Write-Host "[2/2] Generating initial population..." -ForegroundColor Green

& $GATrainerExe init $PopulationSize $genomesDir

Write-Host ""
Write-Host "=== Complete ===" -ForegroundColor Cyan
Write-Host "Initial population created at: $genomesDir" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review generated genomes in: $genomesDir" -ForegroundColor White
Write-Host "  2. Run GA cycle: .\run-ga-cycle.ps1 -Generation $Generation" -ForegroundColor White
Write-Host ""
