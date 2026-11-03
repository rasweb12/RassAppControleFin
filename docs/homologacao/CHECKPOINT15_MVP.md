# Checkpoint 15 — homologação automatizada do MVP

## Escopo e regra de decisão

O comando reproduzível é `scripts/homologacao/Invoke-MvpHomologation.ps1`.
Ele nunca aplica migrations, altera Render, publica artefatos ou escreve em Production. A integração só é habilitada com as três conexões de teste no processo, banco `rass_sprint1_tests`, TLS `VerifyFull`, host explícito e confirmação manual da branch Neon isolada.

## Cobertura automatizada

| Camada | Evidência automatizada | Estado inicial |
|---|---|---|
| .NET domínio, aplicação e segurança | testes unitários e arquitetura | Executável localmente |
| PostgreSQL/API/RLS/RBAC | suíte integrada, apenas com banco isolado confirmado | Requer conexão isolada explícita |
| Flutter | `flutter analyze` e `flutter test` | Executável localmente |
| Flutter em dispositivo/API real | `integration_test` | BLOQUEADO: diretório/instrumentação ausentes |
| Android | APK debug e AAB release | APK depende do SDK; AAB exige assinatura local |
| Smoke HTTP | `/health/live` e `/health/ready` | Requer URL fornecida explicitamente |

## Ambiente em produção

O Neon Production está no schema de 20 migrations segundo a validação do Checkpoint 14. Em 2026-11-03, o serviço Render `RassAppControleFin` foi confirmado como `live`, em `main`, no commit `a78e86e` (MVP6); a preparação do Checkpoint 14 está na branch de trabalho adiante desse commit. A consulta externa de leitura para `/health/ready` expirou e, portanto, não é evidência suficiente de readiness HTTP. Essa divergência impede tratar o deploy ativo como release do código de homologação até merge autorizado, novo deploy e smoke tests efetivos.

## Bloqueadores conhecidos

- Não há `integration_test` Flutter nem evidência de execução em emulador/dispositivo.
- AAB release requer assinatura configurada fora do repositório.
- `applicationId`, versão, política de privacidade, exclusão de conta e metadados Play exigem definição final.
- Data Protection no Render indicou armazenamento de chaves não persistente; requer configuração antes de lançamento comercial.
- A licença do MediatR deve ser confirmada antes de uso comercial.

Resultado inicial do Checkpoint 15: **NO-GO**, até que todas as etapas obrigatórias tenham evidência real.
