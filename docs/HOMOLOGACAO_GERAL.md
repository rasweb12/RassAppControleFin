# Homologação Geral — RassFin

## Escopo desta rodada

Esta rodada executou regressão local, sem migration, deploy, commit, push, publicação ou alteração de Neon/Render Production.

| Comando | Resultado |
| --- | --- |
| `dotnet build src/RassAppControleFin.Api/... -c Release --no-restore` | Aprovado, 0 warnings |
| `dotnet build src/RassAppControleFin.Admin.Web/... -c Release --no-restore` | Aprovado, 0 warnings |
| `dotnet test tests/RassAppControleFin.UnitTests/... -c Release --no-build` | 47/47 aprovados |
| `dotnet test tests/RassAppControleFin.ArchitectureTests/... -c Release --no-build` | 5/5 aprovados |
| `flutter analyze` | Aprovado |
| `flutter test` | 23/23 aprovados |
| `flutter build apk --debug` | APK Debug gerado |

## Não executado

- Integração PostgreSQL/API: bloqueada corretamente nesta estação porque o fixture exige o banco dedicado `rass_sprint1_tests`; os segredos ativos foram usados anteriormente para branch descartável `neondb`. Não houve redirecionamento perigoso da suíte para Production.
- Homologação manual do APK atual: pendente.
- Testes de Play Store, assinatura release e AAB: pendentes.
- Simulação das migrations 11–16: bloqueada pelo provisionamento técnico de roles descrito em `CHECKPOINT13_NEON_PRODUCTION_UPGRADE.md`.

## Roteiro obrigatório de homologação manual

1. Login, logout, recuperação de senha e troca de usuário.
2. Criar, editar e excluir/estornar receita e despesa; conferir saldo e extrato após reiniciar.
3. Criar transferência; confirmar uma linha lógica no extrato e patrimônio inalterado.
4. Criar cartão offline, sincronizar e confirmar substituição de ID `local-*` por ID persistido e versão válida.
5. Registrar compra à vista e parcelada; verificar fatura e parcelas.
6. Pagar fatura parcialmente e integralmente; conferir conta de origem, fatura e ausência de duplicidade após nova sincronização.
7. Validar metas, redução de despesas, investimentos e Primeiro me pago com dados sintéticos.
8. Repetir os fluxos de diálogos, cancelamento e navegação rápida; não aceitar `_dependents.isEmpty`, controller descartado ou overflow.

## Gate

Status: **REPROVADO** até os itens P0 de banco, integração e homologação manual do APK atual serem comprovados.
