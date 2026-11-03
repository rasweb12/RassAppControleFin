# Matriz funcional — estado comprovável

Data: 2026-11-03. Estados refletem evidência disponível no commit de trabalho `5a56ebf`; não substituem teste manual de release.

| Módulo | Flutter | API/banco | Testes | Homologação | Pendência |
|---|---|---|---|---|---|
| Autenticação e sessão | Implementado | Implementado com Neon Auth e `UserSessions` | Integração/RLS aprovada isoladamente | Parcial | Smoke com configuração final do deploy. |
| Tenant e RBAC | Implementado | Implementado, RLS/FORCE RLS | RBAC 10/10, integração 130/130 isolados | Aprovado isolado | Regressão e smoke após deploy do código atual. |
| Contas, categorias, transações e transferências | Implementado | Implementado | Coberto pela integração homologada | Parcial | Regressão em ambiente isolado no commit de release. |
| Cartões, compra, fatura e parcelas | Implementado | Implementado no schema Production 20 | Homologado no ambiente isolado do CP12 | Parcial | APK/dispositivo e smoke do deploy atual. |
| Planejamento e investimentos | Implementado no domínio/API | Implementado | Testes unitários existentes | Parcial | Cenários funcionais de release. |
| Primeiro Me Pago | Implementado no domínio/configuração | Implementado | Testes unitários existentes | Parcial | Validação de UX e fluxo completo. |
| Administração global | Admin.Web + API | Funções isoladas em migrations 15–20 | RBAC 10/10 isolado | Aprovado isolado | Validar menus Admin.Web e deploy atualizado. |
| Sincronização offline | Flutter/Drift | API sync implementada | Homologação CP12 informada | Parcial | Regressão em APK de release. |
| Android / Google Play | Flutter Android | Não aplicável | APK/AAB executados apenas quando o SDK/assinatura estiverem configurados | Bloqueado | applicationId final, assinatura, política, exclusão de conta e AAB. |
| Dívidas e negociação | Não identificado como tela/fluxo Flutter | Não identificado como endpoint dedicado | Sem teste dedicado | Pendente | Confirmar escopo do MVP ou implementar antes de declarar cobertura completa. |
| Configurações e preferências | Parcial: sessão e armazenamento local | Sem módulo de preferências claramente separado | Sem teste de interface dedicado | Parcial | Validar persistência e navegação em dispositivo. |

Legenda: **Aprovado isolado** não significa aprovado em Production.
