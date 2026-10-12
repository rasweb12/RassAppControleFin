# Sprint 0 - Evidencias

Data: 2026-10-09. Versionamento: 1.0.0-alpha.1.

## Implementado

- Arquitetura e analise da referencia antes do codigo.
- Domain sem dependencias, contratos independentes dos casos de uso.
- Result/Result<T>/Error e validacao async via MediatR.
- API versionada operacional, erro centralizado/redigido e correlation ID.
- Fallback de autorizacao fechado, rate limiter, OpenAPI/Swagger em Development.
- Health/live/ready da fundacao, OTEL HTTP/MediatR, configuracoes tipadas.
- Hosts Blazor Web/Admin consomem API com resilience e sem retries de POST.
- MAUI Android/iOS separado, MVVM e abstracao de navegacao.
- Docker/Render e CI preparados; nenhum deploy remoto.

## Verificacao

| Verificacao | Resultado |
| --- | --- |
| Restore locked-mode, servidor e MAUI | Aprovado |
| Build servidor/Web/Admin Release | Aprovado, zero warnings/errors |
| MAUI Android Release/API 36 | Aprovado, zero warnings/errors; APK gerado |
| Testes unitarios | 9/9 |
| Testes HTTP/integracao | 18/18 |
| Testes de arquitetura | 3/3 |
| Dependency audit NuGet, solution servidor | Nenhuma vulnerabilidade conhecida reportada |
| Publish API/Web/Admin | Aprovado |
| HTTP smoke API/OpenAPI/Swagger/health | Aprovado |
| Playwright Web/Admin 1440x900 e 390x844 | Refresh, icones e ausencia de overflow aprovados |
| Revisao visual das capturas | Aprovado |
| Build independente da referencia | Aprovado; 1 warning CS0168 preexistente |
| Testes da referencia sem LocalDB | 22/22 |
| Git da referencia antes/depois | Mesmo SHA256 do status porcelain |

Testes da referencia ApiEndpointsIntegrationTests nao foram executados porque
criam/excluem databases LocalDB. Nenhum banco de referencia foi acessado.
Artefatos da verificacao antiga foram direcionados para artifacts/legacy.
SHA256 do status da referencia:
499A4233C60E511FB055BB28738576011A23F3A146590D1BB0420419ADA61DD8.

Evidencias locais: artifacts/test-results (TRX), artifacts/legacy-test-results,
artifacts/smoke (PNG), artifacts/publish e APK em
src/RassAppControleFin.Mobile/bin/Release/net10.0-android.
APK e de verificacao local, nao uma distribuicao assinada para loja.

Hosts locais iniciados: API :5080, Web :5081, Admin :5082.
Swagger: http://localhost:5080/swagger em Development.
Start-Local.ps1 verifica portas antes de iniciar processos ocultos.

Comando MAUI utilizado neste ambiente:
```powershell
dotnet build RassAppControleFin.Mobile.slnx -c Release -p:AndroidPackageFormats=apk '-p:AndroidSdkDirectory=C:\Users\RASS\AppData\Local\Android\Sdk' '-p:JavaSdkDirectory=C:\Program Files\Java\jdk-17'
```

O SDK Android configurado pelo Visual Studio nao tinha API 36; o SDK do perfil
ja possuia essa plataforma e foi usado sem instalar pacotes de sistema.

## Revisao de arquitetura e seguranca

Sem achado bloqueante observado no escopo implementado. Domain usa somente BCL;
clients referenciam Contracts; providers permanecem fora do Domain. Testes de
dependencia verificam projetos e assembly do Domain. Nenhuma migration antiga,
referencia ao repositorio anterior, SqlServer ou SQLite server-side foi adicionada.

Endpoints protegidos rejeitam acesso anonimo e JWT sem assinatura. Swagger e
OpenAPI sao acessiveis somente em Development. Erros inesperados retornam
ProblemDetails generico; logger customizado registra tipo e correlationId, sem
exception message/body. Falhas Result retornam status apropriado e data null.
HTTP resilience desabilita retry para metodos inseguros. Options/secrets exemplo
nao contem valores reais. Actions CI fixadas em hashes oficiais verificados.

A verificacao nao substitui o hardening completo da Sprint 10: SAST, scanner de
secrets e branch protection ainda devem ser habilitados e executados remotamente.
O audit atual consultou a solution servidor; o restore MAUI tambem passou com
NuGetAudit habilitado e warnings-as-errors.

## Limites explicitos

Sprint 0 nao provisiona Neon, nao executa EF migrations/RLS, nao implementa
sign-in/refresh do Neon Auth e nao envia email Resend. Contas/transferencias,
SQLite/Sync Engine executavel e IA sao sprints posteriores.
Interfaces nao representam implementacoes prontas.
Health/ready nao declara PostgreSQL/Neon/Auth saudaveis nesta etapa.
iOS somente e verificavel em macOS/Xcode.
Docker/Render e GitHub Actions precisam de execucao nos ambientes respectivos.
