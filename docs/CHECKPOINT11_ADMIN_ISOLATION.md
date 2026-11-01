# Checkpoint 11 — isolamento administrativo global

## Implementação local

Foi preparado um executor administrativo separado da runtime financeira:

- `PlatformAdministrationOptions` recebe uma conexão exclusiva da role `rass_platform_admin_v1`.
- `PlatformAdministrationDataSource` só abre essa conexão quando a configuração passa a mesma validação TLS/role da conexão financeira.
- `IsolatedPlatformAdministrationService` verifica o papel global pelo caminho runtime de leitura e executa alterações apenas pelo datasource administrativo.
- A role de login administrativa recebe apenas `EXECUTE` em funções fixas; ela não recebe grants diretos nas tabelas de papéis, auditoria, tenants ou perfis.
- A role sem login `rass_platform_executor_owner_v1` é proprietária das funções e recebe as policies/grants mínimos necessários para executar suas operações atomicas.
- Não existe fallback para `rass_app_runtime_v2`: sem a conexão administrativa válida, endpoints de mutação respondem indisponibilidade controlada.
- A URL de revogação agora transmite o `userId` ao serviço, que confirma que ele é o dono da atribuição antes de revogar.

## Migration aplicada e validada no ambiente isolado

`20261101100000_PlatformAdministrationIsolation`:

1. exige que `rass_platform_admin_v1` exista;
2. revoga `INSERT` e `UPDATE` de `rass_app_runtime_v2` em `PlatformRoleAssignments`;
3. mantém somente `SELECT` de atribuições próprias para a runtime;
4. recria policies RLS separadas por role, sem gate `app.platform_authorized` para escrita;
5. concede ao proprietário sem login das funções somente o necessário para papéis, auditoria e agregados de usuários/tenants; a role de login recebe somente `EXECUTE`, e nenhuma role administrativa recebe acesso a tabelas financeiras;
6. não possui downgrade automático, pois reintroduzir a escrita insegura seria um rollback inseguro. A recuperação deve ser uma correção aditiva aprovada.

## Evidências no ambiente isolado

Ambiente validado: branch `validate-credit-card-migrations-20261101`, banco
`rass_sprint1_tests`. A migration `PlatformAdministrationIsolation` consta no
histórico e `PlatformRoleAssignments` permanece com RLS e FORCE RLS ativos.

- `rass_app_runtime_v2` possui somente `SELECT` sobre
  `PlatformRoleAssignments`; `INSERT` e `UPDATE` são negados inclusive quando
  a sessão define GUCs `app.user_id` e `app.platform_authorized`.
- A runtime não possui `EXECUTE` nas quatro funções `app.platform_*`.
- `rass_platform_executor_owner_v1` é `NOLOGIN` e não lê `Accounts` nem
  `Transactions`. Ela possui apenas os grants administrativos indispensáveis,
  incluindo `CREATE` no schema `app` para ser proprietária das funções.
- `rass_platform_admin_v1` não possui grants diretos nas tabelas e recebe
  somente `EXECUTE` nas funções administrativas fixas.
- Apenas o proprietário da migration (`neondb_owner`) é membro do executor;
  runtime e login administrativo não podem assumir essa identidade.

## Sequência de implantação futura

1. Criar as roles `rass_platform_executor_owner_v1` (sem login) e `rass_platform_admin_v1` (login) com o script `infra/neon/bootstrap-platform-admin.sql`, em ambiente isolado e usando segredo fora do Git. Conceder o executor somente ao proprietário de migration e `CREATE` somente a ele no schema `app`.
2. Configurar `PlatformAdministration__ConnectionString` e `PlatformAdministration__Role` no ambiente isolado, com `VerifyFull` e a role exata.
3. Aplicar `PlatformAdministrationIsolation` no ambiente isolado.
4. Validar, com a runtime real, que `INSERT`, `UPDATE` e `set_config('app.platform_authorized','true',true)` não permitem mutar `PlatformRoleAssignments`.
5. Validar concessão/revogação pelo executor, concorrência, auditoria e correspondência URL/assignment.
6. Publicar o código somente após os testes isolados passarem. Production requer autorização separada.

## Rollback

Não reverter para grants de escrita da runtime. Se ocorrer falha no executor, manter a runtime em leitura e aplicar uma correção forward-only ou restaurar uma versão do executor que use a mesma role administrativa.

## Estado

- Código local: preparado; regressão completa ainda depende de credencial local
  da role administrativa isolada.
- Migration: aplicada exclusivamente no banco isolado.
- Neon/Render/Production: Production, Render e GitHub não foram alterados.
- Testes negativos PostgreSQL: INSERT, UPDATE, GUCs e EXECUTE pela runtime
  foram rejeitados. Falta executar os testes de API/RBAC usando uma credencial
  local válida de `rass_platform_admin_v1`.
