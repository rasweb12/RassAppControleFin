# Segurança RBAC e RLS

O Checkpoint 13 comprovou em banco isolado a separação entre perfis funcionais e roles técnicas PostgreSQL.

| Camada | Autoridade | Limite |
|---|---|---|
| User / TenantAdmin / SuperAdmin | API, claims validadas, memberships e `PlatformRoleAssignments` | Nenhum perfil funcional recebe conexão PostgreSQL direta. |
| Runtime `rass_app_runtime_v2` | Conexão financeira | Sem `CREATE`, sem acesso ao histórico EF, sem funções administrativas, sem DELETE de identidade. |
| Platform admin | Conexão isolada | Apenas `EXECUTE` explícito das funções `app.platform_*`; não é SuperAdmin funcional. |
| Executor owner | Role `NOLOGIN` | Proprietária das funções `SECURITY DEFINER`; sem `BYPASSRLS`. |

Production ainda não está alinhada. O upgrade deve preservar `RLS` e `FORCE RLS`; qualquer resultado diferente de runtime sem privilégio administrativo e `PUBLIC` sem execução administrativa é bloqueador P0.
