#!/usr/bin/env pwsh
# RimAI GA Cycle Runner - PowerShell Script
# Usage: .\run-ga-cycle.ps1 -Generation 0 -PopulationSize 20 -EliteCount 3 -MutationRate 0.15

param(
    [Parameter(Mandatory=$true)]
    [int]$Generation,

    [int]$PopulationSize = 20,
    [int]$EliteCount = 3,
    [double]$MutationRate = 0.15,
    [string]$SelectionMethod = "tournament",
    [switch]$SkipGamePlay,
    [switch]$OnlyStats
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ConfigDir = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\RimAI\GA"
$ExperimentDir = Join-Path $RootDir "ga_experiments\generation_$Generation"
$NextGeneration = $Generation + 1
$NextExperimentDir = Join-Path $RootDir "ga_experiments\generation_$NextGeneration"
$GATrainerExe = Join-Path $RootDir "GATrainer\bin\Debug\net472\GATrainer.exe"

Write-Host "=== RimAI GA Cycle Runner ===" -ForegroundColor Cyan
Write-Host "Generation: $Generation -> $NextGeneration" -ForegroundColor Yellow
Write-Host "Population Size: $PopulationSize" -ForegroundColor Yellow
Write-Host "Elite Count: $EliteCount" -ForegroundColor Yellow
Write-Host "Mutation Rate: $MutationRate" -ForegroundColor Yellow
Write-Host "Selection Method: $SelectionMethod" -ForegroundColor Yellow
Write-Host ""

# Validate paths
if (-not (Test-Path $GATrainerExe)) {
    Write-Error "GATrainer.exe not found at: $GATrainerExe"
    Write-Host "Please build GATrainer first: cd GATrainer && dotnet build"
    exit 1
}

if (-not (Test-Path $ExperimentDir)) {
    Write-Error "Experiment directory not found: $ExperimentDir"
    Write-Host "Please create it or run 'init' command first"
    exit 1
}

# Only show stats
if ($OnlyStats) {
    Write-Host "[1/5] Statistics Only Mode" -ForegroundColor Green
    Write-Host "Loading generation $Generation statistics..." -ForegroundColor White

    $genomesDir = Join-Path $ExperimentDir "genomes"
    $runsDir = Join-Path $ExperimentDir "runs"

    & $GATrainerExe stats $genomesDir $runsDir
    exit 0
}

# Step 1: Copy genomes to RimWorld config
if (-not $SkipGamePlay) {
    Write-Host "[1/5] Copying genomes to RimWorld config..." -ForegroundColor Green

    $genomesDir = Join-Path $ExperimentDir "genomes"
    $targetGenomesDir = Join-Path $ConfigDir "genomes"

    if (-not (Test-Path $targetGenomesDir)) {
        New-Item -ItemType Directory -Force -Path $targetGenomesDir | Out-Null
    }

    $genomeFiles = Get-ChildItem -Path $genomesDir -Filter "gen${Generation}_*.json"

    if ($genomeFiles.Count -eq 0) {
        Write-Error "No genome files found in: $genomesDir"
        exit 1
    }

    foreach ($file in $genomeFiles) {
        Copy-Item $file.FullName $targetGenomesDir -Force
    }

    Write-Host "Copied $($genomeFiles.Count) genome files" -ForegroundColor White
    Write-Host ""

    # Step 2: Game play instructions
    Write-Host "[2/5] Game Play Phase" -ForegroundColor Green
    Write-Host "Please play RimWorld with each genome:" -ForegroundColor White
    Write-Host ""

    $counter = 1
    foreach ($file in $genomeFiles) {
        Write-Host "  [$counter/$($genomeFiles.Count)] $($file.Name)" -ForegroundColor Yellow
        Write-Host "    -> Copy to current.json: Copy-Item '$($file.FullName)' '$targetGenomesDir\current.json'" -ForegroundColor DarkGray
        Write-Host "    -> Launch RimWorld -> New Game -> Play until ending/wipe" -ForegroundColor DarkGray
        Write-Host "    -> Metrics will be saved automatically to runs/" -ForegroundColor DarkGray
        Write-Host ""
        $counter++
    }

    Write-Host "Press Enter when all games are finished..." -ForegroundColor Cyan
    Read-Host

    # Step 3: Collect metrics
    Write-Host "[3/5] Collecting metrics..." -ForegroundColor Green

    $sourceRunsDir = Join-Path $ConfigDir "runs"
    $targetRunsDir = Join-Path $ExperimentDir "runs"

    if (-not (Test-Path $targetRunsDir)) {
        New-Item -ItemType Directory -Force -Path $targetRunsDir | Out-Null
    }

    if (Test-Path $sourceRunsDir) {
        $metricsFiles = Get-ChildItem -Path $sourceRunsDir -Filter "*.json"

        if ($metricsFiles.Count -eq 0) {
            Write-Warning "No metrics files found in: $sourceRunsDir"
        } else {
            foreach ($file in $metricsFiles) {
                Copy-Item $file.FullName $targetRunsDir -Force
            }
            Write-Host "Collected $($metricsFiles.Count) metrics files" -ForegroundColor White
        }
    } else {
        Write-Warning "Runs directory not found: $sourceRunsDir"
    }

    Write-Host ""
} else {
    Write-Host "[1-3/5] Skipping game play phase (--SkipGamePlay)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 4: Show statistics
Write-Host "[4/5] Generation $Generation Statistics" -ForegroundColor Green

$genomesDir = Join-Path $ExperimentDir "genomes"
$runsDir = Join-Path $ExperimentDir "runs"

& $GATrainerExe stats $genomesDir $runsDir

Write-Host ""

# Step 5: Generate next generation
Write-Host "[5/5] Generating next generation..." -ForegroundColor Green

$nextGenomesDir = Join-Path $NextExperimentDir "genomes"
$nextRunsDir = Join-Path $NextExperimentDir "runs"

if (-not (Test-Path $nextGenomesDir)) {
    New-Item -ItemType Directory -Force -Path $nextGenomesDir | Out-Null
}

if (-not (Test-Path $nextRunsDir)) {
    New-Item -ItemType Directory -Force -Path $nextRunsDir | Out-Null
}

& $GATrainerExe evolve `
    $genomesDir `
    $runsDir `
    $nextGenomesDir `
    --size=$PopulationSize `
    --elite=$EliteCount `
    --mutation=$MutationRate `
    --selection=$SelectionMethod

Write-Host ""
Write-Host "=== Complete ===" -ForegroundColor Cyan
Write-Host "Generation $NextGeneration is ready at: $nextGenomesDir" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review next generation genomes in: $nextGenomesDir" -ForegroundColor White
Write-Host "  2. Run next cycle: .\run-ga-cycle.ps1 -Generation $NextGeneration" -ForegroundColor White
Write-Host ""
