# Migracao Mobile para Flutter

## Decisao

O cliente final e desenvolvido exclusivamente em `mobile/rassapp_controlefin` com Flutter/Dart.
O projeto .NET MAUI foi removido depois da decisao arquitetural de substituicao; nenhuma nova
automacao, documentacao ou pipeline deve referencia-lo.

## Inventario Migrado do MAUI

- Sessao: login, cadastro, renovacao, logout e selecao de tenant.
- Financeiro: dashboard, contas e base para sincronizacao SQLite/outbox/inbox.
- Navegacao: Shell e abas para dashboard, contas e perfil.
- Contratos utilizados: `/api/v1/auth/*`, `/api/v1/tenants`, `/api/v1/dashboard`, contas,
  categorias, cartoes, transacoes, transferencias, orcamentos e metas.

## Referencia Visual

O Flutter anterior (`RassAppFinanceiro/RassApp.MobileV2`) foi lido somente como referencia.
A primeira versao preserva a paleta azul-marinho `#001B3D`, azul `#0056D2`, cartoes de
resumo e navegacao inferior. Nenhum arquivo, configuracao ou contrato do projeto anterior
foi reutilizado diretamente.

## Primeira Entrega Flutter

- Splash com verificacao de sessao.
- Login e cadastro protegidos por validacao local.
- Sessao armazenada somente com `flutter_secure_storage`.
- Dashboard e navegacao exibidos apenas depois de uma sessao valida.
- Cliente HTTP para os envelopes atuais da API ASP.NET Core.
- Permissao Android de internet no manifest principal.

## Diagnostico de Identidade

`Dados invalidos` corresponde ao contrato atual: nome obrigatorio, e-mail valido e senha
com 8 a 128 caracteres. `Autenticacao indisponivel` representa falha de configuracao ou
conectividade com Neon Auth/JWKS/origin, nao uma senha incorreta. O cliente mostra esses
casos separadamente.

O cadastro real nao foi executado nesta migracao: ele exige uma configuracao confirmada de
Neon Auth e um usuario de teste autorizado. Recuperacao de senha ainda nao tem contrato
na API e fica bloqueada explicitamente no cliente.

## Execucao Windows

```powershell
cd mobile/rassapp_controlefin
C:\develop\flutter\bin\flutter.bat pub get
C:\develop\flutter\bin\flutter.bat run -d emulator-5554
```

## APK Android

```powershell
cd mobile/rassapp_controlefin
C:\develop\flutter\bin\flutter.bat build apk --debug
```

O Android SDK local ainda pede aceitacao de licencas para uma cadeia de build integral:

```powershell
C:\develop\flutter\bin\flutter.bat doctor --android-licenses
```

## iOS

O build iOS requer macOS, Xcode e uma conta/provisionamento Apple. No Mac, executar
`flutter pub get`, revisar signing em `ios/Runner.xcworkspace` e usar `flutter build ipa`.

## Proximas Etapas

1. Validar Neon Auth em ponta a ponta com uma conta de teste autorizada.
2. Migrar dashboard, contas e transacoes para os contratos existentes.
3. Implementar Drift/SQLite, outbox, inbox e isolamento por usuario.
4. Cobrir contratos HTTP e fluxos com testes de widget e integracao.
5. Migrar os fluxos restantes e manter a cobertura de testes no cliente Flutter.
