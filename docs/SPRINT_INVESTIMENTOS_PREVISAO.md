# Sprint investimentos e previsao

## Bloco atual: persistencia e sincronizacao

Status: implementado localmente e pendente de aplicacao da migration em ambiente autorizado.

### Implementado

- Agregados financeiros isolados por tenant e usuario para `PayYourselfFirstPlan`, revisoes de vigencia, reserva de emergencia, ativos, movimentacoes e alocacoes de carteira-alvo.
- Valores monetarios em `decimal`, com arredondamento financeiro centralizado e sem uso de `double` nos calculos de planejamento.
- Historico append-only da estrategia "Primeiro me Pago" e movimentacoes de investimento.
- Validacao de carteira-alvo com pesos unicos e total exato de 100%.
- Endpoints autenticados e idempotentes para configuracao, ativos, movimentacoes e carteira-alvo.
- Consultas paginadas para ativos e movimentacoes; consultas unitarias para estrategia e reserva.
- Outbox financeira existente reutilizada para todos os comandos novos.
- Flutter sincroniza os novos eventos e snapshots pelo cache Drift existente.
- Migration local `20261030163745_InvestmentPlanningCore` com FKs, indices, constraints, RLS forcado e grants minimos para `rass_app_runtime`.

### Seguranca

- Todas as tabelas novas usam chave e filtro por `TenantId` e `UserId`.
- A migration habilita `ENABLE ROW LEVEL SECURITY` e `FORCE ROW LEVEL SECURITY` em todas as seis tabelas.
- A role de runtime recebe apenas `SELECT`/`INSERT` para registros append-only e `SELECT`/`INSERT`/`UPDATE` para configuracoes mutaveis.
- O readiness passa a exigir a migration de investimentos, evitando que uma API nova rode contra schema antigo.

### Validado localmente

- `dotnet build RassAppControleFin.slnx -c Release --no-restore -m:1 -v:minimal`: aprovado, 0 avisos e 0 erros.
- `dotnet test tests/RassAppControleFin.UnitTests/RassAppControleFin.UnitTests.csproj -c Release --no-restore -m:1 -v:minimal`: 39 aprovados.
- `flutter analyze`: aprovado, sem problemas.
- Script SQL idempotente da migration gerado localmente com sucesso.

### Pendente

- Aplicar a migration em ambiente Neon autorizado, validar RLS e sincronizacao em integracao e somente depois publicar a API.
- Criar a interface Flutter de investimentos e os fluxos offline de escrita para esses endpoints.
- Implementar previsao de caixa, dividas, receitas extraordinarias, reservas derivadas de despesas, metas de investimento, IA e telas de simulacao.
- Criar testes de integracao PostgreSQL para isolamento entre tenants e idempotencia dos endpoints de investimento.

### Ordem segura de publicacao

1. Aplicar `InvestmentPlanningCore` com uma conexao direta de migration no ambiente aprovado.
2. Verificar migrations, RLS, policies e grants com a role `rass_app_runtime`.
3. Publicar a API que exige essa migration no readiness.
4. Liberar o Flutter com as telas e a escrita offline do modulo.
