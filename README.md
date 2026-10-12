# RassAppControleFin

Nova geracao independente. Versao **1.0.0-alpha.1**. Pasta:
D:\Curso\APP\RassAppControleFin. Stack: .NET 10, Neon, Resend, Render e Gemini.

Sprint 2 validada: Neon Auth, sessoes revogaveis, memberships, policies e RLS.
Sprints 0 a 9 concluidas para a beta tecnica. Mobile e o unico frontend do usuario; Admin usa Blazor.
Web anterior preservado como referencia inativa, fora da solution/CI/deploy.
Gemini e o unico provedor generativo permitido; a integracao backend esta pronta e desabilitada por padrao.

## Executar

```powershell
dotnet restore RassAppControleFin.slnx --locked-mode
dotnet build RassAppControleFin.slnx --no-restore -c Release
dotnet test RassAppControleFin.slnx --no-build --no-restore -c Release
.\scripts\Start-Local.ps1
```

API: http://localhost:5080; Swagger em Development: /swagger.
Admin: http://localhost:5082. Nao ha Web do usuario final.
API protegida exige JWT valido, X-App-Session e membership para selecionar tenant.
Esta alpha ainda nao deve receber dados financeiros reais de producao.
API /health/live verifica o processo; /health/ready exige banco, schema, RLS e
role seguro. Sem configuracao de banco, readiness retorna 503, sem afetar liveness.

## PostgreSQL e Testes

Configuracao local ja esta em User Secrets. Para outra maquina, seguir
[Database](docs/database.md). Nao colocar credenciais no arquivo de exemplo.
Os testes requerem PostgreSQL real e banco dedicado rass_sprint1_tests;
RASS_DB_TEST_ADMIN e RASS_DB_TEST_RUNTIME via ambiente ou User Secrets.
O CI provisiona PostgreSQL 18 efemero; nao recebe credenciais Neon.

```powershell
dotnet tool restore
dotnet run --project tools/RassAppControleFin.Database -c Release -- migrate
dotnet run --project tools/RassAppControleFin.Database -c Release -- bootstrap
# Opcional, SOMENTE em banco isolado de desenvolvimento:
dotnet run --project tools/RassAppControleFin.Database -c Release -- seed-development --confirm-development
```

Migrations/grants nunca sao executados pelo startup da API. O tool exige conexao
direta RASS_MIGRATIONS_CONNECTION; API usa pooled TLS VerifyFull e rass_app_runtime.

MAUI tem solution propria, para nao exigir workloads Android/iOS nos hosts Linux:
```powershell
dotnet build RassAppControleFin.Mobile.slnx -c Release
```
iOS exige macOS/Xcode. Android exige SDK API 36 e JDK compativel; caminhos locais
podem ser informados por AndroidSdkDirectory e JavaSdkDirectory sem alterar fontes.

Gate Sprint 2: 107 testes aprovados, build servidor/Android sem warnings,
auth real, RLS e Playwright. Relatorios e limites em docs/sprint-2-review.md.
IA desativada: Gemini:Model configuravel e GEMINI_API_KEY somente no backend.
Nenhuma chave/modelo de IA deve ser configurado no Mobile.

Secrets: User Secrets, variaveis de ambiente, Render/GitHub secret store.
.env.example e referencia; .NET nao carrega dotenv automaticamente.
Nenhuma senha, token, chave privilegiada ou connection string deve ir aos clientes.

## Documentacao

- [Arquitetura e decisoes iniciais](docs/architecture.md)
- [Roadmap](docs/roadmap.md)
- [Evidencias da Sprint 0](docs/sprint-0.md)
- [Evidencias da Sprint 1](docs/sprint-1.md)
- [Evidencias da Sprint 2](docs/sprint-2-review.md)
- [Atualizacao Gemini/Mobile/Neon](docs/prompt-update-2026-10-11.md)
- [Banco, RLS e operacao](docs/database.md)
- [Neon](infra/neon/README.md)
- [Render](infra/render/README.md)
- [Guia de release beta](docs/beta-release-guide.md)

O repositorio antigo foi consultado somente como referencia.
MediatR 14: verificar termos/licenca da organizacao antes da distribuicao.
