# Checkpoint 13 — Atualização Neon Production

Data: 2026-11-01  
Estado: **REPROVADO PARA PRODUCTION — ACL `PUBLIC EXECUTE` residual nas funções administrativas da branch de validação**

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

`IsolatedPlatformAdministrationService` verifica a permissão funcional do ator antes de abrir a datasource administrativa e não possui fallback para a datasource financeira. As funções `SECURITY DEFINER` fixam `search_path = pg_catalog, app`. A validação remota posterior às migrations 11–17 encontrou `PUBLIC EXECUTE` residual; portanto, a exclusividade de execução pela role administrativa ainda não está homologada.

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

## Rodada P0 de desbloqueio — 2026-11-02

### Causa confirmada do provisionamento excessivo

O mecanismo de criação de roles disponível no Neon aceita somente o nome e a opção de login. A criação de `rass_platform_admin_v1` e `rass_platform_executor_owner_v1` pela API do provedor devolveu roles com atributos elevados no catálogo PostgreSQL (`INHERIT`, `CREATEROLE`, `CREATEDB`, `REPLICATION` e `BYPASSRLS`) e membership em `neon_superuser`. Portanto, esse provisionador não é adequado para criar as roles da aplicação por si só.

Uma consulta somente leitura confirmou que a branch descartável `br-flat-rice-b6j99et6` é filha de Production, não primária, e possui as duas roles administrativas; Production não as possui. A existência das roles é isolada por branch no projeto consultado. Isso reduz o risco de uma correção manual na branch descartável afetar Production, mas não autoriza escrita: o operador deve validar novamente a branch no SQL Editor antes de executar qualquer comando.

### Correção local preparada

Foi criada a migration incremental `20261102010000_PlatformAdministrationHardening`. Ela não altera roles de cluster; falha caso as roles ainda tenham atributos ou membership inseguros, reduz grants do executor ao mínimo necessário e remove, ao fim do upgrade, os privilégios temporários usados apenas para transferir ownership das funções.

Também foi corrigido `IsolatedPlatformAdministrationService`: ator, usuário-alvo, justificativa e identificador de atribuição são validados antes da abertura da conexão administrativa. A justificativa é normalizada, obrigatória e limitada a 500 caracteres.

### Procedimento manual mínimo — somente branch descartável

**Pré-condição:** o cabeçalho do SQL Editor deve identificar `validate-checkpoint13-production-upgrade` / `br-flat-rice-b6j99et6` / `neondb`. Interromper se aparecer Production, outra branch ou outra base.

O executor deve ser uma identidade administrativa Neon capaz de `ALTER ROLE` nessa branch. `neondb_owner` usado pela migration não deve ser usado como runtime.

```sql
-- Somente após conferir branch, database e ausência de sessões/dependências.
ALTER ROLE rass_platform_executor_owner_v1 NOLOGIN NOINHERIT NOSUPERUSER NOCREATEROLE NOCREATEDB NOREPLICATION NOBYPASSRLS;
ALTER ROLE rass_platform_admin_v1 LOGIN NOINHERIT NOSUPERUSER NOCREATEROLE NOCREATEDB NOREPLICATION NOBYPASSRLS;
REVOKE neon_superuser FROM rass_platform_executor_owner_v1;
REVOKE neon_superuser FROM rass_platform_admin_v1;

-- Pré-requisitos temporários exclusivos da transferência de ownership da migration 16.
GRANT rass_platform_executor_owner_v1 TO neondb_owner;
GRANT CREATE ON SCHEMA app TO rass_platform_executor_owner_v1;
```

As duas últimas concessões são transitórias. A migration `PlatformAdministrationHardening` as revoga após a migration 16. Não conceder `CREATE` a `rass_app_runtime_v2` ou `rass_platform_admin_v1`; não conceder tabelas financeiras a essas roles.

### Consultas de verificação antes da migration

```sql
SELECT rolname, rolcanlogin, rolinherit, rolsuper, rolcreaterole, rolcreatedb,
       rolreplication, rolbypassrls
FROM pg_roles
WHERE rolname IN ('rass_app_runtime_v2', 'rass_platform_admin_v1', 'rass_platform_executor_owner_v1');

SELECT member_role.rolname AS member, parent_role.rolname AS granted_role
FROM pg_auth_members membership
JOIN pg_roles member_role ON member_role.oid = membership.member
JOIN pg_roles parent_role ON parent_role.oid = membership.roleid
WHERE member_role.rolname IN ('rass_app_runtime_v2', 'rass_platform_admin_v1', 'rass_platform_executor_owner_v1')
   OR parent_role.rolname IN ('rass_platform_executor_owner_v1', 'neon_superuser');

SELECT has_schema_privilege('rass_app_runtime_v2', 'app', 'CREATE') AS runtime_can_create,
       has_schema_privilege('rass_platform_admin_v1', 'app', 'CREATE') AS admin_can_create,
       has_schema_privilege('rass_platform_executor_owner_v1', 'app', 'CREATE') AS executor_can_create_temporarily;
```

Interromper se qualquer role administrativa mantiver `SUPERUSER`, `CREATEROLE`, `CREATEDB`, `REPLICATION`, `BYPASSRLS` ou membership em `neon_superuser`; se a runtime possuir `CREATE`; se a branch não for a descartável; ou se houver dependências/sessões inesperadas que impeçam um ajuste controlado.

### Alternativa se Neon não permitir ALTER ROLE seguro

Não usar o provisionador automático nem tentar contornar os atributos por migration. Solicitar ao suporte/console Neon um mecanismo de criação de role com atributos explícitos na branch, ou manter a administração global indisponível e não aplicar as migrations 15–17. Production permanece inalterada.

### Situação atual

- Migrations 11–16: **PENDENTES** na branch descartável.
- Migration 17 de hardening: **CRIADA LOCALMENTE, NÃO APLICADA**.
- Roles administrativas: **REPROVADAS** até o procedimento manual ser executado e validado.
- Production: **NÃO ALTERADA**.

### Execução autorizada na branch descartável

Em 2026-11-02, antes de qualquer escrita, foram confirmados novamente branch, parent, banco (`neondb`), usuário de conexão (`neondb_owner`), ausência de sessões das roles alvo e ausência de dependências compartilhadas. O lote transacional de `ALTER ROLE`, revogação de `neon_superuser`, grant temporário de membership e `CREATE` no schema foi rejeitado pelo executor Neon com `INVALID_ARGUMENT`, sem SQLSTATE PostgreSQL.

Uma nova consulta somente leitura confirmou que nenhuma alteração parcial ocorreu: as duas roles continuam com atributos excessivos. A execução foi interrompida; migrations 11–17 e grants adicionais não foram aplicados.

## Preparação das migrations 16/17 e scripts operacionais

Os scripts locais em `infra/neon/checkpoint13/` são separados por finalidade:

1. `01-prevalidate-platform-administration.sql` — somente leitura: identidade, atributos, opções da membership (`ADMIN`, `INHERIT`, `SET`), schema, migrations e RLS.
2. `02-temporary-ownership-permissions.sql` — escrita manual para migrations 16/17: concede ao `neondb_owner` somente `INHERIT FALSE, SET TRUE`, além de `USAGE, CREATE` temporários ao executor. Não usa `ADMIN OPTION`.
3. `03-post-migration-validation.sql` — somente leitura: histórico, owner/`SECURITY DEFINER`/`search_path`, `EXECUTE`, policies, RLS/FORCE RLS e remoção das permissões temporárias.
4. `04-diagnose-platform-administration.sql` — somente leitura: sessões, dependências e grants para diagnóstico de bloqueios.
5. `05-temporary-function-acl-permissions.sql` — escrita manual exclusiva da branch descartável antes da migration 18; restaura somente o `SET` transitório necessário para assumir o owner `NOLOGIN` das funções.
6. `06-post-function-acl-validation.sql` — somente leitura após a migration 18; exige ausência de `PUBLIC EXECUTE` e da execução pela runtime nas quatro funções.
7. `02-test-rbac-db-temporary-ownership-permissions.sql`, `05-test-rbac-db-temporary-function-acl-permissions.sql`, `09-test-rbac-db-temporary-executor-set.sql`, `10-test-rbac-db-cleanup-temporary-executor.sql` e `11-test-rbac-db-post-validation.sql` — roteiro exclusivo do banco vazio `rass_sprint1_tests` em branch descartável. Não são intercambiáveis com os scripts destinados ao banco `neondb` da branch de referência.

O orquestrador `scripts/checkpoint13/Invoke-RbacPostgresHomologation.ps1` nunca cria branch, banco ou roles. Ele exige confirmação externa da branch pelo Neon API, valida apenas variáveis de processo com endpoint direto/`VerifyFull`, aplica migrations em etapas, e usa `finally` para tentar a limpeza do grant temporário mesmo se uma migration ou teste falhar.

Fatos verificados: uma membership de `neondb_owner` no executor existe. Ela por si só não prova que a transferência de ownership seja válida. A pré-validação deve confirmar `set_option = true`; a remoção posterior precisa ser limitada ao concedente da concessão temporária, pois memberships de outros concedentes podem permanecer. A script 2 somente é usada depois de validar que as roles possuem os atributos mínimos.

### Correção PostgreSQL 18 — memberships com múltiplos concedentes

Uma validação posterior demonstrou duas concessões para `neondb_owner` → executor: `cloud_admin` concedeu `ADMIN = true, INHERIT = false, SET = false`; `neondb_owner` concedeu `ADMIN = false, INHERIT = false, SET = true`. A tentativa de adicionar `ADMIN OPTION` à própria concessão retornou `0LP01` por concessão administrativa circular. Portanto, a script 2 não tenta mais conceder `ADMIN OPTION`.

O comando temporário mantém somente `INHERIT FALSE, SET TRUE`, necessário para a transferência de ownership da migration 16. A migration 17 usa `REVOKE ... GRANTED BY neondb_owner`, removendo exclusivamente essa concessão temporária sem remover a concessão independente de `cloud_admin`. A pós-validação não trata `MEMBER = true` como erro isolado; exige `SET = false`, `USAGE = false`, ausência de `CREATE` no schema e ausência de uma linha em `pg_auth_members` cujo concedente seja `neondb_owner`.

`PlatformAdministrationIsolation` mantém `SECURITY DEFINER`, `search_path = pg_catalog, app` e políticas específicas para o executor. Na branch de validação, as ACLs efetivas ainda expõem `PUBLIC EXECUTE` após as migrations 16/17. A migration incremental `PlatformFunctionExecuteHardening` (18) assume transitoriamente o owner `NOLOGIN`, revoga `PUBLIC` e runtime, concede somente à role administrativa e falha se qualquer ACL residual permanecer. A migração de Production passa a requerer **18 migrations**; a décima oitava é um forward-fix de segurança, não altera RLS, ownership ou dados financeiros.

O executor consegue operar as funções com `FORCE ROW LEVEL SECURITY` porque as policies da migration 16 são específicas para `rass_platform_executor_owner_v1`; ele não recebe `BYPASSRLS`. A conexão administrativa (`rass_platform_admin_v1`) recebe somente `EXECUTE`; a autorização funcional do ator continua na API, antes da abertura dessa conexão. Como proteção adicional, a API valida e normaliza ator, alvo, justificativa e operação antes de chamar as funções.

O arquivo legado `PlatformAdministrationService.cs` não está registrado para `IPlatformAdministrationService`; a DI usa `IsolatedPlatformAdministrationService`. Ele deve permanecer fora de qualquer novo uso e sua remoção só pode ocorrer após uma revisão de compatibilidade separada.

### Regressão local desta rodada

- API Release: **APROVADO**, sem warnings.
- Unitários .NET: **50/50 APROVADOS**.
- Arquitetura .NET: **5/5 APROVADOS**.
- `flutter analyze`: **APROVADO**.
- `flutter test`: **23/23 APROVADOS**.
- Suíte PostgreSQL/API: resultado final registrado abaixo após execução exclusiva no banco dedicado `rass_sprint1_tests`.

### Forward fix 20 — ACL operacional de identidade e sessão

O banco isolado evidenciou que uma inicialização apenas por migrations não recebia os grants de identidade anteriormente mantidos apenas em `bootstrap-runtime.sql`. Isso bloqueava `IdentityStore.RegisterSessionAsync` em `app."UserSessions"` com `42501`, antes dos cenários RBAC funcionais.

A migration incremental `20261102040000_RuntimeIdentityOperationalAclHardening` torna os grants declarativos e idempotentes: `UserSessions` recebe `SELECT`, `INSERT` e `UPDATE`; `TenantMemberships` e `AuditEntries`, `SELECT`/`INSERT`; `Tenants`, `SELECT`/`INSERT`; `UserProfiles`, `SELECT`/`INSERT`/`UPDATE`. Ela não concede `DELETE`, `TRUNCATE`, `CREATE`, leitura do histórico EF, acesso às funções administrativas ou qualquer privilégio a `PUBLIC`.

As políticas RLS/FORCE RLS permanecem obrigatórias para cada linha. A pré-validação `12-test-rbac-db-pre-validation.sql` identifica precisamente o objeto/operação ausente ou excessivo antes do seed temporário; a pós-validação exige 20 migrations e revalida as ACLs.

### Homologação final RBAC — 2026-11-02

Ambiente homologado exclusivamente: branch descartável `br-odd-silence-b646inmx`, banco `rass_sprint1_tests`. A migration 20 foi aplicada nesse ambiente isolado; Production, a branch de referência e o banco `neondb` não foram alterados.

- Histórico EF: 20 migrations aplicadas.
- `PlatformRbacTests`: **10/10 aprovados**.
- Suíte PostgreSQL/API: **130/130 aprovados**.
- Cleanup de `SET ROLE` temporário: aprovado, sem concessão residual.
- Pós-validação de ACL, RLS, FORCE RLS, ownership e funções administrativas: aprovada.
- Runtime: sem `CREATE` no schema `app`, sem `EXECUTE` administrativo e sem `DELETE` operacional desnecessário.
- `PUBLIC`: sem `USAGE` no schema `app` e sem `EXECUTE` nas funções administrativas.

Os avisos observados sobre licença MediatR e `database.connection_missing` em cenários controlados da suíte não invalidaram os 130 testes aprovados. A configuração de readiness de Production permanece uma verificação independente antes de qualquer implantação.

**Veredito do Checkpoint 13: APROVADO NO AMBIENTE ISOLADO.** Esta evidência não autoriza alterações, deploy ou migrations em Production.

