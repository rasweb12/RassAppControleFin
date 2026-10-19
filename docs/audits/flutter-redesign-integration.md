# Integração do Redesign Flutter

## Referências analisadas

- Aplicativo funcional: `mobile/rassapp_controlefin`.
- Redesign visual: `D:\Curso\APP\stitch_rassapp_flutter_finance_redesign`.
- Legado: não alterado.

## Matriz de telas

| Tela | Redesign | Aplicativo atual | Decisão |
|---|---|---|---|
| Dashboard | Cartão de patrimônio, ações rápidas, indicadores e insight | Dashboard com métricas reais e insight Gemini pelo backend | Tema e dashboard integrados sem dados demonstrativos |
| Transações | Cabeçalho mensal, filtros e cartões | Lista paginada, detalhes e criação de receita/despesa | Preservar consulta real; filtros e edição ainda exigem expansão de UX e contrato |
| Contas | Resumo, cartões e modal de nova conta | Listagem e criação com saldo inicial | Preservar criação real; tipo de conta, cheque especial e detalhes dependem do modelo de domínio |
| Categorias e IA | Chat e categorias em uma composição visual | Páginas funcionais separadas | Manter chat Gemini, consentimento e categorias reais; aproximar visual em etapa posterior |
| Metas | Resumo e formulário em bottom sheet | Criação e progresso de metas | Funcionalidade preservada; visual pode ser refinado sem mudar contrato |
| Menu | Perfil, atalhos e estado de sincronização | Menu com tenant, ações, IA, notificações e sincronização | Manter navegação funcional; nome do perfil requer consumo de `/api/v1/profile` no Flutter |
| Notificações | Atalho e badge | Página funcional de notificações | Preservar; badge de não lidas ainda pendente |

## Correções locais desta etapa

- Atualizado o tema Flutter para a paleta navy/sapphire, cartões, inputs, navegação e ações do redesign.
- Atualizado o dashboard com patrimônio, atalhos reais para receita, despesa, transferência e metas, métricas reais e cartão de insight Gemini.
- Corrigida a preservação de transações pendentes no cache local: uma resposta remota vazia não pode apagar um lançamento aguardando sincronização.
- Adicionado teste de regressão para a preservação de lançamento pendente.

## Limites confirmados

- O contrato de conta atual possui apenas nome, moeda e saldo inicial. Não existe campo de tipo de conta ou cheque especial no domínio, API, migration e sincronização. Não foi criado campo visual sem persistência real.
- O cliente não expõe ainda comandos de edição/cancelamento de transações ou de manutenção de contas e categorias, embora parte dos endpoints de manutenção exista no backend. Esses fluxos precisam de casos de uso Flutter, tratamento de versão e testes.
- O Flutter não consome ainda `/api/v1/profile`; portanto o redesign não deve exibir um nome inventado. O nome real requer essa integração.
- O redesign é HTML com valores estáticos. Nenhum dado de demonstração foi importado para o aplicativo.

## Próxima implementação recomendada

1. Consumir perfil real e exibir nome/abreviação do usuário.
2. Implementar detalhes, renomeação e arquivamento de contas e categorias com controle de versão.
3. Implementar edição e cancelamento de transações com confirmação e idempotência.
4. Evoluir o modelo de conta, migration, RLS, sincronização e UI para suportar cheque especial antes de mostrar esse campo.
5. Adicionar filtros funcionais de período, conta, categoria, fluxo e status à tela de transações.
6. Gerar APK Debug e validar visualmente no emulador após a análise Flutter concluir.
