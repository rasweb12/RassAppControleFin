# Release Android — RassFin

Estado: **PREPARAÇÃO APENAS — NÃO PUBLICAR**

## Configuração auditada

- Application ID de produção: `com.rassapp.rassapp_controlefin`.
- Flavor isolado: sufixo `.homologation`.
- Versão atual declarada: `1.0.0+1`.
- Ícone configurado por `flutter_launcher_icons` em `assets/branding/rass_app_icon.png`.
- A URL padrão da API é a Production Render; homologação deve sobrescrever `API_BASE_URL` explicitamente.
- Assinatura release só é aceita por `key.properties` local ignorado ou variáveis `RASS_ANDROID_*`; não há fallback para assinatura debug.

## Pré-requisitos para release

1. Resolver os bloqueadores P0 de banco e homologação funcional.
2. Confirmar keystore existente fora do Git e as quatro variáveis de assinatura em sessão segura.
3. Atualizar `version` com número de build inédito.
4. Confirmar API Production saudável após atualização autorizada e smoke tests.
5. Revisar permissões Android, política de privacidade, exclusão de conta/dados, recuperação de senha e suporte.
6. Revisar logs para não persistirem token, senha, payload financeiro ou cabeçalho de autorização.

## Geração local após aprovação

```powershell
flutter build appbundle --release --build-name <versao> --build-number <numero>
flutter build apk --release --build-name <versao> --build-number <numero>
```

Validar a assinatura com ferramenta Android local, instalar primeiro em ambiente de homologação e testar atualização sobre a versão anterior sem limpar Drift, Secure Storage ou outbox.

## Artefato desta rodada

Foi gerado somente o APK Debug local. Ele é para homologação e não é elegível para Google Play.

## Gate de publicação

**REPROVADO.** Não gerar nem distribuir AAB/APK release enquanto Production estiver com schema incompatível ou os fluxos financeiros P0 não estiverem homologados no APK atual.
