# Checkpoint 17 — Homologação automática

O orquestrador `scripts/homologacao/Invoke-MvpHomologation.ps1` permanece a entrada única de validação. A suíte integrada só pode rodar com banco isolado `rass_sprint1_tests`, três roles esperadas, TLS `VerifyFull`, host explícito e confirmação manual da branch.

Cobertura atual: build .NET, testes de domínio/arquitetura, guardas de segurança/migrations, `flutter analyze`, `flutter test`, higiene de segredos, smoke HTTP opcional e a guarda E2E Android de configuração. Google Play Billing, fluxos financeiros E2E com API isolada e enforcement Free/Premium continuam pendentes antes de qualquer veredito positivo.

## Rodada local de 2026-11-03

- `flutter analyze`: concluído sem falha de análise reportada.
- `flutter test --reporter compact`: **23/23** aprovados. Mensagens `RASS_HTTP_FAILURE` correspondem aos cenários negativos esperados pelos mocks.
- `flutter build apk --debug --flavor homologation --dart-define=API_BASE_URL=https://homologation.invalid` foi concluído em 2026-11-03. Artefato: `mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-homologation-debug.apk`; SHA-256 `E1E56B13FFBCEDE3251BBF01F33504CFFF6C500004B399A2A39C590C24836E0C`.
- O artefato usa deliberadamente `homologation.invalid`; valida compilação e instalação isolada, mas **não** homologação funcional com API. Um APK com URL da API isolada e teste em emulador permanecem pendentes.
- O orquestrador passou a separar `-BuildAndroidDebug` de `-BuildAndroidRelease`. O primeiro exige `-HomologationApiBaseUrl` explícita e bloqueia a URL pública Production; o segundo permanece opt-in e exige assinatura aprovada.
- O Flutter não possui mais fallback para a API pública: binários sem `API_BASE_URL` válida exibem erro de configuração. HTTPS é obrigatório, exceto `10.0.2.2`/loopback para homologação local.

## Execução central com emulador isolado

- A variante Android `e2e` usa `applicationIdSuffix = ".e2e"`; ela não compartilha FlutterSecureStorage, Drift, cache ou sessões com os pacotes base e `homologation`.
- `integration_test/configuration_guard_test.dart` foi executado em Android 16 com `API_BASE_URL` vazia: **1/1** aprovado. A evidência comprova somente o bloqueio pré-rede de binários não configurados.
- O comando central foi exercitado com `-AndroidDeviceId emulator-5554`. Build, testes .NET, higiene, análise Flutter, testes Flutter e E2E foram aprovados. Banco isolado, APK de homologação com API real, AAB Release e smoke HTTP ficaram deliberadamente **BLOQUEADOS** por ausência de parâmetros/infraestrutura autorizados; o resultado consolidado segue **NO-GO**.

## Execução central repetida — 2026-11-03

- O modo local seguro do orquestrador produziu o relatório sanitizado `artifacts/homologacao/mvp-homologation-20261103-151031.md` no commit `5350df7`.
- Build Release, 55 testes unitários .NET, 5 testes de arquitetura, higiene de segredos, `flutter analyze`, 28 testes Flutter e a guarda `integration_test` da variante Android isolada foram aprovados.
- Integração PostgreSQL/API, APK apontado para API de homologação, AAB assinado e smoke HTTP permaneceram bloqueados de forma explícita. Nenhuma URL, conexão de banco, migration, deploy ou escrita remota foi inferida pelo comando.
