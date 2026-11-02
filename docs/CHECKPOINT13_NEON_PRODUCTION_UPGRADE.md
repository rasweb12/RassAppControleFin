# Checkpoint 13 — Atualização Neon Production

Data: 2026-11-01  
Estado: **REPROVADO PARA PRODUCTION — simulação isolada bloqueada por provisionamento de roles**

## Escopo e proteção

Nenhuma migration, role, grant, tabela, política, segredo do Render ou deploy foi alterado em Production. O banco Production inspecionado foi `neondb` da branch primária `production` (`br-lucky-paper-b6egkr8q`) do projeto Neon `RassAppControleFin` (`morning-dream-96756371`).

A branch descartável criada exclusivamente para a simulação é `validate-checkpoint13-production-upgrade` (`br-flat-rice-b6j99et6`), derivada de Production. Seu endpoint direto é `ep-muddy-hill-b6uk8uct.c-2.sa-east-1.aws.neon.tech`; ela não é a branch primária.

## Correção arquitetural: papéis funcionais e roles PostgreSQL

Os papéis do aplicativo e as identidades técnicas PostgreSQL são camadas diferentes. Nenhuma role PostgreSQL representa, concede ou substitui `User`, `TenantAdmin` ou `SuperAdmin`.

| Perfil funcional | Fonte de autorização | Escopo funcional | Acesso ao banco |
| --- | --- | --- | --- |
| `User` | `TenantMemberships` / `MembershipRole.User` | Aplicativo Flutter e os próprios dados financeiros no tenant resolvido | Nenhum acesso PostgreSQL direto; a API aplica autenticação, tenant e RLS. |
| `TenantAdmin` | `TenantMemberships` / `MembershipRole.TenantAdmin` | Administração básica do próprio tenant, inclusive gerenciamento de membros conforme `Permissions.MembershipsManage` | Nenhum acesso PostgreSQL direto nem permissão global. |
| `SuperAdmin` | `PlatformRoleAssignments` ativo, resolvido no servidor por `IPlatformAuthorizationService` | Administração global da plataforma via políticas `Platform.*`; não recebe leitura financeira multi-tenant por padrão | Nenhum privilégio administrativo PostgreSQL é concedido ao usuário funcional. |

O backend preserva `MembershipRole` com `User`, `TenantAdmin`, `SupportAgent` e `FinancialAdmin`. `SuperAdmin` não foi inserido nesse enum nem em `TenantMemberships`: é armazenado separadamente em `PlatformRoleAssignments`. `PermissionCatalog` concede permissões financeiras e de tenant a partir do vínculo do tenant, enquanto `PlatformPermissions.ForRoles` só libera as permissões `Platform.*` para uma atribuição global ativa de `SuperAdmin` obtida pelo servidor.

As roles PostgreSQL têm somente estas responsabilidades técnicas:

| Role técnica | Finalidade | Limite obrigatório |
| --- | --- | --- |
| `rass_app_runtime_v2` | Conexão normal da API para operações financeiras e leitura da própria atribuição global | Não pode escrever `PlatformRoleAssignments`, executar funções administrativas, herdar o executor ou ter privilégios de role/DDL. |
| `rass_platform_admin_v1` | Conexão administrativa isolada usada pela API apenas para executar as funções `app.platform_*` autorizadas | Não é um SuperAdmin funcional, não é proprietária das funções e não recebe acesso direto a tabelas financeiras. |
| `rass_platform_executor_owner_v1` | Proprietária `NOLOGIN` das funções `SECURITY DEFINER` e dos privilégios mínimos de execução dessas rotinas | Não é uma conta de aplicação, não pode fazer login, não pode ser assumida pela runtime e não recebe privilégios além dos objetos administrativos explicitamente necessários. |

`IsolatedPlatformAdministrationService` verifica a permissão funcional do ator antes de abrir a datasource administrativa e não possui fallback para a datasource financeira. As funções `SECURITY DEFINER` previstas na migration fixam `search_path = pg_catalog, app`, removem `PUBLIC EXECUTE` e concedem execução somente a `rass_platform_admin_v1`. Esse mecanismo separa a autorização funcional da identidade técnica, mas depende do provisionamento correto e do sigilo da conexão administrativa.

### Evidências de autorização no código

- `Program.cs` registra uma fallback policy autenticada e policies por `Permissions` e `PlatformPermissions`; a decisão usa `IUserContext.Permissions` resolvido no servidor, não role enviada por Flutter, header arbitrário ou payload.
- A ordem é autenticação, resolução de tenant, autorização e rate limiting. A resolução exige o vínculo do usuário com o tenant solicitado; ausência do vínculo resulta em `403`.
- `/api/v1/platform/*` exige autenticação, rate limiting e a policy `Platform.*` apropriada. Concessão e revogação exigem confirmação explícita; o serviço rejeita autoconcessão, autorrevogação, duplicidade e remoção do último SuperAdmin, além de registrar auditoria.
- O endpoint de revogação envia `userId` e `assignmentId` à função administrativa; a função valida que o proprietário da atribuição corresponde ao usuário de destino antes de alterar o estado.
- `RassAppControleFin.Admin.Web` é uma interface para a API, não a fronteira de segurança. A página `/platform` exige autenticação e trata a resposta sem permissão, mas atualmente não possui uma policy Blazor específica de SuperAdmin: um usuário autenticado pode alcançar a rota visual e receber a negação da API. Isto não expõe operações administrativas, porém deve ser corrigido em uma tarefa de UX/autorização do painel para bloquear navegação e menu antecipadamente.

### Testes obrigatórios antes de Production

Além de validar RLS e grants técnicos, a aprovação exige testar: `User`, `TenantAdmin`, `FinancialAdmin` e `SupportAgent` sem acesso `Platform.*`; `SuperAdmin` sintético com acesso somente às operações globais autorizadas; nenhum acesso financeiro intertenant automático; tentativa forjada de role/tenant; autoconcessão, autorrevogação, duplicidade, concorrência e proteção do último SuperAdmin. Esses testes funcionais não podem ser substituídos por grants PostgreSQL.

## Estado inicial confirmado

Production possui 10 migrations EF, terminando em `20261030163745_InvestmentPlanningCore`. O código no commit `661e931` possui 16 migrations. As pendentes são:

1. `20261101013424_CreditCardBillingCore`
2. `20261101030821_CreditCardBillingMovements`
3. `20261101031055_CreditCardBillingIndexes`
4. `20261101040238_CreditCardModelAlignment`
5. `20261101052030_PlatformRbacFoundation`
6. `20261101100000_PlatformAdministrationIsolation`

As tabelas ausentes em Production são `CreditCardPurchases`, `CreditCardInvoices` e `CreditCardInstallments`. Isso confirma a causa de `database.migration_pending`: a readiness exige 27 tabelas e não aceita o schema de 10 migrations.

As tabelas existentes consultadas preservam RLS e FORCE RLS. A role `rass_app_runtime_v2` não possui SUPERUSER, CREATEDB, CREATEROLE ou BYPASSRLS. Production ainda não possui as roles administrativas exigidas pela última migration.

## Script EF revisado

O script foi gerado localmente com Infrastructure como `project` e `startup-project`, usando a `DesignTimeDbContextFactory` existente. Não foi necessária dependência adicional na API.

Intervalo gerado: `InvestmentPlanningCore` até `PlatformAdministrationIsolation`, com `--idempotent`.

- Cria 4 tabelas: compras, faturas, parcelas e vínculos globais de plataforma.
- Cria 11 índices.
- Não contém `DROP TABLE`, `DELETE` ou `TRUNCATE` no caminho de subida.
- Inclui DDL de FKs, constraints, RLS/FORCE RLS, policies, grants e funções `SECURITY DEFINER`.

Riscos esperados: locks breves em `CreditCards` e `CardMovements` para constraint, colunas e FKs; locks de criação para novas tabelas e índices. A migration `PlatformAdministrationIsolation` falha deliberadamente no `Down` para não restaurar escrita administrativa da runtime financeira.

## Bloqueio da simulação

As roles administrativas criadas pelo provisionador Neon na branch descartável receberam atributos excessivos por padrão: LOGIN/INHERIT, CREATEROLE, CREATEDB, REPLICATION e BYPASSRLS. O executor conectado não possui permissão para reduzir esses atributos por SQL (`permission denied to alter role`).

Essas roles não podem ser usadas para validar nem implantar a migration de isolamento administrativo. Não foi aplicada qualquer migration na branch descartável.

O comportamento também comprova que roles são branch-específicas neste projeto: Production não possui as roles, enquanto uma branch de validação anterior as possui.

O bloqueio é técnico, não uma ausência de perfil funcional `SuperAdmin`. Provisionar uma role PostgreSQL não promove usuário algum no aplicativo e não deve ser tratado como tal.

## Procedimento manual exigido antes da simulação

Executar no SQL Editor da branch descartável, com uma identidade que possa administrar roles apenas nessa branch:

1. Garantir que `rass_platform_executor_owner_v1` seja `NOLOGIN NOINHERIT NOSUPERUSER NOCREATEROLE NOCREATEDB NOREPLICATION NOBYPASSRLS`.
2. Garantir que `rass_platform_admin_v1` seja `LOGIN NOINHERIT NOSUPERUSER NOCREATEROLE NOCREATEDB NOREPLICATION NOBYPASSRLS`.
3. Garantir ausência de membership em `neon_superuser` e ausência de memberships para `rass_app_runtime_v2`.
4. Conceder `rass_platform_executor_owner_v1` somente a `neondb_owner` para permitir transferência de ownership das funções.
5. Conceder `CREATE` somente no schema `app` ao executor.
6. Não conceder `CREATE` no schema `app` à runtime nem à role administrativa.
7. Não conceder tabelas financeiras a nenhuma das duas roles; os grants da migration ficam limitados a `PlatformRoleAssignments`, `AuditEntries`, `Tenants` e `UserProfiles` para o executor.

Após essa validação, aplicar o script EF gerado exclusivamente na branch descartável. Interromper no primeiro erro. Em seguida validar 16 migrations, 27 tabelas, RLS/FORCE RLS, policies, FKs, índices, ownership seguro das funções e negação de escrita administrativa pela runtime.

## Plano de Production (não autorizado nesta etapa)

1. Criar branch de recuperação a partir de Production e confirmar ponto de restauração Neon.
2. Provisionar as roles com os atributos mínimos acima por procedimento que não atribua privilégios elevados por padrão.
3. Validar roles, grants, memberships e RLS no ambiente de recuperação.
4. Aplicar o script 10→16 por conexão direta administrativa, uma única vez.
5. Consultar `app."__EFMigrationsHistory"`, confirmar 16 migrations e as 27 tabelas requeridas.
6. Validar `/health/live`, `/health/ready`, autenticação, contas, transações, cartões, compra, fatura, pagamento e RBAC.
7. Conferir no Render, sem revelar valor, que `Database__ConnectionString` aponta para o endpoint direto/pooler da branch `production`, banco `neondb` e role `rass_app_runtime_v2` com TLS VerifyFull.

Rollback: não executar `Down` de `PlatformAdministrationIsolation`. Em caso de falha após DDL, manter a API na versão compatível anterior apenas se ela suportar o schema novo; corrigir para frente na branch de recuperação. Não apagar tabelas nem dados financeiros.

## Resultados de regressão disponíveis

- Build Release da API: aprovado anteriormente, sem warnings.
- Unitários .NET: 47/47 aprovados.
- Arquitetura .NET: 5/5 aprovados.
- Flutter analyze: aprovado.
- Flutter test: 23 aprovados.
- Suíte de integração PostgreSQL não foi executada nesta branch: ela bloqueia corretamente destinos diferentes de `rass_sprint1_tests`.

## Arquivos locais alterados neste checkpoint

- `docs/CHECKPOINT13_NEON_PRODUCTION_UPGRADE.md` — relatório e roteiro.
- `mobile/rassapp_controlefin/lib/features/finance/credit_cards_page.dart` — alteração local preexistente, preservada.
- `tests/RassAppControleFin.ArchitectureTests/DependencyTests.cs` — matriz de arquitetura ajustada para a dependência já existente `Security → SharedKernel`.

## Auditoria de artefatos versionados

Há artefatos temporários versionados: `.tmp-credit-card-migration.sql`, `.tmp-production-migration*.sql` e arquivos `.trx` em `.tmp-test-results`. Eles não foram executados nem alterados nesta rodada e não são a fonte de verdade para a atualização 10→16. Uma varredura estática segura, limitada a nomes de arquivos, também indicou que arquivos de configuração de exemplo e documentação podem conter padrões de conexão ou senha de exemplo.

Antes de qualquer commit de release, revisar cada artefato e remover do controle de versão apenas mediante autorização, depois de confirmar que não contém credenciais, dados pessoais ou SQL obsoleto. Se houver qualquer segredo histórico, rotacioná-lo e tratar o caso como incidente; não copiar o valor para tickets, documentação ou logs.

## Veredito P0 atualizado

**REPROVADO PARA PRODUCTION.** A arquitetura separa corretamente os papéis funcionais das roles técnicas no código e no desenho das migrations, mas a simulação das seis migrations permanece bloqueada pelo provisionamento inseguro das roles técnicas na branch descartável. A atualização Production só pode voltar à pauta após provisionamento mínimo verificável, validação isolada das migrations e execução dos testes de autorização e RLS listados acima.

