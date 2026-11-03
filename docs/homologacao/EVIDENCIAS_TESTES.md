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

## Checkpoint 14 — execução controlada em Production

| Evidência | Resultado |
|---|---|
| Produção, migrations 11–17 | Aplicadas com sucesso. |
| Pós-validação de etapa intermediária | Aprovada: 17 migrations, funções protegidas e RLS/FORCE RLS preservados. |
| Pré-estágio da migration 20 | Aprovado: somente o `SELECT` legado no histórico EF foi revogado para a runtime. |
| Produção, migrations 18–20 | Aplicadas com sucesso. |
| Histórico EF Production | 20/20 exatas; sem migrations inesperadas. |
| Testes negativos reais da runtime | Escrita administrativa com GUC forjado e execução de função administrativa negadas. |
| Pós-validação de cleanup | Aprovada: `SET ROLE` transitório removido; runtime/PUBLIC sem ACL administrativa. |

Nenhum dado foi apagado. Nenhum deploy ou alteração de configuração foi realizado no Render. O serviço Render ainda tem último deploy `update_failed` e logs históricos de readiness PostgreSQL indisponível, portanto o banco aprovado não equivale à API publicada/homologada.

## Sprint 16 — fundação de monetização (local)

| Comando/teste | Resultado |
|---|---|
| `dotnet test tests/RassAppControleFin.UnitTests/... -c Release --no-restore -m:1` | **54/54** aprovados. Inclui invariantes de plano vitalício, plano efetivo e fingerprint de evento de pagamento. |
| `dotnet test tests/RassAppControleFin.IntegrationTests/... --filter "FullyQualifiedName~RuntimeIdentityAclMigrationTests"` | **4/4** guard tests locais aprovados. Inclui proteção estática da migration de monetização. |
| Migration `20261103112705_MonetizationFoundation` | Gerada e revisada localmente; não aplicada em banco algum nesta rodada. |

Não executados: integração de compra, validação de recibo Google Play, testes em dispositivo, APK/AAB, API de entitlement e interface Free/Premium. Esses itens são pendentes, não aprovados.

## Sprint 17 — regressão Flutter local

| Comando | Resultado |
|---|---|
| `flutter analyze` | Aprovado; sem falha de análise reportada. |
| `flutter test --reporter compact` | **23/23** aprovados. Os logs HTTP exibidos exercitam casos negativos esperados. |
| `flutter build apk --debug --flavor homologation` | **BLOQUEADO**: o executor encerrou a captura durante a etapa Gradle e não foi produzido APK novo verificável. |
| Guard tests de `Invoke-MvpHomologation.ps1` | **2/2** aprovados; Debug de homologação e AAB Release agora possuem flags separadas. |
| Parser PowerShell 5.1 | Aprovado para `Invoke-MvpHomologation.ps1`. |
