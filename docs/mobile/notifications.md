# Notificacoes

O aplicativo atualmente exibe conflitos e pendencias de sincronizacao no fluxo
financeiro. Ainda nao existe uma entidade de notificacao, central persistida ou
canal local Android/iOS.

A central deve ser introduzida com um modelo versionado por tenant e usuario,
chave de deduplicacao, leitura, expiracao, referencia de recurso e canal. Os
alertas financeiros devem ser produzidos por regras deterministicas no backend.
E-mail via Brevo deve respeitar preferencia e frequencia; push remoto nao sera
incluido sem uma decisao explicita de provedor.
