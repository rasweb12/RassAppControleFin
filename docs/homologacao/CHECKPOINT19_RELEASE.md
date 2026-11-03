# Checkpoint 19 — Release Android e Google Play

Estado: **NO-GO**.

Pré-requisitos: comprovação de Data Protection persistente no Admin.Web após provisionamento do disco Render, licença MediatR válida para uso comercial, aprovação manual do Checkpoint 18, `applicationId` confirmado, assinatura release fora do Git, APK/AAB recém-gerados, política de privacidade, exclusão de conta, metadados e produtos Play configurados.

## Inspeção somente leitura do Render — 2026-11-03

- O único serviço provisionado, `RassAppControleFin`, está `live` no commit `7283d13`; isso comprova somente o deploy da API atual, não a homologação funcional do MVP.
- Logs recentes confirmam o processo ouvindo em `0.0.0.0:10000`, mas também informam que as chaves ASP.NET Data Protection estão em diretório não persistente do container e sem criptografia em repouso configurada.
- Os mesmos logs mantêm o aviso de licença MediatR para ambiente Production. Nenhuma licença foi inventada/configurada e nenhum secret do Render foi alterado.
- Não foram retornados pontos de métricas HTTP/CPU/memória para a janela consultada; portanto não há evidência de tráfego, latência ou smoke HTTP saudável nesta inspeção.

O Admin.Web ainda não aparece como serviço provisionado no workspace Render observado em 2026-11-03. A declaração em `render.yaml` não substitui uma implantação verificada; a criação do serviço e a configuração de suas variáveis permanecem uma operação externa pendente.

Não criar chave definitiva, não publicar AAB e não ativar preços/cobrança sem autorização específica do proprietário.

## Preparação local de 2026-11-03

- A API lê `MediatR__LicenseKey` exclusivamente da configuração de runtime quando uma licença comercial legítima estiver disponível. Nenhum valor foi adicionado ao código, ao Git ou ao Render.
- A API ainda não possui key ring persistente configurado. O Admin.Web persiste a key ring no disco `/var/data/sessions/keys`, mas no Linux essa key ring ainda não possui um mecanismo de criptografia em repouso configurado.
- Antes de release, definir uma estratégia aprovada de proteção de chaves (por exemplo, certificado/provedor de chaves gerenciado), sua rotação e o segredo externo correspondente; validar continuidade de sessão após reinício. Não configurar valor fictício para silenciar o aviso.
