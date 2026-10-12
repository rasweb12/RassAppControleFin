# Sprint 3 - Core Finance

Domain BCL-only: Money, Account, Category, FinancialTransaction, Transfer, CreditCard.
UUIDs, UTC, decimal de duas casas, currency BRL/USD/EUR, versao explicita.
Sem cambio/overdraft. Saldo inicial vira ledger; API nao altera saldo diretamente.

Application coordena CQRS/MediatR/validators e unidade de trabalho. Infrastructure
implementa EF/Npgsql, RLS, locks, auditoria, receipts/outbox. Sem regras em UI/API.

Transfer imutavel: origem/destino, debito/credito, ledger, audit, outbox e receipt
na mesma transacao. Falha parcial/conflito desfaz todos os lados.

Mutacoes exigem OperationId: unique TenantId+OperationId, hash canonico, resposta
persistida e lock transacional para duplicatas concorrentes. Reuso com outro
payload retorna conflito. Outro usuario nunca recebe receipt alheio.

Concorrencia otimista via Version; sem last-write-wins/retry cego. RLS exige
tenant/usuario/membership ativo e FKs compostas impedem referencias cruzadas.

Categorias/cartoes versionados; sem PAN/CVV. Arquivamento preserva historico.
Transacoes pending/posted/cancelled com ajustes/estornos via ledger append-only.
Compras/cartoes/pagamentos separados para nao duplicar despesas bancarias.

Queries paginadas/projetadas. Filtros e resumo de transacoes com mesmo predicate.
Mobile e unico frontend do usuario. Admin e operacional; IA fica na Sprint 8.
Offline/sync na Sprint 4: idempotencia online nao equivale a sync pronto.

Gate: teste 1000/500 -> 700/800, duplicate/retry/payload alterado, concorrencia,
stale version, falha parcial, RLS CRUD, autorizacao/user/tenant isolation,
Domain unit tests, PostgreSQL real/API, Android build e documentacao/revisoes.
