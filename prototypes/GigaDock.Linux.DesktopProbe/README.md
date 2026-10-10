# Protótipo de desktop Linux

Aplicativo experimental Avalonia 12.1.4/.NET 10, independente do frontend WPF. Não é a versão Linux completa nem uma reprodução final do visual. Não carrega configurações, lança programas, altera painéis ou configura autostart.

Na raiz do repositório, em Windows ou Linux com SDK 10:

```text
dotnet run --project prototypes/GigaDock.Linux.DesktopProbe/GigaDock.Linux.DesktopProbe.csproj
```

Opções:

- `--diagnostico`: informações básicas sem inicializar a interface.
- `--smoke`: cria a janela, imprime monitores/escala e encerra após o primeiro trabalho agendado no dispatcher. Não equivale a teste visual ou de interação.

Exemplo:

```text
dotnet run --project prototypes/GigaDock.Linux.DesktopProbe/GigaDock.Linux.DesktopProbe.csproj -- --smoke
```

Publicação Linux, a partir da raiz:

```text
dotnet publish prototypes/GigaDock.Linux.DesktopProbe/GigaDock.Linux.DesktopProbe.csproj -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=false -o prototypes/GigaDock.Linux.DesktopProbe/bin/linux-x64
```

No terminal Linux, executar `./GigaDock.DesktopProbe` dentro dessa pasta. Preservar todos os arquivos publicados. É necessário desktop X11 ou XWayland e bibliotecas nativas .NET/Avalonia; self-contained inclui .NET, mas não todas as bibliotecas do sistema. Consulte [a validação Linux](../../docs/LINUX-VALIDACAO.md).

Controles: Trabalho/Estudos/Pessoal demonstram seleção sem persistência; Ajustes abre popup de monitor/recolhimento; Diagnóstico abre relatório selecionável; Sair encerra. Tab navega pelos controles. Escape expande a faixa recolhida. O recolhimento é opcional e acontece somente quando a janela perde foco e o ponteiro sai; mantém uma faixa visível.

Posicionamento usa a área útil informada pelo toolkit e conversão entre pixels e unidades lógicas. Não implementa EWMH de dock, área exclusiva, layer-shell, Wayland nativo nem rastreamento de janelas de outros aplicativos. Em sessões que ignoram posicionamento de janela, o resultado pode diferir do solicitado. Não usa Topmost; o desktop pode cobri-la como uma janela normal.
