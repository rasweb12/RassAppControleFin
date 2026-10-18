# Sprints 1 a 5 - progresso local

Data da revisao: 18/10/2026.

## Sprint 1 - e-mail transacional

Implementado localmente:

- substituicao da configuracao da API de Resend por `EmailOptions` para Brevo;
- adaptador SMTP `BrevoSmtpEmailSender` com STARTTLS na porta 587;
- logs sem senha SMTP, destinatario ou conteudo de e-mail;
- exemplo de ambiente, Blueprint Render e documentacao operacional atualizados.

O Neon Auth continua responsavel por confirmacao e recuperacao de senha. O
SMTP dele deve ser configurado no painel do Neon Auth, e nao pelas variaveis
`Email__*` da API.

Pendente de configuracao remota: definir os secrets `Email__Username`,
`Email__Password` e `Email__From` no Render antes de habilitar um consumidor
de e-mail transacional da API. Nenhum e-mail real foi enviado nesta revisao.

## Sprint 2 - contas

O fluxo existente para criar e listar contas foi preservado. O contrato atual
de API aceita nome, moeda, saldo e arquivamento. Ele nao possui tipo de conta,
limite de cheque especial, chave Pix ou dados bancarios. Esses campos exigem
evolucao coordenada de Domain, Application, API, migration e sincronizacao;
nao foram adicionados apenas como campos visuais.

## Sprint 3 - transacoes e transferencias

Implementado no Flutter:

- listagem de todas as paginas de transacoes, em vez de somente a primeira;
- detalhes do lancamento ao tocar no item;
- fluxos existentes de receita, despesa e transferencia mantidos;
- insight financeiro no dashboard continua usando o endpoint protegido da API.

Pendente: editar/cancelar transacao no aplicativo, anexos/comprovantes e Pix.
O backend possui contratos para editar e cancelar, mas o cliente HTTP e a
outbox ainda nao possuem uma operacao PATCH/PUT idempotente para expor esses
comandos de forma segura.

## Sprint 4 - categorias

Implementado no Flutter:

- pagina de categorias pelo menu;
- criacao de categoria pelo menu e pelo botao flutuante;
- criacao inline no formulario de receita/despesa, com selecao automatica da
  categoria criada;
- aviso seguro para categoria criada offline: ela so pode ser usada em um
  lancamento depois de sincronizada e receber um identificador do servidor.

Pendente: editar e arquivar categorias no aplicativo. Os endpoints existem,
mas precisam ser integrados ao cliente HTTP e a outbox com versao de recurso.

## Sprint 5 - metas

Implementado no Flutter com os endpoints existentes:

- pagina de metas no menu;
- criacao de meta com valor, moeda BRL e data opcional;
- indicador de progresso e valor acumulado;
- adicao de progresso para metas sincronizadas;
- cache local de metas e atualizacao otimista para progresso salvo offline.

Pendente: arquivamento de meta no aplicativo e reconciliacao de dependencias
entre recursos locais ainda sem ID remoto.

## Validacao executada

- `dotnet build RassAppControleFin.slnx -c Release --no-restore`: aprovado,
  sem avisos ou erros.
- `dotnet test RassAppControleFin.slnx -c Release --no-build --filter
  "Category!=LiveAuth"`: 27 testes unitarios e 5 testes de arquitetura
  aprovados. Os testes de integracao nao produziram casos correspondentes ao
  filtro neste ambiente.
- `flutter analyze`: aprovado, sem problemas.
- `flutter test`: 19 testes aprovados.

Nenhuma migration, deploy, push, alteracao do Render ou alteracao do Neon foi
executada.
