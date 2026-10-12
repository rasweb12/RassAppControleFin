# Database - Sprint 1

## Limites

PostgreSQL 18 e system of record; EF Core 10.0.12, Npgsql/provider 10.0.3 somente
Infrastructure. Nenhum SqlServer/LocalDB/Azure/SQLite no servidor. Domain sem
EF, HTTP ou SDK. Tools/RassAppControleFin.Database e executavel operacional,
fora do startup: migracao, grants e seed explicitos. Nao e servidor nem endpoint.

Schema app possui Tenants e UserProfiles. Perfis pertencem a tenant/subject
opaco da aplicacao; nao armazenam passwords/tokens, nao referenciam neon_auth
e nao substituem identity/membership da Sprint 2. Financas entram na Sprint 3.

UUID distribuidos, timestamptz UTC, comprimentos/check constraints, FK Restrict,
PK perfil (TenantId, Id), unique (TenantId, UserId). Indice composto tambem
cobre FK TenantId. Version bigint e concurrency token, incrementado pelo Domain.
Nenhum JSONB sem necessidade. Migration/snapshot versionados em Persistence/Migrations.

## Conexoes e Secrets

API: Database:ConnectionString em formato Npgsql, nao URL postgres://.
Exemplo SEM valores reais:

```text
Host=<endpoint-pooler>;Database=neondb;Username=rass_app_runtime;Password=<secret>;SSL Mode=VerifyFull
```

Database:RuntimeRole=rass_app_runtime, MaxPoolSize=10, MinPoolSize=0,
ConnectionTimeoutSeconds=10, CommandTimeoutSeconds=30. Runtime remoto exige
VerifyFull; reset no retorno ao pool, sem multiplexing/enlist/parameter logging/
IncludeErrorDetail. EF usa NullLoggerFactory; spans de transacoes sao saneados
e nao incluem SQL, parametros, subject, nome ou segredo.

User Secrets da API: RassAppControleFin-Api-Foundation. Credenciais operacionais
e de testes: RassAppControleFin-Database-Development, chaves:
RASS_MIGRATIONS_CONNECTION, RASS_DB_TEST_ADMIN, RASS_DB_TEST_RUNTIME.
Tools/tests carregam ambiente sobre User Secrets; dotnet-ef aceita
RASS_MIGRATIONS_CONNECTION do ambiente antes do secret.
Preservar chaves existentes ao configurar. User Secrets nao e cofre criptografado
de producao: somente ambiente local controlado, nunca Git ou artefatos publicados.

Deploy Render: Database__ConnectionString e secret manual; runtime sem credencial
de migration. Web/Admin/Mobile nunca recebem conexoes. Job de migration recebe
apenas conexao direta privilegiada pelo secret store. Blueprint foi preparado,
nenhum deploy remoto executado. Nunca executar migrate automaticamente no startup.

## Provisionamento

1. Selecionar projeto/branch de desenvolvimento explicitamente, verificar catalogo
   antes de ramificar. Nao copiar dados pessoais para ambientes de teste.
2. Criar papel restrito VIA SQL, senha aleatoria no secret store. Neon Console/API
   pode criar papeis com BYPASSRLS; nao utilizar tais papeis em runtime.
3. Configurar conexao direta privilegiada e conexao pooled do login restrito.
4. Executar migrate e bootstrap. Validar /health/ready e suite de banco.
5. Promover migration apenas por job revisado, com backup/restore testado antes de
   producao. Nunca importar DbContext/migrations/banco da geracao antiga.

DDL de provisionamento (senha deve ser aplicada pelo canal privado do operador):

```sql
CREATE ROLE rass_app_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
    NOREPLICATION NOBYPASSRLS NOINHERIT;
```

```powershell
dotnet tool restore
dotnet run --project tools/RassAppControleFin.Database -c Release -- migrate
dotnet run --project tools/RassAppControleFin.Database -c Release -- bootstrap
dotnet ef migrations has-pending-model-changes --project src/RassAppControleFin.Infrastructure --startup-project src/RassAppControleFin.Infrastructure -c Release
```

Bootstrap versionado/embutido: infra/neon/bootstrap-runtime.sql. Runtime recebe
USAGE app, CRUD apenas Tenants/UserProfiles e SELECT migration history. Sem CREATE,
TRUNCATE, ownership, membership privilegiado ou grants a PUBLIC. Novas tabelas
exigem grants revisados; nao ha ALTER DEFAULT PRIVILEGES permissivo.
FORCE nao restringe superuser ou BYPASSRLS, mesmo quando sao donos da tabela.
O tool recusa host -pooler para operacao de schema e retorna erros saneados.

## RLS e Transacoes

Migration habilita ENABLE e FORCE. Tenants: Id = app.tenant_id e subject presente.
UserProfiles: TenantId = app.tenant_id e UserId = app.user_id, USING + WITH CHECK
em todos os comandos. Ausencia de qualquer contexto nega perfis. Public nao tem
grants; anon e testado como runtime sem contexto, nao como role Neon inventado.

ITenantTransactionFactory recebe ITenantContext e ICurrentUser do servidor,
nunca DTO/body/header como autoridade. API da Sprint 1 ainda injeta contextos
fechados; fixtures autenticadas sao apenas de testes, nao login falso.
Sprint 2 deve resolver membership ANTES de disponibilizar ITenantContext.

Begin valida identidade/tenant, verifica papel/schema/RLS, abre transacao EF e
usa set_config parametrizado (..., true) para contexto LOCAL na mesma conexao.
Query filters sao defesa adicional, nao substitutos de RLS. SaveChanges exige
transacao autorizada e escopo compativel. Dispose reverte sem commit e limpa tracker/
scope. Cancelamento reverte. Conflitos de Version/unique/FK sao Result Conflict;
check e Domain; privilege e Forbidden; indisponibilidade e Infrastructure.

Sem retry automatico de escrita. Falha de rede durante commit pode ter resultado
desconhecido; nao prometer rollback remoto nem reenviar cegamente. Antes das
operacoes financeiras, Sprint 3 exige OperationId/idempotencia/resposta duravel.
RLS com GUC confia no servidor: nao protege contra comprometimento do runtime/
SQL injection capaz de redefinir contexto. Autorizacao/policies, queries
parametrizadas e segredo do login exclusivo de servidor continuam obrigatorios.

## Testes e Seed

Criar rass_sprint1_tests dedicado; ambas conexoes devem apontar para o mesmo
endpoint/branch (admin direto, runtime pooled). Fixture recusa outro nome/host.
Teste sem credenciais falha, nunca pula RLS silenciosamente. CI usa postgres:18
local efemero; senhas literais do CI nao sao credenciais de producao.

```powershell
dotnet test RassAppControleFin.slnx -c Release
# Apenas camada de banco:
dotnet test tests/RassAppControleFin.IntegrationTests -c Release --filter FullyQualifiedName~IntegrationTests.Database.
# Opcional SOMENTE em branch/banco de desenvolvimento selecionado:
dotnet run --project tools/RassAppControleFin.Database -c Release -- seed-development --confirm-development
```

DevelopmentSeed e idempotente, transacional e sintetico: tenants A/B, subjects
fixture-alice/bob/carol. Nao cria login/membership nem dados financeiros. Testes
executam seed duas vezes. CRUD usa rollback; teste de concorrencia cria IDs
aleatorios e remove somente seus registros autorizados no banco dedicado.
Neondb dev permanece sem fixtures ate o operador executar seed opt-in.

Health/live: processo. Health/ready: conexao, migration esperada, policies,
ENABLE/FORCE e privilegios seguros; falha 503 sem excecao de provider no payload.
Health sozinho nao substitui testes de politicas/autorizacao nem monitoramento.

## Fontes

- [Neon pooling](https://neon.com/docs/connect/connection-pooling): pooled runtime,
  direct migrations e ausencia de estado persistente de sessao.
- [PostgreSQL 18 RLS](https://www.postgresql.org/docs/18/ddl-rowsecurity.html):
  USING/WITH CHECK, FORCE, limites de BYPASSRLS e TRUNCATE.
- [Npgsql EF](https://www.npgsql.org/efcore/): provider EF para PostgreSQL.
- [Neon roles](https://neon.com/docs/manage/roles): gerenciamento de roles; atributos
  foram tambem verificados no catalogo real da branch antes do uso.
