# Roadmap

Executar uma sprint por entrega. Ordem do prompt mais recente:
Atualizacao 2026-10-11: Neon confirmado, Gemini exclusivo, Mobile-only para usuario,
Admin Blazor, sem Stripe/Azure/SQL Server. IA funcional continua na Sprint 8.

| Sprint | Escopo | Gate principal |
| --- | --- | --- |
| 0 | Foundation | build, testes, arquitetura, seguranca e smoke |
| 1 | Neon/PostgreSQL | migrations/RLS CRUD com roles reais e pool |
| 2 | Identity/Multi-tenancy | JWT EdDSA, membership, user/tenant/admin isolation |
| 3 | Core Finance | invariantes, transferencia atomica, idempotencia, concorrencia |
| 4 | Offline-first | SQLite/outbox/inbox, crash/retry/conflict, sync incremental |
| 5 | Dashboard Mobile | filtros consistentes em todos os componentes |
| 6 | Budgets/Goals | planejamento e alertas autorizados |
| 7 | Admin | suporte/tickets/chat/auditoria/monitoramento |
| 8 | Gemini | fatos autorizados reais, fallback deterministico, limites/custos/metricas |
| 9 | Observability/Resilience | tracing/metrica/logs de todos os fluxos |
| 10 | Security/DevSecOps | SAST/secrets/dependencies/RLS e CI hardening |
| 11 | UX/Design System | navegacao/responsive/accessibility Mobile/Admin |
| 12 | Beta | validar fluxos reais, operacao e recuperacao |

Definition of Done: implementacao e testes adequados, autorizacao/isolamento,
offline quando aplicavel, telemetria, documentacao, zero warnings e checks verdes.
Testes criticos 1000/500 -> transferencia 300 -> 700/800, duplicates, concorrencia,
offline, conflitos e falha parcial sao gates das sprints 3/4, nao simulados na 0.

Branch protection/required checks sao configuracao do repositorio remoto.
O arquivo de CI nao configura sozinho bloqueio de merge, SAST ou secret scanning.
