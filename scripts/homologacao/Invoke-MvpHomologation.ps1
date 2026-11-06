[CmdletBinding()]
param(
    # Integration tests are opt-in because they write synthetic records.
    [switch]$IncludeIntegration,
    [ValidatePattern('^[a-z0-9-]+\.c-2\.sa-east-1\.aws\.neon\.tech$')]
    [string]$ExpectedTestHost,
    [ValidatePattern('^br-[a-z0-9-]+$')]
    [string]$ExpectedTestBranchId,
    [switch]$NeonTestBranchConfirmed,
    # Debug homologation needs an explicit non-Production API target.
    [switch]$BuildAndroidDebug,
    [ValidatePattern('^https?://')]
    [string]$HomologationApiBaseUrl,
    # Release AAB remains separately opt-in because it needs approved signing material.
    [switch]$BuildAndroidRelease,
    # Device E2E is opt-in. A concrete identifier prevents a test from
    # silently choosing a desktop/browser or a profile with unrelated state.
    [string]$AndroidDeviceId,
    # Optional read-only smoke target. It is never inferred from a connection string.
    [ValidatePattern('^https?://')]
    [string]$ApiBaseUrl
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$flutterProject = Join-Path $repositoryRoot 'mobile\rassapp_controlefin'
$resultsDirectory = Join-Path $repositoryRoot 'artifacts\homologacao'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$reportPath = Join-Path $resultsDirectory "mvp-homologation-$timestamp.md"
$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param([string]$Name, [string]$Status, [string]$Evidence)
    $results.Add([pscustomobject]@{ Name = $Name; Status = $Status; Evidence = $Evidence })
    Write-Host "[$Status] $Name - $Evidence"
}

function Invoke-CheckedProcess {
    param([string]$Name, [string]$FilePath, [string[]]$Arguments, [string]$WorkingDirectory)
    try {
        Push-Location -LiteralPath $WorkingDirectory
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) { throw "exit code $LASTEXITCODE" }
        Add-Result $Name 'APROVADO' 'Comando concluído com êxito.'
        return $true
    }
    catch {
        Add-Result $Name 'REPROVADO' ('Falhou sem expor parâmetros sensíveis: ' + $_.Exception.Message)
        return $false
    }
    finally { Pop-Location -ErrorAction SilentlyContinue }
}

function Test-IsolatedConnectionString {
    param([string]$VariableName, [string]$ExpectedRole)
    $value = [Environment]::GetEnvironmentVariable($VariableName, [EnvironmentVariableTarget]::Process)
    if ([string]::IsNullOrWhiteSpace($value)) { throw "$VariableName ausente no processo." }
    $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $builder.ConnectionString = $value
    $host = [string]$builder['Host']
    if ([string]::IsNullOrWhiteSpace($host)) { $host = [string]$builder['Server'] }
    $database = [string]$builder['Database']
    $username = [string]$builder['Username']
    if ([string]::IsNullOrWhiteSpace($username)) { $username = [string]$builder['User ID'] }
    $sslMode = [string]$builder['SSL Mode']
    if ($host -cne $ExpectedTestHost -or $database -cne 'rass_sprint1_tests' -or
        $username -cne $ExpectedRole -or $sslMode -cne 'VerifyFull' -or
        $host -match 'pooler' -or $host -eq 'ep-square-rain-b6i4dhum.c-2.sa-east-1.aws.neon.tech') {
        throw "$VariableName não atende ao destino isolado ou à role esperada."
    }
}

function Invoke-ReadOnlySmoke {
    param([string]$BaseUrl)
    $normalized = $BaseUrl.TrimEnd('/')
    foreach ($path in @('/health/live', '/health/ready')) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri ($normalized + $path) -Method Get -TimeoutSec 20
            if ($response.StatusCode -ne 200) { throw "HTTP $($response.StatusCode)" }
        }
        catch { throw "Smoke $path falhou: $($_.Exception.Message)" }
    }
}

New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
Push-Location -LiteralPath $repositoryRoot
try {
    [void](Invoke-CheckedProcess 'Build .NET Release' 'dotnet' @('build', 'RassAppControleFin.slnx', '-c', 'Release', '--no-restore') $repositoryRoot)
    [void](Invoke-CheckedProcess 'Testes unitários .NET' 'dotnet' @('test', 'tests\RassAppControleFin.UnitTests\RassAppControleFin.UnitTests.csproj', '-c', 'Release', '--no-build', '--no-restore') $repositoryRoot)
    [void](Invoke-CheckedProcess 'Testes de arquitetura .NET' 'dotnet' @('test', 'tests\RassAppControleFin.ArchitectureTests\RassAppControleFin.ArchitectureTests.csproj', '-c', 'Release', '--no-build', '--no-restore') $repositoryRoot)
    [void](Invoke-CheckedProcess 'Higiene de segredos' 'powershell.exe' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', 'scripts\Test-SecretHygiene.ps1') $repositoryRoot)

    $flutter = if (Get-Command flutter -ErrorAction SilentlyContinue) { 'flutter' } elseif (Test-Path 'C:\develop\flutter\bin\flutter.bat') { 'C:\develop\flutter\bin\flutter.bat' } else { $null }
    if ($null -eq $flutter) {
        Add-Result 'Flutter analyze e testes de widget' 'BLOQUEADO' 'Flutter SDK não localizado no ambiente.'
    }
    else {
        [void](Invoke-CheckedProcess 'Flutter analyze' $flutter @('analyze') $flutterProject)
        [void](Invoke-CheckedProcess 'Flutter testes unitários e widgets' $flutter @('test') $flutterProject)
        $integrationDirectory = Join-Path $flutterProject 'integration_test'
        if (-not (Test-Path $integrationDirectory)) {
            Add-Result 'Flutter integration_test' 'BLOQUEADO' 'Não há diretório integration_test; não há evidência E2E de aplicativo/dispositivo.'
        }
        elseif ([string]::IsNullOrWhiteSpace($AndroidDeviceId)) {
            Add-Result 'Flutter integration_test' 'BLOQUEADO' 'Exige -AndroidDeviceId explícito para não selecionar um perfil de dispositivo implícito.'
        }
        else {
            # E2E uses its own application id, so the emulator cannot reuse
            # FlutterSecureStorage, Drift, cache, or session state from a
            # normal/homologation profile. The empty URL validates the
            # configuration guard before a network-capable flow can start.
            [void](Invoke-CheckedProcess 'Flutter integration_test' $flutter @(
                'test', 'integration_test', '--flavor', 'e2e', '-d', $AndroidDeviceId,
                '--dart-define=API_BASE_URL='
            ) $flutterProject)
        }
        if ($BuildAndroidDebug) {
            if ([string]::IsNullOrWhiteSpace($HomologationApiBaseUrl) -or
                $HomologationApiBaseUrl -match 'rassappcontrolefin-1\.onrender\.com') {
                Add-Result 'Android APK debug de homologação' 'BLOQUEADO' 'Exige URL explícita que não seja Production.'
            }
            else {
                [void](Invoke-CheckedProcess 'Android APK debug de homologação' $flutter @(
                    'build', 'apk', '--debug', '--flavor', 'homologation',
                    "--dart-define=API_BASE_URL=$HomologationApiBaseUrl"
                ) $flutterProject)
            }
        }
        else { Add-Result 'Android APK debug de homologação' 'BLOQUEADO' 'Use -BuildAndroidDebug com -HomologationApiBaseUrl explícita e não-Production.' }

        if ($BuildAndroidRelease) {
            [void](Invoke-CheckedProcess 'Android AAB release' $flutter @('build', 'appbundle', '--release') $flutterProject)
        }
        else { Add-Result 'Android AAB release' 'BLOQUEADO' 'Requer -BuildAndroidRelease, assinatura aprovada e aceite manual.' }
    }

    if ($IncludeIntegration) {
        if (-not $NeonTestBranchConfirmed -or [string]::IsNullOrWhiteSpace($ExpectedTestHost) -or [string]::IsNullOrWhiteSpace($ExpectedTestBranchId)) { throw 'Integração exige -NeonTestBranchConfirmed, -ExpectedTestHost e -ExpectedTestBranchId.' }
        try {
            Test-IsolatedConnectionString 'RASS_DB_TEST_ADMIN' 'neondb_owner'
            Test-IsolatedConnectionString 'RASS_DB_TEST_RUNTIME' 'rass_app_runtime_v2'
            Test-IsolatedConnectionString 'RASS_DB_TEST_PLATFORM_ADMIN' 'rass_platform_admin_v1'
            Add-Result 'Preflight do banco isolado' 'APROVADO' 'Banco rass_sprint1_tests, TLS VerifyFull e três roles validadas; branch foi confirmada pelo operador.'
            [void](Invoke-CheckedProcess 'Integração PostgreSQL/API, RLS e RBAC' 'dotnet' @('test', 'tests\RassAppControleFin.IntegrationTests\RassAppControleFin.IntegrationTests.csproj', '-c', 'Release', '--no-build', '--no-restore') $repositoryRoot)
        }
        catch { Add-Result 'Integração PostgreSQL/API, RLS e RBAC' 'BLOQUEADO' ('Nenhum teste integrado foi iniciado: ' + $_.Exception.Message) }
    }
    else { Add-Result 'Integração PostgreSQL/API, RLS e RBAC' 'BLOQUEADO' 'Requer -IncludeIntegration e confirmação explícita do banco isolado.' }

    if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) { Add-Result 'Smoke HTTP da API' 'BLOQUEADO' 'Nenhuma URL explícita foi fornecida; o script não infere nem consulta Production.' }
    else {
        try { Invoke-ReadOnlySmoke $ApiBaseUrl; Add-Result 'Smoke HTTP da API' 'APROVADO' 'health/live e health/ready responderam HTTP 200.' }
        catch { Add-Result 'Smoke HTTP da API' 'REPROVADO' $_.Exception.Message }
    }
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
    $go = ($results.Count -gt 0) -and -not ($results.Status -contains 'REPROVADO') -and -not ($results.Status -contains 'BLOQUEADO')
    $verdict = if ($go) { 'GO' } else { 'NO-GO' }
    $commit = (& git -C $repositoryRoot rev-parse --short HEAD 2>$null | Select-Object -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($commit)) { $commit = 'indisponível' }
    $lines = @('# Checkpoint 15 — relatório automático', '', "Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')", "Commit: $commit", "Veredito: **$verdict**", '', '| Etapa | Resultado | Evidência |', '|---|---|---|')
    foreach ($result in $results) { $lines += "| $($result.Name) | $($result.Status) | $($result.Evidence.Replace('|','/')) |" }
    $lines += '', '> The command never executes migrations, deploys or Production writes. Testes integrados exigem banco isolado explicitamente confirmado.'
    Set-Content -LiteralPath $reportPath -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Relatório sanitizado: $reportPath"
    if ($verdict -ne 'GO') { exit 1 }
}
