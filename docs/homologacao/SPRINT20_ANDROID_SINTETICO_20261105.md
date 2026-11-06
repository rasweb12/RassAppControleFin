# Android integrado — 2026-11-05

Destino: variante com.rassapp.rassapp_controlefin.homologation, emulator-5554, API publicada existente. Autorização do proprietário para dados sintéticos. Nenhum dado foi excluído.

| Cenário | Resultado | Evidência |
|---|---|---|
| Sessão restaurada | APROVADO neste ciclo | Dashboard abriu sem login |
| Criação de meta sintética | APROVADO | S20_SYNTH_20261105, alvo R$100, listada após leitura remota |
| Edição sintética | APROVADO | Alvo atualizado para R$200 via interface/API publicada |
| Persistência após reinício | APROVADO | force-stop e reabertura mantiveram a meta sintética com R$200 |
| Preservação da meta anterior | APROVADO no observado | Meta teste permaneceu com alvo R$10000 e progresso zero |
| Consulta autenticada de 1 mês | APROVADO no cenário | R$567 recentes, R$5 anteriores, diferença R$562; recente coincide com Dashboard e meta mensal |
| Consulta autenticada de 3 meses | APROVADO no cenário | R$572 recentes, R$0 anteriores |
| Consulta autenticada de 12 meses | APROVADO no cenário | R$572 recentes, R$0 anteriores; seleção visível de 12 meses |
| Rejeição autenticada de 0/13 | BLOQUEADO | Interface limita a 1–12; não houve cliente externo com token seguro disponível |
| Conflito integrado de versão | NÃO EXECUTADO | Exige duas gravações com a mesma versão; não inferir aprovação dos testes locais |
| Progresso, arquivamento, reativação e rede offline | NÃO EXECUTADO nesta rodada | Não foram alterados para produzir evidência presumida |

Correção local comprovada por inspeção: o botão Editar chamava onEdit sem refresh ao retornar. Acrescentado refresh após a edição. Flutter: 35/35 aprovados; flutter analyze sem issues (3,8 segundos). A execução Android acima antecede a instalação dessa correção de refresh.

Captura de 12 meses: mobile/rassapp_controlefin/artifacts/homologacao/s20-20261105/reduction-12-months.png (artefato local).

A meta sintética permanece identificada para continuidade; a permissão de exclusão não foi utilizada. Sem migrations, alteração RLS, deploy ou merge nesta rodada. MVP NO-GO / Google Play NO-GO.

### Continuação: leitura offline comprovada

- Reteste sem reiniciar o processo: Wi-Fi e dados móveis desativados, saída e reentrada na tela de Metas e espera de 25 segundos para leitura remota falhar e usar cache.
- Resultado Android: S20_SYNTH_20261105 com R$41/R$200; total consolidado R$41. APROVADO para leitura offline nesse cenário, sem inferir aprovação de fila offline ou reinício offline.
- Captura: mobile/rassapp_controlefin/artifacts/homologacao/s20-20261105/offline-read.png.
- Rede restaurada no finally.
- Verificação posterior: wifi_on=1 e mobile_data=1.
- Adicionado teste do handler FinanceMutationHandlers: primeira edição com versão atual, segunda edição com versão antiga; deve retornar VERSION_CONFLICT, realizar um único commit e manter nome/alvo/progresso da primeira edição. Persistência em memória: não equivale a concorrência PostgreSQL/API remota.

### Fila offline e compatibilidade de eventos

- Teste local com SQLite em memória e HTTP simulado APROVADO: PUT offline preserva versão 7 na fila; reconexão com HTTP409 bloqueia operação, registra conflito e não reenvia na sincronização seguinte. Outro tenant permanece sem operações pendentes.
- Falha encontrada por inspeção: UpdateGoalCommand não constava nos eventos aceitos pelo cliente de sincronização. Adicionada a entrada para que a edição publicada não seja rejeitada como evento desconhecido.
- Quatro testes direcionados do repositório aprovados. Não equivalem à prova de reconciliação contra PostgreSQL remoto.
- Regressão completa: Flutter 37/37; flutter analyze sem issues (72,9 segundos).
- APK homologation compilado (69,7 segundos); SHA-256 B5D589F23B63FADFA4A9980ED7B16E16D4ECE0010235076847B2DB919379F3B7.

## Continuação: progresso e arquivamento

- Adição de R$40 reproduziu tela vermelha `_dependents.isEmpty`. Após reabrir, o servidor devolveu exatamente R$40 de R$200 (20%); não houve reenvio da gravação.
- O diálogo descartava seu TextEditingController imediatamente após showDialog retornar. Extraído GoalProgressDialog com controlador pertencente ao State e descarte em dispose.
- Novo teste de widget exercita campo focado, valor brasileiro 40,50 e fechamento completo sem exceção.
- Regressão Flutter: 36/36; flutter analyze sem issues (12 segundos).
- Arquivamento sintético confirmado: R$40 preservados, botão Reativar meta presente e objetivo total ativo voltou a R$10000. Reativação acionada; consulta final ainda necessária.
- APK corrigido compilado e instalado com install -r: Success. SHA-256 228847E5DEA304E23901AB794C710AD3E65AD58010479792ED7302310A763C2E.
- Captura da falha: mobile/rassapp_controlefin/artifacts/homologacao/s20-20261105/progress-state.png.

### Reteste Android do APK corrigido

- Reativação confirmada após atualização/reabertura: meta ativa, R$40/R$200; meta anterior intacta.
- Adição única de R$1 no novo diálogo: fechamento sem tela vermelha; após carregamento, R$41/R$200. Nenhuma repetição da gravação foi necessária.
- A interface mostra 21% por arredondamento de 20,5%.
- Captura final: mobile/rassapp_controlefin/artifacts/homologacao/s20-20261105/progress-fixed.png.
- Progresso, arquivamento e reativação APROVADOS nos cenários sintéticos exercitados. Conflito simultâneo de versão, rejeição autenticada de períodos inválidos e rede offline permanecem não executados nesta rodada.

### Tentativa de reinício offline

Wi-Fi e dados foram desativados para testar leitura após force-stop. O emulador perdeu conexão ADB antes da captura e do controle da interface, impedindo verificar o resultado. Cenário NÃO HOMOLOGADO por falha de instrumentação. Após reconexão, Wi-Fi e dados foram restaurados; settings global wifi_on=1 e mobile_data=1 confirmados. Nenhuma gravação sintética adicional foi enviada nessa tentativa.
