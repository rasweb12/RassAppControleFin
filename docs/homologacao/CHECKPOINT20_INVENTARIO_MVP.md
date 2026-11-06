# Checkpoint 20 — Inventário funcional e homologação do MVP

Data: 2026-11-04  
Branch examinada: `codex/sprint16-monetization-admin`  
HEAD: `24277f9` mais correções locais não commitadas de sessão, cartão, bloqueio de envios concorrentes e leitura offline do perfil.

## Base da decisão

O proprietário confirmou manualmente: cadastro, login, restauração de sessão no cenário testado, Dashboard, contas, transferências, receitas, despesas, cartões, compras e faturas. Essas aprovações foram incorporadas sem repetir os fluxos.

O código foi confrontado com o Flutter, endpoints .NET, Admin.Web e testes. A matriz canônica está em `MATRIZ_FUNCIONAL.md`.

## Totais

| Classificação | Quantidade |
|---|---:|
| Capacidades encontradas no código | 26 |
| Linhas da matriz homologadas manualmente | 8 |
| Fluxos manuais confirmados pelo proprietário | 10 |
| Homologada apenas em ambiente isolado | 1 |
| Parciais | 5 |
| Não testadas funcionalmente | 11 |
| Ausentes do código, fora da contagem | 3 |

## Prioridades

- **P0:** sincronização offline e Primeiro Me Pago. A fila de receita, cartão e distribuição planejada possui cobertura local; falta validar visualmente a distribuição persistida e completar os cenários offline restantes no APK contra a API existente.
- **P1:** categorias, metas, investimentos, orçamentos, notificações, perfil/segurança, TenantAdmin, SuperAdmin/Admin.Web e monetização.
- **P2:** assistente financeiro, preferências, relatórios, exportação e funcionalidades ainda ausentes do código.

## Evidências relevantes

- Sessão: reinício por `force-stop` retornou `RASS_SESSION_RESTORE status=restored` e reabriu o Dashboard sem login. A evidência visual local está fora do Git em `artifacts`/diretório de evidências.
- Despesa: confirmação manual e inspeção de interface mostraram lançamento `Posted`, com total mensal atualizado.
- Cartões, compras e faturas: confirmação manual do proprietário; não foi repetida neste checkpoint.
- Banco/RBAC: Checkpoint 13 permanece evidência isolada (10/10 RBAC e 130/130 integração), e não substitui a homologação visual do Admin.Web.

## Regressão P0 local — 2026-11-04

Comando executado:

```powershell
flutter test test\offline_finance_store_test.dart test\pay_yourself_first_plan_test.dart --reporter compact
```

Resultado inicial: **13/13 aprovados**. A regressão completa posterior terminou com **30/30 aprovados** e `flutter analyze` sem issues. Os registros `RASS_HTTP_FAILURE` dos logs são esperados: os testes simulam indisponibilidade de rede para confirmar cache e fila local, sem envio parcial.

Cobertura comprovada localmente:

- fila offline isolada por `subject` e `tenant`;
- manutenção do mesmo `operationId` nas tentativas;
- inbox idempotente, cursor e aplicação atômica de página remota;
- lançamento de receita offline e preservação do snapshot local;
- planejamento Primeiro Me Pago offline, sem transação, transferência ou aplicação financeira;
- reconciliação atômica de cartão `local-*` para ID/versão do servidor.

### Sincronização offline — homologação Android controlada

- A causa do bloqueio visual offline foi confirmada: `FinanceRepository.profile()` não possuía fallback de cache, e sua falha de rede interrompia o carregamento completo da tela inicial.
- A correção passou a armazenar o perfil remoto por `tenant` e a usar o valor cacheado somente para falhas classificadas como indisponibilidade de rede; falhas de autenticação, autorização ou contrato continuam explícitas.
- Novo teste de regressão: perfil cacheado é carregado quando `GET /api/v1/profile` recebe falha de socket.
- No emulador, com Wi-Fi e dados móveis desligados, a sessão foi restaurada e o Dashboard permaneceu acessível após reinício da variante `.homologation`.
- Uma receita sintética de **R$ 7,00** foi criada offline. A outbox registrou `pending=1`; após restaurar conectividade e acionar **Atualizar dados**, houve envio `entityType=transaction operationType=create`, sem falha. O Dashboard passou de **R$ 3.000,00** para **R$ 3.007,00**. Isso comprova envio único do lançamento nesse ciclo.

Situação: **sincronização offline de receita — APROVADA no cenário exercitado**. Permanecem pendentes conflitos e demais tipos de operação.

### Sincronização offline de cartão — homologação Android controlada

- Um cartão sintético de R$ 321,00 foi criado com Wi-Fi e dados móveis desligados. A API registrou falha de socket esperada, o cartão apareceu na lista local e a interface informou que aguardava sincronização.
- Após encerramento completo e reabertura da mesma variante ainda offline, a leitura do cache identificou o cartão com ID `local-*` e versão 0; após a tela terminar o carregamento, ele permaneceu visível junto ao cartão previamente existente.
- Com a rede restaurada, a outbox registrou `pending=1`, enviou uma operação `card/create` e emitiu `CHECKPOINT12_SYNC_CARD_RECONCILED: versionValid=true`.
- A consulta subsequente da API retornou exatamente o cartão existente e o novo cartão com ID persistido e versão **1**. A tela final apresentou os dois cartões, sem cópia adicional.

Situação: **sincronização offline de cartão — APROVADA no cenário exercitado**. A validação preservou o ID local até a reconciliação e não executou movimentação financeira.

### Primeiro Me Pago — validação visual inicial

- A tela carregou na variante `.homologation`, com percentual inicial de **20%**, opção de evolução para **30%** e aviso explícito de que planejamento não movimenta dinheiro nem realiza aplicações.
- Com renda prevista de R$ 5.000,00, renda recebida de R$ 4.500,00 e obrigações de R$ 3.800,00, a simulação utilizou a renda recebida, sugeriu R$ 900,00 e apresentou R$ 630,00 / R$ 180,00 / R$ 90,00 para reserva e renda fixa / fundos imobiliários / renda variável. O alerta de insuficiência foi mostrado corretamente.
- A primeira tentativa de salvar falhou com HTTP 400. A causa foi comprovada: o cliente enviava `effectiveFrom` como timestamp ISO, mas o contrato .NET requer uma data civil `DateOnly` no formato `yyyy-MM-dd`.
- A correção passou a enviar e armazenar somente a data civil; o teste de regressão valida o payload e o cache. A nova instalação do APK salvou o planejamento após confirmação explícita, sem erro HTTP na leitura posterior; ao reabrir a tela, a meta de **35%** persistiu.
- A distribuição deixou de ser fixa: ela usa a carteira-alvo já existente. Cada alocação é editável, deve totalizar exatamente 100% e é enviada como um único `PUT /api/v1/investments/target-portfolio`. Falhas transitórias preservam a configuração no armazenamento local e a colocam em uma única operação de sincronização; novas edições ficam bloqueadas até a reconciliação para impedir duplicações. Nenhuma aplicação, transferência ou pagamento é criado.
- A regressão Flutter final terminou em **32/32 aprovados**; `flutter analyze` não apresentou issues. APK atualizado: SHA-256 `E47F68B6EDAF56CD801B86C7FC090F190E6D932E214B8C3D6D92333A04D4B372`.
- No emulador, a distribuição sintética padrão 70% / 20% / 10% foi salva com a confirmação **“Distribuição planejada salva.”**. Após `force-stop` da variante `.homologation`, a tela foi reaberta e mostrou os mesmos três percentuais, total de **100,00%** e controles novamente habilitados. Não houve criação de transação, transferência, compra, pagamento ou aplicação.
- Situação: **PARCIAL**. Plano, simulação e distribuição online foram homologados; a fila offline da distribuição possui teste automatizado, mas ainda falta seu cenário manual Android e o histórico mensal de execução planejada versus realizada.

### Roteiro manual P0 restante

1. Entrar na variante `.homologation` com a identidade já utilizada.
2. Desativar a conectividade do emulador, criar uma receita ou cartão sintético e confirmar o estado pendente/local.
3. Encerrar e reabrir o app sem limpar dados; confirmar que a operação ainda existe uma única vez.
4. Restaurar conectividade e usar **Sincronizar** uma única vez.
5. Confirmar ID persistido, versão positiva quando houver cartão, totais corretos e fila vazia.
6. Para Primeiro Me Pago, informar renda prevista/recebida e obrigações; alterar percentuais para totalizar 100%, salvar, fechar/reabrir a tela e confirmar sugestão e alerta de insuficiência, sem criar movimentação financeira.

## Artefato Android de homologação — 2026-11-04

- Artefato: `mobile/rassapp_controlefin/build/app/outputs/flutter-apk/app-homologation-debug.apk`.
- Configuração explícita: `API_BASE_URL=https://rassappcontrolefin-1.onrender.com`; não contém credenciais administrativas.
- SHA-256: `E47F68B6EDAF56CD801B86C7FC090F190E6D932E214B8C3D6D92333A04D4B372`.
- `flutter analyze`: aprovado, sem issues.
- `flutter test --reporter compact`: **32/32 aprovados**. As mensagens `RASS_HTTP_FAILURE` são exercitadas por mocks de cenários negativos; a suíte terminou aprovada.

## Deliberação

### Redução de despesas — implementação local posterior

- A tela existente de metas manuais foi preservada e passou a consultar uma projeção somente leitura por categoria.
- A API local compara despesas `Posted` do período recente com o período anterior equivalente; aceita janelas de **1 a 12 meses** e exclui lançamentos pendentes, transferências e qualquer mutação financeira.
- As oportunidades são ordenadas pelo aumento real de gasto. O aplicativo mostra categoria, gasto recente, período anterior e oportunidade calculada. Falhas de leitura não ocultam metas manuais nem alteram dados.
- Evidências locais: API Release sem avisos, unitários .NET **68/68**, `flutter analyze` sem issues e Flutter **32/32**.
- Situação: **implementada localmente, não homologada integrada**. A API publicada ainda não contém o endpoint; é necessário deploy autorizado antes do APK validar esse fluxo com dados sintéticos.

Nenhum serviço, migration, role, secret ou configuração de Production foi alterado nesta rodada. Nenhuma identidade nova foi criada. Nenhum dado do emulador foi apagado.

O MVP continua **NO-GO** porque sincronização offline e Primeiro Me Pago não foram homologados integralmente, além de existirem módulos de lançamento sem evidência manual. Google Play também continua **NO-GO**.
