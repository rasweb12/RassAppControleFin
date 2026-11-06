# Relatório automático — Checkpoint 15

O relatório de cada execução é gerado em `artifacts/homologacao/mvp-homologation-<timestamp>.md` e omite parâmetros de conexão, tokens e senhas.

## Comando local seguro

```powershell
.\scripts\homologacao\Invoke-MvpHomologation.ps1
```

O comando padrão executa apenas build, testes locais, análise Flutter e higiene de segredos; integração PostgreSQL, builds Android e smoke HTTP exigem flags explícitas.

## Decisão

O script só retorna `GO` se nenhuma etapa obrigatória estiver reprovada ou bloqueada. Ausência de emulador, `integration_test`, assinatura release ou banco isolado impede homologação integral e produz **NO-GO**.

## Sprint 16 — escopo ainda não automatizado

A fundação persistente de monetização foi adicionada localmente, porém a homologação automática não considera compras como aprovadas: não existe integração configurada com Google Play, verificação de recibo no backend, UI de assinatura ou dispositivo/emulador validado. O estado permanece **NO-GO** até essas etapas e a homologação visual manual.
