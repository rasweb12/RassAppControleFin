# Arquitetura - RassAppControleFin

Decisoes iniciais e evolucao. Atualizado: 2026-10-11. Versao: 1.0.0-alpha.1.

Atualizacao normativa: [Prompt 2026-10-11](prompt-update-2026-10-11.md).
Neon PostgreSQL/Auth confirmado pelo usuario; Gemini exclusivo; Mobile e unico
frontend do usuario. Web anterior e referencia inativa, excluida de build/deploy.
Google Play Billing substitui qualquer plano de Stripe. Sprints 0/1 abaixo
registram contexto historico; Sprint 2 validada em sprint-2-review.md.

## Escopo e referencia

Pasta canonica: D:\Curso\APP\RassAppControleFin, repositorio independente que
estava vazio. O caminho antigo ja nao existia no inicio da analise.
RassAppFinanceiro, branch feature/v0.9.0-devsec, e referencia somente de leitura.
Stack ativa: Neon, Resend, Render e Gemini; Admin.Web e cliente Flutter; versao
1.0.0-alpha.1. Nenhuma dependencia do repositorio antigo.

Evidencias da referencia: Domain referencia EF Core; interfaces de tenant
duplicadas; processors de sync paralelos em Application/Synchronization;
ConflictResolver compara timestamps por reflexao; Infrastructure usa SqlServer.
production-readiness.md registra sessao mobile em memoria, LGPD/observabilidade
pendentes e necessidade de backup/rollback. Preservar conceitos financeiros e
reimplementar limites, consistencia e isolamento.

## Arvore da solution

```text
RassAppControleFin/
  RassAppControleFin.slnx
  mobile/
    rassapp_controlefin
  src/
    RassAppControleFin.Domain
    RassAppControleFin.Application
    RassAppControleFin.Infrastructure
    RassAppControleFin.Api
    RassAppControleFin.Admin.Web
    RassAppControleFin.Contracts
    RassAppControleFin.SharedKernel
    RassAppControleFin.Security
    RassAppControleFin.MultiTenancy
    RassAppControleFin.Synchronization
    RassAppControleFin.AI
    RassAppControleFin.EventBus
  tests/
    RassAppControleFin.UnitTests
    RassAppControleFin.IntegrationTests
    RassAppControleFin.ArchitectureTests
  tools/
    RassAppControleFin.Database
  infra/{docker,render,neon}
  docs/
  scripts/
```

Contracts e adicionado para compartilhar DTOs sem transportar Domain/Application
para os hosts .NET. O cliente Flutter consome exclusivamente os contratos HTTP
publicos da API; iOS exige macOS/Xcode. Nao ha projetos sem responsabilidade:
Security e MultiTenancy definem portas; Synchronization e AI definem contratos;
EventBus define a porta de publicacao, sem escolher broker nesta sprint.

## Dependencias

Domain: somente BCL, nenhum ProjectReference/PackageReference. SharedKernel e
Contracts: somente BCL. Security/MultiTenancy/EventBus: contratos independentes.
AI/Synchronization: SharedKernel e DTOs quando necessario. Application: Domain e
portas internas. Infrastructure: Application e implementacoes de adapters.
Api: Application/Infrastructure/Contracts, composition root. Admin/Mobile:
Contracts e transport HTTP; nunca banco, Infrastructure ou Application.
O fluxo de execucao HTTP -> Application -> Domain -> persistencia nao e a
direcao das dependencias de compilacao: Infrastructure depende de portas internas.
Domain events nao implementam interfaces MediatR.

## NuGet

Versoes fixas e centralizadas em Directory.Packages.props. Sem floating versions.

| Modulo | Pacotes | Sprint |
| --- | --- | --- |
| Application | MediatR 14, FluentValidation 12, DI abstractions | 0 |
| API | ASP.NET Core 10, JwtBearer 10, OpenAPI/Swagger | 0 base |
| Telemetria | OpenTelemetry Hosting, AspNetCore, Http, OTLP | 0 base, 9 completo |
| HTTP externo | Microsoft.Extensions.Http.Resilience (Polly) | 0 base |
| Infrastructure | EF Core 10, Npgsql EF 10, Design private assets | 1 |
| Mobile | Flutter/Dart; HTTP, Secure Storage e SQLite no fluxo real | migracao em andamento |
| Testes | xUnit, Test.Sdk, MVC.Testing | 0 |

MediatR 14 tem termos de licenca comerciais: verificar elegibilidade/licenca da
organizacao antes de distribuicao. Manter MediatR como requisito. Nao utilizar
FluentValidation.AspNetCore; pipeline de validacao async na Application.

## Neon e PostgreSQL

PostgreSQL e system of record. EF/Npgsql apenas Infrastructure. Runtime com
endpoint pooled TCP/TLS; migrations com endpoint direto e papel proprio.
Na Sprint 1: projeto Neon isolado, branch dev, migrations novas, seed explicito.
Nunca aplicar migrations automaticamente no startup nem no banco antigo.
UUID, UTC/timestamptz, numeric para valores, unique/check/FK constraints,
concorrencia otimista explicita. JSONB apenas quando estrutura flexivel e util.
Queries projetadas/paginadas; dashboard agrega no banco e nao carrega tabelas.

Storage atras de porta provider-neutral, privado e autorizado por recurso.
Confirmar disponibilidade/regiao ao provisionar. Nao pressupor que exista SDK
Neon Realtime equivalente ao Supabase: SignalR no host .NET e alternativa
planejada. Eventos avisam o cliente; sync incremental continua fonte confiavel.
Neon nao substitui Domain/Application, autorizacao ou sync.

## Auth e multi-tenancy

Neon Auth e Managed Better Auth. Subject e string opaca; nao assumir UUID.
Nao importar auth.uid(), auth.users ou service_role do modelo Supabase.
Credenciais privilegiadas, senha do banco e API keys apenas servidor.

Sprint 0: fallback de autorizacao fechado para endpoints protegidos;
operacionais explicitamente anonimos; nenhum token sem assinatura e confiado.
Auth real na Sprint 2: issuer/audience/signature/expiration/subject/algoritmo,
rotacao de JWKS e sessao/revogacao testados com o provedor. Managed Auth usa
EdDSA/Ed25519; confirmar adapter mantido compativel com .NET IdentityModel antes
de habilitar acesso. Nao presumir suporte nativo nem OIDC discovery completo.
Nao desligar validacao ou trocar algoritmo para fazer exemplos funcionarem.

Sessao Better Auth nao e refresh_token OAuth padrao. Admin: sessao
server-side, cookie HttpOnly/Secure, antiforgery e chaves persistidas.
Mobile: SecureStorage, restauracao/revogacao e fluxo nativo validado.
Logout de sessao nao revoga imediatamente todo JWT ja emitido.

ICurrentUser/IUserContext vem da identidade autenticada. ITenantContext e
inicializado somente depois de resolver membership ativo da aplicacao.
Header/rota pode selecionar tenant, mas nao e autoridade. Roles e permissoes
em policies/handlers, sempre membership + recurso. Administradores recebem
grants explicitos e auditados, nunca bypass generico.

## RLS

API authorization + Application authorization + PostgreSQL RLS.
Runtime role NOSUPERUSER/NOBYPASSRLS e nao dono das tabelas. ENABLE + FORCE RLS;
USING e WITH CHECK para SELECT/INSERT/UPDATE/DELETE. Contexto ausente nega acesso.
Dentro da mesma transacao/connection de cada operacao:
set_config('app.tenant_id', ..., true) e app.user_id, valores autorizados pelo
servidor. Nunca variaveis de sessao que vazem pelo pool.
Roles anon/authenticated sao cenarios, nao pressupostos de roles Neon existentes.
Politicas sao migrations versionadas e testadas com roles reais, sem superuser.
FORCE nao impede BYPASSRLS: runtime e criado por SQL, verificado em cada Begin
e readiness. O owner privilegiado e usado apenas por ferramenta operacional.

## SQLite e Sync

SQLite somente Mobile, particionado por tenant/user/device. Troca/logout encerra
worker e impede acessar cache anterior. Alteracao local e outbox na mesma
transacao. Inbox/outbox/cursor persistentes; IDs UUID distribuidos.
Metadados: Id, TenantId, UserId, DeviceId, OperationId, EntityId, EntityType,
OperationType, CreatedAt, ClientTimestamp, ServerTimestamp, Version, SyncStatus,
RetryCount, LastError; payload com versao de schema.

Portas: ISyncEngine, ISyncQueue, IOutboxStore, IInboxStore,
ISyncConflictResolver, ISyncTransport, ISyncStateStore. Worker com lease,
cancellation, backoff/jitter, recuperacao de crash. Pull com cursor opaco e
tombstones. Ack apos dados/inbox/cursor locais persistidos atomicamente.

Servidor: UNIQUE(TenantId, OperationId), hash do payload, actor/device e resposta
duravel. Replay autorizado retorna resultado; payload diferente com mesmo ID
retorna 409. Claim idempotente, mutacao financeira, outbox, auditoria e resposta
na mesma transacao. Replay ainda valida identidade/permissao.

| Conflito | Resolucao |
| --- | --- |
| Transaction/Account/Transfer | expected version; rejeitar e preservar ambos payloads |
| Budget/Goal | merge comprovadamente independente, ou escolha explicita |
| Category/Profile | merge por campo com base comum; sem LWW global |

Transfer e agregado proprio: contas distintas, mesmo tenant, moeda/valor/
autorizacao validos. Debito, credito, versoes, idempotencia, auditoria e eventos
na mesma transacao PostgreSQL; falha desfaz os dois lados. Mobile registra unica
operacao composta e saldo provisorio ate confirmacao.

## API e Result

/api/v1, DTOs em Contracts, sem entidades expostas. Envelope: success, data,
error(code/message/details), meta(correlationId). Result/Result<T>/Error/
ErrorType/ValidationError com estados consistentes. Pipeline FluentValidation
async retorna Result sem reflexao nem exceptions para fluxo normal.
400 Validation, 401 Unauthorized, 403 Forbidden, 404 NotFound, 409 Conflict,
422 Domain, 503 Infrastructure. Unexpected 500 com ProblemDetails generico;
sem stack trace no cliente. Cancellation de request nao vira erro interno.

## Resend, Render, AI, observabilidade

Resend implementara IEmailSender em Infrastructure: outbox, idempotency key,
dominio verificado e webhooks assinados. Configuracao tipada na fundacao;
nenhum envio ou dominio conectado nesta sprint. Emails de identidade continuam
no fluxo Neon Auth; SMTP customizado compativel antes de producao.

Render: containers separados API/Admin, binding 0.0.0.0:$PORT, health/live
e health/ready. Migrations como job controlado. Blazor precisa WebSockets e
continuidade dos circuitos; planejar scale-out e DataProtection key store.
Nenhum deploy remoto ou criacao de recursos na Sprint 0.

Timeout/circuit breaker em chamadas externas; retry apenas GET/HEAD/OPTIONS ou
operacao com idempotencia comprovada. Sem retries cegos em POST financeiro.
Sem fallback com dados financeiros inventados.

AI exclusivamente Gemini: IAiProvider, IFinanceContextService, IPromptBuilder, IAiOrchestrator,
IAiAuditService. Application fornece agregacoes minimas autorizadas; IA nao
consulta banco. Rate limit, token budget, boundaries e output validation.
Auditoria guarda metadados minimos; nao prompts/valores completos por padrao.
LGPD: consentimento/retencao/exportacao/exclusao como futuros casos de uso.

OTEL ASP.NET/HttpClient e ActivitySource MediatR na fundacao. Instrumentacao
DB/sync/auth/AI nas sprints respectivas. TraceId/SpanId/CorrelationId; sem body,
senha, bearer tokens, secrets ou dados financeiros completos. Tenant/user nao
sao labels de metrics de alta cardinalidade.

## Testes e execucao

Sprint 0: Result invariants, validacao async, API envelopes/health/erros/auth,
dependency rules. Sprint 1: RLS CRUD, contexto ausente, tenants e reuse do pool.
Sprint 2: JWT real/session e tenant/user/admin isolation. Sprint 3: contas
1000/500, transferencia 300 -> 700/800; retry/duplicate/concurrency/rollback.
Sprint 4: offline/restart/conflict/inbox/outbox/cursor. IA: contexto isolado.
Mocks nao sao evidencia de PostgreSQL/RLS/Auth funcionando.

CI: restore/build warnings-as-errors/test (unit/integration/architecture),
dependency scan/publish. O cliente Flutter executa analyze/test no seu proprio
ambiente Android/iOS. SAST/secret scanning
e branch protection precisam ser required checks antes do merge. Sprint fecha
com build, tests, revisao de seguranca/arquitetura e evidencias documentadas.
Entrega atual: Sprint 1. Auth/membership entram na Sprint 2; financas na Sprint 3.

## Decisoes de persistencia da Sprint 1

Reutilizar o projeto Neon morning-dream-96756371, PostgreSQL 18, aws-sa-east-1.
Production possui apenas schema gerenciado neon_auth, com zero usuarios/sessoes
na inspecao. Desenvolvimento isolado: development-sprint-1. Nao alterar production,
neon_auth nem o banco antigo. O plano Neon nao permite customizar suspend timeout;
manter configuracao permitida pelo provedor.

Schema app: Tenants e UserProfiles. Sao agregados da aplicacao, nao credenciais
nem copias das tabelas Neon Auth. UserId e subject opaco. Chave de perfil composta
por TenantId/Id, unicidade TenantId/UserId, FK indexada, Version bigint explicita.
Nao implementar membership, operacoes financeiras ou sync antes das suas sprints.

RLS tenant em Tenants; tenant + usuario em UserProfiles. Contexto autenticado e
membership ainda nao habilitados: a composition root fornece contextos fechados.
ITenantTransactionFactory valida contexto antes de abrir conexao e usa SET LOCAL
via set_config parametrizado na mesma transacao EF. Dispose sem commit desfaz.
Sem estrategia automatica de retry de escrita: o chamador futuro tratara
idempotencia e conflitos. SaveChanges sem transacao autorizada e proibido.

Runtime rass_app_runtime: LOGIN, NOSUPERUSER, NOBYPASSRLS, NOCREATEDB, NOCREATEROLE,
sem membership privilegiado/ownership/CREATE/TRUNCATE. Bootstrap de privilegios
versionado separado das migrations. Migrations usam conexao direta privilegiada
apenas no job explicito; API usa conexao pooled TLS VerifyFull. Health/readiness
valida conexao, schema, migrations, RLS e papel, sem publicar erros do provedor.

Testes PostgreSQL reais em banco de testes dedicado na branch dev e em PostgreSQL
18 no CI. Nao simular RLS com EF InMemory/SQLite. Fixtures sinteticas; dados de
cada teste em transacoes revertidas. Seed opt-in, idempotente e apenas sintetico.
Credenciais locais via User Secrets; nenhuma connection string em Git/logs.
Ferramenta Database com responsabilidade operacional clara, nunca referenciada
pela API. Configuracao/detalhes/testes em docs/database.md e docs/sprint-1.md.

## Fontes primarias

- https://neon.com/docs/llms.txt
- https://neon.com/docs/auth/authentication-flow
- https://neon.com/docs/auth/guides/plugins
- https://neon.com/docs/connect/connection-pooling
- https://render.com/docs/web-services
- https://render.com/docs/blueprint-spec
- https://www.nuget.org/packages/MediatR/14.2.0
- https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.2
