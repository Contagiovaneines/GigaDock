# GigaDock

Uma dock personalizável para Windows 10 e 11, com atalhos, widgets e ambientes dedicados a **Trabalho, Estudos e Pessoal**.

Desenvolvido em **C#, .NET 10 e WPF**, com interface em português do Brasil, configurações locais e sem conta ou telemetria própria. O projeto está em **beta**: os recursos implementados e as limitações de validação estão documentados abaixo.

[Instalação](#instalação) · [Recursos](#recursos) · [Desenvolvimento](#desenvolvimento) · [Em beta](#o-que-ainda-está-em-beta) · [Ideias futuras](#ideias-futuras)

![Prévia da GigaDock](docs/dock-apps-preview.jpg)

*A captura é de uma versão anterior; alguns estilos e controles foram atualizados desde então.*

## Recursos

- **Ambientes independentes:** aplicativos fixados, widgets, estilos e visibilidade por ambiente; itens globais aparecem em todos quando configurados explicitamente.
- **Dock personalizável:** temas, cores, altura, transparência, cantos arredondados, divisores, coleções e ocultação automática.
- **Interação com aplicativos:** expansão suave dos ícones, resposta ao foco de teclado, miniaturas de janelas, ativação e fechamento explícitos. Novos lançamentos da GigaDock sinalizam a instância existente.
- **Loja local de widgets:** catálogo integrado com escolha de estilos e referências visuais. Permite um widget de cada tipo por ambiente.
- **Mídia integrada ao Windows:** capa, título, artista, controles e progresso; cores do cartão derivadas da capa e escala proporcional à dock.
- **RGB musical e gamer:** contorno animado durante a reprodução ou continuamente no modo gamer. O modo gamer é configurado nos ajustes ou no menu da bandeja.
- **Alertas visuais:** WhatsApp em verde e Teams em violeta. Alertas têm prioridade sobre RGB musical/gamer; mensagens e chamadas do WhatsApp usam tratamentos distintos.
- **Controles rápidos:** atalhos para configurações e ações do Windows, com seleção dos itens visíveis e estilos por ambiente.
- **Lixeira integrada:** ao ativar sua exibição na dock, oculta o ícone do desktop; ao desativar, restaura. A alteração ocorre por ação explícita do usuário.
- **Ajustes adaptativos:** navegação conforme a largura da janela, cartões, foco de teclado e controles de aparência padronizados.

### Catálogo de widgets

| Widget | Recursos disponíveis | Observações |
| --- | --- | --- |
| Relógio | Hora, data, estilos digitais e analógicos, relógios mundiais, cronômetro e temporizador | Fusos predefinidos; temporizador de cinco minutos. |
| Pomodoro | Foco, pausas, ciclos, controles e som de transição | Configuração por ambiente. |
| Calendário e reuniões | Eventos locais, importação iCalendar, próximo compromisso e link HTTPS | Recorrências e fusos; sem login Google/Outlook. |
| Mídia | Capa, controles, progresso e estilos compacto, mini e barra | Depende da sessão publicada pelo player no Windows; sem busca de posição pela barra. |
| Clima | Temperatura, condição, previsão, próximas horas, vento e horários solares | Consulta wttr.in; dados ausentes são indicados. |
| Monitor do sistema | CPU, RAM, rede e armazenamento; anéis e gráficos | Coletor compartilhado e histórico de até 30 amostras; sem GPU. |
| Bateria | Porcentagem, carregamento e estilo em anel | Diferencia ausência de bateria, estado desconhecido e falha de leitura. |
| GitHub | Grade pública de contribuições e animações arcade | Consulta HTML público; sem integração de Actions ou pull requests. |
| WhatsApp | Notificações locais e indicação estimada de chamadas | Depende de permissões e notificações do Windows. |
| Teams | Notificações, aplicativo aberto e indicação estimada de reunião/chamada | Sem consulta oficial de presença. |
| Notas | Editor integrado e salvamento automático local | Conteúdo compartilhado entre ambientes. |
| Discord | Atalho para abrir o aplicativo | Integração de voz ainda indisponível. |
| OBS | Atalho para abrir o aplicativo | Controle de gravação ainda indisponível. |

Cotação de moedas existe no modelo, mas ainda não possui widget funcional no catálogo.

### Personalizar widgets

1. Abra **Ajustes → Ambientes** e selecione o ambiente desejado.
2. Acesse **Widgets → + Loja de Widgets** para adicionar um widget e escolher seu estilo.
3. Nos instalados, use **Escolher estilo** ou **Mudar estilo** na loja.
4. Nos widgets compatíveis da dock, use **botão direito** ou **Shift+F10 → Personalizar**.

Trocar o estilo preserva a visibilidade. Dados como notas, usuário GitHub, localização do clima e alguns compromissos ainda são globais.

No widget GitHub, o menu de clique direito permite escolher **Cobrinha, Pac-Man, Breakout, Galaga, Puzzle Bobble, Bomberman e Minesweeper**, ou desativar a animação. A escolha fica salva localmente. São adaptações nativas à grade compacta, com pausa quando ocultas; não são jogos interativos nem reprodução dos SVGs de referência. [Detalhes](docs/GITHUB-ANIMACOES.md).

### Controles e prévias

- **Wi-Fi, Bluetooth, modo escuro e foco:** abrem configurações do Windows; os seletores da dock controlam a visibilidade dos atalhos.
- **Aplicativos em segundo plano:** a seta junto à bateria abre um painel da própria GigaDock, sem revelar a barra nativa. Ele agrupa executáveis em funcionamento na sessão atual, mostra seus ícones e permite abrir ou ativar o aplicativo. A lista é uma visão de processos de usuário; a API pública do Windows não permite copiar exatamente os ícones registrados na bandeja do Explorer.
- **Bloquear teclado:** bloqueio temporário de 30 segundos, com liberação por F12, clique, troca de ambiente ou encerramento. Não substitui o bloqueio da sessão.
- **Bloquear tela e suspender:** ações nativas; suspensão solicita confirmação.
- **Miniaturas de janelas:** usam DWM, com paginação, ativação e fechamento. Representam janelas, não abas internas dos aplicativos.
- **Prévias de pastas:** até 100 itens, imagens e trechos de texto limitados; outros formatos mostram metadados.
- **Abrir por clique ou mouse:** configurável em **Ajustes → Visualizações** para os painéis compatíveis.

## Instalação

O pacote local é gerado em `release/GigaDock-Setup.exe`. Para versões publicadas, consulte as **Releases** do repositório; para builds de desenvolvimento, use os artefatos de **Actions → GigaDock - Build Windows**. [Guia dos artefatos](docs/GITHUB-ACTIONS.md).

**Requisitos:** Windows 10/11 x64 e **.NET Desktop Runtime 10 x64**. O instalador possui runtime próprio, mas o aplicativo empacotado depende do runtime instalado no Windows.

O instalador não ativa automaticamente o modo de barra principal. Na atualização, confira preferências antigas explicitamente habilitadas. Os scripts em [tools/](tools/) incluem restauração da barra nativa.

**Distribuição em beta:** os executáveis atuais não estão assinados e podem ser bloqueados pelo Controle de Aplicativo do Windows. O fluxo de assinatura está preparado, mas exige certificado confiável ou integração de assinatura aprovada. [Guia de assinatura](docs/ASSINATURA-DIGITAL.md).

O pacote local documentado inclui as mudanças até a regra de vídeo em tela cheia. As revisões posteriores de prévias, progresso de mídia, ajustes, bandeja e animações GitHub precisam de novo empacotamento. Consulte o [manifesto do pacote](docs/installer-validation.json) e o [histórico](docs/STATUS.md); a data do código não garante que o instalador contenha todas as alterações.

## Desempenho e atividade

O gerenciador compartilhado distingue widgets habilitados, ambiente ativo, visibilidade e permissão de animação. Widgets ocultos suspendem trabalho visual e consultas dispensáveis, preservando tarefas necessárias, como conclusão de temporizadores e recebimento de notificações.

| Componente | Enquanto visível | Quando oculto ou inativo |
| --- | --- | --- |
| Monitor | Métricas solicitadas a cada 2 segundos | Interrompe coleta e libera contador CPU. |
| Bateria | Leitura a cada 30 segundos | Interrompe consultas. |
| Mídia | Metadados por eventos; progresso durante reprodução | Suspende atualização visual; eventos mínimos podem sustentar RGB musical. |
| Relógio e Pomodoro | Atualização conforme estilo e contagem | Preserva prazos e conclusão sem redesenho contínuo. |
| Calendário, clima e GitHub | Atualização por necessidade e cache | Interrompe polling/animações e cancela consultas dispensáveis. |
| RGB e efeitos visuais | Conforme preferências e atividade | Suspende animações; preserva a preferência gamer. |

O cache de ícones tem limites de **128 entradas e 8 MiB de pixels estimados**. Capas também possuem cache limitado. Esses limites não representam a memória total do processo: **ainda não há benchmark comparável que comprove uma redução de RAM ou CPU**.

[Auditoria de atividade](docs/AUDITORIA-ATIVIDADE.md) · [Refatoração dos widgets](docs/REFATORACAO-WIDGETS.md)

## Dados e privacidade

As configurações e notas ficam no computador. A dock não possui backend, conta ou telemetria própria, mas alguns widgets acessam serviços externos.

| Dado | Local ou serviço |
| --- | --- |
| Configurações | `%LOCALAPPDATA%\DockWindows\settings.json`, com backup e migrações |
| Notas | `%LOCALAPPDATA%\DockWindows\notas.txt` |
| Instalação padrão | `%LOCALAPPDATA%\Programs\DockWindows` |
| Clima | wttr.in |
| Contribuições | HTML público do GitHub |
| Calendário | Arquivo local ou URL iCalendar configurada |

Links são abertos por ação do usuário. URLs iCalendar são armazenadas nas configurações; sua validação e o tratamento de endereços com tokens ou credenciais ainda precisam de revisão.

## Desenvolvimento

Requisitos: Windows e SDK .NET compatível com [global.json](global.json). Versão dos projetos centralizada em [Directory.Build.props](Directory.Build.props).

```powershell
dotnet restore DockWindows.slnx
dotnet build DockWindows.slnx -c Release --no-restore
dotnet run --project src/DockWindows.App/DockWindows.App.csproj
```

Para executar os testes sem os casos que modificam autostart ou barra de tarefas:

```powershell
dotnet test tests/DockWindows.Tests/DockWindows.Tests.csproj -c Release --filter "Category!=SystemIntegration"
```

Para gerar o instalador:

```powershell
.\build_release.ps1
```

O script publica o aplicativo, recria o ZIP embutido e publica o instalador, preservando artefatos anteriores em pastas datadas. A execução depende das políticas locais do Windows. O workflow [.github/workflows/build-windows.yml](.github/workflows/build-windows.yml) compila, testa e empacota em push, pull request ou execução manual; artefatos ficam disponíveis por 14 dias. Ele ainda não assina nem publica Releases automaticamente.

### Estrutura do repositório

| Diretório | Responsabilidade |
| --- | --- |
| `src/DockWindows.Core` | Modelos, catálogo, validação, contratos e motor Pomodoro |
| `src/DockWindows.Infrastructure` | Persistência e integrações com Windows |
| `src/DockWindows.App` | Interface WPF, ViewModels, ajustes e loja |
| `src/DockWindows.Installer` | Instalação, atualização, atalhos e desinstalação |
| `tests/DockWindows.Tests` | Testes automatizados |
| `docs/` | Status, auditorias, guias e resultados de validação |
| `vercel/` | Site estático e demonstração web |
| `assets/` | Ícone e recursos gráficos |
| `tools/` | Scripts de build e manutenção |
| `release/`, `dist/` | Artefatos locais de distribuição e prévia |

### Validação

- A refatoração anterior registrou **140 testes aprovados**, excluindo dois casos `SystemIntegration`; quatro testes adicionais da lixeira também foram aprovados. Esses resultados são históricos, não uma certificação de todas as mudanças posteriores.
- A primeira execução de GitHub Actions foi concluída com sucesso, conforme o resultado apresentado pelo mantenedor.
- O build Release mais recente das animações GitHub passou com **zero erros e zero avisos**. Os 12 testes específicos foram bloqueados localmente pelo Controle de Aplicativo ao carregar uma DLL (`0x800711C7`), sem casos aprovados nessa execução.
- Instalação real, integração com aplicativos, aparência e escalas de tela ainda exigem validação manual.

[Histórico completo](docs/STATUS.md) · [Relatório da refatoração](docs/REFATORACAO-WIDGETS.md) · [Validação das animações](docs/GITHUB-ANIMACOES.md)

## Site e demonstração

A pasta [vercel/](vercel/) contém a apresentação e uma demonstração interativa com dados ilustrativos. Ela não controla o Windows nem substitui o aplicativo desktop.

```powershell
python -m http.server 8000 --directory vercel
```

Abra `http://localhost:8000`. A verificação anterior da demonstração registrou 13 cenários aprovados em desktop e celular. [Detalhes](docs/SITE-VERCEL.md).

## Contribuição e licença

Leia [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) e as regras em [AGENTS.md](AGENTS.md). Distribuído sob a licença [MIT](LICENSE).

Criado e mantido por **Giovane Ines**. [GitHub](https://github.com/Contagiovaneines) · [LinkedIn](https://www.linkedin.com/in/giovaneines/) · Contato: `giovaneinesdev@gmail.com`.

## O que ainda está em beta

Os itens abaixo possuem implementação parcial, dependências externas ou validação pendente. “Disponível no código” não significa que esteja validado em todas as instalações.

| Área | Limitação ou validação pendente |
| --- | --- |
| Instalação e assinatura | Assinatura confiável pendente; validar instalação, atualização e desinstalação em Windows 10/11, preservando dados e restaurando a barra. |
| Interface e acessibilidade | Conferir estilos, galerias, menus, teclado, leitor de tela, múltiplos monitores e escalas de 100% a 200%. |
| Aplicativos em segundo plano | Painel próprio baseado em processos da sessão; precisa de validação com aplicativos empacotados/protegidos e não reproduz exatamente a bandeja do Explorer. |
| Alertas WhatsApp/Teams | Dependem de permissões e notificações. No WhatsApp, o término do contorno de chamada depende da remoção da notificação, sem confirmação direta de atendimento. |
| Tela cheia | Ocultação por processo/título de players e serviços conhecidos; não confirma reprodução de vídeo. |
| Mídia e métricas | Conferir players reais, troca de capas, bateria física e métricas sob carga; VPN pode duplicar tráfego. |
| Calendário e rede | Validar importações, falhas de rede, cancelamento e privacidade das URLs iCalendar. |
| Animações GitHub | Adaptações compactas recém-adicionadas; validar legibilidade e executar os testes em ambiente que permita carregar as DLLs. |
| Persistência e comandos | Revisar migrações de schema, validação central de caminhos/protocolos, mensagens de erro e textos do catálogo. |
| Consumo de recursos | Medir CPU/RAM, uso prolongado, concorrência e possíveis vazamentos; sem promessa de consumo mínimo. |
| Isolamento de dados | Alguns conteúdos e preferências ainda são globais; ampliar personalização por ambiente. |

## Ideias futuras

Propostas para evolução do projeto, **sem prazo ou compromisso de entrega**:

O diagnóstico arquitetural, a ordem recomendada, os limites das APIs do Windows e os critérios de aceite estão no [plano de implementação das ideias futuras](docs/PLANO-IMPLEMENTACAO-IDEIAS-FUTURAS.md).

- Exibir rede Wi-Fi, intensidade do sinal e dispositivos Bluetooth conectados.
- Selecionar saída de áudio, controlar volume por aplicativo e silenciar microfone.
- Adicionar widgets de área de transferência, capturas, Downloads e arquivos recentes.
- Criar lembrete de água e cotação de moedas.
- Integrar Discord e OBS por interfaces apropriadas, com estados reais.
- Tornar a integração de contribuições GitHub mais robusta.
- Permitir várias instâncias de um mesmo tipo de widget por ambiente.
- Configurar duração, alarmes e fusos dos controles de tempo.
- Ampliar prévias de documentos e criar uma estante de arquivos.
- Explorar compartilhamento de arquivos entre dispositivos Windows.
- Expandir dados e aparência independentes por ambiente.
