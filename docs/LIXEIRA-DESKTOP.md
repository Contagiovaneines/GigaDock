# Lixeira na dock e na área de trabalho

Etapa de 06/10/2026: sincronizar a opção **Mostrar Lixeira** dos ajustes com o ícone do desktop.

- Ativar: mostra o atalho na dock e oculta a Lixeira na área de trabalho.
- Desativar: remove o atalho da dock e solicita mostrar a Lixeira na área de trabalho.
- Apenas mudanças explícitas da opção alteram o Windows. Abrir o aplicativo, carregar preferências e trocar de ambiente não reescrevem o registro.
- Falhas de acesso mantêm o ajuste anterior e mostram um alerta. Nenhum conteúdo da Lixeira é excluído.
- A escolha da área de trabalho pertence ao usuário do Windows e persiste depois de fechar a dock. Esta opção é global, não por ambiente.

## Implementação e limites

`ILixeiraDesktopService` permite testar o fluxo sem alterar a máquina. `LixeiraDesktopService` altera exclusivamente o DWORD da Lixeira `{645FF040-5081-101B-9F08-00AA002F954E}` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel`: 1 oculta, 0 mostra. A pasta virtual do desktop é obtida via `SHGetSpecialFolderLocation`; `SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT)` pede a atualização sem reiniciar o Explorer.

Referências: [orientação Microsoft sobre a chave da Lixeira](https://learn.microsoft.com/es-es/answers/questions/3175683/windows-10-qu-hacer-para-recuperar-la-papelera-de), [SHGetSpecialFolderLocation](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetspecialfolderlocation), [SHChangeNotify](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shchangenotify). A chave é descrita em suporte Microsoft, mas não oferece um contrato formal de API para visibilidade da Lixeira; precisa de validação manual em Windows 10/11. A notificação não confirma a renderização do Explorer. Políticas corporativas e a opção geral de ocultar todos os ícones podem impedir a exibição; o aplicativo não sobrescreve essas opções. Se o Explorer não redesenhar imediatamente, atualizar a área de trabalho com F5 é uma alternativa manual.

## Validação

Quatro testes passaram: habilitar/desabilitar via ajustes, inicialização sem alterar o desktop e preservação do estado/alerta em falha nos dois sentidos. [TRX](TestResults/lixeira-desktop.trx). Os testes usam serviço simulado; o registro real e a área de trabalho não foram alterados durante a execução. Validação visual real permanece pendente. O instalador não foi reempacotado nesta etapa.
