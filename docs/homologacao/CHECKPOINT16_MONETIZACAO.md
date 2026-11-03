# Checkpoint 16 — Monetização e Administração

## Evidência de auditoria

Em 2026-11-03, a base possuía constantes de permissão para planos, assinaturas e entitlements, mas não possuía entidades, tabelas, endpoints, catálogo administrativo nem integração de Google Play Billing. Portanto, a monetização anterior era **ausente**, não uma funcionalidade parcialmente validada.

O Admin.Web possuía somente painel global de SuperAdmin. A API continua sendo a fronteira de autorização; o cliente não é autoridade para permissões.

## Fundação implementada localmente

- `PlanDefinition`: catálogo global, preço de referência, oferta habilitada e identificador externo opcional.
- `PlanEntitlement`: matriz configurável de funcionalidade/limite por plano.
- `UserSubscription`: estado do acesso e validade, sem validade artificial para vitalício.
- `PaymentEvent`: idempotência/auditoria por evento do provedor, com fingerprint SHA-256 em vez de token de compra bruto.
- Migration `20261103112705_MonetizationFoundation`: RLS/FORCE RLS e ACL mínima. Runtime somente lê catálogo e a própria assinatura; não lê nem escreve eventos de pagamento; `PUBLIC` não recebe grants.
- `GET /api/v1/monetization/entitlements`: leitura autenticada dos direitos efetivos do próprio usuário, calculados pelo backend sob RLS. Não cria assinatura, não recebe token de compra e não concede Premium.

Preços de referência cadastrados na migration: Free R$ 0, Premium Mensal R$ 14,90, Premium Anual R$ 119,90 e Premium Vitalício R$ 299,90. As três ofertas pagas iniciam **desabilitadas** e sem `StoreProductId`; o banco não cria produto, preço ou cobrança na Google Play.

## Matriz inicial proposta — não comercialmente aprovada

| Recurso | Free | Premium |
|---|---:|---:|
| Contas | 2 | Sem limite configurado |
| Cartões | 1 | Sem limite configurado |
| Planejamento, investimentos, Primeiro Me Pago | Básico | Completo a definir por regra de backend |
| Reserva de emergência | 1 meta | Sem limite configurado |
| Projeções e relatórios avançados | Não | Sim |

Os entitlements são um catálogo; a aplicação ainda não os aplica a cada fluxo. Enquanto a enforcement não estiver implementada e testada no backend e Flutter, nenhum plano pode ser anunciado como comercialmente funcional.

## Admin.Web

A rota global `/platform` agora redireciona qualquer identidade que não seja SuperAdmin antes de renderizar controles globais. O backend mantém as políticas `Platform.*`. Não há ainda telas de TenantAdmin para administração restrita ao tenant; esse requisito continua **PENDENTE**.

## Operações bloqueadas externamente

- Google Play Billing requer produtos configurados, credenciais de verificação server-side e ambiente de teste autorizado.
- A concessão de SuperAdmin para `okisegu@gmail.com` exige localizar e confirmar a identidade verificada no Neon Auth e uma autorização específica antes de qualquer escrita em Production.
- A migration 21 é local e não foi aplicada a Neon ou Production.

## Estado

Sprint 16: **PARCIAL / NO-GO**. A fundação está pronta para revisão; não há cobrança real, oferta Premium ativa ou acesso administrativo concedido.
