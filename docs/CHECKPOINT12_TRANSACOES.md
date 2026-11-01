# Checkpoint 12 — Extrato, datas e transferências

## Implementado localmente

- Criação de receita, despesa e transferência com seleção explícita da data
  efetiva.
- Edição de lançamento com alteração de valor, descrição e data efetiva.
- Serialização da data como meio-dia UTC, evitando que o dia civil selecionado
  seja deslocado nos fusos horários brasileiros durante a reconciliação
  Flutter, Drift, API e PostgreSQL.
- Extrato mensal com navegação entre meses, busca textual, filtros por receita,
  despesa e transferência, ordenação crescente/decrescente e agrupamento por
  data efetiva.
- Cartão mensal de entradas, saídas e saldo líquido. Transferências não entram
  nos totais de receita ou despesa.
- Transferências continuam uma única operação lógica no extrato geral e exibem
  origem, destino, data e estado.

## Regras preservadas

- O contrato existente já persiste `OccurredAt` e o agregado financeiro aplica
  alterações através do handler/ledger existente, com versão e `OperationId`.
- Operações offline mantêm o mesmo `OperationId`, cache local e outbox.
- A reconciliação não substitui um extrato local não vazio por uma resposta
  remota vazia.
- O parcelamento de cartões existente foi preservado; não foi criada uma segunda
  estrutura de fatura ou parcelas.

## Itens que exigem nova evolução de domínio

Vencimento independente, séries recorrentes e parcelamentos que não pertencem
a cartões não existem no domínio, contratos, Drift ou schema atuais. Criá-los
corretamente exige entidades próprias, endpoints, outbox, políticas RLS e uma
migration incremental. Nenhuma migration foi criada ou aplicada neste
checkpoint porque a execução foi explicitamente restrita a não aplicar
migrations. Esses itens não são declarados implementados.

## Testes executados

- `flutter analyze`: aprovado, sem issues.
- `flutter test`: 21/21 aprovados.
- `dotnet build ... -c Release`: aprovado, sem warnings.
- Testes unitários .NET: 47/47 aprovados.
- Integração PostgreSQL/API no banco isolado: 116/116 aprovados.

## Ambiente e limites

Os testes de integração usaram somente o ambiente Neon isolado já autorizado.
Não houve migration, deploy, commit, push, alteração no Render ou alteração em
Production.
