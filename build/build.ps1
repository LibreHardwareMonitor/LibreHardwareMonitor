param(
  [string]$Configuration = "Release",
  [string]$Solution = "LibreHardwareMonitor.sln"
)

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$SolutionPath = Join-Path $ScriptRoot "..\$Solution"
$Artifacts = Join-Path $ScriptRoot "..\artifacts\$Configuration"

if (Test-Path $Artifacts) {
    Remove-Item $Artifacts -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $Artifacts | Out-Null

Write-Host "Building solution: $SolutionPath (Configuration=$Configuration)"

if (Get-Command msbuild -ErrorAction SilentlyContinue) {
    & msbuild $SolutionPath /p:Configuration=$Configuration /m
    $exit = $LASTEXITCODE
} elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    & dotnet build $SolutionPath -c $Configuration
    $exit = $LASTEXITCODE
} else {
    Write-Error "Neither 'msbuild' nor 'dotnet' were found on PATH. Install Visual Studio Build Tools or .NET SDK."
    exit 1
}

if ($exit -ne 0) { Write-Error "Build failed with exit code $exit"; exit $exit }

# Copy library outputs to artifacts
$libOut = Join-Path $ScriptRoot "..\LibreHardwareMonitorLib\bin\$Configuration"
if (Test-Path $libOut) {
    Write-Host "Copying outputs from $libOut to $Artifacts"
    Copy-Item -Path (Join-Path $libOut "*") -Destination $Artifacts -Recurse -Force
} else {
    Write-Warning "Library output path not found: $libOut. You may need to adjust the script for custom project paths." 
}

Write-Host "Build complete. Artifacts are in: $Artifacts"
