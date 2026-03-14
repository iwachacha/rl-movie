param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [string]$ResultsDir = ".tmp-training-results",
    [string]$VenvDir = ".venv-mlagents-1.1.0",
    [string]$EnvPath,
    [int]$Seed = 1,
    [switch]$Resume,
    [switch]$NoGraphics,
    [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath([string]$PathValue) {
    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return Join-Path $repoRoot $PathValue
}

$resolvedConfigPath = Resolve-RepoPath $ConfigPath
$resolvedResultsDir = Resolve-RepoPath $ResultsDir
$resolvedVenvDir = Resolve-RepoPath $VenvDir
$resolvedEnvPath = Resolve-RepoPath $EnvPath
$pythonExe = Join-Path $resolvedVenvDir "Scripts\python.exe"

if (-not (Test-Path $pythonExe)) {
    throw "Python environment not found: $pythonExe`nRun .\Notebooks\setup_local_training_env.ps1 first."
}

if (-not (Test-Path $resolvedConfigPath)) {
    throw "Config YAML not found: $resolvedConfigPath"
}

New-Item -ItemType Directory -Force -Path $resolvedResultsDir | Out-Null

$command = @(
    "-m",
    "mlagents.trainers.learn",
    $resolvedConfigPath,
    "--run-id=$RunId",
    "--results-dir=$resolvedResultsDir",
    "--seed=$Seed"
)

if ($Resume) {
    $command += "--resume"
}
else {
    $command += "--force"
}

if ($resolvedEnvPath) {
    if (-not (Test-Path $resolvedEnvPath)) {
        throw "Unity environment executable not found: $resolvedEnvPath"
    }

    $command += "--env=$resolvedEnvPath"

    if ($NoGraphics) {
        $command += "--no-graphics"
    }
}

if ($ExtraArgs.Count -gt 0) {
    $command += $ExtraArgs
}

Write-Host "Launching ML-Agents"
Write-Host ("  " + $pythonExe + " " + ($command -join " "))

if (-not $resolvedEnvPath) {
    Write-Host "Editor mode: run command first, then press Play in Unity with Behavior Type = Default."
}

& $pythonExe @command
exit $LASTEXITCODE
