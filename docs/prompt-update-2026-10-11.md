# Atualizacao do Prompt Mestre - 2026-10-11

Este documento prevalece sobre decisoes anteriores conflitantes. Confirmacao humana nesta conversa: migrar de Supabase para Neon. Portanto, banco principal continua Neon PostgreSQL e identidade continua Neon Auth. Nenhum outro projeto Supabase sera modificado.

## Regras permanentes

- Frontend do usuario: somente Flutter/Dart, Android/iOS. Nao publicar Web do usuario.
- Admin: Blazor Server; API: ASP.NET Core .NET 10; infraestrutura: Neon, Resend e Render.
- SQLite apenas no Mobile, Sprint 4; PostgreSQL e system of record.
- Pagamentos Android: Google Play Billing. Stripe, Azure, SQL Server e LocalDB proibidos.
- IA generativa: exclusivamente API oficial Google Gemini. Sem SDK, endpoints, chaves ou fallback OpenAI.
- Toda chamada generativa fica no backend protegido. Model via Gemini:Model, sem modelo fixo; chave somente GEMINI_API_KEY no ambiente do backend.
- Mudancas limitadas a D:/Curso/APP/RassAppControleFin. RassAppFinanceiro permanece somente referencia de leitura.

## Sequencia por sprint

Sprint 2 ja validada com Neon Auth/memberships/RLS. Sprint 3 implementa financas deterministicas, consistencia, audit e idempotencia. Sprint 4 implementa offline/sync. Sprint 5 contempla somente dashboard Mobile. Sprint 7 contempla Admin. Sprint 8 implementa Gemini e sua administracao/metricas reais. Nao antecipar um chatbot com dados ficticios.

A atualizacao arquitetural/configuracao nao significa que a Sprint 8 ou o criterio final de IA estejam concluidos. FinanceContextService e demais servicos precisam de repositorios financeiros reais/autorizados e dos periodos da Sprint 3.

## Arquitetura Gemini na Sprint 8

Application/AI/Interfaces: IAiProvider, IFinanceContextService. Application/AI/Services: FinanceContextService, FinancialInsightEngine, AiPromptBuilder, ChatbotOrchestrator. DTOs independentes de SDK. Infrastructure/AI/Gemini: GeminiAiProvider, GeminiClient, GeminiOptions. API: endpoint/controller protegido -> MediatR, sem regra financeira no controller.

As portas do modulo AI existente serao consolidadas na Application ao implementar os casos de uso; evitar interfaces duplicadas e providers ficticios. Somente Gemini tera implementacao concreta. Domain permanece BCL-only.

Fluxo: Mobile -> API/autorizacao -> contexto financeiro Neon via porta Application -> calculos deterministas -> insights -> prompt minimo -> Gemini -> validacao -> Mobile.

Nao enviar TenantId/UserId, credenciais, JWT, PAN/CVV, nomes/e-mails ou transacoes completas ao provider. Escopo interno e dados enviados ao modelo sao DTOs distintos. O usuario escolhe periodo; tenant/subject derivam do contexto autenticado.

## Seguranca e resposta

Resposta oficial de saldo/total e sempre deterministica. Gemini apenas explica/gera insights. Quando indisponivel, retornar fatos autorizados calculados no backend, com indicacao de indisponibilidade; nunca chamar outro modelo generativo.

Prompt com fronteiras explicitas e dados tratados como nao confiaveis; sem ferramentas de escrita/SQL. A resposta estruturada exige validacao local de schema, limites de tamanho e campos permitidos. JSON Schema do provider nao substitui validacao semantica.

Rate limit por usuario/tenant/plano, budget atomico antes da chamada, timeout, retry limitado contabilizado, circuit breaker. Registrar apenas interaction id, model, outcome, latency, input/output/thinking tokens e custo estimado; sem prompts/valores financeiros completos nos logs.

Admin mostra indicadores operacionais persistidos reais, feature flags e limites por plano. Custos calculados com tabela configuravel/versionada por modelo e data; nao fixar preco ou inferir custo a partir de tokens incompletos. Requisicao em timeout pode ter sido cobrada: registrar estimativa/consumo desconhecido explicitamente.

Chat de sessao isolado, retencao/consentimento LGPD e minimizacao. Nenhuma operacao financeira executada diretamente pela IA; mutacoes seguem comandos autorizados e confirmacao humana.

## Gate Sprint 8

Mocks/fakes de IAiProvider: tenant/user isolation, nenhum dado, injection, provider indisponivel, timeout, rate/cost limits, tokens, respostas invalidas, erros e fallback deterministico. Sem chamadas pagas em testes automatizados.

Build e tests sem warnings, zero dependencias OpenAI, nenhum segredo Mobile, metricas reais Admin e referencia antiga intacta. Chave, modelo, conta/plano Google e consentimentos sao configurados antes de ativar IA em producao.

## Operacao e custos

Uso sensivel requer revisar politicas de tratamento de dados do Google, inclusive diferencas entre tiers gratuitos/pagos, regiao e consentimento. Valores variam por modelo, modalidade, cache e tokens de pensamento; consultar preco atual no momento de ativacao. Nenhuma chamada Gemini ou assinatura paga foi feita nesta etapa.

Fontes oficiais consultadas: [API keys](https://ai.google.dev/gemini-api/docs/api-key), [structured output](https://ai.google.dev/gemini-api/docs/structured-output), [precos](https://ai.google.dev/gemini-api/docs/pricing), [termos](https://ai.google.dev/gemini-api/terms).
