# Sprint 2 - Identity e multi-tenancy

## Entregas

Neon Auth Managed Better Auth integrado por REST documentado, sem banco paralelo de senhas. JWT Ed25519/EdDSA validado por IdentityModel + ScottBrady.IdentityModel 4.1.0. JWKS com cache/atualizacao; issuer, audience, assinatura, algoritmo, expiracao e subject obrigatorios.

Sessao vinculada ao subject e hashes SHA-256 do cookie/JWT. X-App-Session identifica a sessao; revogacao local invalida o acesso imediatamente. Cadastro/login, renovacao por sessao Neon e logout via CQRS/MediatR. Nao existe refresh_token OAuth inventado.

Tenant pessoal e membership criados atomicamente. X-Tenant-ID e uma candidatura: membership ativo e permissoes sao obtidos no servidor. Policies na API, autorizacao adicional na Application e RLS versionado. UserSessions e AuditEntries nao armazenam JWT/cookie/senha em claro. Auditoria append-only.

Web/Admin: cookies HttpOnly/SameSite Strict, tickets criptografados persistidos fora do Git, antiforgery. DPAPI no Windows; Linux exige diretorio privado, disco persistente e protecao operacional das chaves. Mobile: autenticacao nativa, restauracao/renovacao em SecureStorage, HTTPS configuravel e logout online. Sem SQLite/offline nesta sprint.

## Gate

Build Release: zero erros/warnings. 107 testes passando: 14 unitarios, 88 integracao, 5 arquitetura, incluindo fluxo real Neon Auth e RLS PostgreSQL. Relatorios: artifacts/tests/sprint2-gate*.trx.

Android Release/APK compilado sem erros/warnings. Nao executado em dispositivo/emulador; iOS requer macOS/Xcode e permanece nao validado.

Playwright: Web/Admin 1440x900 e 390x844, CSRF, login, cookie opaco, ausencia de tokens no HTML/storage do browser, reload e overflow. Capturas: artifacts/smoke/auth-*.png. Nenhum deploy realizado.

NuGet audit transitivo: nenhuma vulnerabilidade conhecida reportada. Nao equivale a pentest/SAST completo (Sprint 10).

## Revisao de seguranca/arquitetura

- Claim role e campos de tenant/usuario/permissao no body nunca autorizam acesso.
- CookieContainer local captura somente aquela resposta. HttpClient usa UseCookies=false, sem contaminacao entre sessoes.
- Mutacoes de auth sem retry automatico; GETs com resilience. Sem logs de payloads, tokens ou SQL financeiro.
- Domain BCL-only; clientes dependem somente de Contracts. Presentation.Shared reutiliza BFF sem dependencia de backend.
- Migrations/owner exclusivos do operador. Alteracoes somente na branch development-sprint-1 e banco dedicado de testes.
- Memberships administrados pelo owner nesta fase. Administracao global, suporte privilegiado, remocao/alteracao de membros e SystemAdmin exigem grants/fluxos adicionais na Sprint 7; nenhum bypass global criado.

## Limites operacionais

Revogacao externa no Neon, fora do logout da aplicacao, pode deixar JWT emitido valido ate expirar. Renovacao subsequente exige sessao real do provider. Nao prometemos revogacao instantanea de JWT externo.

Tickets BFF em disco suportam uma instancia por host. Render com discos exige plano pago e interrompe servico durante deploy; Blueprint apenas preparado, nao aplicado, sem custos gerados. Scale-out requer ITicketStore distribuido e chaves compartilhadas.

Neon Auth deve ser configurado por ambiente, incluindo origins HTTPS confiaveis. Endpoints/JWKS de desenvolvimento nao sao valores de producao. Credenciais em User Secrets. CI usa fixtures criptograficas/PostgreSQL e exclui LiveAuth sem credenciais externas.

Resend SMTP customizado, OAuth social, recuperacao de senha/MFA e testes iOS nao habilitados nesta sprint. Nenhuma alteracao manual em neon_auth.
