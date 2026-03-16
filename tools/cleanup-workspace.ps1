[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("report", "safe", "deep")]
    [string]$Mode = "report",
    [switch]$IncludeLibrary,
    [switch]$IncludeVenv,
    [switch]$IncludeRootLogs,
    [switch]$FullUvCacheClean
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Get-DirectoryBytes([string]$Path) {
    if (-not (Test-Path $Path)) {
        return 0L
    }

    $sum = (
        Get-ChildItem $Path -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer } |
        Measure-Object Length -Sum
    ).Sum

    if ($null -eq $sum) {
        return 0L
    }

    return [int64]$sum
}

function Get-FilesBytes([object[]]$Files) {
    if (-not $Files -or $Files.Count -eq 0) {
        return 0L
    }

    $sum = ($Files | Measure-Object Length -Sum).Sum
    if ($null -eq $sum) {
        return 0L
    }

    return [int64]$sum
}

function Format-Bytes([int64]$Bytes) {
    if ($Bytes -ge 1GB) {
        return ("{0:N2} GB" -f ($Bytes / 1GB))
    }
    if ($Bytes -ge 1MB) {
        return ("{0:N1} MB" -f ($Bytes / 1MB))
    }
    if ($Bytes -ge 1KB) {
        return ("{0:N1} KB" -f ($Bytes / 1KB))
    }
    return ("{0} B" -f $Bytes)
}

function Remove-DirectoryIfPresent([string]$RelativePath, [string]$Reason) {
    if (-not (Test-Path $RelativePath)) {
        return
    }

    if ($PSCmdlet.ShouldProcess($RelativePath, "Remove directory ($Reason)")) {
        Remove-Item -LiteralPath $RelativePath -Recurse -Force
        Write-Host ("Removed directory: {0}" -f $RelativePath)
    }
}

function Remove-FilesIfPresent([object[]]$Files, [string]$Reason) {
    if (-not $Files -or $Files.Count -eq 0) {
        return
    }

    foreach ($file in $Files) {
        if ($PSCmdlet.ShouldProcess($file.FullName, "Remove file ($Reason)")) {
            Remove-Item -LiteralPath $file.FullName -Force
            Write-Host ("Removed file: {0}" -f $file.FullName)
        }
    }
}

function Invoke-UvCacheAction([string]$Action) {
    $uv = Get-Command uv -ErrorAction SilentlyContinue
    if (-not $uv) {
        Write-Warning "uv was not found on PATH, so cache maintenance was skipped."
        return
    }

    $cacheDir = Join-Path $repoRoot ".uv-cache"
    if (-not (Test-Path $cacheDir)) {
        return
    }

    $description = if ($Action -eq "clean") { "Clear local uv cache" } else { "Prune local uv cache" }
    if ($PSCmdlet.ShouldProcess($cacheDir, $description)) {
        $args = @("cache", $Action, "--cache-dir", $cacheDir)
        if ($Action -eq "clean") {
            $args += "--force"
        }
        & $uv.Source @args
        if ($LASTEXITCODE -ne 0) {
            throw "uv cache $Action failed with exit code $LASTEXITCODE."
        }
    }
}

$rootLogs = Get-ChildItem $repoRoot -File -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -in @(".log", ".pid") }

$reportRows = @(
    [pscustomobject]@{
        Target = ".uv-cache"
        Size = Format-Bytes (Get-DirectoryBytes ".uv-cache")
        DefaultMode = if ($FullUvCacheClean) { "safe/deep (full clean)" } else { "safe/deep (prune)" }
        Notes = "Safe to trim regularly. Full clean may slow the next install."
    }
    [pscustomobject]@{
        Target = "Temp"
        Size = Format-Bytes (Get-DirectoryBytes "Temp")
        DefaultMode = "safe/deep"
        Notes = "Root temporary files."
    }
    [pscustomobject]@{
        Target = "AI-RL-Movie/Temp"
        Size = Format-Bytes (Get-DirectoryBytes "AI-RL-Movie\\Temp")
        DefaultMode = "safe/deep"
        Notes = "Unity temp output that can be regenerated."
    }
    [pscustomobject]@{
        Target = "AI-RL-Movie/Logs"
        Size = Format-Bytes (Get-DirectoryBytes "AI-RL-Movie\\Logs")
        DefaultMode = "safe/deep"
        Notes = "Unity logs."
    }
    [pscustomobject]@{
        Target = ".tmp-training-results"
        Size = Format-Bytes (Get-DirectoryBytes ".tmp-training-results")
        DefaultMode = "safe/deep"
        Notes = "Local scratch training output."
    }
    [pscustomobject]@{
        Target = "AI-RL-Movie/ColabBuilds"
        Size = Format-Bytes (Get-DirectoryBytes "AI-RL-Movie\\ColabBuilds")
        DefaultMode = "deep"
        Notes = "Generated builds; remove when old uploads are no longer needed."
    }
    [pscustomobject]@{
        Target = "root *.log / *.pid"
        Size = Format-Bytes (Get-FilesBytes $rootLogs)
        DefaultMode = "deep + -IncludeRootLogs"
        Notes = "Useful for short-term debugging, but often disposable."
    }
    [pscustomobject]@{
        Target = "AI-RL-Movie/Library"
        Size = Format-Bytes (Get-DirectoryBytes "AI-RL-Movie\\Library")
        DefaultMode = "optional + -IncludeLibrary"
        Notes = "Huge and regenerable, but Unity will take time to rebuild it."
    }
    [pscustomobject]@{
        Target = ".venv-mlagents"
        Size = Format-Bytes (Get-DirectoryBytes ".venv-mlagents")
        DefaultMode = "optional + -IncludeVenv"
        Notes = "Python environment. Deleting it saves space but requires reinstall."
    }
    [pscustomobject]@{
        Target = ".venv-mlagents-1.1.0"
        Size = Format-Bytes (Get-DirectoryBytes ".venv-mlagents-1.1.0")
        DefaultMode = "optional + -IncludeVenv"
        Notes = "Legacy venv path if it still exists."
    }
)

Write-Host ""
Write-Host "Cleanup candidates"
$reportRows | Format-Table -AutoSize

if ($Mode -eq "report") {
    Write-Host ""
    Write-Host "No files were removed. Re-run with -Mode safe or -Mode deep."
    return
}

Write-Host ""
Write-Host ("Running cleanup mode: {0}" -f $Mode)

Invoke-UvCacheAction ($(if ($FullUvCacheClean) { "clean" } else { "prune" }))
Remove-DirectoryIfPresent "Temp" "root temp"
Remove-DirectoryIfPresent "AI-RL-Movie\\Temp" "Unity temp"
Remove-DirectoryIfPresent "AI-RL-Movie\\Logs" "Unity logs"
Remove-DirectoryIfPresent ".tmp-training-results" "local training scratch output"

if ($Mode -eq "deep") {
    Remove-DirectoryIfPresent "AI-RL-Movie\\ColabBuilds" "generated Colab build output"

    if ($IncludeRootLogs) {
        Remove-FilesIfPresent $rootLogs "root log and pid cleanup"
    }
}

if ($IncludeLibrary) {
    Remove-DirectoryIfPresent "AI-RL-Movie\\Library" "Unity Library rebuild"
}

if ($IncludeVenv) {
    Remove-DirectoryIfPresent ".venv-mlagents" "active ML-Agents venv"
    Remove-DirectoryIfPresent ".venv-mlagents-1.1.0" "legacy ML-Agents venv"
}

Write-Host ""
Write-Host "Cleanup complete."
