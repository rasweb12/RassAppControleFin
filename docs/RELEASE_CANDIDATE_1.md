# Release Candidate 1 (RC1)

Data de preparo: 2026-10-30

## Objetivo

Preparar o RassFin para homologacao controlada e Beta fechado sem declarar
producao pronta e sem publicar, aplicar migrations remotas ou alterar servicos
externos por esta etapa documental.

## Estado da RC1

**Status: bloqueada para Beta.** A homologacao funcional do usuario cobre
autenticacao, Dashboard, contas, categorias, metas, reducao de despesas,
assistente Gemini, notificacoes, sincronizacao, identidade visual e perfil.
Essa evidencia nao substitui a validacao tecnica abaixo.

| Area | Estado | Evidencia | Gate pendente |
|---|---|---|---|
| Autenticacao e Dashboard | Homologado pelo usuario | Registro de homologacao | Teste de regressao autenticado em build candidato |
| Contas, categorias, metas e notificacoes | Homologado pelo usuario | Registro de homologacao | Regressao offline e sincronizacao |
| Transacoes | Bloqueante | `GET /api/v1/transactions` registrou `InvalidOperationException` no Render | Confirmar causa, listar dados reais e testar criacao, edicao, cancelamento e reinicio |
| Perfil e foto | Parcial | `UserProfile` existe; avatar seguro nao esta implementado | Endpoint, armazenamento privado, autorizacao e sincronizacao |
| Investimentos | Em desenvolvimento local | `InvestmentPlanningCore` e testes unitarios locais | Validacao de migration/RLS, UI e testes de integracao |
| Android | Artefato de homologacao existente | `app-debug.apk` gerado em 2026-10-30 | Instalacao, teste manual e Release assinado |

## Criticos antes do Beta

1. Corrigir e homologar a listagem de transacoes com dados reais do tenant.
2. Confirmar no ambiente aprovado o historico da migration, RLS, `FORCE RLS`,
   policies e grants minimos, sem desabilitar isolamento.
3. Executar build e testes .NET, `flutter analyze` e `flutter test` no commit
   candidato e registrar os resultados reais.
4. Instalar o APK candidato em dispositivo/emulador e executar o plano de Beta.
5. Revisar segredos, HTTPS, CORS, logs, recovery de sync e Data Protection do
   Admin antes de qualquer distribuicao publica.

## Perfil e foto

O nome exibido deve vir do `UserProfile` autenticado, e nao de dados fixos.
Uma foto requer armazenamento privado, validacao de tamanho e MIME, URL ou
identificador com controle de acesso, remocao e isolamento por usuario/tenant.
Nao e permitido armazenar blobs de avatar em tabelas financeiras nem expor uma
URL publica previsivel. Ate essa implementacao, usar somente iniciais derivadas
do nome real.

## Build e diagnostico

A API e o aplicativo devem expor versao semantica, identificador de build e
commit curto em diagnostico autenticado ou cabecalho seguro, sem revelar
segredos. O APK Debug existente nao constitui uma release assinada.

## Criterio de saida

A RC1 somente pode ser marcada como apta ao Beta quando todos os itens criticos
forem comprovados e o responsavel registrar a aprovacao do build candidato.
