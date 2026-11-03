# Checkpoint 17 — Homologação automática

O orquestrador `scripts/homologacao/Invoke-MvpHomologation.ps1` permanece a entrada única de validação. A suíte integrada só pode rodar com banco isolado `rass_sprint1_tests`, três roles esperadas, TLS `VerifyFull`, host explícito e confirmação manual da branch.

Cobertura atual: build .NET, testes de domínio/arquitetura, guardas de segurança/migrations, `flutter analyze`, `flutter test`, higiene de segredos e smoke HTTP opcional. E2E Flutter em dispositivo, Google Play Billing e enforcement Free/Premium devem ser adicionados antes de qualquer veredito positivo.

## Rodada local de 2026-11-03

- `flutter analyze`: concluído sem falha de análise reportada.
- `flutter test --reporter compact`: **23/23** aprovados. Mensagens `RASS_HTTP_FAILURE` correspondem aos cenários negativos esperados pelos mocks.
- `flutter build apk --debug --flavor homologation --dart-define=API_BASE_URL=https://homologation.invalid` foi concluído em 2026-11-03. Artefato: `mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-homologation-debug.apk`; SHA-256 `E1E56B13FFBCEDE3251BBF01F33504CFFF6C500004B399A2A39C590C24836E0C`.
- O artefato usa deliberadamente `homologation.invalid`; valida compilação e instalação isolada, mas **não** homologação funcional com API. Um APK com URL da API isolada e teste em emulador permanecem pendentes.
- O orquestrador passou a separar `-BuildAndroidDebug` de `-BuildAndroidRelease`. O primeiro exige `-HomologationApiBaseUrl` explícita e bloqueia a URL pública Production; o segundo permanece opt-in e exige assinatura aprovada.
- O Flutter não possui mais fallback para a API pública: binários sem `API_BASE_URL` válida exibem erro de configuração. HTTPS é obrigatório, exceto `10.0.2.2`/loopback para homologação local.
