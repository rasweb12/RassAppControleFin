# Matriz funcional — estado comprovável

Data: 2026-11-02. Estados refletem evidência disponível no commit `a78e86e`; não substituem teste manual de release.

| Módulo | Flutter | API/banco | Testes | Homologação | Pendência |
|---|---|---|---|---|---|
| Autenticação e sessão | Implementado | Implementado com Neon Auth e `UserSessions` | Integração/RLS aprovada isoladamente | Parcial | Smoke com config final de Production. |
| Tenant e RBAC | Implementado | Implementado, RLS/FORCE RLS | RBAC 10/10, integração 130/130 isolados | Aprovado isolado | Aplicar 10→20 em Production. |
| Contas, categorias, transações e transferências | Implementado | Implementado | Coberto pela integração homologada | Parcial | Regressão contra schema Production atualizado. |
| Cartões, compra, fatura e parcelas | Implementado | Migrations 11–13 pendentes em Production | Homologado no ambiente isolado do CP12 | Parcial | Upgrade e smoke em Production. |
| Planejamento e investimentos | Implementado no domínio/API | Implementado | Testes unitários existentes | Parcial | Cenários funcionais de release. |
| Primeiro Me Pago | Implementado no domínio/configuração | Implementado | Testes unitários existentes | Parcial | Validação de UX e fluxo completo. |
| Administração global | Admin.Web + API | Funções isoladas em migrations 15–20 | RBAC 10/10 isolado | Aprovado isolado | Aplicar upgrade e bloquear menus Admin.Web antecipadamente. |
| Sincronização offline | Flutter/Drift | API sync implementada | Homologação CP12 informada | Parcial | Regressão em APK de release. |
| Android / Google Play | Flutter Android | Não aplicável | Não executado nesta rodada | Bloqueado | applicationId final, assinatura, política, exclusão de conta e AAB. |

Legenda: **Aprovado isolado** não significa aprovado em Production.
