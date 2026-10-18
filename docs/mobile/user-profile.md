# Perfil do usuario

O banco possui `UserProfile` com nome de exibicao e versao, criado durante o
onboarding. Ainda nao existe contrato autenticado para consulta ou edicao pelo
Flutter. Dados de identidade como e-mail e senha continuam sob controle do
Neon Auth e nao devem ser duplicados pela aplicacao.

A evolucao deve criar endpoints autenticados de leitura e atualizacao apenas
para os campos complementares permitidos, com versao otimista, tenant validado,
RLS e sincronizacao de perfil. Avatar requer armazenamento dedicado e validacao
de tipo/tamanho; nao deve receber URL ficticia nem blob em tabelas financeiras.
