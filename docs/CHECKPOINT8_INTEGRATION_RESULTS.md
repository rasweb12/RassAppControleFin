# Checkpoint 8 e RBAC — resultados de integração

## Resultados anteriores preservados

- Testes unitários: 47/47 aprovados.
- Integração PostgreSQL/API antes desta extensão: 106/106 aprovados.

## Extensão RBAC

Arquivo: `tests/RassAppControleFin.IntegrationTests/Database/PlatformRbacTests.cs`.

Resultado local contra o Neon isolado: **8/8 aprovados** na extensão RBAC; a regressão completa ficou em **113/113 aprovados**.

Cobertura: negação para `User`, `TenantAdmin`, `FinancialAdmin` e `SupportAgent`; acesso de SuperAdmin sintético; ausência de acesso financeiro multi-tenant; autopromoção; duplicidade; concessão concorrente; revogação; proteção do último administrador e auditoria.

## Correções encontradas pelos testes

1. A revogação inicialmente retornava sucesso sem persistir porque o contexto EF é `NoTracking` por padrão. A consulta de revogação agora usa `AsTracking()`.
2. A validação de escrita bloqueava a revogação por outro SuperAdmin ao restringir a alteração ao criador da concessão. A autorização agora é comprovada antes da escrita e reforçada pela policy RLS transacional.

Nenhum teste, migration ou operação deste checkpoint foi executado em Production.
