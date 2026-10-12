# Sprint 1 - Neon e PostgreSQL

Data: 2026-10-09. Versao: 1.0.0-alpha.1. Caminho: D:\Curso\APP\RassAppControleFin.
Escopo encerrado: persistencia e RLS foundation, nao identidade nem financas.

## Entregas

- EF Core/Relational/Design 10.0.12, Npgsql/provider 10.0.3, lockfiles atualizados.
- Domain Tenant/UserProfile BCL; portas de transacao em Application, EF somente
  Infrastructure. Nenhum DTO exposto ou endpoint financeiro adicionado.
- Migration inicial 20261009114112_InitialFoundation e snapshot, UUID/UTC,
  constraints, FK/unique index, Version/concurrency, ENABLE/FORCE RLS.
- Runtime login SQL rass_app_runtime sem bypass/admin/membership/ownership/DDL/
  TRUNCATE; bootstrap de grants versionado e embutido.
- Contexto tenant/subject parametrizado LOCAL por transacao; filtros adicionais,
  write guard, rollback/dispose/cancel, conflitos explicitos sem LWW ou retry.
- Health readiness real e spans de persistencia sem SQL/valores sensiveis.
- Tool operacional migrate/bootstrap/seed-development; seed sintetico opt-in.
- CI com PostgreSQL 18 efemero e testes de banco obrigatorios; Render blueprint
  recebe apenas secret runtime. Nenhum deploy ou configuracao de merge remota.

## Neon Real

Projeto existente morning-dream-96756371, regiao aws-sa-east-1, PostgreSQL 18.
Branch criada development-sprint-1 / br-steep-surf-b60fqoqs, parent production /
br-lucky-paper-b6egkr8q. Bancos neondb (dev) e rass_sprint1_tests (fixtures).
Migrate executado e repetido sem reaplicar schema; bootstrap aplicado com sucesso.

Inspecao inicial: production apenas neon_auth, zero usuarios/sessoes. Inspecao
final: production ainda sem schema app. Banco/projeto antigo nao tocado.
Plano rejeitou customizacao de suspend timeout; defaults mantidos.

Role via API veio privilegiado; sua tentativa de reducao via SQL foi recusada
pelo Neon. Papel foi removido, substituido por login criado VIA SQL sem
BYPASSRLS/CREATEROLE/CREATEDB/REPLICATION/inheritance/memberships. Bootstrap valida
e recusa papel inseguro, sem tentar alterar atributos reservados. Segredos
locais em User Secrets, nunca nos arquivos da solution.

## Verificacao

Restore --locked-mode aprovado. Build Release: zero warnings/erros.
90 testes aprovados, zero falhas/skips: 14 unitarios, 71 integracao, 5 arquitetura.
Integracao: 18 API, 12 configuracao e 41 persistencia/RLS. Sem SQLite/InMemory
para evidencia de banco. TRX em artifacts/test-results (gitignored).

Cobertura: SELECT/INSERT/UPDATE/DELETE, tenant/user/missing context, rows nao
visiveis, mudanca de ownership/tenant negada, constraints, reuse de pool apos
commit/rollback/dispose/cancelamento, filtro EF + RLS, identidade/tenant exigidos,
write guard, rollback parcial, conflito Version explicito, seed repetido,
schema/model alinhados, readiness real e owner inseguro rejeitado.

NuGet transitive audit: nenhuma vulnerabilidade conhecida reportada.
Varredura exata dos fontes/lockfiles/configuracoes: nenhuma das credenciais
locais de desenvolvimento encontrada. Nao equivale a secret scanning/SAST formal.
Servidores reiniciados: API 5080, Web 5081, Admin 5082. API /health, /health/live
e /health/ready: 200; /api/v1/session sem identidade: 401.
Playwright Web/Admin em 1440x900 e 390x844: refresh/icone/layout sem overflow
ou erros de pagina. Capturas em artifacts/smoke (gitignored).
Revisao de arquitetura: Domain/clients sem EF/Npgsql; API sem migrations ou
credencial migration; ferramenta operacional separada; referencia antiga intacta.
Revisao de seguranca: TLS VerifyFull remoto, role restrito verificado, sem grants
PUBLIC/CREATE/TRUNCATE, RLS FORCE, contexto transacional, parametros, erros/logs
saneados, secrets fora do repositorio e auth deny-by-default preservado.

## Limites e Proxima Sprint

Ainda nao e sistema financeiro pronto para producao. Membership, JWT real Neon,
JWKS/EdDSA e sessao/revogacao entram na Sprint 2; politicas admin e testes reais
de identidade/claims precisam dela. RLS nao substitui autorizacao da aplicacao.
Schema gerenciado neon_auth nao foi modificado nem identidade customizada criada.

Contas/transferencias/auditoria financeira/idempotencia entram na Sprint 3;
SQLite/outbox/sync na 4. Nenhum resultado financeiro simulado para aparentar pronto.
SAST/secret scanning formal e branch protection permanecem no roadmap DevSecOps.
Docker/CI remoto/Render nao foram executados neste host; PostgreSQL real validado
no Neon e publish local validado. Android/iOS nao mudaram; evidencia da fundacao
na Sprint 0, sem afirmar validacao iOS/macOS neste host Windows.
