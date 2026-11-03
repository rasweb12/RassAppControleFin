# Checkpoint 17 — Homologação automática

O orquestrador `scripts/homologacao/Invoke-MvpHomologation.ps1` permanece a entrada única de validação. A suíte integrada só pode rodar com banco isolado `rass_sprint1_tests`, três roles esperadas, TLS `VerifyFull`, host explícito e confirmação manual da branch.

Cobertura atual: build .NET, testes de domínio/arquitetura, guardas de segurança/migrations, `flutter analyze`, `flutter test`, higiene de segredos e smoke HTTP opcional. E2E Flutter em dispositivo, Google Play Billing e enforcement Free/Premium devem ser adicionados antes de qualquer veredito positivo.

## Rodada local de 2026-11-03

- `flutter analyze`: concluído sem falha de análise reportada.
- `flutter test --reporter compact`: **23/23** aprovados. Mensagens `RASS_HTTP_FAILURE` correspondem aos cenários negativos esperados pelos mocks.
- A tentativa de `flutter build apk --debug --flavor homologation` não gerou artefato novo verificável: o executor local encerrou a captura durante a tarefa Gradle e o APK existente permaneceu datado de 2026-11-01. Portanto, APK Debug fresco está **BLOQUEADO**, não aprovado.
- O orquestrador passou a separar `-BuildAndroidDebug` de `-BuildAndroidRelease`. O primeiro exige `-HomologationApiBaseUrl` explícita e bloqueia a URL pública Production; o segundo permanece opt-in e exige assinatura aprovada.
- O Flutter não possui mais fallback para a API pública: binários sem `API_BASE_URL` válida exibem erro de configuração. HTTPS é obrigatório, exceto `10.0.2.2`/loopback para homologação local.
