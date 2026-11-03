# Matriz funcional — estado comprovável

Data: 2026-11-03. Estados refletem evidência disponível no commit de trabalho `5a56ebf`; não substituem teste manual de release.

| Módulo | Flutter | API/banco | Testes | Homologação | Pendência |
|---|---|---|---|---|---|
| Autenticação e sessão | Implementado | Implementado com Neon Auth e `UserSessions` | Integração/RLS aprovada isoladamente | Parcial | Smoke com configuração final do deploy. |
| Tenant e RBAC | Implementado | Implementado, RLS/FORCE RLS | RBAC 10/10, integração 130/130 isolados | Aprovado isolado | Regressão e smoke após deploy do código atual. |
| Contas, categorias, transações e transferências | Implementado | Implementado | Coberto pela integração homologada | Parcial | Regressão em ambiente isolado no commit de release. |
| Cartões, compra, fatura e parcelas | Implementado | Implementado no schema Production 20 | Homologado no ambiente isolado do CP12 | Parcial | APK/dispositivo e smoke do deploy atual. |
| Planejamento e investimentos | Implementado no domínio/API | Implementado | Testes unitários existentes | Parcial | Cenários funcionais de release. |
| Primeiro Me Pago | Planejamento percentual configurável, com confirmação explícita e fila offline | Plano percentual, revisão e endpoints existentes | Contrato Flutter e teste offline; sem E2E remoto | Parcial | Implementar previsão por receita, distribuição por destino, análise de obrigações e homologar contra API isolada. |
| Administração global | Painel SuperAdmin com bloqueio visual de acesso direto | Funções isoladas em migrations 15–20 | RBAC 10/10 isolado; guard de navegação local | Parcial | TenantAdmin ainda não possui páginas administrativas próprias; validar no deploy atualizado. |
| Administração de tenant | Sem tela administrativa de membros | `POST /api/v1/tenants/members` existe, mas RLS permite inserção somente ao proprietário e não há convite/listagem segura | Testes de identidade cobrem negações e escopo | Reprovado funcionalmente | P1: projetar convite/aceite e policy RLS testada; não ampliar RLS sem modelo de segurança. |
| Monetização e entitlements | Tela somente leitura de plano/recursos; sem compra ou restauração | Fundação local de planos, direitos, assinaturas e eventos de pagamento; endpoint autenticado de leitura de direitos; migration pendente de aplicação | Testes de domínio, guard estático e contrato Flutter | Parcial | Aplicar e testar em ambiente isolado; catálogo Admin, limites nos fluxos e Google Play Billing continuam pendentes. |
| Sincronização offline | Flutter/Drift | API sync implementada | Homologação CP12 informada | Parcial | Regressão em APK de release. |
| Android / Google Play | Flutter Android | Não aplicável | APK/AAB executados apenas quando o SDK/assinatura estiverem configurados | Bloqueado | applicationId final, assinatura, política, exclusão de conta e AAB. |
| Dívidas e negociação | Não identificado como tela/fluxo Flutter | Não identificado como endpoint dedicado | Sem teste dedicado | Pendente | Confirmar escopo do MVP ou implementar antes de declarar cobertura completa. |
| Configurações e preferências | Parcial: sessão e armazenamento local | Sem módulo de preferências claramente separado | Sem teste de interface dedicado | Parcial | Validar persistência e navegação em dispositivo. |

Legenda: **Aprovado isolado** não significa aprovado em Production.

## Sprint 16 — atualização local

- A migration `20261103112705_MonetizationFoundation` foi criada apenas no código e **não foi aplicada** em Neon ou Production.
- Planos pagos permanecem desativados por padrão; não há cobrança, produto Play nem concessão Premium ativa.
- A concessão SuperAdmin solicitada permanece bloqueada: a consulta somente leitura ao Neon Auth de Production não localizou uma identidade verificada correspondente. Após o cadastro/verificação, será necessária confirmação explícita antes da atribuição funcional.
