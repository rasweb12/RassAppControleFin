# Plano de Production

Use [CHECKPOINT14_PRODUCTION_UPGRADE.md](../production/CHECKPOINT14_PRODUCTION_UPGRADE.md) como fonte operacional. A ordem obrigatória é: branch de recuperação → preflight de leitura → roles técnicas mínimas → migrations 11–17 → pós-validação → grant SET transitório → migrations 18–20 → pós-validação/smoke → deploy compatível autorizado.

Nenhum deploy Render é parte deste plano até a atualização de schema ser aprovada.
