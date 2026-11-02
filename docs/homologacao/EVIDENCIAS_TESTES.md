# Evidências de testes

## Evidências históricas confirmadas

- Checkpoint 13, ambiente isolado `br-odd-silence-b646inmx` / `rass_sprint1_tests`: `PlatformRbacTests` **10/10**; integração PostgreSQL/API **130/130**; cleanup e pós-validação aprovados.
- Essa evidência não foi repetida nesta rodada e não corresponde a Production.

## Rodada Checkpoint 14 — local

| Comando | Resultado |
|---|---|
| Parser Windows PowerShell 5.1 de `Invoke-ProductionUpgrade.ps1` | Aprovado. |
| `dotnet build tools/RassAppControleFin.Database/... -c Release --no-restore` | Aprovado, 0 warnings. |
| `dotnet build tests/RassAppControleFin.IntegrationTests/... -c Release --no-restore` | Aprovado, 0 warnings. |
| Testes `ProductionUpgradeGuardTests`, `RuntimeIdentityAclMigrationTests`, `RbacFinalValidationScriptTests` | 6/6 aprovados. |
| `dotnet build RassAppControleFin.slnx -c Release --no-restore` | Aprovado, 0 warnings (após restore local). |
| Unitários .NET | 50/50 aprovados. |
| Arquitetura .NET | 5/5 aprovados. |
| `dart analyze` | Aprovado, sem issues. |
| `flutter test --reporter expanded` | 23/23 aprovados. |

Não executados nesta rodada: leitura de Production, migrations, API local contra Production, APK/AAB, smoke tests de release. Permanecem pendentes, não aprovados.
