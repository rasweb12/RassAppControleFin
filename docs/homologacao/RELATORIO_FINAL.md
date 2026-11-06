# Relatório final consolidado de homologação — RassAppControleFin

**Data de consolidação:** 2026-11-05  
**Repositório/branch examinados:** `rasweb12/RassAppControleFin` / `main`  
**Baseline funcional:** `f8d565e5830ffeb10c2cc63c6cc860d3ea7d972e` (`MVP7`, 2026-11-05).  
**Versão Android declarada nos documentos:** `1.0.0+1`; não equivale a APK release homologado.  
**Deploy Render:** última evidência documental, de 2026-11-03, cita `a78e86e` (`MVP6`) e readiness indisponível/`update_failed`. **Não foi feita nova consulta em tempo real à implantação.**  
**Decisão:** **NO-GO para liberar o MVP integrado como concluído, iniciar cobrança real ou publicar na Google Play.** Não é uma reprovação retroativa dos testes aprovados em ambientes isolados.

## Critério de leitura, fontes e precedência

Este é o **único relatório consolidado**: atualização de `docs/homologacao/RELATORIO_FINAL.md`, usando `docs/homologacao/MATRIZ_FUNCIONAL.md` como matriz histórica existente, sem criar outra matriz em arquivo. A tabela abaixo é a visão consolidada da mesma matriz para decisão de release.

Fontes: `docs/homologacao/{MATRIZ_FUNCIONAL,EVIDENCIAS_TESTES,CHECKPOINT15_MVP,RELATORIO_AUTOMATICO,SEGURANCA_RBAC_RLS}.md`, `docs/{HOMOLOGACAO_GERAL,FINALIZACAO_MVP,BETA_TEST_PLAN,RELEASE_READINESS,RELEASE_ANDROID,PRODUCTION_READINESS,ROADMAP_MONETIZACAO,BETA_RC1_TRANSACOES_CORRECOES}.md`, `docs/production/CHECKPOINT14_PRODUCTION_UPGRADE.md`, `docs/publicacao/GOOGLE_PLAY_CHECKLIST.md` e commits `552e8aa` e `f8d565e`.

**Precedência temporal:** documentos de 01/11 que afirmam que Production tem apenas 10 migrations estão superados pelos registros de execução de 03/11: **20/20 aplicadas e pós-validadas**. O bloqueio de schema inicial foi resolvido; **compatibilidade e smoke do deploy atual continuam sem aprovação**. Declarações antigas de `applicationId` “TODO” divergem de `RELEASE_ANDROID.md`, que declara `com.rassapp.rassapp_controlefin`; verificar a configuração final, sem pressupor que está inválida. Evidências anteriores não provam comportamento do commit MVP7 nem de um APK atual.

**Legenda:** APROVADO = evidência conclusiva no escopo descrito; REPROVADO = falha comprovada ainda aberta; PENDENTE = cenário/artefato não comprovado; BLOQUEADO = dependência impede aceite/execução segura. Status trata do **gate funcional atual**, não de “código implementado”. Não converter ausência de evidência em falha comprovada.

## Matriz funcional consolidada (mesmos módulos, sem matriz concorrente)

| Módulo / cenário | Status | Evidência válida e lacuna atual | Gate |
|---|---|---|---|
| Banco Neon Production: migrations, ACL/RLS estrutural | **APROVADO** | Checkpoint 14 e `EVIDENCIAS_TESTES.md`: 20/20 migrations, ACL/RLS e testes negativos de runtime após upgrade em 03/11. **Aprovação estrutural, não da API/deploy.** | Infraestrutura estrutural |
| PostgreSQL/API/RBAC em banco isolado | **APROVADO** | Checkpoint 14.2: 132/132 integração e 10/10 RBAC no banco dedicado; sem repetir. Não é regressão do MVP7. | Base histórica |
| Builds e testes automatizados históricos | **APROVADO** | Checkpoint 14: 50/50 unitários, 5/5 arquitetura, 23/23 Flutter, builds .NET e análise Dart aprovados; contagens 47/47 e 130/130 pertencem a rodadas anteriores. | Base histórica |
| API .NET e compatibilidade com Production atual | **BLOQUEADO** | Render documentado em `a78e86e`, diferente de `f8d565e`; readiness/smoke do candidato não demonstrados. | **MVP P0** |
| Autenticação, logout, recuperação, sessão e perfil | **PENDENTE** | Homologação histórica do usuário; faltam B01–B03/B10 na versão candidata, troca de identidades, expiração/revogação e recuperação. Foto de perfil segura não comprovada. | **MVP P0** para sessão; avatar não bloqueia MVP mínimo |
| Tenant, RBAC funcional e Admin Web | **PENDENTE** | RBAC/RLS isolado aprovado; menus, rota de Admin, perfis e negações na API publicada ainda sem smoke. | **MVP P0** isolamento; UX Admin P1 se sem exposição |
| Dashboard, contas e categorias | **PENDENTE** | Funcionalidade historicamente homologada; faltam dados reais autorizados de teste sintético, filtros/arquivamento e conciliação no APK candidato. | **MVP P0** |
| Receita, despesa, extrato, edição e estorno | **PENDENTE** | Correções documentadas no RC1 (append-only, `PUT` offline, extrato); falta verificar B04/B06/B07, saldo e ledger no candidato. Não concluir “REPROVADO” sem reproduzir bug atual. | **MVP P0** |
| Transferências entre contas | **PENDENTE** | Integração historicamente coberta; B05 e extrato com uma linha lógica, dois efeitos de conta, patrimônio constante não comprovados no APK. | **MVP P0** |
| Cartões, compras, parcelas, faturas e pagamentos | **PENDENTE** | Esquema em Production atualizado, testes isolados históricos; compra à vista/parcelada, pagamento parcial/total, idempotência e persistência no APK pendentes. | **MVP P0** se incluídos no MVP prometido |
| Offline, Drift/outbox, reconciliação e multiusuário | **BLOQUEADO** | Sem `integration_test` Flutter/emulador; B07–B10 e atualização in-place exigem confirmação de outbox/IDs/versões e ausência de duplicidade. | **MVP P0** |
| Metas, orçamentos e redução de despesas | **PENDENTE** | Fluxos históricos parcialmente homologados; arquivar/reativar, integridade e alterações do MVP7 em `Goal`/API exigem teste focado. | **MVP P1** ou P0 se fluxo anunciado |
| Notificações financeiras | **PENDENTE** | MVP7 alterou `financial_notifications_page.dart`; validar somente cenários impactados e não reexecutar todo o módulo. | **MVP P1** |
| Investimentos, planejamento e Primeiro me Pago | **PENDENTE** | Domínio/API existentes; correção de revisão de mesmo dia (`552e8aa`) e alterações MVP7; UX, validade de revisões, pesos 100%, precisão, histórico e sync ponta a ponta não comprovados. | **P1** se exigidos no escopo MVP; senão futuro |
| Vencimentos, recorrência e parcelamentos gerais | **PENDENTE** | `FINALIZACAO_MVP.md` aponta cobertura parcial e ausência de prova funcional. | Escopo a fechar; P1, salvo requisito MVP |
| Dívidas e negociação | **PENDENTE** | `MATRIZ_FUNCIONAL.md` não identificou fluxo Flutter/endpoint dedicado nem teste. | **Futuro** salvo compromisso explícito do MVP |
| Configurações e preferências | **PENDENTE** | Persistência e navegação em Android não comprovadas. | P1; P0 para opções indispensáveis de conta |
| Assistente Gemini e IA | **PENDENTE** | Homologação funcional histórica; backend desabilitado por padrão no README; não é dependência da contabilidade central. | Futuro/opcional, sujeito a privacidade e consentimento |
| Android candidato e atualização preservando dados | **BLOQUEADO** | APK Debug histórico não equivale a APK/AAB assinado e instalado; sem emulador/dispositivo, regressão de atualização e estabilidade de navegação. | **MVP P0** para beta externo |
| Google Play e conformidade | **BLOQUEADO** | Assinatura release, `applicationId` final, versão inédita, AAB testado, privacidade, exclusão de conta, Data safety e ficha da loja sem evidência final. | **Publicação P0** |
| Planos pagos, entitlements e Google Play Billing | **PENDENTE** | `ROADMAP_MONETIZACAO.md` define fases futuras; não há homologação de cobrança, confirmação server-side, restauração ou cancelamento. | **Monetização P0**, não impede MVP gratuito |

## Bloqueios efetivos e critérios de aceite — sem executar nesta consolidação

| ID | Prioridade / impede | Evidência e dependência | Critério objetivo de aceite |
|---|---|---|---|
| G1 | **P0 MVP/Release** | `CHECKPOINT15_MVP.md`, `CHECKPOINT14_PRODUCTION_UPGRADE.md`: deploy `a78e86e` anterior ao `MVP7`; depende de configuração segura, revisão/implantação **posteriormente autorizada**. | Build candidato e hash conhecidos; `/health/live` e `/health/ready` 200 no ambiente alvo, endpoints reais compatíveis com schema 20 e smoke autenticado registrados, sem segredos. |
| G2 | **P0 MVP** | `HOMOLOGACAO_GERAL.md`, `BETA_TEST_PLAN.md`: faltam B01–B10 ponta a ponta no Android atual; depende de APK identificável, aparelho/emulador e usuário sintético. | Cada B01–B10 **APROVADO** com build, data, passos, resultado/captura sem dado pessoal; saldo, transferência, fatura, estorno e reinício consistentes. |
| G3 | **P0 MVP** | `CHECKPOINT15_MVP.md`: `integration_test` indisponível, outbox sem prova atual em dispositivo; depende de infraestrutura de teste e backend compatível. | Criar/editar offline, reiniciar, sincronizar e repetir requisição: operação única, `operationId` preservado, ID local reconciliado, nenhuma perda/duplicidade nem vazamento intertenant. |
| G4 | **P0 MVP/Segurança** | `EVIDENCIAS_TESTES.md` prova RLS estrutural, não autorizações da API publicada; depende de G1 e contas sintéticas de perfis diferentes. | Sessão revogada/expirada negada, mudança de tenant sem vínculo negada, `401/403` corretos, nenhuma leitura cruzada, permissão Platform limitada aos perfis autorizados. |
| G5 | **P1 MVP, sobe a P0 conforme escopo contratado** | `MATRIZ_FUNCIONAL.md`, commits `552e8aa` e `f8d565e`: metas/notificações/investimentos alterados ou sem UX homologada; depende de aceite explícito das funcionalidades do MVP. | Metas e notificações impactadas, “Primeiro me Pago” com revisão de mesma data, persistência/sync e histórico auditáveis; requisitos anunciados integralmente cobertos ou formalmente retirados do release. |
| G6 | **P0 Beta público/Google Play** | `RELEASE_ANDROID.md`, `GOOGLE_PLAY_CHECKLIST.md`: AAB/assinatura/identidade/versionamento não comprovados; depende de G1–G4, keystore fora do Git, metadados. | AAB Release assinado e validado; nome de pacote definitivo; build único; instalação/atualização sem perda da base local; checklist Play completo. |
| G7 | **P0 Publicação/Comercial** | `PRODUCTION_READINESS.md`, `CHECKPOINT15_MVP.md`: política, exclusão de conta, privacidade e persistência das chaves Data Protection pendentes; licença MediatR ainda exige verificação. | URLs legais e fluxo de exclusão disponíveis e testados; chave persistente/segura em reinício; licença comercial/documentação verificadas, logs sem segredos, plano de rollback aprovado. |
| G8 | **P0 Monetização, NÃO bloqueia MVP gratuito** | `ROADMAP_MONETIZACAO.md`: catálogo e restrições server-side, billing e entitlement não comprovados; depende de MVP estável, Google Play e desenho de planos. | Feature flags e limites aplicados pela API; compra validada no servidor, entitlement íntegro, restauração, cancelamento, reembolso e auditoria testados; nenhuma cobrança real antes disso. |

## Obrigatórias agora versus melhorias futuras

**Obrigatórias para fechar MVP conforme escopo mínimo financeiro:** G1–G4; resolver inconsistências comprovadas nesses fluxos; definir por escrito se cartões/metas/investimentos/vencimentos integram o MVP anunciado, sem declarar módulo ausente como aprovado. Se o beta for distribuído a terceiros, também cumprir o gate de APK assinado e privacidade correspondente de G6/G7.

**Obrigatórias apenas para monetização:** G8 e partes comerciais/legais de G7; não implementar cobrança antecipadamente. **Obrigatórias apenas para publicação Google Play:** G6 e G7 integralmente, somados aos gates financeiros.

**Melhorias futuras (não bloquear MVP financeiro básico sem compromisso explícito):** dívidas/negociação dedicada, projeção de caixa, simuladores, investimentos avançados, avatar, Gemini avançado, otimizações de desempenho não críticas, recursos Pro e automação de investimentos. Falha de segurança, perda de dados ou fluxo prometido nunca será reclassificado como melhoria.

## Testes ainda pendentes — identificados, **não executados**

1. Smoke autenticado da API e health do deploy compatível com `MVP7`/schema 20; versão, logs sanitizados e Data Protection em reinício.
2. Beta B01–B10 com dispositivos e dados sintéticos, dando preferência apenas a cenários não aprovados ou impactados; B11–B12 conforme escopo aceito.
3. `integration_test` Flutter, persistência Drift após reinício, alternância online/offline e atualização in-place sem limpar outbox.
4. Ledger e invariantes: receita/despesa, estorno idempotente, transferência neutra, compra/fatura/parcelas, pagamento parcial/integral e conciliação sem duplicidade.
5. Autenticação real, recuperação de senha, logout/revogação, troca de usuário/tenant, respostas 401/403 e acesso Admin/Platform por perfil.
6. Regressão focada dos arquivos alterados em `MVP7`: metas, validações/consultas API, notificações e testes; também revisão `Primeiro me Pago` do commit `552e8aa`.
7. Release Android assinado, atualização e smoke Google Play interno; política/eliminação de conta/segurança e assinatura.
8. Somente se houver monetização no ciclo: compra/restauração/cancelamento, validação de entitlement backend, planos e faturamento. **Não reexecutar** 132/132, 10/10, 50/50, 5/5 e 23/23 históricos sem delta pertinente.

## Registro de decisão e limites desta rodada

**MVP funcional:** **NO-GO**, por ausência de comprovação do candidato integrado, não por nova falha reproduzida. **Banco estrutural:** **APROVADO** em sua verificação de 03/11; não executar migrations novamente. **Monetização:** **NO-GO**, escopo de cobrança ainda não homologado. **Google Play:** **NO-GO**. **Falhas novas comprovadas nesta rodada:** nenhuma; foi realizada somente análise de documentos e metadados de commits. Nenhum teste, alteração de código, banco, dados reais, Android, RLS, Render ou deploy foi executado.


---

## Homologação pós-merge — 2026-11-05 (baseline atualizado; prevalece sobre o quadro pré-merge acima)

**Fonte do candidato:** HEAD remoto `main = 1dcaafff9cfd42409abf5b0dcc084d52e52e70c7` (merge de `codex/sprint16-monetization-admin`); `main` está 40 commits adiante de `f8d565e`. O diff inclui mudanças em IA, redução de despesas/Flutter, autenticação/offline e fundação de monetização. O commit de merge registra conflitos em `RELATORIO_FINAL.md` e `InvestmentPlanningTests.cs`; resolução ainda não validada pela suíte no HEAD. Não inferir que o merge fez migrations 21 ou validou endpoints novos.

**Render (consulta somente leitura nesta rodada):** workspace `tea-db67u04s728c73fgg84g`, API `srv-db6850id0e5s739om5kg`, deployment `dep-dbmi1v3ncjis739jvh7g` do SHA `1dcaafff`, status **live** em 2026-11-06T00:37:45Z. Isto **substitui a referência obsoleta** a `a78e86e` como deploy atual. `live` significa implantação técnica, **não** autenticação/readiness funcional, nem aplicação da migration 21, nem aprovação comercial. Tentativa de consulta pública de `/health/live`/`ready` não retornou resposta verificável por este meio; smoke pós-merge permanece PENDENTE.

**Artefato Android rastreável anterior ao merge:** variante `com.rassapp.rassapp_controlefin.homologation`, SHA-256 `228847E5DEA304E23901AB794C710AD3E65AD58010479792ED7302310A763C2E` (relatório Sprint20 Android); outra geração `B5D589F23B63FADFA4A9980ED7B16E16D4ECE0010235076847B2DB919379F3B7`. API configurada em build histórico `https://rassappcontrolefin-1.onrender.com`. **Não há APK instalado comprovadamente gerado a partir de `1dcaafff`, nem hash/versão do binário atual confirmados**. Versão declarada histórica `1.0.0+1`. Nenhuma sessão ADB/emulador, identidade sintética autenticada ou runner de testes está disponível nesta execução conectada; **nenhum teste Android ou API autenticada foi executado agora**.

### Matriz de deltas e defeitos: APROVADO/REPROVADO/PENDENTE/BLOQUEADO

| ID / Gate | Status | Evidência e versão | Reprodução/causa e limite | Prioridade / critério de aceite |
|---|---|---|---|---|
| G1 — merge e deploy do código | **APROVADO** (somente deploy) | GitHub `main=1dcaafff`, Render `live` no mesmo SHA. | Não comprova health ou schema atualizado. | P0: concluir smoke autenticado e validar migration 21 antes de marcar API aprovada funcionalmente. |
| G1 — API/health/monetização schema | **BLOQUEADO** | Migration `20261103112705_MonetizationFoundation` no diff, sem evidência nova de aplicação em Production; health sem resposta confirmada. | Dependência: estado real do schema 21, config e credenciais sintéticas. | P0: `live/ready` HTTP 200 + contratos reais e read-only de entitlements no deploy atual. Sem migration sem autorização. |
| B01–B03 / G2 | **PENDENTE** | Cadastro/login/restauração homologados manualmente antes do merge no `CHECKPOINT20_INVENTARIO_MVP.md`; `session_store.dart` alterado depois. | Não repetidos por indisponibilidade de Android candidato/identidades sintéticas atuais. | P0: smoke login, renovação, logout, troca de usuário/tenant e recuperação sem cache cruzado. |
| B04–B06 / G2 | **PENDENTE** | Transações, transferências e cartões historicamente homologados; `finance_repository.dart` e `finance_home_shell.dart` alterados. | Testar somente impactos e invariantes após novo APK. | P0: extrato/saldo, reversão única, transferência neutra, compra/parcelamento/fatura/pagamento sem duplicação. |
| B07–B09 / G3 | **PENDENTE** | Receita offline e cartão offline já aprovados nos cenários Android isolados anteriores; meta Android leitura offline também aprovada em sessão ativa; `offline_finance_store.dart` e sync alterados no merge. | Reinício offline da meta foi **BLOQUEADO por perda ADB** no registro Sprint20; edição/conflito de metas só testados localmente. | P0: reiniciar sem limpar dados, preservar outbox, reconciliar ID/operação uma vez, testar 409 e isolamento. |
| B10 / G4 | **PENDENTE** | RBAC e RLS isolados aprovados anteriormente, não substituem 401/403 no build atual. | Depende de identidades sintéticas autorizadas e API candidata. | P0: 401/403 específicos, revogação/sessão expirada e isolamento tenant sem vazamento. |
| G5 — Metas, Primeiro Me Pago e notificações | **PENDENTE** | Sprint20 Android aprovou criação/edição/progresso/arquivamento/reativação da meta sintética; 35/37 Flutter históricos; primeiro me pago parcial; Flutter alterado após o merge. | Não reexecutar cenários imunes; retestar somente fluxo alterado, offline, revisão e notificações. | P0 no escopo anunciado: manter valores/percentuais e estado após reinício, sem escrita duplicada. |
| IA-01 — comparação de 2 meses indisponível | **PENDENTE** (relato PO P0) | PO relatou mensagem “Assistente financeiro indisponível”; código `FinanceAiServices.cs` agora suporta janela de 2–12 meses. | Sem requisição autenticada/correlation ID; não há prova se provedor, configuração, HTTP, ausência de histórico ou consulta causou erro. | P0: comparação de 2 meses com totais consistentes, ou indicação explícita de histórico insuficiente; erro técnico identificado sem mensagem genérica. |
| IA-02 — redução por categorias não demonstrada | **PENDENTE** (relato PO P0) | `FinanceContextService` compõe `realized.category.<mês>.<moeda>.<categoria>.expense`, contas, totais mensais e projeções; prompt usa agregados. | Código **disponibiliza categorias**, mas não prova que o Gemini as recebeu ou utilizou; ausência de contexto/geração não reproduzida. | P0: análises 2/3/6/12 meses, maiores altas, fixas/variáveis, R$300 de economia com valores e limites verificáveis. |
| IA-03 — Markdown literal/formatação | **REPROVADO** (defeito de apresentação comprovado no código) | `financial_assistant_page.dart` exibe resposta em `Text(text)` sem parser Markdown ou formatter monetário. Observação de Markdown literal pelo PO; versão fonte `1dcaafff`. | Reproduzir com resposta contendo `**texto**` ou lista Markdown; cliente apresenta caracteres literais. Formatação monetária é dependente da resposta e precisa reteste. | P1: renderizar/sanitizar Markdown permitido, moeda em pt-BR; sem exibir tokens de marcação. |
| RD-01 — histórico anterior zero | **PENDENTE** (relato PO P0) | `FinanceReadStore.ExpenseReductionOpportunitiesAsync` usa intervalos consecutivos de mesmo tamanho em meses, `Posted` por categoria. Android Sprint20: 1 mês R$567/R$5; 3 e 12 meses R$572/R$0. | Os zeros históricos **não são por si prova de erro**; excluir ausência real com consulta sintética controlada. | P0: reconciliar períodos, dados `Posted` e zeros com Dashboard/Transações, exibindo “sem histórico” quando aplicável. |
| RD-02 — 100% dos gastos como corte | **REPROVADO** (regra de recomendação comprovada por inspeção) | `FinanceReadStore.cs`: `Increase=max(0,Recent-Previous)` e `Opportunity=max(0,Recent-Previous)`. Quando `Previous=0`, `Opportunity=Recent` (100%). | Com categoria de gasto recente positivo sem gasto anterior, reproduz sugestão de corte igual a todo o gasto, sem prova de redução possível. | P0: separar crescimento observado de economia **viável**, com validação de essencialidade/contrato; não declarar 100% como corte seguro. |
| RD-03 — financiamento/empréstimo como corte integral | **PENDENTE** (relato PO P0, risco confirmado) | Consulta filtra `Posted`/`Expense` e categoria, **sem distinção explícita de obrigação contratual** na regra de oportunidade. | O código **permite** que categoria contratual com aumento apareça; falta evidência de categoria concreta e dado sintético para reproduzir integralmente. | P0: classificar despesa contratual, não propor suspensão integral como economia, indicar renegociação/antecipação quando pertinente e com ressalvas. |
| RD — meta R$2.800, progresso 100%, valores disponíveis | **PENDENTE** | Dados visuais mencionados pelo PO; sem payload/snapshot atual para confrontar com ledger. | Não afirmar cálculo correto/incorreto sem período, metas e movimentos. | P0: total/meta/disponível derivados de dados verificáveis, distinguir realizado, previsto, possibilidade e falta de histórico. |
| G6–G8 — Google Play e cobrança | **BLOQUEADO** | Fundação de monetização adicionada no merge; `CHECKPOINT16_MONETIZACAO.md` registra ausência de enforcement/Billing; sem AAB assinado ou validação da migration 21. | Nenhuma compra/entitlement real foi exercitada. | P0 para comercialização: schema, enforcement server-side, Billing verificado, políticas, licença, assinatura, restauração/cancelamento e teste autorizado. |

### Encaminhamento dirigido ao MPO / Desenvolvimento (somente defeitos que pedem implementação)

1. **RD-02 (P0, comprovado em fonte):** não converter automaticamente aumento `Recent-Previous` em economia possível. Implementar critério distinto para gasto redutível e tratar `Previous=0` como histórico insuficiente, com teste financeiro focado.
2. **IA-03 (P1, comprovado em fonte):** o chat usa widget `Text` para Markdown. Implementar apresentação segura de Markdown/valores BRL e teste de renderização.
3. **RD-03 (P0, hipótese com risco fundamentado):** exigir classificação contratual/essencial e evidência controlada antes de corrigir a regra; não presumir que toda despesa possa ser eliminada.
4. **IA-01 e IA-02 (P0, relatos não reproduzidos):** solicitar correlação de requisição, contexto enviado e resultado do Gemini em cenário sintético **antes de atribuir causa ou editar código**.
5. **RD-01 (P0, não confirmado):** somente investigar com períodos iguais e histórico sintético; zeros observados nos dados históricos podem ser legítimos.
6. **Bug histórico do diálogo de meta `_dependents.isEmpty`:** corrigido e retestado no Android Sprint20; não reabrir sem regressão demonstrada.

### Plano de teste pendente (não executado nesta sessão)

- APK do exato commit `1dcaafff`: identificar SHA-256, versão, flavor e `API_BASE_URL`, preservar instalação existente; executar só B01–B10 impactados usando dados sintéticos.
- API: health/readiness, migration de monetização compatível, RBAC/401/403 com sessão sintética, sem tocar dados reais.
- IA: cinco perguntas exigidas (2 meses, 3/6/12, alta por categoria, fixos/variáveis, economia R$300), confrontando request context, intervalos e somas; distinguir indisponibilidade de Gemini, histórico vazio, contexto ausente e cálculo errado.
- Redução: 1/3/12 meses com valores `Posted`, comparação paritária, categorias sem histórico, essencial/contratual e equivalência dos totais com Dashboard.
- Offline: operação `pending` após reinício, envio idempotente após reconexão, conflito 409, nenhuma duplicação e revisão do Primeiro Me Pago; sem limpar Drift nem tocar dados não sintéticos.
- G6–G8: AAB release, assinatura, políticas, Billing e Admin em ambiente de homologação autorizado, só quando dependências anteriores estiverem disponíveis.

**Decisão pós-merge:** deploy técnico **APROVADO**, integração Android/API **PENDENTE/BLOQUEADA**, IA comercial **NÃO APROVADA**, redução de despesas **REPROVADA no critério de oportunidade**; MVP comercial **NO-GO**. Não foram rodados testes novos ou alterações de aplicativo, banco, RLS, migrations, infraestrutura, merge ou deploy; somente leitura remota, inspeção dirigida do código e atualização deste documento.