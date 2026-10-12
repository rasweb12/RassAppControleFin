$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $files = Get-ChildItem -Recurse -File -Include *.cs,*.csproj,*.json,*.yml,*.yaml,*.ps1 |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' -and $_.Name -ne 'Test-SecretHygiene.ps1' }
    $secretPattern = '(?i)(?:gemini|resend|neon|render)[_-]?(?:api)?[_-]?key\s*[:=]\s*["'']?(?!["'']?(?:\$\{|\{|$))[A-Za-z0-9_-]{16,}'
    $matches = $files | Select-String -Pattern $secretPattern
    if ($matches) {
        $matches | Select-Object Path, LineNumber, Line | Format-Table -Wrap
        throw 'Potential secret literal detected.'
    }
    $forbidden = $files | Select-String -Pattern 'OpenAI|Azure OpenAI|EntityFrameworkCore\.SqlServer|UseSqlServer|Supabase'
    if ($forbidden) {
        $forbidden | Select-Object Path, LineNumber, Line | Format-Table -Wrap
        throw 'Forbidden provider or database reference detected.'
    }
    Write-Output 'Secret hygiene and provider policy checks passed.'
}
finally { Pop-Location }
