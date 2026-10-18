# Brevo SMTP

O backend usa o adaptador `BrevoSmtpEmailSender` para e-mails transacionais
disparados pela aplicacao. A autenticacao e a recuperacao de senha continuam
sob responsabilidade do Neon Auth; os parametros SMTP configurados no Neon
Auth devem permanecer separados das variaveis da API.

## Variaveis da API no Render

Configure somente no servico da API:

| Variavel | Valor |
| --- | --- |
| `Email__SmtpHost` | `smtp-relay.brevo.com` |
| `Email__SmtpPort` | `587` |
| `Email__UseSsl` | `true` |
| `Email__Username` | usuario SMTP informado pelo Brevo |
| `Email__Password` | chave SMTP do Brevo, como secret |
| `Email__From` | remetente previamente verificado no Brevo |

O adaptador utiliza STARTTLS na porta 587. `Email__Username`,
`Email__Password` e `Email__From` nao podem ser incluídos em Git, APK ou
configuracoes publicas.

## Neon Auth

Para e-mails de identidade (confirmacao e redefinicao de senha), configure o
SMTP diretamente no painel do Neon Auth com os nomes exigidos pelo provedor:
`SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURITY=STARTTLS`, `SMTP_USERNAME`,
`SMTP_APIKEY` e `SMTP_FROMADDRESS`.

Nao copie a chave SMTP para o aplicativo Flutter. Depois de alterar o SMTP do
Neon Auth, execute uma solicitacao de recuperacao de senha em uma conta de
teste e confirme o recebimento do e-mail e a validade do link.

## Validacao e incidentes

1. Confirme que o remetente e o dominio estao verificados no Brevo.
2. Defina as seis variaveis `Email__*` como secrets no Render e redeploye a
   API quando houver um consumidor de e-mail transacional ativo.
3. Envie apenas um e-mail de teste para uma conta de teste autorizada.
4. Revogue e substitua a chave SMTP se ela for exibida, registrada ou enviada
   por engano.

O backend registra somente `OperationId`, o template e a categoria da falha.
Ele nunca deve registrar senha SMTP, endereco do destinatario ou conteudo do
e-mail.
