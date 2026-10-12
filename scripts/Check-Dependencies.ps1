$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $raw = & dotnet list RassAppControleFin.slnx package --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) { throw 'Dependency scan failed.' }
    $report = ($raw -join [Environment]::NewLine) | ConvertFrom-Json
    if ($report.problems.Count -gt 0) { throw 'Dependency audit returned problems.' }
    $findings = @()
    foreach ($project in $report.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
                if ($null -ne $package -and $package.vulnerabilities.Count -gt 0) {
                    $findings += $package
                }
            }
        }
    }
    if ($findings.Count -gt 0) {
        $findings | Select-Object id, resolvedVersion | Format-Table
        throw 'Vulnerable dependencies detected.'
    }
    Write-Output 'No known vulnerable dependencies reported by NuGet.'
}
finally { Pop-Location }
