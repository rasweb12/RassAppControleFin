# Plano de migration financeira P5

Nenhuma migration desta etapa foi aplicada em Neon.

## Recursos ainda sem modelo persistido

- `ExpenseReductionGoals` para metas de reducao;
- `FinancialInsights` para insights deterministas e estado de leitura;
- `Notifications` para central e deduplicacao;
- preferencias de perfil e consentimento de IA auditavel;
- extensoes de conta, categoria e transacao que demandam novos contratos.

## Pre-requisitos de uma migration local

1. Definir agregados e invariantes no Domain.
2. Criar commands, queries, validators e contratos API versionados.
3. Mapear tabelas no schema `app` com UUID, `TenantId`, `UserId`, versao,
   constraints e indices de consulta.
4. Habilitar e forcar RLS, criando politicas `USING` e `WITH CHECK` baseadas
   no contexto transacional atual.
5. Conceder somente privilegios minimos a `rass_app_runtime`.
6. Atualizar Drift, outbox/inbox e handlers de eventos conhecidos.
7. Gerar migration local, script SQL revisavel e testes de RLS/idempotencia.

## Rollback

O rollback deve ser uma migration compensatoria aprovada. Nenhuma tabela com
dados financeiros ou auditoria deve ser descartada automaticamente. Antes de
aplicar em qualquer branch remota, gerar branch Neon de homologacao, backup e
plano de validacao aprovado por humano.
