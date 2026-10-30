# Production Readiness

## Status

**Nao aprovado para producao.** Este documento e um checklist de liberacao; nao
e uma declaracao de que as verificacoes foram executadas.

## Banco e multi-tenancy

- [ ] Historico EF confirmado no banco e branch alvo.
- [ ] `InvestmentPlanningCore`, quando aplicavel, validada antes da API que a exige.
- [ ] RLS e `FORCE ROW LEVEL SECURITY` confirmados em tabelas financeiras.
- [ ] Policies `USING` e `WITH CHECK` validadas com dois tenants.
- [ ] Role runtime sem `BYPASSRLS`, superuser ou grants amplos.
- [ ] Pooling valida `SET LOCAL` e limpa contexto por transacao.
- [ ] Backup/restauracao e plano de incidente testados sem apagar dados.

## API e seguranca

- [ ] JWT: issuer, audience, assinatura, expiracao e sessao revogada testados.
- [ ] CORS restrito a origens necessarias; HTTPS obrigatorio.
- [ ] Respostas e logs nao expõem tokens, senhas, SQL parametrizado ou dados financeiros.
- [ ] Rate limit e recuperacao de senha com resposta neutra validados.
- [ ] Health: `/health/live` mede processo; `/health/ready` mede banco/schema,
      sem tornar IA/e-mail opcional um falso bloqueio financeiro.
- [ ] Data Protection do Admin possui key ring persistente e protecao em repouso
      por segredo rotacionavel; nao usar disco efemero como persistencia alegada.

## Privacidade e operacao

- [ ] Politica de privacidade, termos, exclusao de conta e retencao publicados.
- [ ] Foto de perfil usa armazenamento privado, autorizacao e remocao segura.
- [ ] Brevo/remetente e Neon Auth de recuperacao verificados em ambiente alvo.
- [ ] Gemini possui chave somente no backend, consentimento, limites e mensagens
      claras quando nao ha dados suficientes.
- [ ] Observabilidade inclui versao/build, correlation ID e alertas sem segredos.

## Qualidade e distribuicao

- [ ] `dotnet build` e suites .NET aprovados no commit de release.
- [ ] `flutter analyze` e `flutter test` aprovados no commit de release.
- [ ] Testes de integracao cobrem RLS, idempotencia e sync em banco isolado.
- [ ] APK/AAB Release assinado com chave fora do Git; assinatura debug nao e distribuida.
- [ ] Plano de rollback para API, banco e aplicativo revisado.

## Decisao

Somente o responsavel de release pode marcar producao apta depois de todas as
caixas bloqueantes terem evidencia anexada e aprovacao humana registrada.
