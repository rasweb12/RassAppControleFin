# Finalização do MVP — RassFin

Data: 2026-11-01  
Estado: **REPROVADO PARA PRODUCTION E PUBLICAÇÃO**

## Diagnóstico executivo

O código compila e a regressão local disponível está verde, mas a versão publicada não pode ser promovida enquanto Production tiver somente 10 migrations e a simulação das seis migrations pendentes estiver bloqueada pelo provisionamento inseguro de roles técnicas no Neon. A funcionalidade financeira não deve ser considerada completamente homologada apenas pelos builds e testes abaixo.

## Matriz de funcionalidades

| Funcionalidade | Flutter | API | Banco | Testes | Homologação | Bloqueadores | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Autenticação, sessão e perfil | Implementado | Implementado | RLS/identidade | Unitários e Flutter | Parcial histórica | Revalidar fluxo real antes de release | Parcial |
| Dashboard, contas e categorias | Implementado | Implementado | Persistência/RLS | Unitários e integração existente | Manual histórica | Integração atual não executada | Parcial |
| Receitas, despesas e transferências | Implementado | Implementado | Ledger/RLS | Flutter offline e integração existente | Manual histórica | Regressão PostgreSQL pendente nesta rodada | Parcial |
| Extrato, filtros e edição | Implementado | Implementado | Transações/RLS | Flutter | Manual histórica | Validar em APK atual | Parcial |
| Exclusão/estorno de transação | Parcial | Endpoint ainda se chama `cancel` | Auditoria/ledger | Não reexecutado | Não comprovada nesta rodada | Revisar distinção excluir x estornar | Pendente |
| Vencimentos, recorrências e parcelamentos gerais | Parcial | Parcial | Migration/contratos a confirmar | Não comprovado | Não comprovada | Cobertura funcional insuficiente | Pendente |
| Cartões, compra, fatura e pagamento | Implementado | Implementado | Requer 3 tabelas ausentes em Production | Fluxo isolado homologado anteriormente | Parcial isolada | Production sem migrations 11–16 | Bloqueado |
| Sincronização offline e reconciliação | Implementado | Implementado | Outbox/inbox | 8 testes Flutter de store/sync | Cartão isolado histórico | Retestar no APK atual | Parcial |
| Metas, orçamentos e redução de despesas | Implementado | Implementado | RLS | Não reexecutado | Histórico parcial | Fluxos de arquivo/reativação ainda exigem roteiro | Parcial |
| Investimentos e Primeiro me pago | Contratos e endpoints existem | Implementado | `InvestmentPlanningCore` | Não reexecutado ponta a ponta | Não comprovada | UX e regras de negócio exigem homologação | Pendente |
| RBAC tenant/global | Implementado | Policies `Permissions`/`Platform.*` | RLS e `PlatformRoleAssignments` após migration | Testes isolados históricos | Não em Production | Migrations e provisionamento técnico bloqueados | Bloqueado |
| Admin Web | Implementado parcialmente | API é autoridade | Sem acesso financeiro direto | Build Release | Não comprovada | Bloqueio visual por perfil funcional ausente | Parcial |
| Android Release | Configurado | N/A | N/A | Debug gerado | Não | Assinatura, AAB e checklist Play pendentes | Bloqueado |

## Backlog priorizado

### P0

1. Provisionar as roles técnicas da branch descartável com menor privilégio, por procedimento Neon supervisionado; não usar roles com `BYPASSRLS`, `CREATEROLE`, `CREATEDB`, `REPLICATION` ou herança indevida.
2. Simular as migrations 11–16 em `validate-checkpoint13-production-upgrade`; validar 27 tabelas, RLS/FORCE RLS, grants e funções `SECURITY DEFINER`.
3. Reexecutar a suíte PostgreSQL somente em `rass_sprint1_tests`, com segredos de teste apontando ao banco correto.
4. Homologar no APK atual: compra, pagamento de fatura, transação, transferência, exclusão/estorno e sincronização após reinicialização.

### P1

1. Expor ao Admin Web um contrato de perfil funcional assinado/resolvido no servidor para bloquear menu e rota de plataforma para `User`, preservando o 403 da API como proteção final.
2. Cobrir vencimentos, recorrências, parcelamentos gerais e a diferença entre excluir, cancelar edição e estornar.
3. Completar roteiro de reativação de metas e redução de despesas arquivadas.
4. Revisar artefatos temporários versionados (`.tmp-*.sql` e `.tmp-test-results/*.trx`) e remover somente após auditoria de dados/segredos e autorização explícita.

### P2

1. Medir inicialização Flutter e reduzir trabalho síncrono que cause perda de frames.
2. Completar UX de investimentos e Primeiro me pago, incluindo educação contextual e evidência de sincronização.

## Evidências desta rodada

- API Release: aprovado, 0 warnings.
- Admin Web Release: aprovado, 0 warnings.
- Unitários .NET: 47/47 aprovados.
- Arquitetura .NET: 5/5 aprovados.
- `flutter analyze`: aprovado.
- `flutter test`: 23/23 aprovados.
- APK Debug local: gerado em `mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-debug.apk`.

## Veredito

**REPROVADO.** Os bloqueadores P0 impedem declarar Production e Google Play prontos.
