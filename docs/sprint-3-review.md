# Sprint 3 - Core Finance

Status: concluida para o nucleo financeiro do backend e API. Mobile de escrita offline fica para a Sprint 4.

## Entregas

- Agregados financeiros com UUID, UTC, moeda BRL/USD/EUR, duas casas decimais e concorrencia otimista.
- Contas, categorias, transacoes, transferencias e cartoes sem armazenamento de PAN, CVV ou outros dados de cartao.
- Transferencia atomica: debito, credito, registro de transferencia e dois movimentos de ledger na mesma transacao.
- Idempotencia por TenantId + OperationId, com hash do payload e recibo persistido para repeticoes.
- RLS por tenant e usuario para todas as tabelas financeiras; runtime sem privilegios de DDL, owner ou bypass.
- Outbox financeiro e auditoria de metadados na mesma transacao de negocio.
- Endpoints HTTP protegidos por policies; TenantId e UserId nao existem nos DTOs de escrita.

## Migrations

- CoreFinance cria o esquema.
- CoreFinanceHardening e incremental/idempotente para bancos de teste que receberam a primeira migration antes do endurecimento. Ela garante RLS, grants e o trigger diferido de transferencia.
- Aplicar com conexao Neon direta de migration; a API usa o pooler com a role restrita.

## Verificacao

- Build Release: zero avisos e zero erros.
- Integracao Neon dedicada: 103 testes aprovados.
- Teste critico aprovado: Conta A 1.000, Conta B 500, transferencia 300, saldos finais 700 e 800.
- Repeticao idempotente, reutilizacao divergente de OperationId e isolamento entre usuarios cobertos.

## Adiado deliberadamente

- SQLite, outbox local e sincronizacao offline: Sprint 4.
- Dashboard e experiencia financeira no Mobile: Sprint 5/11.
- Gemini: Sprint 8, exclusivamente no backend.
