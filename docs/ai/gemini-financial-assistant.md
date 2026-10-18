# Assistente financeiro Gemini

O aplicativo Flutter acessa o assistente exclusivamente por `POST /api/v1/ai/ask`.
A chave `GEMINI_API_KEY` permanece somente no backend. O cliente nao consulta o
banco e nao chama a API Google diretamente.

Antes da primeira pergunta, o usuario aceita o uso de um resumo financeiro
minimo e autorizado. Esse consentimento local e armazenado no secure storage,
isolado pela combinacao de usuario e tenant, e pode ser revogado na tela. O
historico do chat e mantido somente enquanto a tela estiver aberta.

Quando nao houver conexao ou o Gemini estiver indisponivel, o aplicativo mostra
uma mensagem segura e mantem os indicadores financeiros locais disponiveis.
O assistente nao pode executar comandos financeiros, transferencias ou alterar
recursos.

Pendente para uma proxima migration: consentimento auditavel no servidor e
retencao configuravel de conversas, ambos com LGPD, RLS e politica de exclusao.
