# Checklist Google Play

- [ ] Definir e registrar applicationId final (o Gradle ainda contém o TODO de template).
- [ ] Definir versão e build number de release.
- [ ] Criar/configurar assinatura fora do repositório; confirmar que `key.properties` e chaves não são versionados.
- [ ] Gerar e testar `.aab` assinado.
- [ ] Confirmar URL/API Production, TLS e login real.
- [ ] Publicar política de privacidade e fluxo de exclusão de conta/dados.
- [ ] Preencher Data safety, contato, classificação etária, descrição e capturas.
- [ ] Testar login, recuperação, offline, sincronização e encerramento de conta em dispositivo/emulador.

Estado: **BLOQUEADO** até a conclusão do upgrade Production e dos itens acima. Não publicar automaticamente.
