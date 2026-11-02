# Recuperação de Production

As migrations 16–20 são forward-only. Não rodar `database update` para migration anterior, nem `Down`, `DROP`, `TRUNCATE` ou restauração destrutiva.

Em caso de falha: interromper o rollout, preservar histórico e logs sanitizados, validar a branch de recuperação criada antes da janela e preparar forward-fix reproduzido primeiro em branch descartável. Uma reinicialização de dados sintéticos exige inventário e autorização específica; não é um rollback automático.
