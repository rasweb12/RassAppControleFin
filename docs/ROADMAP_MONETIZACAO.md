# Roadmap de Monetizacao e Planejamento Futuro

## Principios

Nenhuma cobranca real, integracao com loja ou limitacao artificial de dados e
introduzida pela RC1. Recursos financeiros existentes permanecem disponiveis
conforme a politica vigente.

## Fase 1 - Fundacao de planos

- Catalogo de planos Gratuito/Pro no backend, com feature flags por tenant.
- Limites avaliados no servidor, auditaveis e refletidos no Flutter sem confiar
  exclusivamente no cliente.
- Admin para usuarios, tenants, planos, permissoes e auditoria.
- Nenhum provedor de pagamento nesta fase.

## Fase 2 - Assinaturas e lojas

- Integracao Google Play Billing somente apos desenho de entitlement,
  verificacao server-side de compra, restauracao e cancelamento.
- Estado de assinatura separado de autorizacao financeira e sujeito a auditoria.
- Politicas de suporte, reembolso, privacidade e tratamento de falhas de loja.

## Fase 3 - Planejamento de investimentos

- Simuladores de renda fixa e variavel com premissas visiveis, sem promessa de
  retorno e sem compra automatica.
- `Primeiro me Pago` com 10%, 15%, 20%, 25% e 30%, confirmacao explicita,
  historico de vigencia e aportes manuais.
- Carteira-alvo, reserva, metas, projecoes e previsao de caixa integradas ao
  modulo financeiro, sincronizacao offline e RLS.

## Guardrails

- Nunca realizar PIX, compra de ativos ou movimentacao bancaria automaticamente.
- IA usa somente dados autorizados do tenant e informa insuficiencia de dados.
- Toda regra de plano e assinatura deve possuir testes de autorizacao, isolamento
  de tenant, idempotencia e recuperacao de eventos.
