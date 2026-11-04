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
| `flutter test --reporter compact` | **24/24** aprovados. Inclui a proteção contra fallback não configurado para Production; os logs HTTP exibidos exercitam casos negativos esperados. |
| `flutter build apk --debug --flavor homologation --dart-define=API_BASE_URL=https://homologation.invalid` | APK Debug compilado em 2026-11-03; SHA-256 `E1E56B13FFBCEDE3251BBF01F33504CFFF6C500004B399A2A39C590C24836E0C`. Não equivale a homologação funcional remota. |
| Guard tests de `Invoke-MvpHomologation.ps1` | **2/2** aprovados; Debug de homologação e AAB Release agora possuem flags separadas. |
| Parser PowerShell 5.1 | Aprovado para `Invoke-MvpHomologation.ps1`. |
| Instalação Android do APK `homologation` | Aprovada no emulador Android 16; variante `.homologation` coexistiu com o pacote base e abriu sem credenciais pré-preenchidas. Nenhuma conexão remota foi realizada. |

## Checkpoint 15 — execução local segura (2026-11-03)

O comando padrão foi executado sem credenciais de banco, URL de API ou flags que habilitem operações de integração. O relatório sanitizado foi gerado em `artifacts/homologacao/` e não é versionado.

| Etapa | Resultado |
|---|---|
| Build .NET Release (`RassAppControleFin.slnx`) | Aprovado, 0 warnings. |
| Testes unitários .NET | **55/55** aprovados. |
| Testes de arquitetura .NET | **5/5** aprovados. |
| Higiene de segredos | Aprovada. |
| `flutter analyze` | Aprovado, sem issues. |
| `flutter test` | **24/24** aprovados. Os registros de falha HTTP pertencem aos casos negativos esperados. |
| Integração PostgreSQL/API, RLS e RBAC | **BLOQUEADO**: nenhuma branch/banco isolado foi confirmada nesta execução. |
| `integration_test` Flutter | **BLOQUEADO**: diretório e infraestrutura E2E de dispositivo ainda não existem. |
| APK Debug remoto e AAB Release | **BLOQUEADOS**: exigem URL de homologação explícita e assinatura Release aprovada, respectivamente. |
| Smoke HTTP | **BLOQUEADO**: o orquestrador não aceita inferir uma URL de Production. |

## Android E2E — guarda de configuração (2026-11-03)

- Foi incluído `integration_test/configuration_guard_test.dart`, que inicia o app sem `API_BASE_URL` e exige a tela de bloqueio antes de qualquer autenticação ou chamada HTTP. O teste é deliberadamente limitado a esse limite de segurança e não representa homologação financeira.
- Dependências, análise estática e testes de unidade/widget continuaram aprovados após a inclusão (`flutter analyze` sem issues; **24/24** testes).
- A primeira tentativa revelou que o runner usava o `applicationId` base e, por isso, podia reutilizar estado persistente do emulador; ela foi interrompida sem apagar dados. Foi criada a variante `e2e` com sufixo de applicationId próprio.
- Na primeira tentativa, os logs locais registraram tentativas de renovação da sessão persistida do pacote base que expiraram. O processo foi encerrado sem limpeza de dados; essa tentativa não é evidência de homologação e não será repetida nesse perfil.
- A repetição na variante `.e2e`, com `API_BASE_URL` vazia, foi aprovada no emulador Android 16: **1/1** em cerca de sete segundos. O teste prova apenas o bloqueio seguro de configuração; fluxos financeiros, login e sincronização E2E continuam pendentes de API isolada.

## Sprint 16 — tela de direitos do plano (2026-11-03)

- A tela Flutter **Plano e recursos** foi adicionada como leitura autenticada de `/api/v1/monetization/entitlements`. Ela não inicia compra, restauração, troca de plano ou concessão local de Premium.
- `flutter analyze`: aprovado, sem issues.
- `flutter test`: **15/15** aprovados nesta execução, incluindo dois testes do contrato de leitura de entitlements.
- `dotnet test` unitários: **55/55**; arquitetura: **5/5**.
- A tela não foi validada contra servidor remoto porque `MonetizationFoundation` continua não aplicada. Portanto, o fluxo Free/Premium continua **não homologado**.

## Android Debug isolado — Primeiro Me Pago (2026-11-03)

- `flutter build apk --debug --flavor e2e --dart-define=API_BASE_URL=`: aprovado.
- Artefato: `mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-e2e-debug.apk`.
- SHA-256: `221B6622C4436D113D7E9C7995ABE639AC6A6C05A26D4285ECF53FF7183DFC6B`.
- Instalação no emulador Android confirmada para o package isolado `com.rassapp.rassapp_controlefin.e2e`. Nenhum dado do package habitual foi removido ou reutilizado.
- A inspeção automática desta sessão não obteve foco/árvore de UI do emulador após o launch; portanto instalação **não** é evidência de validação visual/manual.

## Primeiro Me Pago — persistência offline (2026-11-03)

- O planejamento percentual agora possui tela Flutter, confirmação explícita e não realiza transferência, despesa ou aplicação automaticamente.
- Teste de armazenamento offline: **11/11**. A perda de rede cria somente a operação pendente `ConfigurePayYourselfFirstPlanCommand`, mantém percentual/meta no cache do tenant e não cria transação financeira.
- Esta evidência não cobre previsão por receita, distribuição entre classes de investimento, disponibilidade de saldo, obrigações essenciais ou homologação contra API isolada; esses requisitos permanecem pendentes.

## Render — inspeção somente leitura (2026-11-03)

O serviço `RassAppControleFin` permaneceu `live` no commit publicado informado anteriormente. Entretanto, os últimos erros recuperáveis do log continham readiness PostgreSQL indisponível em 2026-11-02. Uma consulta HTTP externa aos endpoints públicos expirou no ambiente de auditoria. Isso não comprova indisponibilidade atual, mas impede marcar a API publicada como homologada até coletar uma janela recente de logs e smoke tests HTTP bem-sucedidos.

## Regressão local posterior — 2026-11-03

| Comando | Resultado |
|---|---|
| `dotnet build RassAppControleFin.slnx -c Release --no-restore` | Aprovado, 0 warnings. |
| Testes unitários .NET | **55/55** aprovados. |
| Testes de arquitetura .NET | **5/5** aprovados. |
| `flutter analyze` | Aprovado, sem issues. |
| `flutter test` a partir da raiz do app | **28/28** aprovados. |

A primeira invocação de `flutter test` com o diretório como argumento encerrou antes de iniciar os testes por uma falha de listagem em um artefato transitório de `build/flutter_secure_storage`. A repetição a partir da raiz do app foi aprovada e não exigiu limpeza, alteração de código ou acesso remoto. Os logs `RASS_HTTP_FAILURE` pertencem aos cenários negativos mockados.

## Checkpoint 15 — execução central local segura (2026-11-03)

- Relatório sanitizado: `artifacts/homologacao/mvp-homologation-20261103-151031.md`, commit `5350df7`.
- Aprovados: build Release, 55/55 unitários .NET, 5/5 arquitetura, higiene de segredos, análise Flutter, 28/28 Flutter e guarda Android E2E isolada.
- A guarda Android usa `applicationId` `.e2e` e `API_BASE_URL` vazia; comprova o bloqueio antes de rede e não reutiliza o perfil habitual. Não é uma homologação de login, finanças ou sincronização.
- Permaneceram bloqueados por design: integração PostgreSQL/API sem banco isolado confirmado, APK com API homologada, AAB assinado e smoke HTTP com URL explícita.

## Dependências .NET — auditoria local (2026-11-03)

- `dotnet list RassAppControleFin.slnx package --vulnerable --include-transitive` não identificou pacotes vulneráveis nas fontes NuGet configuradas.
- O resultado é uma consulta de advisories de dependências; não substitui licença comercial válida do MediatR nem a homologação de runtime/Production.

## Primeiro Me Pago — projeção mensal não mutável (2026-11-03)

- A regra de domínio usa a renda recebida quando existente, ou a renda prevista quando ainda não houve recebimento; calcula reserva planejada, saldo após obrigações e distribuição exata por percentuais.
- O cálculo não cria transferência, despesa, pagamento ou investimento. Saldo projetado negativo sinaliza insuficiência para o usuário.
- `InvestmentPlanningTests`: **8/8** aprovados, incluindo arredondamento, distribuição e insuficiência após obrigações.
- A interface ainda não consome a projeção e não há persistência de histórico mensal; esta entrega é parcial e não é homologação funcional completa.

## APK Debug isolado — projeção de Primeiro Me Pago (2026-11-03)

- `flutter test`: **29/29** aprovados após a integração da simulação mensal na tela.
- Artefato: `mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-e2e-debug.apk`.
- SHA-256: `9C276A75BD3FBC11E6525A50B60469C9A47806CA92410F82C59A7DAB4C3E8B2C`.
- A variante usa o identificador isolado `.e2e` e foi compilada com `API_BASE_URL` vazia. Ela não é um APK conectado à API de homologação nem prova os fluxos financeiros remotos.

## APK Debug isolado — estorno auditável (2026-11-03)

- O artefato `.e2e` foi recompilado após a correção do rótulo de estorno de lançamento.
- SHA-256 atual: `B3554FA6B20E47B4E0B75E5C3EFD52318BF9C3F7E049A7B34375376A5B80F266`.
- O build permaneceu com `API_BASE_URL` vazia e não contém credenciais nem destino remoto configurado.
