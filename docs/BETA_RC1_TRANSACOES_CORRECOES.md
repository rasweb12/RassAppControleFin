# Beta RC1 - Correcao de Transacoes

Data: 2026-10-31

## Causa raiz

1. **Edicao e cancelamento**: `AppDbContext.ValidateWrites` bloqueava qualquer entidade que implementasse o marcador de historico append-only. Os handlers de edicao e cancelamento, entretanto, atualizam o estado/versionamento da projecao `FinancialTransaction` e registram o efeito financeiro como `Adjustment` ou `Reversal` no livro-caixa. Isso terminava em `InvalidOperationException` antes da gravacao.
2. **Transferencias ausentes do extrato**: `GET /api/v1/transactions` consultava somente `Transactions`. As transferencias eram gravadas corretamente em `Transfers` e em duas entradas de livro-caixa, mas eram expostas apenas por `GET /api/v1/transfers`; a tela de extrato nunca as recebia.
3. **Fila offline de edicao**: a sincronizacao reenviava toda operacao pendente por `POST`. Uma edicao necessita de `PUT`, portanto uma edicao pendente nao poderia ser reproduzida corretamente.

## Correcoes locais

- Permite somente a atualizacao controlada de `FinancialTransaction`; `LedgerEntry`, demais registros append-only e `Transfer` continuam imutaveis.
- Une transacoes e transferencias na projecao de `GET /api/v1/transactions`, mantendo transferencia fora dos totais de receita/despesa e do patrimonio consolidado.
- Inclui `entryType`, conta de origem e conta de destino no contrato de extrato.
- Exibe transferencias com tipo, origem, destino, valor, data e status; elas nao oferecem edicao/cancelamento pela tela de lancamentos.
- Cria uma entrada local de extrato para transferencia pendente offline e preserva recursos locais pendentes durante reconciliacao.
- Reenvia edicoes pendentes por `PUT`; cancelamentos continuam `POST` e preservam o mesmo `operationId` em retentativas.
- Atualiza cache local, saldo e resumo ao editar ou cancelar offline.

## Arquivos modificados

- `src/RassAppControleFin.Infrastructure/Persistence/AppDbContext.cs`
- `src/RassAppControleFin.Infrastructure/Persistence/FinanceReadStore.cs`
- `src/RassAppControleFin.Application/Features/Finance/FinanceContracts.cs`
- `src/RassAppControleFin.Contracts/FinanceContracts.cs`
- `mobile/rassapp_controlefin/lib/core/storage/offline_finance_store.dart`
- `mobile/rassapp_controlefin/lib/features/finance/finance_repository.dart`
- `mobile/rassapp_controlefin/lib/features/synchronization/finance_sync_service.dart`
- `mobile/rassapp_controlefin/lib/features/home/finance_home_shell.dart`
- `tests/RassAppControleFin.UnitTests/FinanceDomainTests.cs`

## Testes executados

| Verificacao | Resultado |
| --- | --- |
| `dotnet build RassAppControleFin.slnx -c Release --no-restore` | Aprovado |
| `dotnet test tests/RassAppControleFin.UnitTests/RassAppControleFin.UnitTests.csproj -c Release --no-restore` | Aprovado: 40/40 |
| `flutter analyze` | Aprovado: sem issues |
| `flutter test` | Aprovado: 21/21 |
| Integracao PostgreSQL/RLS | Nao concluido: a execucao nao retornou antes do limite e foi encerrada; nao considerado aprovado |
| Emulador com dados reais | Pendente |

## Riscos e pendencias

- A correcao de API e Flutter ainda nao foi publicada, por restricao desta rodada. O ambiente Render continua com a versao anterior ate haver autorizacao especifica.
- E necessario homologar no emulador: editar receita/despesa, cancelar duas vezes a mesma operacao, transferencia no extrato e nos dois historicos de conta, reinicio e sincronizacao offline/online.
- A suite PostgreSQL/RLS precisa ser executada em ambiente de integracao disponivel para comprovar RLS, concorrencia e idempotencia ponta a ponta.
- Nenhuma migration, deploy, push ou alteracao no Neon/Render foi realizada nesta rodada.

## Prontidao do Beta

**Reprovado para distribuicao neste momento**, ate a homologacao no emulador e a suite de integracao PostgreSQL/RLS serem concluidas com sucesso. O patch local esta pronto para essas validacoes, mas ainda nao e um artefato publicado.
