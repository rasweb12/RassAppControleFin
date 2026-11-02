
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('br-odd-silence-b646inmx')]
    [string]$ConfirmedBranchId,

    [Parameter(Mandatory)]
    [switch]$NeonBranchConfirmed,

    [Parameter(Mandatory)]
    [switch]$ConfirmPasswordRotation
)

$ErrorActionPreference = 'Stop'

$expectedHost = 'ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech'
$expectedDatabase = 'rass_sprint1_tests'
$expectedRole = 'rass_platform_admin_v1'

if (-not $NeonBranchConfirmed -or -not $ConfirmPasswordRotation) {
    throw 'This operation requires independent Neon branch confirmation and explicit password-rotation confirmation.'
}

if ([string]::IsNullOrWhiteSpace($env:RASS_DB_TEST_ADMIN)) {
    throw 'RASS_DB_TEST_ADMIN must be available in the current process.'
}

Add-Type -AssemblyName System.Data

# Preserve the previous connection string in case the operation fails.
$previousPlatformAdminConnection = $env:RASS_DB_TEST_PLATFORM_ADMIN

$randomBytes = $null
$generatedPassword = $null
$connection = $null
$rng = $null
$rotationSucceeded = $false

try {
    # Cryptographically secure random generation.
    # Compatible with Windows PowerShell 5.1 and PowerShell 7.
    $randomBytes = New-Object byte[] 48
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($randomBytes)

    $generatedPassword = [Convert]::ToBase64String($randomBytes).
        TrimEnd('=').
        Replace('+', '-').
        Replace('/', '_')

    $connection = New-Object System.Data.Common.DbConnectionStringBuilder

    $connection['Host'] = $expectedHost
    $connection['Port'] = 5432
    $connection['Database'] = $expectedDatabase
    $connection['Username'] = $expectedRole
    $connection['Password'] = $generatedPassword
    $connection['SSL Mode'] = 'VerifyFull'
    $connection['Maximum Pool Size'] = 2
    $connection['Timeout'] = 10
    $connection['Command Timeout'] = 30
    $connection['Include Error Detail'] = 'false'
    $connection['Log Parameters'] = 'false'
    $connection['No Reset On Close'] = 'false'
    $connection['Multiplexing'] = 'false'

    # Available only in the current PowerShell process.
    $env:RASS_DB_TEST_PLATFORM_ADMIN = $connection.ConnectionString

    Write-Host 'Starting isolated platform-admin password rotation...'

    & dotnet run `
        --project tools/RassAppControleFin.Database/RassAppControleFin.Database.csproj `
        -c Release `
        --no-build `
        -- `
        rotate-platform-admin-password-isolated `
        --confirmed-branch-id $ConfirmedBranchId `
        --confirm-platform-admin-password-rotation

    if ($LASTEXITCODE -ne 0) {
        throw "Platform administration password rotation failed with exit code $LASTEXITCODE."
    }

    $rotationSucceeded = $true

    Write-Host 'Password rotation command completed successfully.'
}
finally {
    if ($null -ne $rng) {
        $rng.Dispose()
    }

    if ($null -ne $randomBytes) {
        [Array]::Clear($randomBytes, 0, $randomBytes.Length)
    }

    if (-not $rotationSucceeded) {
        # Restore the previous value on failure.
        $env:RASS_DB_TEST_PLATFORM_ADMIN = $previousPlatformAdminConnection
    }

    Remove-Variable `
        generatedPassword, connection, randomBytes, rng, `
        previousPlatformAdminConnection `
        -ErrorAction SilentlyContinue
}
