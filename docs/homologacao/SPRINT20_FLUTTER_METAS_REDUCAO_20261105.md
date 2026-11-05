# Integração Flutter — Metas e Redução de Despesas

Data: 2026-11-05. Base: 9e06b55, com alterações locais da tela e teste dedicado.

## Implementação e evidência local

- Edição de metas envia versão otimista e preserva o progresso no contrato; alvo e data são editáveis.
- Redução de despesas permite selecionar 1–12 meses por período, padrão 3. A leitura usa o tenant autenticado.
- Erros ao atualizar metas e oportunidades recebem feedback seguro. Metas existentes permanecem visíveis.
- Flutter completo: 35/35 aprovados.
- flutter analyze: No issues found! (48,8 segundos).
- Novos testes usam HTTP simulado e SQLite em memória: tenant/período/valores, rejeição de período inválido sem fila de escrita, edição com versão e data civil.

Esses testes não são evidência de HTTP autenticado real ou de execução Android.

## Artefato Android

- Build Debug homologation concluído (assembleHomologationDebug: 37,4 segundos).
- API_BASE_URL: https://rassappcontrolefin-1.onrender.com.
- APK: mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-homologation-debug.apk.
- SHA-256: F89D9B6B9B19C8C61C9E1C4B83E41FE6026DE42CDFABA544E17973A347EE5334.
- adb -s emulator-5554 install -r: Success. Instalação preservada; não houve desinstalação ou limpeza de dados. Aplicativo não foi aberto automaticamente.

## Homologação integrada restante

1. Consultar oportunidades com sessão existente, períodos 1 e 12; confrontar totais com lançamentos Posted dos dois períodos.
2. Verificar que 0 e 13 retornam validação em requisição autenticada, sem escrita.
3. Abrir tela e alterar período, verificar estados vazio, erro e recuperação.
4. Edição e conflito de versão exigem meta sintética explicitamente autorizada. Nenhuma meta existente foi modificada nesta rodada.
5. Preservar a instalação .homologation. Não limpar dados nem desinstalar. O listener de reconexão pode chamar sincronização; confirmar ausência de operações pendentes antes de iniciar o teste conectado sob restrição de escrita.

Sem migration, deploy, alteração de permissões ou dados remotos nesta rodada. MVP NO-GO / Google Play NO-GO.
