# Sprint 2 - Identity e Multi-tenancy

Reutilizar branch development-sprint-1, nunca production. Neon Managed Better
Auth ja provisionado. REST oficial via adapter .NET, sem alterar neon_auth.
Fluxo real confirmou EdDSA/Ed25519, issuer/audience = origem HTTPS do Auth,
cookie __Secure-neon-auth.session_token. Nao presumir endpoints OIDC nem OAuth
refresh_token. SDK browser nao e introduzido: Blazor e Flutter usam a API/BFF .NET.

JWT validado por Microsoft.IdentityModel + ScottBrady.IdentityModel EdDSA,
JWKS HTTPS com cache/refresh e rotacao. Validar issuer/audience/signature/exp/
subject/algoritmo; ignorar role/permissions/tenant claims. Sem crypto propria.
Sessao da aplicacao e UUID publico + hashes de JWT/cookie, expiracao/revogacao.
Bearer exige X-App-Session ligado ao hash/sub do JWT; logout revoga no banco.
Sessao Neon continua autoridade externa; refresh verifica get-session e token.
App nao armazena password, cookie Neon ou JWT em plaintext no banco.

Portas de auth/identity em Application; adapters em Infrastructure; contextos/
policies HTTP em API. Web/Admin: cookie opaco HttpOnly/SameSite, antiforgery,
ticket server-side protegido com DataProtection em storage persistente. Flutter:
SecureStorage, refresh/restauracao/logout e abstracao de navegacao. Nenhum banco
direto nos clientes. Retry somente GET, nunca signup/signin/signout.

Tenant possui owner subject, tipo pessoal e unique owner parcial. Onboarding
cria tenant/membership/profile numa transacao com IDs gerados no servidor.
Memberships sao fonte de role/permissoes. Seletor de tenant em rota/header e
apenas candidato: API consulta membership ativo antes de resolver contexto.
Application valida permissoes novamente nos handlers e recurso/owner na Sprint 3.

RLS: membership SELECT somente subject; INSERT/UPDATE somente owner do tenant,
sem auto-elevacao. Profiles/financas exigem tenant + subject + membership ativo.
Metadata de tenants pode ser listada para memberships proprios via user context,
sem abrir dados financeiros sem tenant. Bootstrap usa user scope trusted antes
da criacao do tenant; papel runtime continua sem bypass/DDL/ownership.

Auditoria append-only de metadados, sem email/password/JWT/valores financeiros.
Migracao nova, grants explicitamente revisados. Fixtures sinteticas adaptadas.
Gates: JWT negativos/rotacao, login/refresh/logout, RLS membership, tenant/user/
role isolation, restart/restauracao, build/test/audit/documentacao. Auth live
testado na branch dev; CI usa fixtures assinadas, nao as confunde com provedor.

Sprint 3 so inicia apos o gate desta sprint. Financas: accounts/categories/
transactions/transfers/cards, IDs distribuidos, optimistic version, idempotency
duravel e transferencia + auditoria na mesma transacao. Offline engine na 4.
