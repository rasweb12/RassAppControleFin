# Validação de segurança RBAC

## Ambiente validado

- Branch isolada: `validate-credit-card-migrations-20261101`
- Banco: `rass_sprint1_tests`
- Runtime: `rass_app_runtime_v2`
- SSL: `VerifyFull`

## Evidências

- `PlatformRoleAssignments` possui RLS e FORCE RLS.
- A runtime recebe somente `SELECT`, `INSERT` e `UPDATE` na tabela; não recebe `DELETE`, propriedade, `BYPASSRLS`, `SUPERUSER`, `CREATEDB` ou `CREATEROLE`.
- Policies de leitura permitem apenas a própria concessão; policies administrativas exigem gate transacional configurado somente após o serviço comprovar uma concessão ativa do ator.
- Testes sintéticos validam negação para papéis de tenant, aceitação para SuperAdmin, ausência de privilégio financeiro multi-tenant, duplicidade, autopromoção, concorrência de concessão, revogação e auditoria.

## Risco residual

Uma futura leitura global de usuários/tenants não pode ser implementada usando `IgnoreQueryFilters` como bypass de RLS. Ela deverá usar uma fronteira administrativa dedicada, com autorização no servidor e auditoria, antes de ser liberada.
