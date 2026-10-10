# Arquitetura

O GigaDock tem dois frontends na mesma solução .NET 10. Windows usa WPF; Linux usa Avalonia porque WPF depende do Windows. As interfaces não possuem paridade completa.

| Projeto | Responsabilidade |
| --- | --- |
| `DockWindows.Core` | Modelos, contratos, validação e regras independentes de interface |
| `GigaDock.Services` | Persistência e serviços compartilhados |
| `DockWindows.Infrastructure` | Adaptação às APIs e diretórios Windows |
| `DockWindows.App` | Interface WPF, ViewModels, widgets e recursos gráficos |
| `DockWindows.Installer` | Instalação Windows por usuário e guia inicial |
| `GigaDock.Infrastructure.Linux` | XDG, catálogo/abertura de aplicativos e comandos Linux |
| `GigaDock.App.Linux` | Interface Avalonia e composição Linux |
| `GigaDock.Linux.DesktopProbe` | Protótipo/diagnóstico Avalonia em `prototypes/` |

Testes de domínio/serviços ficam em `tests/GigaDock.Tests.Core`; adaptações Linux em `tests/GigaDock.Tests.Linux`; WPF/Windows em `tests/DockWindows.Tests`. Empacotamento Linux usa Python/Bash e tem testes próprios.

## Limites de integração

Dados de cada ambiente ficam locais. Configurações contêm referências a aplicativos e widgets; não devem ser versionadas com dados do usuário. Credenciais não entram no código nem em relatórios públicos.

No Linux, comandos usam argumentos separados e limites de tempo/saída. Abertura de aplicativos e execução manual de scripts têm fluxos próprios. Integrações opcionais mostram indisponibilidade quando dependências não existem. O app não substitui automaticamente painéis do sistema.

O frontend Windows e seus recursos gráficos são referenciados pelo frontend Linux quando necessário. Mover arquivos de `src/` sem atualizar projetos pode quebrar recursos incorporados; a organização pública preservou esses caminhos. Diretórios de build e laboratório permanecem ignorados.
