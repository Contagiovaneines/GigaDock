# Central de notificações — Windows

O sino da dock abre um painel próprio com as cores do tema. Ele mostra aplicativo, horário, título e texto dos avisos retornados pela API `UserNotificationListener`, sem abrir a central nativa.

**Estado atual:** interface e provedor implementados; leitura real ainda depende de identidade assinada, registro e consentimento no Windows. O instalador de desenvolvimento gerado localmente não tem essa assinatura e mantém a ativação indisponível. Não foi validado como leitor real de notificações nesta máquina.

## Ativação

1. Use uma distribuição que inclua a identidade de notificações assinada por certificado confiável no computador. No instalador, marque **Preparar central de notificações**. A opção vem desmarcada e fica desabilitada se o pacote embutido não tiver assinatura.
2. Abra a dock após a instalação. Clique no sino e em **Ativar notificações**. Esse botão solicita o consentimento do Windows; abrir a dock ou o painel não solicita permissão implicitamente.
3. Se o acesso for negado ou revogado, o painel informa o estado e descarta o conteúdo da memória. A permissão pode ser revista nas configurações do Windows.

Registrar uma identidade não equivale a conceder acesso. O Windows valida a assinatura e a confiança do pacote antes do registro.

## Comportamento

- Atualização ao abrir e a cada dez segundos quando autorizado. O contador considera até 200 avisos recentes; acima de 99, mostra `99+`.
- Cartões são reutilizados quando o conteúdo não muda. Conteúdo em memória, sem histórico em disco, backend ou telemetria.
- O X remove o aviso pela API oficial, incluindo da central do Windows. **Limpar tudo** pede confirmação porque também limpa os avisos do Windows.
- O timer é encerrado ao revogar o acesso ou fechar o aplicativo. Recursos e assinaturas de eventos são liberados.
- Não reproduz os botões internos das notificações, respostas a mensagens, calendário, Não Perturbe ou configurações globais do Windows.
- Integração somente Windows. Linux requer outro provedor; não foi implementado nesta etapa.

## Empacotamento e assinatura

`packaging/windows/Identity.AppxManifest.xml` declara `userNotificationListener`, `runFullTrust` e `unvirtualizedResources`. O pacote com localização externa mantém o instalador existente e aponta para seus arquivos. `src/DockWindows.App/app.manifest` contém os metadados correspondentes.

O build do instalador chama `tools/build-notification-identity.ps1`, gera logos a partir do recurso do próprio projeto e incorpora `identity/GigaDock.Identity.msix` no payload. Precisa do Windows SDK com MakeAppx. A desinstalação remove somente a identidade própria do usuário atual, após restaurar a barra nativa.

Para distribuição assinada, o Publisher dos dois manifestos precisa coincidir com o Subject do certificado. O valor preparado é `CN=GigaDockOpenSource`; ajuste ambos antes de compilar se o certificado confiável tiver outro Subject. O código de validação em `NotificationIdentityRegistration.Publisher` deve ser mantido em sincronia. O script de assinatura verifica essa correspondência.

Exemplo com certificado já disponível e confiável:

```powershell
.\build_release.ps1 -Assinar -CertificadoThumbprint SEU_THUMBPRINT
```

Não inclua certificado privado, senha ou PFX no repositório. O build não cria certificados nem importa confiança. Assinar apenas o EXE, inclusive por um serviço de CI, não assina automaticamente o MSIX: é necessário assinar também esse pacote antes de incorporá-lo ao instalador.

## Verificação executada

- Solução completa Release: zero avisos/erros.
- Dez testes: consulta sem pedido implícito, ativação explícita, ausência de identidade, remoção, confirmação de limpeza, revogação, erro, reutilização de cartões e recusa de pacote sem assinatura.
- Prévia WPF isolada em claro/escuro, 96/144/192 DPI, com duas notificações **ilustrativas**. Nenhuma notificação pessoal usada nas capturas.
- MakeAppx gerou o MSIX de identidade sem assinatura. Uma consulta real sem identidade retornou lista vazia e mensagem apropriada, sem solicitar acesso.
- Não executados: registro de identidade assinada, consentimento real, leitura/remoção/limpeza das notificações pessoais, instalação sobre a dock em uso e validação em vários monitores.

Próximo passo necessário para concluir a validação funcional: assinar e registrar a identidade em uma instalação de teste, conceder acesso e verificar avisos reais, atualização e revogação. As integrações de WhatsApp na loja continuam em desenvolvimento até essa validação.

Fontes: [listener e consentimento](https://learn.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/notification-listener), [identidade com localização externa](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/grant-identity-to-nonpackaged-apps).
