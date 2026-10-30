# Plano de Beta Fechado

## Escopo

O Beta fechado valida um APK Release assinado, nunca uma publicacao em loja.
Usar contas de teste autorizadas e dados nao sensiveis. Nao usar credenciais de
producao em capturas, logs ou relatos.

## Pre-requisitos

- API candidata saudavel em `/health/live` e `/health/ready`.
- Migrations esperadas verificadas no ambiente aprovado.
- RLS, policies e grants validados para a role de runtime.
- APK Release assinado fora do repositorio e identificado por versao/build.
- Plano de rollback: interromper a distribuicao e preservar outbox/local DB;
  nao limpar dados como resposta a uma falha.

## Casos obrigatorios

| ID | Caso | Resultado esperado |
|---|---|---|
| B01 | Instalacao limpa e login | Sessao valida, tenant correto e Dashboard real |
| B02 | Logout e novo login | Sessao anterior nao restaura; dados pertencem ao usuario atual |
| B03 | Troca de usuario/tenant | Nenhum cache de contas ou transacoes cruza identidades |
| B04 | Criar receita e despesa online | Item aparece no extrato, saldos e Dashboard sem duplicidade |
| B05 | Criar transferencia | Debito e credito consistentes; nao conta como receita/despesa |
| B06 | Editar/cancelar transacao | Versao, saldo e historico permanecem coerentes |
| B07 | Reiniciar aplicativo | Lista e pendencias persistem corretamente |
| B08 | Criar lancamento offline | Outbox persiste e UI identifica pendencia |
| B09 | Retornar conexao | Operacao sincroniza uma unica vez e a lista reconcilia |
| B10 | API 500/401/403 | UI mostra erro especifico, sem lista vazia silenciosa |
| B11 | Categorias e contas | Arquivar/desarquivar e filtros preservam vinculos |
| B12 | IA, notificacoes e perfil | Somente dados autorizados; nome real e nenhuma informacao inventada |

## Evidencias

Registrar por caso: versao/build, dispositivo, data, usuario de teste, resultado,
correlation ID quando houver erro e captura sem dados pessoais. Falhas de sync
devem guardar o `operationId`, sem tokens ou payload financeiro em logs.

## Criterios de aceite

Todos os casos B01-B10 sao bloqueantes. B11-B12 devem ser aprovados ou ter
risco formalmente aceito antes da ampliacao do grupo Beta.

## Regressao de atualizacao

Atualizar de uma versao Beta anterior sem desinstalar e repetir B04, B08 e B09.
Confirmar que Drift/SQLite preserva dados e que nenhuma outbox pendente foi
descartada.
