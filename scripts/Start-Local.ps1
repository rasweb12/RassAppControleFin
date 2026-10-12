param(
    [int]$ApiPort = 5080,
    [int]$AdminPort = 5082
)
$ErrorActionPreference = 'Stop'
if ($ApiPort -eq $AdminPort) {
    throw 'Each service requires a distinct port.'
}
$root = Split-Path -Parent $PSScriptRoot
$services = @(
    @{ Name = 'Api'; Port = $ApiPort },
    @{ Name = 'Admin.Web'; Port = $AdminPort }
)
foreach ($service in $services) {
    if (Get-NetTCPConnection -State Listen -LocalPort $service.Port -ErrorAction SilentlyContinue) {
        throw ("Port {0} is already in use." -f $service.Port)
    }
    $dll = Join-Path $root ("src/RassAppControleFin.{0}/bin/Release/net10.0/RassAppControleFin.{0}.dll" -f $service.Name)
    if (-not (Test-Path -LiteralPath $dll)) {
        throw 'Run dotnet build RassAppControleFin.slnx -c Release first.'
    }
}
$logDirectory = Join-Path $root 'artifacts/local'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
foreach ($service in $services) {
    $dll = Join-Path $root ("src/RassAppControleFin.{0}/bin/Release/net10.0/RassAppControleFin.{0}.dll" -f $service.Name)
    $workingDirectory = Split-Path -Parent $dll
    $arguments = @($dll, '--urls', ("http://localhost:{0}" -f $service.Port),
        '--environment', 'Development', '--Api:BaseUrl', ("http://localhost:{0}" -f $ApiPort))
    $process = Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $workingDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDirectory ($service.Name + '.out.log')) -RedirectStandardError (Join-Path $logDirectory ($service.Name + '.err.log'))
    Write-Output ("{0}: http://localhost:{1} (PID {2})" -f $service.Name, $service.Port, $process.Id)
}
