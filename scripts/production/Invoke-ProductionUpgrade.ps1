[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9-]+\.c-2\.sa-east-1\.aws\.neon\.tech$')]
    [string]$ExpectedHost,

    [Parameter(Mandatory)]
    [switch]$NeonProductionBranchConfirmed,

    # Read-only preflight is the default.
    [switch]$Apply,

    [string]$ConfirmProductionWrite,

    [ValidateSet('PlatformHardening', 'FinalHardening')]
    [string]$Stage = 'PlatformHardening'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$databaseTool = Join-Path $repositoryRoot 'tools\RassAppControleFin.Database\RassAppControleFin.Database.csproj'
$productionBranchId = 'br-lucky-paper-b6egkr8q'
$writePhrase = 'APPLY-CHECKPOINT14-PRODUCTION'
$target = if ($Stage -eq 'PlatformHardening') {
    '20261102010000_PlatformAdministrationHardening'
} else {
    '20261102040000_RuntimeIdentityOperationalAclHardening'
}

function Invoke-CheckedDotnet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed with exit code $LASTEXITCODE." }
}

if (-not $NeonProductionBranchConfirmed) {
    throw 'Confirm Production branch br-lucky-paper-b6egkr8q in the Neon Console/API before this command.'
}
if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable(
        'RASS_PRODUCTION_MIGRATIONS_CONNECTION', [EnvironmentVariableTarget]::Process))) {
    throw 'RASS_PRODUCTION_MIGRATIONS_CONNECTION must be set only in the current process.'
}
if ($Apply -and $ConfirmProductionWrite -cne $writePhrase) {
    throw "-Apply requires -ConfirmProductionWrite $writePhrase."
}

Push-Location -LiteralPath $repositoryRoot
try {
    Write-Host 'Building the migration tool in Release configuration.'
    Invoke-CheckedDotnet @('build', $databaseTool, '-c', 'Release', '--no-restore')

    Write-Host 'Running read-only Production destination and migration preflight.'
    Invoke-CheckedDotnet @('run', '--project', $databaseTool, '-c', 'Release', '--no-build', '--',
        'validate-production-upgrade-connection', '--expected-host', $ExpectedHost,
        '--confirmed-branch-id', $productionBranchId)

    if (-not $Apply) {
        Write-Host "Dry run complete. No DDL or data change was issued. Approved next stage: $Stage ($target)."
        return
    }

    Write-Host "Applying approved Production migration stage $Stage."
    Invoke-CheckedDotnet @('run', '--project', $databaseTool, '-c', 'Release', '--no-build', '--',
        'apply-production-upgrade', '--expected-host', $ExpectedHost,
        '--confirmed-branch-id', $productionBranchId, '--target', $target,
        '--confirm-production-write')
    Write-Host 'Stage completed. Stop and execute the documented independent post-stage validation before the next stage.'
}
finally { Pop-Location }
