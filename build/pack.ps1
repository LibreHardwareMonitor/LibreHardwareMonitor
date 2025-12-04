param(
  [string]$Configuration = "Release",
  [string]$Project = "LibreHardwareMonitorLib/LibreHardwareMonitorLib.csproj",
  [string]$Output = "..\artifacts\Release"
)

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectPath = Join-Path $ScriptRoot "..\$Project"
$OutputPath = Join-Path $ScriptRoot $Output

if (-not (Test-Path $OutputPath)) { New-Item -ItemType Directory -Path $OutputPath | Out-Null }

Write-Host "Packing project: $ProjectPath (Configuration=$Configuration)"

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    & dotnet pack $ProjectPath -c $Configuration -o $OutputPath
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet pack failed."; exit $LASTEXITCODE }
    Write-Host "Packages written to: $OutputPath"
    exit 0
}

Write-Warning "dotnet CLI not available. If this is a classic .NET Framework project, create a .nuspec and run 'nuget pack'."
Write-Host "You can also create a .nupkg manually from the built assemblies in artifacts."
