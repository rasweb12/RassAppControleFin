# Matriz funcional — estado comprovável

Data: 2026-11-04. Estados refletem evidência disponível no commit de trabalho `24277f9` mais a homologação manual confirmada pelo proprietário. Teste unitário, integração isolada e homologação manual são evidências distintas.

## Sprint 20 — inventário consolidado

Foram encontradas **26 capacidades funcionais** no Flutter, API ou Admin.Web. As **10 aprovações manuais** confirmadas pelo proprietário são consolidadas em 8 linhas da matriz (por exemplo, cadastro/login e compras/faturas aparecem agrupados); há **5 capacidades parciais** e **11 ainda sem homologação funcional**. Os recursos ausentes do código não entram nessa contagem.

| # | Módulo/capacidade existente | Tela/cliente | API e persistência | Testes existentes | Evidência manual | Estado | Prioridade e dependência |
|---:|---|---|---|---|---|---|---|
| 1 | Cadastro e login | `register_page.dart`, `login_page.dart` | `/api/v1/auth/sign-up`, `/sign-in`; Neon Auth e `UserSessions` | identidade, API e RLS | Confirmada | **Homologado** | P0; regressão após mudança de autenticação |
| 2 | Restauração de sessão | `AuthController`, `SessionStore` | sessão segura local e `/renew` | `auth_controller_test.dart` | Confirmada após `force-stop`; Dashboard reabriu | **Homologado** | P0; cenários de expiração/rede ainda futuros |
| 3 | Dashboard financeiro | `finance_home_shell.dart` | `GET /api/v1/dashboard` | integração financeira | Confirmada | **Homologado** | P0; regressão após cálculos financeiros |
| 4 | Contas financeiras | formulários e aba Contas | `/accounts` POST/GET/PUT/PATCH | `CoreFinanceTests` | Confirmada | **Homologado** | P0 |
| 5 | Categorias | menu Categorias e formulário | `/categories` POST/GET/PATCH | `CoreFinanceTests` | Não isolada pelo proprietário | **Não testado** | P1; pré-requisito de cenários financeiros novos |
| 6 | Receitas e despesas | ações rápidas e Transações | `/transactions` POST/PUT/cancel/GET | domínio, integração e armazenamento offline | Confirmadas | **Homologado** | P0; editar/excluir requer regressão dirigida |
| 7 | Transferências próprias | ação rápida e Transações | `/transfers` POST/GET | domínio e integração | Confirmada; não contabilizada como receita/despesa | **Homologado** | P0 |
| 8 | Cartões de crédito | `credit_cards_page.dart` | `/credit-cards` e movimentos | integração de cartão e testes Flutter de contrato | Confirmado | **Homologado** | P0; regressão apenas se a tela/API mudar |
| 9 | Compras, parcelas, faturas e pagamento | detalhe do cartão | purchases, invoices e payment endpoints | integração CP12 e contratos | Confirmados manualmente | **Homologado** | P0; validar após mudança de cobrança/cartão |
| 10 | Sincronização offline/online | `finance_sync_service.dart`, Drift | fila e endpoints financeiros idempotentes | `offline_finance_store_test.dart`, CP12 isolado | Receita e cartão sintéticos criados offline, reiniciados e reconciliados uma vez após reconexão | **Parcial** | P0; repetir para conflitos e demais operações |
| 11 | Primeiro Me Pago | `pay_yourself_first_page.dart` | `/investments/pay-yourself-first`, `/investments/target-portfolio`; cache/fila offline | cálculo, data civil, distribuição e fila offline | Simulação, plano e distribuição 70/20/10 salvos/reabertos no APK; total 100%; sem movimentação automática | **Parcial** | P0; validar manualmente a fila offline da distribuição e implementar histórico planejado versus realizado |
| 12 | Reserva de emergência | fluxo de planejamento/investimento | `/investments/emergency-fund` | testes de planejamento | Não homologada | **Não testado** | P1; cenário de meta e persistência |
| 13 | Ativos e aportes de investimento | repositório/menu financeiro | investment assets, transactions e target portfolio | `InvestmentPlanningTests` | Não homologados | **Não testado** | P1; sem integração com corretora/cotação real |
| 14 | Orçamentos | API/domínio; sem tela Flutter dedicada identificada | `/budgets` | testes de domínio/integração financeira | Não homologados | **Não testado** | P1; concluir cliente antes do cenário manual |
| 15 | Metas financeiras | menu/formulário de metas | `/goals` e progress/archive | testes de domínio | Não homologadas | **Não testado** | P1 |
| 16 | Redução de despesas | menu financeiro | metas manuais e `GET /expense-reduction-opportunities` (1–12 meses) | validação de período e suíte local | Não homologada no Android | **Implementada, sem homologação integrada** | P1; requer deploy autorizado e cenário Android com dados sintéticos |
| 17 | Notificações financeiras | `financial_notifications_page.dart` | `/notifications` e read | testes de domínio | Não homologadas | **Não testado** | P1 |
| 18 | Assistente financeiro | `financial_assistant_page.dart` | serviço AI/endpoints existentes | `AiSafetyTests` | Não homologado | **Não testado** | P2; depende de configuração AI válida |
| 19 | Perfil e segurança da conta | fluxo de sessão/perfil | `/profile`, reset e sign-out | identidade/RLS | Não homologado além de login/sessão | **Não testado** | P1; reset, logout e atualização de perfil |
| 20 | Preferências/configurações locais | Menu e armazenamento local | sem serviço de preferências separado identificado | sem teste de interface dedicado | Não homologadas | **Não testado** | P2 |
| 21 | Plano e recursos | `entitlements_page.dart` | `GET /monetization/entitlements` | domínio e contrato Flutter | Não homologado; migration local não aplicada | **Parcial** | P1; sem compra/restauração/limite efetivo |
| 22 | TenantAdmin | não há painel próprio de membros | `/tenants/members` | identidade/RLS de negação | Reprovado funcionalmente antes | **Parcial** | P1; convite/aceite e policy RLS segura |
| 23 | SuperAdmin/plataforma | `Admin.Web/Platform.razor` | `/api/v1/platform/*` | RBAC 10/10 e integração isolada | Não homologado no Admin.Web publicado | **Não testado** | P1; identidade verificada e autorização específica |
| 24 | Admin.Web | Login, Home e Platform | clientes `SystemClient`, `PlatformClient`, `AdminAccessClient` | guard de acesso local/RBAC | Não homologado de ponta a ponta | **Parcial** | P1; autenticação, sessão e implantação do painel |
| 25 | Administração de planos/monetização | somente fundação e tela de leitura | modelo de planos, direitos e eventos; sem Billing Play | domínio e guard estático | Não homologada | **Parcial** | P1; catálogo Admin, limites API e Billing oficial |
| 26 | Multi-tenant/RLS/RBAC técnico | transparente ao cliente | policies RLS/FORCE RLS e funções administrativas | RBAC 10/10; integração 130/130 isolados | Não é fluxo visual do proprietário | **Homologado isolado** | P0; manter regressão em banco isolado |

### Recursos ausentes do código atual

- Dívidas, negociação e quitação: não há tela Flutter nem endpoint dedicado.
- Recorrências financeiras: não há fluxo/endpoint dedicado identificável.
- Relatórios/exportação dedicados: não há módulo de tela ou endpoint específico; o dashboard não equivale a relatório.

### Roteiro manual restante

1. **P0:** criar conta offline, receita/despesa/cartão offline, reconectar, sincronizar, reiniciar e comparar quantidades/saldos sem duplicação.
2. **P0:** Primeiro Me Pago com renda recebida e prevista, obrigações suficientes/insuficientes, percentuais totalizando 100% e confirmação sem movimentação automática.
3. **P1:** categorias, metas, reserva, investimentos, orçamentos, redução de despesas e notificações.
4. **P1:** atualização de perfil, logout, recuperação de senha, TenantAdmin e SuperAdmin no Admin.Web.
5. **P1:** plano/recursos, limites Free/Premium e, quando autorizado, compra/restauração em ambiente oficial Google Play de teste.
6. **P2:** assistente financeiro, preferências e qualquer módulo novo somente após evidência de implementação.

**Veredito Sprint 20: MVP NO-GO / Google Play NO-GO.** A aprovação manual acima dispensa repetição desses fluxos, salvo regressão dirigida por alteração posterior.

| Módulo | Flutter | API/banco | Testes | Homologação | Pendência |
|---|---|---|---|---|---|
| Autenticação e sessão | Implementado | Implementado com Neon Auth e `UserSessions` | Integração/RLS aprovada isoladamente | Parcial | Smoke com configuração final do deploy. |
| Tenant e RBAC | Implementado | Implementado, RLS/FORCE RLS | RBAC 10/10, integração 130/130 isolados | Aprovado isolado | Regressão e smoke após deploy do código atual. |
| Contas, categorias, transações e transferências | Implementado | Implementado | Coberto pela integração homologada | Parcial | Regressão em ambiente isolado no commit de release. |
| Cartões, compra, fatura e parcelas | Implementado | Implementado no schema Production 20 | Homologado no ambiente isolado do CP12 | Parcial | APK/dispositivo e smoke do deploy atual. |
| Planejamento e investimentos | Implementado no domínio/API | Implementado | Testes unitários existentes | Parcial | Cenários funcionais de release. |
| Primeiro Me Pago | Planejamento percentual configurável, confirmação explícita e distribuição editável da carteira-alvo | Plano percentual, revisão, carteira-alvo e fila offline; cálculo mensal não mutável de renda/obrigações/distribuição | Contrato Flutter, testes de cálculo e de fila única de carteira-alvo; sem E2E remoto da distribuição | Parcial | Homologar salvamento/reabertura visual e os cenários offline no APK; histórico mensal continua pendente. |
| Administração global | Painel SuperAdmin com bloqueio visual de acesso direto | Funções isoladas em migrations 15–20 | RBAC 10/10 isolado; guard de navegação local | Parcial | TenantAdmin ainda não possui páginas administrativas próprias; validar no deploy atualizado. |
| Administração de tenant | Sem tela administrativa de membros | `POST /api/v1/tenants/members` existe, mas RLS permite inserção somente ao proprietário e não há convite/listagem segura | Testes de identidade cobrem negações e escopo | Reprovado funcionalmente | P1: projetar convite/aceite e policy RLS testada; não ampliar RLS sem modelo de segurança. |
| Monetização e entitlements | Tela somente leitura de plano/recursos; sem compra ou restauração | Fundação local de planos, direitos, assinaturas e eventos de pagamento; endpoint autenticado de leitura de direitos; migration pendente de aplicação | Testes de domínio, guard estático e contrato Flutter | Parcial | Aplicar e testar em ambiente isolado; catálogo Admin, limites nos fluxos e Google Play Billing continuam pendentes. |
| Sincronização offline | Flutter/Drift | API sync implementada | Receita e cartão offline criados/reiniciados/sincronizados uma única vez no APK `.homologation`; cartão reconciliado com versão positiva; CP12 informado | Parcial | Regressão de conflitos e demais comandos antes de aprovação integral. |
| Android / Google Play | Flutter Android | Não aplicável | APK/AAB executados apenas quando o SDK/assinatura estiverem configurados | Bloqueado | applicationId final, assinatura, política, exclusão de conta e AAB. |
| Dívidas e negociação | Não identificado como tela/fluxo Flutter | Não identificado como endpoint dedicado | Sem teste dedicado | Pendente | Confirmar escopo do MVP ou implementar antes de declarar cobertura completa. |
| Configurações e preferências | Parcial: sessão e armazenamento local | Sem módulo de preferências claramente separado | Sem teste de interface dedicado | Parcial | Validar persistência e navegação em dispositivo. |

Legenda: **Aprovado isolado** não significa aprovado em Production.

## Sprint 16 — atualização local

- A migration `20261103112705_MonetizationFoundation` foi criada apenas no código e **não foi aplicada** em Neon ou Production.
- Planos pagos permanecem desativados por padrão; não há cobrança, produto Play nem concessão Premium ativa.
- A concessão SuperAdmin solicitada permanece bloqueada: a consulta somente leitura ao Neon Auth de Production não localizou uma identidade verificada correspondente. Após o cadastro/verificação, será necessária confirmação explícita antes da atribuição funcional.
