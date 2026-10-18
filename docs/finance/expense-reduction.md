# Reducao de despesas

O produto ainda nao possui uma entidade persistida de meta de reducao. O
`Budget` existente representa um limite por periodo e nao deve ser reutilizado
como meta de economia, pois as duas regras possuem semanticas diferentes.

A proxima evolucao deve criar `ExpenseReductionGoal` com categoria opcional,
periodo de referencia, limite planejado, economia esperada, status e versao.
O gasto realizado deve ser calculado deterministicamente a partir das
transacoes autorizadas; Gemini pode apenas explicar o resultado.

Nao ha tela de reducao nesta entrega para evitar apresentar dados ou alertas
sem persistencia, calculo historico equivalente e sincronizacao confiavel.
