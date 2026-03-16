param(
    [string]$VenvDir = ".venv-mlagents",
    [string]$PythonVersion = "3.10.12",
    [switch]$Recreate
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$requirementsPath = Join-Path $PSScriptRoot "mlagents-training-requirements.txt"
$venvPath = if ([System.IO.Path]::IsPathRooted($VenvDir)) { $VenvDir } else { Join-Path $repoRoot $VenvDir }
$pythonExe = Join-Path $venvPath "Scripts\python.exe"

if (-not (Test-Path $requirementsPath)) {
    throw "Requirements file not found: $requirementsPath"
}

if ($Recreate -and (Test-Path $venvPath)) {
    Remove-Item -Recurse -Force $venvPath
}

if (-not (Test-Path $venvPath)) {
    Write-Host "Creating virtual environment at $venvPath"
    uv venv $venvPath --python $PythonVersion
}

Write-Host "Installing pinned ML-Agents/export dependencies"
uv pip install --python $pythonExe -r $requirementsPath

Write-Host "Validating imports"
$validateScript = "from importlib.metadata import version; import mlagents, mlagents_envs, torch, onnx; print('mlagents=' + version('mlagents')); print('mlagents_envs=' + version('mlagents_envs')); print('torch=' + torch.__version__); print('onnx=' + onnx.__version__); print('protobuf=' + version('protobuf'))"
& $pythonExe -c $validateScript

Write-Host ""
Write-Host "Environment ready."
Write-Host "Next steps:"
Write-Host "  1. Local Editor training: .\Notebooks\run_local_training.ps1 -ConfigPath <path-to-config.yaml> -RunId <scenario_slug>__v1__local-check__$(Get-Date -Format yyyyMMdd_HHmm)"
Write-Host "  2. Local build training:  .\Notebooks\run_local_training.ps1 -ConfigPath <path-to-config.yaml> -RunId <scenario_slug>__v1__local-build__$(Get-Date -Format yyyyMMdd_HHmm) -EnvPath <path-to-linux-build> -NoGraphics"
