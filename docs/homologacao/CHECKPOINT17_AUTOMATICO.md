# Checkpoint 17 — Homologação automática

O orquestrador `scripts/homologacao/Invoke-MvpHomologation.ps1` permanece a entrada única de validação. A suíte integrada só pode rodar com banco isolado `rass_sprint1_tests`, três roles esperadas, TLS `VerifyFull`, host explícito e confirmação manual da branch.

Cobertura atual: build .NET, testes de domínio/arquitetura, guardas de segurança/migrations, `flutter analyze`, `flutter test`, higiene de segredos e smoke HTTP opcional. E2E Flutter em dispositivo, Google Play Billing e enforcement Free/Premium devem ser adicionados antes de qualquer veredito positivo.
