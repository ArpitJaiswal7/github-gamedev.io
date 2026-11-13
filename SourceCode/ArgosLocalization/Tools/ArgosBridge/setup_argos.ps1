# Provision .argos_venv + Argos Translate + offline language packs (Windows).
# Run from the Unity project root:  powershell -ExecutionPolicy Bypass -File Tools\ArgosBridge\setup_argos.ps1
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

$pythonCmd = $null
foreach ($candidate in @("py -3.11", "py -3.12", "py -3.10", "python", "py -3")) {
    try {
        $ver = (Invoke-Expression "$candidate --version 2>&1") | Out-String
        if ($ver -match "Python 3\.(10|11|12|13)") { $pythonCmd = $candidate; break }
    } catch {}
}
if (-not $pythonCmd) {
    Write-Host "Installing Python 3.11 via winget..."
    winget install -e --id Python.Python.3.11 --scope user --accept-package-agreements --accept-source-agreements
    $pythonCmd = "$env:LOCALAPPDATA\Programs\Python\Python311\python.exe"
}

Invoke-Expression "$pythonCmd -m venv .argos_venv"
$py = Join-Path $root ".argos_venv\Scripts\python.exe"
& $py -m pip install --upgrade pip setuptools wheel
& $py -m pip install argostranslate
& $py (Join-Path $PSScriptRoot "precache_models.py")
