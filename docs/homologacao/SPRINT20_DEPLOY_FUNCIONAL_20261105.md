# Publicação restrita — 2026-11-05

Autorização: Metas, Redução de Despesas e Notificações; sem migrations ou escrita funcional de dados.

- Base de main: `552e8aadffd7fcd3294078fc944c5d4c1c4721e1`.
- Patch de origem: `b8c506379b5601702f0cb038ce6a0e3c88d42202`.
- Commit publicado: `f8d565e5830ffeb10c2cc63c6cc860d3ea7d972e`.
- Branch isolada: `codex/sprint20-functional-api-release`, integrada por fast-forward em main.
- Diff: 12 arquivos, 141 inserções e 5 remoções. Excluídos os incrementos acumulados de IA, monetização, sessão e Primeiro Me Pago. Os arquivos originais foram preservados.
- Render workspace: `tea-db67u04s728c73fgg84g`, confirmado pelo operador.
- Serviço API: `srv-db6850id0e5s739om5kg`.
- Deploy: `dep-dbm64jqvcj2c73859u6g`, status `live`, concluído em 2026-11-05T11:04:06Z.

## Validação da versão isolada

- Solução Release: sucesso, zero avisos e erros.
- Unitários: 58/58 aprovados.
- Arquitetura: 5/5 aprovados.
- Diff sem erros de whitespace.
- Nenhuma migration, configuração de infraestrutura ou arquivo Admin.Web integra o patch.

## Pós-deploy somente leitura

- GET /health/live: HTTP 200.
- GET /health/ready: HTTP 200.
- GET /api/v1/expense-reduction-opportunities?months=1: HTTP 401 sem autenticação.
- GET /api/v1/expense-reduction-opportunities?months=12: HTTP 401 sem autenticação.
- Logs error entre 11:04:06Z e 11:04:47Z: nenhum resultado.

## Limites da evidência

Não havia token de usuário disponível no processo. A validação autenticada dos períodos de 1–12 meses, rejeição de 0/13 e consistência dos valores permanece BLOQUEADA por sessão autenticada disponível por mecanismo seguro. Nenhum login, renovação, cadastro, alteração de meta, marcação de notificação ou escrita financeira foi realizado.

O Flutter de edição de Metas e análise por categoria permanece no trabalho local original; não foi incluído automaticamente junto às demais alterações acumuladas do commit 9e06b55. O deploy publica a API e o tratamento de erro de notificações versionado, mas não distribui um novo aplicativo Android.

MVP NO-GO / Google Play NO-GO. Publicação técnica aprovada; homologação funcional integrada ainda incompleta.
