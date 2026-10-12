# Sprints 4 a 6 - Offline, dashboard e planejamento

## Sprint 4 - Fundacao offline-first

- O Mobile usa SQLite apenas como armazenamento local, em `SqliteSyncStore`; o PostgreSQL/Neon continua sendo o system of record.
- A outbox guarda operacoes com identificadores UUID, tenant, usuario, dispositivo, entidade, versao, payload, tentativas, erro e lease.
- A inbox deduplica operacoes recebidas. O cursor de sincronizacao e persistido por tenant e usuario.
- O escopo local e definido somente apos autenticacao e selecao de tenant. Operacoes fora do tenant, usuario ou dispositivo ativos sao rejeitadas.
- `SyncEngine` usa lote, lease, acknowledgement, retry e encaminhamento explicito de conflitos para `ISyncConflictResolver`.
- O transporte continua uma porta (`ISyncTransport`): nenhum transporte HTTP de producao foi registrado antes de existir contrato de lote autenticado e mapeamento seguro de cada comando financeiro. Assim, a aplicacao nao simula uma sincronizacao que ainda nao pode garantir atomicidade e idempotencia de ponta a ponta.

## Sprint 5 - Dashboard

- O dashboard e calculado deterministicamente no backend a partir de saldos, transacoes postadas, orcamentos e metas autorizados.
- O contrato retorna patrimonio, receitas, despesas, taxa de economia, uso de orcamento e quantidade de metas por moeda.
- A tela Mobile "Resumo" consome apenas o agregado autorizado da API para o mes corrente; nao recalcula saldos localmente.

## Sprint 6 - Orcamentos e metas

- Foram adicionados os agregados `Budget` e `Goal`, comandos CQRS, validadores, consultas, endpoints e contratos.
- Orçamentos podem ser gerais ou associados a uma categoria de despesa. O valor gasto e derivado de transacoes postadas no periodo, nunca informado pelo cliente.
- Metas preservam moeda, valor alvo, progresso, estado e data opcional. Progresso acima do alvo, moeda divergente e alteracao de metas inativas sao recusados.
- A migracao `PlanningAndDashboard` cria tabelas, indices, constraints, RLS forçado e grants minimos para `rass_app_runtime`.

## Validacao

- Arquitetura: 5 testes aprovados.
- Integracao Neon isolada: 104 testes aprovados, incluindo RLS de orcamento e meta.
- Unitarios: 21 testes aprovados.
- Android Release: compilacao aprovada, sem avisos ou erros.

## Ambiente Neon

O banco exclusivo de testes recebeu e validou a migracao automaticamente pela fixture de integracao. A aplicacao na branch de desenvolvimento foi tentada duas vezes pelo utilitario protegido e recusada com `InvalidOperationException`, sem detalhes por desenho para proteger a string de conexao. Nenhuma credencial foi alterada e a producao nao foi acessada. A aplicacao no desenvolvimento deve ser repetida somente apos revisar a configuracao `RASS_MIGRATIONS_CONNECTION` do secret store.
