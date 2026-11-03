# Release readiness — 2026-11-02

| Área | Estado | Evidência / bloqueador |
|---|---|---|
| Checkpoint 13 RBAC/RLS isolado | APROVADO | 20 migrations, RBAC 10/10 e integração PostgreSQL 130/130 em `rass_sprint1_tests`. |
| Production PostgreSQL | PARCIAL | Schema atualizado para 20 migrations e ACL/RLS validadas; falta regressão da API e smoke do deploy do código atual. |
| API .NET | PARCIAL | Build e testes locais aprovados no commit de trabalho; Render ativo usa `a78e86e`, não a branch de preparação. |
| Flutter | PARCIAL | `flutter analyze` e 23 testes aprovados; faltam `integration_test`, emulador/dispositivo e persistência após reinício. |
| Google Play | BLOQUEADO | `applicationId` ainda contém TODO de template, versão `1.0.0+1`; assinatura release, política de privacidade, exclusão de conta e metadados de loja exigem confirmação. |
| GitHub | PENDENTE | Alterações desta rodada ainda precisam de revisão, commit em branch de trabalho e PR; nenhum merge automático. |

Decisão atual: **NO-GO para Google Play** e **NO-GO para tratar o deploy atual como release completo**, até merge autorizado, deploy compatível e smoke funcional.
