# Runbook: primeiro SuperAdmin

## Estado atual

O bootstrap real não foi executado. Nenhuma identidade real foi promovida.

## Pré-requisitos obrigatórios

1. Ambiente isolado ou Production explicitamente autorizado.
2. Identity imutável (`UserId`) recuperada de Neon Auth e status de e-mail verificado.
3. Credencial administrativa separada da role runtime.
4. Backup e revisão da migration já aplicada.
5. Ausência confirmada de SuperAdmin ativo.

## Procedimento futuro

1. Resolver e validar a identidade pelo identificador imutável, não apenas por e-mail.
2. Executar uma operação administrativa única, transacional e auditada usando a conexão administrativa aprovada.
3. Inserir uma concessão ativa com motivo operacional, preservando RLS/FORCE RLS.
4. Validar, em uma nova sessão, o acesso a `Platform.Dashboard.Read` e a ausência de acesso financeiro multi-tenant.
5. Registrar correlation ID, administrador responsável e evidência de auditoria.

Não usar variável de ambiente permanente, endpoint anônimo ou a role runtime como bootstrap. Em caso de perda do último SuperAdmin, seguir um procedimento administrativo offline aprovado, com auditoria e dupla revisão.
