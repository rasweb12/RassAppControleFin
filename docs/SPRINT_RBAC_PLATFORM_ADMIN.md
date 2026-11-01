# Sprint RBAC de Plataforma e Admin

## Estado

Implementação local e validação somente na branch Neon isolada `validate-credit-card-migrations-20261101`, banco `rass_sprint1_tests`. Production não foi alterada.

## Arquitetura

- `MembershipRole` continua exclusivo de vínculos de tenant: `User`, `TenantAdmin`, `FinancialAdmin` e `SupportAgent`.
- `PlatformRoleAssignment` é independente e possui, inicialmente, apenas `SuperAdmin`.
- A API resolve o papel global no servidor a cada requisição a partir da identidade JWT/sessão validada. Claims, headers e payloads do cliente não concedem papel global.
- As policies `Platform.*` protegem os endpoints `/api/v1/platform/*`.
- A role runtime continua `rass_app_runtime_v2`, sem `BYPASSRLS`, `SUPERUSER`, `CREATEDB` ou `CREATEROLE`.

## Migration

`20261101052030_PlatformRbacFoundation` cria `app.PlatformRoleAssignments`, índices, constraint de papel, RLS, FORCE RLS, policies explícitas e grants mínimos.

Ela foi aplicada somente no ambiente isolado. Não está autorizada para Production neste checkpoint.

## Endpoints disponíveis

- `GET /api/v1/platform/dashboard`
- `GET /api/v1/platform/users/{userId}/roles`
- `POST /api/v1/platform/users/{userId}/roles`
- `DELETE /api/v1/platform/users/{userId}/roles/{assignmentId}`

As mutações exigem confirmação explícita e motivo. Autopromoção, duplicidade, autorrevogação e remoção do último SuperAdmin são recusadas. Grants/revogações recebem advisory lock transacional, e o índice parcial continua protegendo a unicidade de concessões ativas.

## Admin Blazor

Foi adicionada a rota autenticada `/platform` com painel administrativo para os indicadores expostos pela API. A interface não substitui as policies da API e não mostra finanças privadas de terceiros.

## Limitações conhecidas

Listagem global de usuários, vínculos por tenant, auditoria global e gestão visual de papéis requerem uma fronteira administrativa de leitura global adicional. Ela não foi simulada com `IgnoreQueryFilters` ou desativação de RLS; portanto, permanece pendente.
