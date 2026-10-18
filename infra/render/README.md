# Render

Blueprint: ../../render.yaml. Imagens: ../docker/*.Dockerfile.
Arquivo preparado, sem deploy remoto executado.

Api__BaseUrl exige URL HTTPS real da API no Admin em Production.
AllowedHosts deve ser o hostname publico de cada servico, sem esquema.
Program.cs respeita PORT e usa binding 0.0.0.0. O runtime usa usuario nao root.

API /health/ready verifica PostgreSQL, migrations, RLS e runtime role.
Admin health verifica seu proprio host; nao comprova Neon/Auth/Brevo/Gemini.
Migrations sao jobs separados, nunca startup da API.

Antes de dados reais: completar Auth/RLS, secrets, persistencia de DataProtection
keys em Blazor, WebSockets, continuidade de circuitos, backups e rollback.

Blueprint ativo: API/Admin apenas. Web do usuario removido na atualizacao
2026-10-11. Admin usa disco persistente para tickets/chaves, uma instancia,
plano pago e deploy com interrupcao. Arquivo preparado, nao provisionado.
Gemini sera habilitado apenas apos o gate Sprint 8 e consentimentos/limites.
