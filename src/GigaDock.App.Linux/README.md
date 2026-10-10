# GigaDock Linux — beta x64

Frontend Avalonia 12.1.4/.NET 10 na mesma solução do frontend WPF Windows. Ambientes, aplicativos/arquivos/sites fixados, coleções, aparência, widgets locais, Pokédex e integrações opcionais. A interface segue a base visual Windows, com diferenças de fontes/ícones/controles e sem blur nativo.

Da raiz, em Linux com SDK e dependências:

```bash
dotnet run --project src/GigaDock.App.Linux/GigaDock.App.Linux.csproj
bash tools/build-linux.sh
```

O pacote completo é gerado em `release/linux/`, com instalação por usuário. [Instalação, atualização e desinstalação](../../docs/INSTALACAO-LINUX.md).

Configurações: `${XDG_CONFIG_HOME:-$HOME/.config}/gigadock/settings.json`; dados/cache/estado usam XDG_DATA_HOME/XDG_CACHE_HOME/XDG_STATE_HOME. Variáveis relativas são ignoradas. Notas ficam nos dados e não são apagadas pela desinstalação. Backend UsePlatformDetect: X11/XWayland; Wayland nativo e layer-shell não estão ativados.

F2 abre os ajustes; menu de contexto oferece ambientes/ajustes/saída. A dock não oculta painéis do sistema. Aplicativos .desktop são abertos pelo GIO, com tratamento de Exec/Terminal/ativação pelo desktop. Não há shell montado a partir de Exec. Executáveis absolutos precisam de permissão Linux; argumentos de texto importados não são executados. Ícones PNG de temas/inheritance são aceitos; SVG/XPM usam fallback vetorial.

Novas opções: duplicação/ordem/cor de ambientes, widget de tarefas por ambiente, busca de seções, guia rápido em Geral, seleção de tela e decorações opcionais em Aparência. Ctrl+PageUp/PageDown alterna ambientes com a dock em foco; roda sobre o seletor também alterna. Alt + arrastar reordena itens do ambiente ativo. Escape fecha painéis; Sair fica no menu. A prévia da aparência só é salva ao aplicar. Notas e tarefas ficam nos dados locais e são preservadas ao desinstalar. [Entrega e limitações](../../docs/LINUX-VALIDACAO.md).

Mídia usa playerctl, áudio pactl e janelas wmctrl apenas em X11; serviços ausentes aparecem como indisponíveis. Clima/câmbio/GitHub consultam provedores públicos somente por ação no painel. OBS é local, com senha em memória apenas; nenhum desses widgets conecta automaticamente na inicialização.

No Windows, prévia isolada:

```powershell
dotnet run --project src/GigaDock.App.Linux/GigaDock.App.Linux.csproj -- --preview --data-root "$env:TEMP\GigaDock-Linux-Preview"
```

A prévia não altera `%LOCALAPPDATA%/DockWindows` nem abre programas Linux.

CLI:

```bash
./GigaDock --diagnostico
./GigaDock --smoke --data-root /tmp/gigadock-smoke-isolado
```

Diagnóstico inicializa/carrega configurações e imprime capacidades. Smoke exige pasta explícita, adiciona dados de teste, valida 151 sprites, aplicação de aparência e notas pela UI, captura PNGs em `state/previews` e encerra. Fora do Linux, adicionar `--preview`.

Apenas uma instância por pasta de estado, via lock mantido aberto. Depois de falha, o SO libera o lock. Erro de inicialização tenta gravar somente o tipo da exceção no estado, sem mensagem/argumentos/credenciais.

[Validação e limitações](../../docs/LINUX-VALIDACAO.md). Pacote beta: não equivale à homologação de GNOME/KDE, todos os widgets Windows ou todas as distribuições.
