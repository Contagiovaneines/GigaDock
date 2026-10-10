# Instalar e testar o GigaDock no Linux

9 de outubro de 2026. Há uma **beta Linux x64 instalável** em `release/linux/GigaDock-3.2.0-linux-x64.tar.gz`, com SHA-256 ao lado. Windows continua com seu próprio instalador/WPF. Não há publicação online, .deb, AppImage ou Flatpak nesta entrega.

## Instalar o pacote existente

Copie o `.tar.gz` e o `.sha256` para o Linux. Use uma pasta vazia para extração. No terminal dessa pasta:

```bash
sha256sum -c GigaDock-3.2.0-linux-x64.tar.gz.sha256
tar -xzf GigaDock-3.2.0-linux-x64.tar.gz
bash GigaDock-linux-x64/install-linux.sh
"$HOME/.local/bin/gigadock"
```

O instalador usa Bash e Python 3, sem sudo. Instala em `~/.local/opt/gigadock`, cria `~/.local/bin/gigadock` e uma entrada **GigaDock** no menu. Configurações e notas seguem XDG. Início automático vem desativado; habilite em **Ajustes › Geral** somente se desejar. Para outro destino exclusivo, use `--prefix /caminho/absoluto/gigadock` na instalação e na desinstalação.

A entrada do menu usa XDG_DATA_HOME; variável relativa é ignorada. O instalador recusa atravessar links simbólicos no destino e não sobrescreve pastas/launchers de outra origem. O SHA-256 detecta alteração; não substitui uma assinatura ou a confiança na origem do pacote.

## Dependências e compatibilidade

O runtime .NET 10 está incluído; não é preciso instalar SDK para usar o pacote. Ainda são necessárias bibliotecas nativas compatíveis com .NET/Avalonia. A beta usa X11; em sessões Wayland depende de XWayland. Sem DISPLAY/XWayland ela não abre. ARM64 não é alvo desta versão.

Em Ubuntu/Debian compatível, exemplos de bibliotecas que podem faltar:

```bash
sudo apt install python3 libglib2.0-bin libx11-6 libice6 libsm6 libfontconfig1 libfreetype6
```

ICU, glibc e demais dependências devem corresponder à distribuição e à versão .NET. Consulte [dependências .NET Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install) e [Avalonia Linux](https://docs.avaloniaui.net/docs/platform-specific-guides/linux). Em Fedora ou outra distribuição, use os pacotes equivalentes, não os comandos apt acima. Estes comandos são orientação para sua máquina; não foram executados globalmente no computador do usuário.

O GIO (`gio`, normalmente libglib2.0-bin) abre aplicativos, arquivos, pastas e sites. Widgets opcionais:

- Música: `playerctl` e um player que ofereça MPRIS.
- Som: `pactl` (pulseaudio-utils no Ubuntu) e servidor PulseAudio ou PipeWire com compatibilidade PulseAudio.
- Janelas: `wmctrl`, somente em sessão X11. Em Wayland a lista/ativação ficam desativadas.
- OBS: OBS aberto com servidor WebSocket v5 ativado; conexão somente local, porta padrão 4455. Senha não é salva.
- Temperatura e ventoinhas: leituras disponíveis em `/sys/class/hwmon`, sem controlar o hardware.
- Aplicativos Flatpak: exige `flatpak`; consulta e abre apps instalados no usuário ou no sistema, sem instalar ou atualizar pacotes.
- Áreas de trabalho: `swaymsg` no Sway ou `hyprctl` no Hyprland. GNOME/KDE ainda não possuem provedor; não habilita Wayland nativo.
- Script local: executável escolhido pelo usuário, somente pelo botão Executar; limite de três segundos e contrato JSON. Confira [widgets extras Linux](WIDGETS-LINUX.md).
- Clima/GitHub/câmbio: consulta pública à internet somente ao clicar no painel, sem conta/token.

## Usar

F2 ou a engrenagem abre os ajustes. Em **Ambientes**, adicione itens pelo catálogo `.desktop` ou informe arquivo, pasta, executável absoluto ou site HTTP/HTTPS. Reordene pelos botões e crie coleções. Itens Windows existentes precisam ser removidos/reassociados a aplicativos Linux.

Em **Widgets**, ative os widgets desejados para o ambiente. Clique no widget da dock para abrir seu painel; clique fora para fechar. Notas só são gravadas ao clicar em **Salvar nota**. Pokédex usa os sprites existentes, com créditos no pacote. Aparência permite escolher os cinco temas e salvar altura/opacidade/cantos.

A dock convive com o painel nativo; não esconde a barra do sistema nem reserva área exclusiva. Escape na dock ou o menu de contexto encerra o app. Uma segunda instância com as mesmas configurações é recusada, sem ativação remota da primeira janela.

## Atualizar e desinstalar

Feche o GigaDock, extraia o novo pacote em outra pasta vazia e execute o instalador novamente com o mesmo `--prefix`, se personalizado. Ele verifica o manifesto, substitui a instalação e preserva os dados XDG. Em falha durante a substituição/menu, tenta restaurar a versão anterior. Não existe atualização automática pela internet nesta beta.

Para desinstalar a instalação padrão:

```bash
bash "$HOME/.local/opt/gigadock/uninstall-linux.sh"
```

Configurações, notas e dados permanecem. Entrada de início automático gerenciada que aponta para essa instalação é removida. Se houver arquivos extras na pasta do programa ou arquivos modificados, a remoção é recusada para evitar apagar dados desconhecidos.

## Compilar e testar no desenvolvimento

Com SDK .NET 10 compatível com global.json, Bash e Python 3, da raiz do repositório:

```bash
dotnet build src/GigaDock.App.Linux/GigaDock.App.Linux.csproj -c Release
dotnet test tests/GigaDock.Tests.Core/GigaDock.Tests.Core.csproj -c Release
dotnet test tests/GigaDock.Tests.Linux/GigaDock.Tests.Linux.csproj -c Release
python3 tests/test_linux_package.py -v
bash tools/build-linux.sh
```

Não compile a solução WPF inteira como alvo Linux. `DOTNET` pode selecionar o executável do SDK e `GIGADOCK_ARTIFACTS` uma pasta exclusiva para os objetos de build. `GIGADOCK_RUNTIME` aceita apenas linux-x64 nesta versão.

No Windows, conferir o frontend com configurações isoladas:

```powershell
dotnet run --project src/GigaDock.App.Linux/GigaDock.App.Linux.csproj -- --preview --data-root "$env:TEMP\GigaDock-Linux-Preview"
```

Essa prévia não abre aplicativos Linux nem altera as configurações WPF. Para teste gráfico automático no Linux:

```bash
./GigaDock --smoke --data-root /tmp/gigadock-teste-isolado
```

O smoke adiciona dados de exemplo nessa pasta, abre painéis/ajustes, exercita gravação de nota/aparência e gera PNGs em `state/previews`; não usa os dados padrão do usuário. Depois encerra.

## O que foi realmente validado

Build Windows, testes portáveis/nativos, execução gráfica WSLg, publicação, instalação/atualização/desinstalação em HOME/XDG temporários. Ainda faltam desktop GNOME/KDE completo, múltiplos monitores, escalas diferentes e máquina limpa. **Não há garantia de funcionar em qualquer Linux**, nem equivalência integral com o WPF. Consulte [resultado e limites](LINUX-VALIDACAO.md).
