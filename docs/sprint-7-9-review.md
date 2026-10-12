# Sprints 7 a 9 - Administracao, IA e operacao

## Sprint 7 - Administracao

- O Admin Blazor continua separado do aplicativo Mobile e possui autenticacao de servidor, health checks e cliente HTTP resiliente para a API.
- A pagina administrativa de status apresenta disponibilidade, produto, versao e etapa sem expor informacao financeira ou segredos.
- A API preserva permissoes centralizadas para dados financeiros e auditoria; o painel administrativo nao recebe chaves de infraestrutura nem credenciais Gemini.

## Sprint 8 - IA financeira

- O unico provedor concreto e GeminiAiProvider, por HTTPS, no backend.
- GEMINI_API_KEY e lida diretamente da variavel de ambiente no momento da chamada. Ela nao integra options, contratos, Mobile, logs ou arquivos de configuracao.
- O modelo e configuravel por Gemini__Model; a validacao aceita somente identificadores gemini-*.
- O contexto e calculado pelo backend a partir do dashboard autorizado no tenant/usuario atual. Sao enviados apenas agregados por moeda e periodo.
- A resposta e limitada, validada e retorna falha padronizada quando Gemini estiver desabilitado, sem chave, indisponivel ou responder de forma invalida.
- O endpoint protegido POST /api/v1/ai/ask exige leitura financeira e possui rate limit por usuario autenticado.
- Prompts com padroes de injection e mensagens acima do limite sao rejeitados antes do provedor.

## Sprint 9 - Observabilidade e resiliencia

- O cliente Gemini usa timeout total de 35 segundos, uma tentativa adicional limitada e circuit breaker do pipeline padrao.
- A telemetria inclui ActivitySource e metricas de requisicoes/tokens do Gemini; prompts, chaves e agregados financeiros nao sao registrados.
- A API continua com JSON logs, correlation id, OpenTelemetry HTTP, health endpoints e instrumentacao MediatR/database ja existentes.

## Validacao

- Arquitetura: 5 testes aprovados.
- Integracao Neon isolada: 104 testes aprovados.
- Unitarios: 23 testes aprovados, incluindo injection e indisponibilidade do provedor.
- Compilacao Release: aprovada sem avisos ou erros.
