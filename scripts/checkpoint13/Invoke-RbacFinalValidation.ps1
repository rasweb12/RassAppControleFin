[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^br-[a-z0-9-]+$')]
    [string]$ExpectedBranchId,

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9-]+\.c-2\.sa-east-1\.aws\.neon\.tech$')]
    [string]$ExpectedHost,

    [Parameter(Mandatory)]
    [switch]$NeonBranchConfirmed,

    # Rewrites only this PowerShell process from local User Secrets to the
    # allowlisted disposable endpoint/database. Nothing is persisted.
    [switch]$UseIsolatedRbacTestEndpoint,

    # Test-only path: validates and normalizes credentials in a child process
    # without opening a database connection or invoking an RBAC SQL phase.
    [switch]$CredentialPreparationOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$databaseTool = Join-Path $repositoryRoot 'tools\RassAppControleFin.Database\RassAppControleFin.Database.csproj'
$integrationTests = Join-Path $repositoryRoot 'tests\RassAppControleFin.IntegrationTests'
$approvedBranchId = 'br-odd-silence-b646inmx'
$approvedHost = 'ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech'

if (-not $NeonBranchConfirmed) {
    throw 'Confirm the Neon branch ID and endpoint through the Neon API before running this script.'
}
if ($ExpectedBranchId -ne $approvedBranchId -or $ExpectedHost -ne $approvedHost) {
    throw 'This final validation is allowlisted only for the approved disposable RBAC test branch and direct endpoint.'
}
if ($CredentialPreparationOnly -and -not $UseIsolatedRbacTestEndpoint) {
    throw 'CredentialPreparationOnly requires UseIsolatedRbacTestEndpoint.'
}

function Write-Stage {
    param([string]$Name, [System.Diagnostics.Stopwatch]$Clock)
    Write-Host ("[{0:O}] {1} (elapsed: {2})" -f [DateTime]::UtcNow, $Name, $Clock.Elapsed)
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

function Build-DatabaseTool {
    Invoke-CheckedDotnet @('build', $databaseTool, '-c', 'Release', '--no-restore')
}

function Build-IntegrationTests {
    Invoke-CheckedDotnet @('build', $integrationTests, '-c', 'Release', '--no-restore')
}

function Set-IsolatedRbacTestProcessConnections {
    $secretId = 'RassAppControleFin-Database-Development'

    $expectedRoles = @{
        'RASS_DB_TEST_ADMIN' = 'neondb_owner'
        'RASS_DB_TEST_RUNTIME' = 'rass_app_runtime_v2'
        'RASS_DB_TEST_PLATFORM_ADMIN' = 'rass_platform_admin_v1'
    }

    function Get-UserSecretsValue {
        param([string]$Name)

        $appData = $env:APPDATA
        if ([string]::IsNullOrWhiteSpace($appData)) {
            $appData = [Environment]::GetFolderPath('ApplicationData')
        }
        if ([string]::IsNullOrWhiteSpace($appData)) {
            throw "$Name is unavailable because the local User Secrets location cannot be resolved."
        }
        $path = Join-Path $appData ("Microsoft\\UserSecrets\\{0}\\secrets.json" -f $secretId)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "$Name is unavailable in the process and local User Secrets."
        }

        try {
            $secrets = Get-Content -LiteralPath $path -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            throw "$Name could not be read from local User Secrets because its format is invalid."
        }

        $property = $secrets.PSObject.Properties[$Name]
        if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            throw "$Name is unavailable in the process and local User Secrets."
        }
        return [string]$property.Value
    }

    function ConvertTo-ApprovedConnection {
        param([string]$Name, [string]$ExpectedRole, [string]$Value, [string]$Source)

        function Get-ConnectionField {
            param([string]$Connection, [string[]]$Keys)

            foreach ($key in $Keys) {
                $pattern = '(?i)(?:^|;)\s*' + [regex]::Escape($key) + '\s*=\s*(?<value>"(?:[^"]|"")*"|''(?:[^'']|'''')*''|[^;]*)'
                $matches = [regex]::Matches($Connection, $pattern)
                if ($matches.Count -gt 1) {
                    throw "Connection string contains duplicate $key fields."
                }
                if ($matches.Count -eq 1) {
                    $field = $matches[0].Groups['value'].Value.Trim()
                    if ($field.Length -ge 2 -and $field.StartsWith('"') -and $field.EndsWith('"')) {
                        return $field.Substring(1, $field.Length - 2).Replace('""', '"')
                    }
                    if ($field.Length -ge 2 -and $field.StartsWith("'") -and $field.EndsWith("'")) {
                        return $field.Substring(1, $field.Length - 2).Replace("''", "'")
                    }
                    return $field
                }
            }
            return ''
        }

        $connectionHost = Get-ConnectionField $Value @('Host', 'Server')
        $database = Get-ConnectionField $Value @('Database', 'Initial Catalog')
        $username = Get-ConnectionField $Value @('Username', 'User ID', 'UID')
        $sslMode = Get-ConnectionField $Value @('SSL Mode')
        $password = Get-ConnectionField $Value @('Password', 'Pwd')

        if (($connectionHost -ine $approvedHost) -or ($database -cne 'rass_sprint1_tests') -or ($username -cne $ExpectedRole) `
            -or ($sslMode -ine 'VerifyFull') -or [string]::IsNullOrWhiteSpace($password) -or ($connectionHost -match '(?i)-pooler')) {
            throw "$Name from $Source is not approved for the isolated endpoint, database, role, or TLS policy."
        }

        $escapedPassword = $password.Replace('"', '""')
        return ('Host={0};Port=5432;Database=rass_sprint1_tests;Username={1};Password="{2}";SSL Mode=VerifyFull;Maximum Pool Size=2;Timeout=10;Command Timeout=30;Include Error Detail=false;Log Parameters=false;No Reset On Close=false;Multiplexing=false' -f $approvedHost, $ExpectedRole, $escapedPassword)
    }

    foreach ($name in $expectedRoles.Keys) {
        $value = [Environment]::GetEnvironmentVariable($name, [System.EnvironmentVariableTarget]::Process)
        $source = 'process environment'
        if ([string]::IsNullOrWhiteSpace($value)) {
            $value = Get-UserSecretsValue $name
            $source = 'local User Secrets fallback'
        }

        $connection = ConvertTo-ApprovedConnection $name $expectedRoles[$name] $value $source
        [Environment]::SetEnvironmentVariable($name, $connection, [System.EnvironmentVariableTarget]::Process)
    }
}

$rbacArguments = @(
    '--expected-host', $ExpectedHost,
    '--confirmed-branch-id', $ExpectedBranchId,
    '--confirm-isolated-rbac-tests'
)
$clock = [System.Diagnostics.Stopwatch]::StartNew()
$cleanupRequired = $false
$primaryFailure = $null
$cleanupFailure = $null
$postValidationFailure = $null
$locationPushed = $false
$originalProcessConnections = @{}
$processConnectionsHydrated = $false
$postValidationRequired = $false

try {
    Push-Location -LiteralPath $repositoryRoot
    $locationPushed = $true

    if ($UseIsolatedRbacTestEndpoint) {
        foreach ($name in @('RASS_DB_TEST_ADMIN', 'RASS_DB_TEST_RUNTIME', 'RASS_DB_TEST_PLATFORM_ADMIN')) {
            $originalProcessConnections[$name] = [Environment]::GetEnvironmentVariable($name, [System.EnvironmentVariableTarget]::Process)
        }
        $processConnectionsHydrated = $true
        Set-IsolatedRbacTestProcessConnections

        if ($CredentialPreparationOnly) {
            Write-Host 'Isolated process credentials were prepared and policy-validated locally.'
            return
        }
    }

    Write-Stage 'Building the current .NET database tool' $clock
    Build-DatabaseTool

    Write-Stage 'Building the current PostgreSQL integration suite' $clock
    Build-IntegrationTests

    Write-Stage 'Validating approved process connections' $clock
    Invoke-DatabaseTool (@('validate-rbac-test-connections') + $rbacArguments)
    $postValidationRequired = $true

    Write-Stage 'Running read-only RBAC, ACL, RLS, function, and migration diagnostics' $clock
    Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'pre-validation') + $rbacArguments)

    # The only temporary capability is SET ROLE for synthetic setup in PlatformRbacTests.
    # It is armed for cleanup before the grant is attempted.
    $cleanupRequired = $true
    Write-Stage 'Granting temporary SET ROLE for synthetic RBAC test setup' $clock
    Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'test-seed') + $rbacArguments)

    Write-Stage 'Executing PlatformRbacTests' $clock
    Invoke-CheckedDotnet @('test', $integrationTests, '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~PlatformRbacTests')

    Write-Stage 'Executing complete PostgreSQL integration suite' $clock
    Invoke-CheckedDotnet @('test', $integrationTests, '-c', 'Release', '--no-build')
}
catch {
    $primaryFailure = $_
    Write-Error ("RBAC final validation primary failure: {0}" -f $_.Exception.Message)
}
finally {
    if ($cleanupRequired) {
        try {
            Write-Stage 'Cleaning temporary SET ROLE capability' $clock
            Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'cleanup') + $rbacArguments)
        }
        catch {
            $cleanupFailure = $_
        }
    }

    if ($postValidationRequired) {
        try {
            Write-Stage 'Running mandatory post-cleanup security validation' $clock
            Invoke-DatabaseTool (@('execute-rbac-test-sql', '--phase', 'post-validation') + $rbacArguments)
        }
        catch {
            $postValidationFailure = $_
        }
    }

    if ($locationPushed) { Pop-Location }
    if ($processConnectionsHydrated) {
        foreach ($name in $originalProcessConnections.Keys) {
            [Environment]::SetEnvironmentVariable($name, $originalProcessConnections[$name], [System.EnvironmentVariableTarget]::Process)
        }
    }
}

if ($null -ne $primaryFailure) {
    if ($null -ne $cleanupFailure) { Write-Error 'The test flow failed and temporary-role cleanup also failed.' }
    if ($null -ne $postValidationFailure) { Write-Error 'The test flow failed and post-cleanup validation also failed.' }
    throw $primaryFailure
}
if ($null -ne $cleanupFailure) { throw $cleanupFailure }
if ($null -ne $postValidationFailure) { throw $postValidationFailure }

Write-Stage 'RBAC final validation completed successfully' $clock
