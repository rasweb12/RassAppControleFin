# Finalização do MVP — RassFin

Data: 2026-11-03
Estado: **REPROVADO PARA PUBLICAÇÃO; evidências históricas de Production devem ser lidas em `docs/homologacao/`**

## Diagnóstico executivo

O código compila e a regressão local disponível está verde, mas a funcionalidade financeira não deve ser considerada completamente homologada apenas pelos builds e testes. Este documento preserva o diagnóstico de MVP; os bloqueadores de migrations 10→20 foram tratados nos Checkpoints 13 e 14 e não devem ser interpretados como estado atual de Production.

## Matriz de funcionalidades

| Funcionalidade | Flutter | API | Banco | Testes | Homologação | Bloqueadores | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Autenticação, sessão e perfil | Implementado | Implementado | RLS/identidade | Unitários e Flutter | Parcial histórica | Revalidar fluxo real antes de release | Parcial |
| Dashboard, contas e categorias | Implementado | Implementado | Persistência/RLS | Unitários e integração existente | Manual histórica | Integração atual não executada | Parcial |
| Receitas, despesas e transferências | Implementado | Implementado | Ledger/RLS | Flutter offline e integração existente | Manual histórica | Regressão PostgreSQL pendente nesta rodada | Parcial |
| Extrato, filtros e edição | Implementado | Implementado | Transações/RLS | Flutter | Manual histórica | Validar em APK atual | Parcial |
| Exclusão/estorno de transação | Parcial | Endpoint ainda se chama `cancel` | Auditoria/ledger | Não reexecutado | Não comprovada nesta rodada | Revisar distinção excluir x estornar | Pendente |
| Vencimentos, recorrências e parcelamentos gerais | Parcial | Parcial | Migration/contratos a confirmar | Não comprovado | Não comprovada | Cobertura funcional insuficiente | Pendente |
| Cartões, compra, fatura e pagamento | Implementado | Implementado | Schema 20 migrations relatado como aplicado | Fluxo isolado homologado anteriormente | Parcial isolada | Retestar no APK atual e smoke do deploy | Parcial |
| Sincronização offline e reconciliação | Implementado | Implementado | Outbox/inbox | 8 testes Flutter de store/sync | Cartão isolado histórico | Retestar no APK atual | Parcial |
| Metas, orçamentos e redução de despesas | Implementado | Implementado | RLS | Não reexecutado | Histórico parcial | Fluxos de arquivo/reativação ainda exigem roteiro | Parcial |
| Investimentos e Primeiro me pago | Contratos e endpoints existem | Implementado | `InvestmentPlanningCore` | Não reexecutado ponta a ponta | Não comprovada | UX e regras de negócio exigem homologação | Pendente |
| RBAC tenant/global | Implementado | Policies `Permissions`/`Platform.*` | RLS e `PlatformRoleAssignments` | Testes isolados históricos | Aprovado isolado; smoke Production pendente | Regressão/smoke do deploy atual | Parcial |
| Admin Web | Implementado parcialmente | API é autoridade | Sem acesso financeiro direto | Build Release | Não comprovada | Bloqueio visual por perfil funcional ausente | Parcial |
| Android Release | Configurado | N/A | N/A | Debug gerado | Não | Assinatura, AAB e checklist Play pendentes | Bloqueado |

## Backlog priorizado

### P0

1. Homologar no APK atual: compra, pagamento de fatura, transação, transferência, exclusão/estorno e sincronização após reinicialização.
2. Executar smoke atual do deploy e validar readiness PostgreSQL em janela recente.
3. Aplicar e testar a migration de monetização apenas em ambiente isolado; não habilitar produtos ou cobrança.
4. Implementar o fluxo Flutter completo de Primeiro Me Pago antes de anunciá-lo como recurso do MVP.

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
