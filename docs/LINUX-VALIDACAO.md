# Linux beta — validação e limitações

A beta Linux x64 usa Avalonia e compartilha modelos e serviços com o Windows. O pacote `.tar.gz` inclui runtime .NET e instalação por usuário; ainda não representa compatibilidade universal nem paridade integral com WPF.

## Executado em 10 de outubro de 2026

Laboratório Ubuntu 26.04.1 x64, WSL2/WSLg, um monitor e escala 100%.

- 76 testes Linux e 44 compartilhados aprovados, sem skips.
- Sete testes Python do empacotamento aprovados no Linux.
- Interface aberta: dock, ambientes, aparência, notas, tarefas, busca, duplicação, 151 sprites e quatro painéis de widgets extras.
- Script local salvo e reaberto; salvar não o executou. Execução pelo botão e resultado na dock verificados.
- Pacote self-contained instalado, atualizado e desinstalado em HOME/XDG isolados com espaços; checksum e preservação de configurações verificados.

No Windows, os testes portáveis da suíte Linux tiveram 67 aprovações e nove skips por exigirem Linux. Isso não valida APIs Linux no Windows.

## Matriz de suporte

| Recurso | Estado | Limite |
| --- | --- | --- |
| Interface gráfica | Executada em WSLg | X11/XWayland; Wayland nativo/layer-shell ausentes |
| Ambientes e widgets locais | Testes e smoke aprovados | Outras escalas/monitores pendentes |
| Sensores de temperatura/RPM | Leitura sysfs e testes com arquivos simulados | Laboratório sem leituras disponíveis; hardware físico pendente |
| Flatpak | Parsing, escopo, argumentos e falhas testados com respostas simuladas | Aplicativos reais não instalados no laboratório |
| Workspaces | Protocolos Sway/Hyprland e comandos testados com respostas simuladas | Sessões reais pendentes; GNOME/KDE não integrados |
| Script local | Executado no Linux e pela interface | Manual, permissões do usuário, sem sandbox; três segundos/4096 caracteres |
| Mídia e áudio | Integrações opcionais implementadas | Exigem `playerctl` e `pactl`; sem mixer equivalente ao Windows |
| Janelas | Lista/ativação opcionais em X11 | Exige `wmctrl`; desativado em Wayland |
| Distribuição | `.tar.gz` x64 por usuário | Sem atualização online, ARM64, AppImage, Flatpak ou DEB próprios |

Faltam homologação em desktops GNOME/KDE completos, sessões Sway/Hyprland, sensores físicos, diferentes distribuições, escalas e múltiplos monitores. Não foram movidas janelas nem ocultados painéis de outros programas.

[Instalação e dependências](INSTALACAO-LINUX.md) · [Widgets extras](WIDGETS-LINUX.md) · [Status](STATUS.md).

Resultados TRX, capturas e relatórios antigos estão em `docs/local/` e `docs/TestResults/` na cópia de desenvolvimento. Eles não acompanham o repositório público; novas execuções podem ser reproduzidas pelos comandos do README e pelos workflows.
