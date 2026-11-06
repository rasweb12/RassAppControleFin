# Checkpoint 18 — Roteiro de homologação manual

Não iniciar antes de existir APK debug fresco, ambiente isolado e conta sintética.

1. Instalar APK e confirmar versão/commit exibidos no relatório.
2. Criar conta sintética, entrar e sair; validar recuperação de senha em ambiente de teste.
3. Criar contas, categorias, receitas, despesas, transferências, cartão, compra parcelada e pagamento de fatura.
4. Fechar e reabrir o app; confirmar persistência, sincronização e ausência de duplicação.
5. Validar restrições Free/Premium somente depois de enforcement server-side implementado.
6. Entrar no Admin.Web como User, TenantAdmin e SuperAdmin; confirmar navegação e APIs negadas quando aplicável.
7. Registrar falhas com versão, passo, resultado esperado e evidência visual.

Estado: **BLOQUEADO** até APK com API isolada confirmada, testes E2E e fluxos de monetização estarem disponíveis. Há um APK Debug fresco, mas ele aponta deliberadamente para `homologation.invalid` e não deve ser usado para homologação financeira.

## Evidência Android local — 2026-11-03

- O APK Debug foi instalado e aberto no emulador Android 16 `emulator-5554`.
- Os pacotes `com.rassapp.rassapp_controlefin` e `com.rassapp.rassapp_controlefin.homologation` coexistem; a atividade em foco foi a variante `homologation`.
- A árvore de acessibilidade confirmou a tela de login e campos de e-mail/senha vazios. Não houve login, cadastro, dados sintéticos ou conexão financeira.
- Isso aprova somente instalação, abertura e isolamento do `applicationId`; não aprova comunicação com API, persistência, navegação financeira nem experiência visual pelo proprietário.
