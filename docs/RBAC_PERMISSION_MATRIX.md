# Matriz de permissões RBAC

| Recurso | User | TenantAdmin | FinancialAdmin | SupportAgent | SuperAdmin |
| --- | --- | --- | --- | --- | --- |
| Dados financeiros próprios | Sim | Sim | Conforme policy existente | Conforme policy existente | Apenas próprios |
| Membros do próprio tenant | Não | Conforme `Memberships.Manage` | Conforme policy existente | Não | Não automaticamente |
| Endpoints `Platform.*` | Não | Não | Não | Não | Sim |
| Ler finanças de outro tenant | Não | Não | Não | Não | Não |
| Conceder/revogar SuperAdmin | Não | Não | Não | Não | Sim, com confirmação e auditoria |

`SuperAdmin` não é um `MembershipRole` e não pode ser concedido por request do Flutter, por header ou por claim não validada.
