[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^br-[a-z0-9-]+$')]
    [string]$ExpectedBranchId,

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9-]+\.c-2\.sa-east-1\.aws\.neon\.tech$')]
    [string]$ExpectedHost,

    [Parameter(Mandatory)]
    [switch]$NeonBranchConfirmed
)

$ErrorActionPreference = 'Stop'
$testDatabase = 'rass_sprint1_tests'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$databaseTool = Join-Path $repositoryRoot 'tools\RassAppControleFin.Database\RassAppControleFin.Database.csproj'
$integrationTests = Join-Path $repositoryRoot 'tests\RassAppControleFin.IntegrationTests'
$approvedBranchId = 'br-odd-silence-b646inmx'
$approvedHost = 'ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech'

if (-not $NeonBranchConfirmed) {
    throw 'Confirm the Neon branch ID and endpoint through the Neon API before running this script.'
}
if ($ExpectedBranchId -ne $approvedBranchId -or $ExpectedHost -ne $approvedHost) {
    throw 'This procedure is allowlisted only for the approved disposable RBAC test branch and direct endpoint.'
}

function Invoke-CheckedDotnet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Invoke-DatabaseTool {
    param([string[]]$CommandArguments)

    Invoke-CheckedDotnet (@('run', '--project', $databaseTool, '-c', 'Release', '--no-build', '--') + $CommandArguments)
}

$rbacArguments = @(
    '--expected-host', $ExpectedHost,
    '--confirmed-branch-id', $ExpectedBranchId,
    '--confirm-isolated-rbac-tests'
)
$previousMigrationConnection = [Environment]::GetEnvironmentVariable('RASS_MIGRATIONS_CONNECTION', 'Process')
$cleanupRequired = $false
$migration18Applied = $false
$primaryFailure = $null
$cleanupFailure = $null
$postValidationFailure = $null
$locationPushed = $false

try {
    Push-Location -LiteralPath $repositoryRoot
    $locationPushed = $true

    # The database and roles must have been created separately in the confirmed
    # disposable branch. The .NET tool rejects endpoint, database, role, TLS,
    # pooling, and security divergences before any remote operation.
    Invoke-DatabaseTool (@('validate-rbac-test-connections') + $rbacArguments)
    # EF creates app.__EFMigrationsHistory before it evaluates InitialFoundation.
    # A brand-new database therefore needs only the empty app schema first.
    Invoke-DatabaseTool (@('bootstrap-rbac-test-schema') + $rbacArguments)
    Invoke-DatabaseTool (@('migrate-rbac-test-database', '--target', '20261101052030_PlatformRbacFoundation') + $rbacArguments)

    # Set before the first temporary-grant attempt. Cleanup is safe after a
    # partial grant and is mandatory for all later paths.
    $cleanupRequired = $true
    Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'ownership') + $rbacArguments)
    Invoke-DatabaseTool (@('migrate-rbac-test-database', '--target', '20261102010000_PlatformAdministrationHardening') + $rbacArguments)

    Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'acl') + $rbacArguments)
    Invoke-DatabaseTool (@('migrate-rbac-test-database', '--target', '20261102020000_PlatformFunctionExecuteHardening') + $rbacArguments)
    Invoke-DatabaseTool (@('migrate-rbac-test-database', '--target', '20261102030000_RuntimeSchemaUsageHardening') + $rbacArguments)
    $migration18Applied = $true

    Invoke-CheckedDotnet @('run', '--project', $databaseTool, '-c', 'Release', '--no-build', '--', 'probe-tests')
    Invoke-CheckedDotnet @('run', '--project', $databaseTool, '-c', 'Release', '--no-build', '--', 'probe-platform-admin-isolated')

    # Required only because PlatformRbacTests seeds synthetic roles through SET LOCAL ROLE.
    Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'test-seed') + $rbacArguments)
    Invoke-CheckedDotnet @('test', $integrationTests,
        '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~PlatformRbacTests')
    Invoke-CheckedDotnet @('test', $integrationTests,
        '-c', 'Release', '--no-build')
}
catch {
    $primaryFailure = $_
}
finally {
    if ($cleanupRequired) {
        try {
            Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'cleanup') + $rbacArguments)
        }
        catch {
            $cleanupFailure = $_
        }
    }

    if ($migration18Applied) {
        try {
            Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'post-validation') + $rbacArguments)
        }
        catch {
            $postValidationFailure = $_
        }
    }

    if ($null -eq $previousMigrationConnection) {
        Remove-Item Env:RASS_MIGRATIONS_CONNECTION -ErrorAction SilentlyContinue
    }
    else {
        $env:RASS_MIGRATIONS_CONNECTION = $previousMigrationConnection
    }
    if ($locationPushed) {
        Pop-Location
    }
    Remove-Variable previousMigrationConnection -ErrorAction SilentlyContinue
}

if ($null -ne $primaryFailure) {
    if ($null -ne $cleanupFailure) { Write-Error 'The primary operation failed and temporary RBAC cleanup also failed.' }
    if ($null -ne $postValidationFailure) { Write-Error 'The primary operation failed and post-migration RBAC validation also failed.' }
    throw $primaryFailure
}
if ($null -ne $cleanupFailure) { throw $cleanupFailure }
if ($null -ne $postValidationFailure) { throw $postValidationFailure }

Write-Host 'RBAC PostgreSQL homologation completed successfully on the approved isolated branch.'
