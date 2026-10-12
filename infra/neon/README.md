# Neon - Sprint 1

Projeto existente reutilizado: RassAppControleFin, morning-dream-96756371,
PostgreSQL 18 em aws-sa-east-1. Branch development-sprint-1:
br-steep-surf-b60fqoqs, derivada de production br-lucky-paper-b6egkr8q.

A inspecao previa identificou somente tabelas gerenciadas neon_auth, com zero
usuarios/sessoes. Production e o projeto antigo nao foram alterados. O plano
Neon recusou alteracao de suspend timeout; branch usa defaults permitidos.

Banco neondb da branch dev recebeu app.Tenants, app.UserProfiles, migration
20261009114112_InitialFoundation, ENABLE/FORCE RLS e grants minimos. Banco
rass_sprint1_tests e dedicado aos testes reais e fixtures sinteticas.

## Roles

NAO criar runtime via Console/API/CLI e assumir que e seguro. A verificacao real
do papel criado via API encontrou BYPASSRLS/CREATEROLE/CREATEDB e neon_superuser.
Esse papel temporario foi removido antes de ser usado pela aplicacao.

rass_app_runtime foi criado via SQL: LOGIN, NOSUPERUSER, NOBYPASSRLS, NOCREATEDB,
NOCREATEROLE, NOREPLICATION, NOINHERIT, sem memberships ou ownership.
bootstrap-runtime.sql valida atributos/membership antes de conceder privilegios.
Nao tenta remover atributos que o Neon reserva ao provedor. Papel inseguro
faz bootstrap/readiness/transacao falharem fechados.

Senha aleatoria fica apenas em User Secrets local/secret store do servidor.
Roles criados por SQL podem nao aparecer como roles gerenciados no Console:
configurar Npgsql diretamente com o login restrito, sem trocar pelo owner.

Runtime: endpoint pooled TCP/TLS VerifyFull, pool limitado e contexto por
transacao. Migrations/bootstrap: conexao direta privilegiada, execucao explicita.
Schema app separado de neon_auth. Nunca modificar tabelas gerenciadas de auth.
Detalhes operacionais: [Database](../../docs/database.md).

## Proximas Sprints

Sprint 2: Managed Better Auth JWT EdDSA/Ed25519, adapter .NET validado, claims,
JWKS rotation, membership, sessions e fluxo MAUI. Nao presumir OIDC discovery
nem refresh_token OAuth. Contextos ainda fechados e API protegida retorna 401.

Storage privado e realtime entram no caso de uso correspondente, por portas
neutras. Confirmar disponibilidade na regiao antes de provisionar; nao migrar
regiao ou inventar SDK para atender uma lista de produtos.
