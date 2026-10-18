# Guia beta: Neon, Brevo e Render

## 1. Preflight local

1. Confirme que o build, testes e scans estao verdes.
2. Use uma branch Neon de desenvolvimento ou homologacao; nunca aplique migrations pela API.
3. Configure secrets fora do Git: User Secrets no desenvolvimento e secrets do provedor no deploy.
4. Mantenha Gemini desabilitado ate definir consentimento, limite de custo e chave no backend.

## 2. Neon

1. No Neon, selecione a branch de homologacao e obtenha duas conexoes TLS: direta para migrations e pooled para a API.
2. No secret store local, configure RASS_MIGRATIONS_CONNECTION com a conexao direta. Configure Database__ConnectionString com a conexao pooled e Database__RuntimeRole=rass_app_runtime.
3. Aplique, nesta ordem, somente pelo utilitario:

   dotnet run --project tools/RassAppControleFin.Database -c Release -- migrate
   dotnet run --project tools/RassAppControleFin.Database -c Release -- bootstrap

4. Execute os testes de integracao com o banco dedicado de testes, nunca contra homologacao ou producao.
5. No Neon Auth, configure issuer, audience, JWKS e origin. Copie os valores apenas para os secrets da API.
6. Valide /health/ready usando a API publicada. A resposta deve confirmar banco, RLS e runtime role restrito.

## 3. Brevo

1. Verifique o remetente ou dominio no painel Brevo antes de habilitar emails.
2. Configure `Email__SmtpHost=smtp-relay.brevo.com`, `Email__SmtpPort=587` e `Email__UseSsl=true` no backend.
3. Armazene `Email__Username`, `Email__Password` e `Email__From` somente como secrets do Render.
4. Configure separadamente o SMTP do Neon Auth para confirmacao e recuperacao de senha.
5. Nunca coloque senha SMTP no Mobile, Admin, APK, repositorio ou arquivos dotenv versionados.

## 4. Render

1. Publique o repositorio em GitHub, GitLab ou Bitbucket e valide render.yaml.
2. Crie o Blueprint no Render; ele provisiona API e Admin como servicos separados.
3. No servico API, informe os secrets marcados sync:false: banco pooled, configuracao Neon Auth, `Email__Username`, `Email__Password`, `Email__From`, `GEMINI_API_KEY`, `Gemini__Model` e OTEL quando houver coletor.
4. Mantenha Gemini__Enabled=false no primeiro deploy. Habilite-o somente apos um teste autenticado em homologacao e definicao de limite de custo.
5. No Admin, configure Api__BaseUrl com a URL HTTPS publica da API e AllowedHosts com o dominio do Admin.
6. Aplique migrations antes do deploy da API usando uma conexao direta Neon de operador. A API nunca deve possuir RASS_MIGRATIONS_CONNECTION.
7. Verifique /health/live e /health/ready em ambos os servicos e teste login, tenant isolation e uma transferencia no ambiente de homologacao.
8. Ative auto deploy somente depois dos gates beta e das revisoes de segredo/monitoramento.

## 5. Android beta

APK Debug Flutter:

mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-debug.apk

Para distribuir beta, gere um APK/AAB Flutter Release assinado por chave de distribuicao protegida fora do repositorio. Google Play Billing e assinatura de producao permanecem uma etapa posterior.
