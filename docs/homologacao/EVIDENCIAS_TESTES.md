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

## Rodada Checkpoint 14.2 — simulação isolada de upgrade

Ambiente: branch Neon `br-frosty-cherry-b6b841fl`; banco dedicado de testes `rass_sprint1_tests`. Production não recebeu escrita e terminou a rodada com 10 migrations.

| Evidência | Resultado |
|---|---|
| Simulação EF 10→20 em clone isolado | Aprovada; 20 migrations exatas. |
| Pós-validação de `SECURITY DEFINER`, ACLs e schema | Aprovada; runtime/public sem execução administrativa, executor sem `CREATE` e admin com `EXECUTE` explícito. |
| `PlatformRbacTests` no banco dedicado | 10/10 aprovados. |
| Integração PostgreSQL/API no banco dedicado | 132/132 aprovados. |
| Limpeza de `SET ROLE` temporário | Aprovada; `SET=false` e `USAGE=false` efetivos para `neondb_owner` sobre executor. |
| Pós-validação RLS/FORCE RLS | Aprovada nas quatro tabelas críticas verificadas. |

Observações: a ferramenta `dotnet-ef` local estava em 10.0.12 enquanto o runtime EF estava em 10.0.13; o aviso não impediu builds ou migrations. Os testes exibiram aviso de licença do MediatR, permitido em desenvolvimento/teste, que permanece pendência de licenciamento antes de uso comercial em Production.
