# Checkpoint 19 — Release Android e Google Play

Estado: **NO-GO**.

Pré-requisitos: correção de Data Protection, licença MediatR válida para uso comercial, aprovação manual do Checkpoint 18, `applicationId` confirmado, assinatura release fora do Git, APK/AAB recém-gerados, política de privacidade, exclusão de conta, metadados e produtos Play configurados.

Não criar chave definitiva, não publicar AAB e não ativar preços/cobrança sem autorização específica do proprietário.

## Preparação local de 2026-11-03

- A API lê `MediatR__LicenseKey` exclusivamente da configuração de runtime quando uma licença comercial legítima estiver disponível. Nenhum valor foi adicionado ao código, ao Git ou ao Render.
- A API ainda não possui key ring persistente configurado. O Admin.Web persiste a key ring no disco `/var/data/sessions/keys`, mas no Linux essa key ring ainda não possui um mecanismo de criptografia em repouso configurado.
- Antes de release, definir uma estratégia aprovada de proteção de chaves (por exemplo, certificado/provedor de chaves gerenciado), sua rotação e o segredo externo correspondente; validar continuidade de sessão após reinício. Não configurar valor fictício para silenciar o aviso.
