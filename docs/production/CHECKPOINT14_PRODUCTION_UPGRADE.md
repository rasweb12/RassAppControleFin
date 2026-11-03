# Checkpoint 14 — Upgrade controlado de Production

Status: **PREPARADO, NÃO AUTORIZADO PARA ESCRITA EM PRODUCTION**
Escopo: branch Neon `br-lucky-paper-b6egkr8q`, database `neondb`.

## Inventário do delta

Production foi auditada em modo somente leitura com 10 migrations, terminando em `20261030163745_InvestmentPlanningCore`. O código e a homologação isolada possuem 20. O upgrade deve manter a ordem abaixo:

| # | Migration | Dependência e impacto |
|---:|---|---|
| 11 | `20261101013424_CreditCardBillingCore` | Novas tabelas de compras, faturas e parcelas; altera constraint/colunas de cartões; cria RLS e índices. |
| 12 | `20261101030821_CreditCardBillingMovements` | Vincula movimentos já existentes a compra/fatura. |
| 13 | `20261101031055_CreditCardBillingIndexes` | Índices de suporte ao faturamento. |
| 14 | `20261101040238_CreditCardModelAlignment` | Migration vazia de alinhamento de modelo; mantém o histórico EF. |
| 15 | `20261101052030_PlatformRbacFoundation` | Cria `PlatformRoleAssignments`, RLS e a base global de RBAC. |
| 16 | `20261101100000_PlatformAdministrationIsolation` | Exige roles técnicas pré-provisionadas; cria funções `SECURITY DEFINER`. |
| 17 | `20261102010000_PlatformAdministrationHardening` | Remove grants temporários e reduz ACLs administrativas. |
| 18 | `20261102020000_PlatformFunctionExecuteHardening` | Corrige `PUBLIC EXECUTE`; exige temporariamente `SET ROLE` do owner. |
| 19 | `20261102030000_RuntimeSchemaUsageHardening` | Concede somente `USAGE` no schema `app` à runtime. |
| 20 | `20261102040000_RuntimeIdentityOperationalAclHardening` | Declara as ACLs mínimas de identidade/sessão e remove DELETE/histórico EF da runtime. |

Nenhuma migration de subida faz `DROP TABLE`, `TRUNCATE` ou `DELETE`. Há locks curtos esperados em `CreditCards`/`CardMovements` durante FKs, colunas, constraints e índices. As migrations 16–20 são forward-only; recuperação é por forward-fix ou branch de recuperação, nunca por `Down` automático.

## Segurança e roles técnicas

As roles PostgreSQL não representam `User`, `TenantAdmin` ou `SuperAdmin`. A API continua sendo a única autoridade dos perfis funcionais. Em Production, antes de executar migration alguma, deve existir e ser comprovado:

| Role | Requisito |
|---|---|
| `rass_app_runtime_v2` | LOGIN técnico restrito, sem `CREATE`, `DELETE` em `Tenants`/`UserProfiles`, leitura de histórico EF ou execução de `app.platform_*`. |
| `rass_platform_admin_v1` | LOGIN, `NOINHERIT`, sem atributos elevados; somente `EXECUTE` explícito nas quatro funções administrativas depois da migration 18. |
| `rass_platform_executor_owner_v1` | `NOLOGIN`, `NOINHERIT`, proprietária das funções `SECURITY DEFINER`, sem `BYPASSRLS`. |

Não usar o provisionador Neon se ele não puder garantir esses atributos sem membership em `neon_superuser`. O roteiro manual em `infra/neon/production/02-provision-platform-roles-manual.sql` só é executável após confirmação explícita de escopo da branch e por identidade administrativa Neon adequada.

## Sequência de implantação proposta

1. Criar branch Neon de recuperação a partir de Production e anotar ID, horário e endpoint.
2. Executar `infra/neon/production/01-production-preflight.sql` no SQL Editor de Production, somente leitura; salvar o resultado sem segredos.
3. Provisionar e verificar as duas roles técnicas pelo procedimento manual supervisionado. Interromper se houver atributo elevado, `neon_superuser`, `BYPASSRLS` ou dúvida sobre escopo de role.
4. Em uma janela controlada, executar apenas a etapa 1 (migrations 11–17). A migration 17 remove o `SET ROLE` transitório.
5. Executar `infra/neon/production/03-production-post-stage-validation.sql`. Conferir 17 migrations, RLS/FORCE RLS e nenhum `PUBLIC EXECUTE` ainda aceito como estado final.
6. Conceder transitoriamente somente `SET ROLE` para `neondb_owner` conforme comentário no script 02; executar a etapa 2 (migrations 18–20). A migration 18 remove esse grant.
7. Executar novamente o script 03 e os smoke tests de API: `/health/live`, `/health/ready`, autenticação, tenant, transação, cartão, compra/fatura/pagamento e endpoints Platform protegidos.
8. Conferir que a connection string de runtime do Render aponta à branch Production, role runtime e TLS `VerifyFull`, sem revelar valor. Somente então autorizar deploy compatível.

## Orquestrador

O orquestrador é `scripts/production/Invoke-ProductionUpgrade.ps1`. É compatível com Windows PowerShell 5.1 e aceita exclusivamente a variável de **processo** `RASS_PRODUCTION_MIGRATIONS_CONNECTION`; não lê variáveis de teste nem User Secrets.

Dry run (somente leitura; não executar sem confirmar previamente o endpoint Production):

```powershell
.\scripts\production\Invoke-ProductionUpgrade.ps1 `
  -ExpectedHost '<endpoint-direto-production>' `
  -NeonProductionBranchConfirmed
```

Etapa 1 com escrita, sujeita a autorização explícita posterior:

```powershell
.\scripts\production\Invoke-ProductionUpgrade.ps1 `
  -ExpectedHost '<endpoint-direto-production>' `
  -NeonProductionBranchConfirmed -Apply -Stage PlatformHardening `
  -ConfirmProductionWrite 'APPLY-CHECKPOINT14-PRODUCTION'
```

Etapa 2 só após pós-validação independente da etapa 1 e nova concessão transitória revisada:

```powershell
.\scripts\production\Invoke-ProductionUpgrade.ps1 `
  -ExpectedHost '<endpoint-direto-production>' `
  -NeonProductionBranchConfirmed -Apply -Stage FinalHardening `
  -ConfirmProductionWrite 'APPLY-CHECKPOINT14-PRODUCTION'
```

O script aborta se: branch confirmada não for a allowlist, endpoint for pooler, TLS não for `VerifyFull`, a conexão não for `neondb_owner`/`neondb`, o baseline de migrations não for o esperado, as roles prévias não existirem ou qualquer comando retornar falha. Ele não apaga dados e não executa rollback.

## Recuperação

Não execute downgrade das migrations 16–20. Em falha, interrompa a API/deploy compatível, preserve o banco e trabalhe em forward-fix usando a branch de recuperação para reproduzir. Como os dados atuais são sintéticos, uma reinicialização controlada pode ser avaliada depois de inventário e autorização explícita, mas não é parte deste plano.

## GO / NO-GO atual

**NO-GO para escrita em Production.** Falta criar/verificar a branch de recuperação, provisionar as roles técnicas com atributos mínimos em escopo comprovado, executar o dry run com credencial de Production e fazer a simulação atualizada de 10→20 contra clone recente de Production.

## Evidência de provisionamento inicial — 2026-11-03

Após autorização explícita, as roles técnicas foram criadas exclusivamente em Production (`br-lucky-paper-b6egkr8q` / `neondb`) por SQL administrativo direto. Nenhuma migration, grant de schema, alteração de RLS, dado de aplicação ou configuração Render foi executada.

| Role | LOGIN | INHERIT | SUPERUSER / CREATEROLE / CREATEDB / BYPASSRLS | Schema `app` |
|---|---:|---:|---:|---|
| `rass_platform_executor_owner_v1` | não | não | todos não | sem `USAGE`, sem `CREATE` |
| `rass_platform_admin_v1` | sim | não | todos não | sem `USAGE`, sem `CREATE` |

O Neon registrou memberships independentes, concedidas por `cloud_admin`, de `neondb_owner` para ambas as roles. A verificação efetiva confirmou `INHERIT = false`, `SET = false` e `USAGE = false`; portanto, `neondb_owner` não herda nem pode assumir essas roles neste estado. A associação não foi removida, pois ela não é uma concessão temporária da aplicação e a sua remoção exigiria decisão de segurança específica.

O próximo bloqueio é a criação de uma branch de recuperação e a concessão transitória, controlada e revisada, necessária somente durante as migrations 16 e 18. Production continua **NO-GO** até a simulação atualizada, aplicação em etapas, pós-validação e smoke tests.
