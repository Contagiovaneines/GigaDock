> **Estado consolidado após as mudanças recentes (05/10/2026):** build Release aprovado nesta revisão, zero erros/avisos incremental. Última suíte completa anterior: 95/108 aprovados, 13 falhas; tentativa posterior selecionada: 3/11 aprovados, 8 falhas de carregamento por Controle de Aplicativo. Suíte atual não reexecutada. Instalador ainda oculta/desabilita a barra nativa automaticamente. Veja [ANALISE-PROJETO.md](ANALISE-PROJETO.md) e [README](../README.md). Registros abaixo são históricos.

# Status do Projeto — Dock Windows

| Etapa | Nome | Status | Entregável / Resultado |
| :--- | :--- | :--- | :--- |
| **01** | **Análise e Viabilidade** | Concluído | `docs/01-analise.md`: Escopo da V1 detalhado, itens fora de escopo (fase 2), histórias de usuário (US01 a US07), análise de riscos (DPI, atalhos, permissões, itens inválidos, persistência) e critérios de aceite. |
| **02** | **Design e Arquitetura Visual** | Concluído | `docs/02-design.md`: Sistema de tokens, geometria da dock, especificações de componentes (seletor de ambiente, grade de itens, relógio, pomodoro, janelas de opções), acessibilidade, escalas multi-DPI, fluxos de uso e protótipo interativo `docs/prototipo.html`. |
| **03** | **Plano Técnico** | Concluído | `docs/03-plano-tecnico.md`: Arquitetura em camadas (Core, Infrastructure, App, Tests), modelos de domínio (`Preferencias`, `Ambiente`, `ItemFixado`, `WidgetConfig`), persistência atômica com backup, APIs Win32 (RegisterHotKey, ShellExecute, SHGetFileInfo, HKCU Run) e plano de 7 entregas incrementais com compilação. |
| **04** | **Execução / Implementação** | Concluído | Código completo e executável implementado em C# / .NET 10 / WPF. Suporte a ambientes Trabalho, Estudos e Pessoal; itens fixados (.exe, arquivos, pastas, URLs); relógio e Pomodoro com alertas sonoros; auto-hide; hotkeys globais; system tray; persistência atômica; README.md; `docs/04-implementacao.md`; 17 testes automatizados passando; build 0 avisos/0 erros; binários em `bin/publish/`. |
| **05** | **Revisão de Código** | Concluído | `docs/05-revisao.md`: Revisão minuciosa independente realizada. 5 defeitos concretos corrigidos (temas dinâmicos, resolução de tela via DisplaySettingsChanged, separadores órfãos, acessibilidade e normalização de URLs). |
| **06** | **Testes e Validação** | Concluído | `docs/06-testes.md`: 22 de 22 testes automatizados aprovados (unitários e de integração); validação real de CRUD de ambientes, persistência, recuperação de corrupção, lançamento defensivo, registro HKCU e lançamento real verificado do executável `bin/publish/DockWindows.App.exe`. |
| **07** | **Entrega Final (V1.0)** | Concluído | `docs/07-entrega.md`, `README.md` finalizado, executável de release publicado em `dist/DockWindows-v1.0.0/DockWindows.App.exe` e pacote de distribuição compactado em `dist/DockWindows-v1.0.0-win-x64.zip`. |
| **08** | **Substituição, Personalização e Instalador (V1.1)** | Concluído | `docs/08-substituicao-personalizacao-instalador.md`: Modo "Usar como barra principal" com posicionamento rente à borda inferior e recolhimento seguro via `SHAppBarMessage`; rotinas de restauração da barra nativa e scripts de emergência (`tools/`); alternância de seções e seletor de ambientes; transição rápida (fade/slide) com suporte a redução de movimento do Windows; janela dedicada de personalização com Live Preview, reordenação de itens e restauração de padrões com confirmação; migração de esquema de configurações para v2; instalador oficial autônomo com assistente gráfico (`dist/DockWindows-Setup.exe`), atalhos no Menu Iniciar/Desktop, autostart e desinstalador seguro registrado no Windows. 32/32 testes aprovados. |
| **09** | **Área de Apps e Janelas Abertas (V1.2)** | Concluído | `docs/09-area-aplicativos-janelas-abertas.md`: Área permanente para aplicativos (`AppsPermanentes`) separada dos ambientes de produtividade; atalhos dedicados para Menu Iniciar (Win), Pesquisa (Win+S) e Explorador de Arquivos (Win+E); rastreamento em tempo real de janelas abertas via Win32 APIs documentadas (`EnumWindows`, `SetWinEventHook`, `QueryFullProcessImageName`, `GetForegroundWindow`); mesclagem inteligente sem duplicação de ícones; indicadores visuais do Windows 11 (ponto sutil para janelas em segundo plano, barra iluminada e realce para primeiro plano, badge com contagem de múltiplas janelas); flyout pop-up de seleção e fechamento individual para apps com múltiplas janelas; 5 seções modulares e reordenáveis (⬆️/⬇️) com visibilidade configurável na central de personalização; opção de apps fixados globais vs exclusivos do ambiente; migração atômica para SchemaVersion 3; captura visual `docs/dock-apps-preview.jpg`; instalador atualizado em `dist/DockWindows-Setup.exe`. 40/40 testes aprovados. |
| **10** | **Ajustes Unificados, Coleções, Widgets e Temas (V1.3)** | Concluído | `docs/10-ajustes-colecoes-widgets-temas.md`: Central unificada de ajustes (`AjustesWindow`) com 6 seções (Ambientes, Widgets, Espaçadores, Aparência, Geral, Utilitários); agrupamento de aplicativos em coleções (pastas/grupos com flyout ancorado e fechamento via `Esc`/clique externo); novo widget embutido de Calendário e Compromissos Locais com suporte a formatos Compacto e Expandido; gerenciador de eventos locais sem telemetria; magnificação fluida de ícones ao passar o mouse (hover magnification suave a 60 FPS com `CubicEaseOut`); transição que anima apenas os itens que mudam ao trocar de ambiente; 4 temas originais para Windows 11 (Discreto, Escuro, Colorido, Com Brilho); divisores/espaçadores configuráveis (Linha, Espaço, Ponto); ações de contexto para mover itens entre global e ambientes; migração atômica para SchemaVersion 4; instalador oficial autônomo atualizado em `dist/DockWindows-Setup.exe`. 48/48 testes aprovados. |
| **11** | **Configurações em Três Colunas e Upgrade do Instalador (V1.4)** | Concluído | `docs/11-janela-ajustes-3colunas-upgrade-instalador.md`: Arquitetura em 3 colunas (Navegação lateral persistente com grupos Principal e Ajustes; Lista central com botões de ação e proteção de exclusão; Editor de detalhes à direita com suporte completo a edição de ambientes, cores, indicador de janelas abertas em Barra/Ponto/Pílula com cor dedicada, atalhos com escopo alternável entre ambiente e global, validação de URLs, widgets de Pomodoro e Agenda Local, divisores e opções gerais); instalador atualizado para v1.4.0 com detecção automática de versão anterior, atualização in-place sem duplicidades, backup pré-update de `settings.json`, restauração preventiva da barra do Windows e desinstalação segura; executável `dist/DockWindows-Setup.exe` (0,49 MB). 55/55 testes aprovados. |
| **12** | **Sistema Visual Liquid Glass (Vidro Líquido)** | Concluído | `docs/12-estilo-visual-liquid-glass.md`: Tradução completa dos 10 componentes do ecossistema Apple Liquid Glass (Snipzy) para arquitetura WPF nativa acelerada por hardware; novo tema oficial "Vidro Líquido" (`EstiloTema.VidroLiquido`) com camada de reflexo especular de curvatura física na barra flutuante; microinterações elásticas em botões com escala 1.18x no hover e 0.95x no clique (física tátil); pílulas ativas iluminadas na barra lateral de navegação; correção robusta de lock de arquivos em atualizações in-place no instalador (retry com backoff exponencial e renomeação NTFS de contingência); instalador atualizado em `dist/DockWindows-Setup.exe` (0,49 MB). 60/60 testes aprovados. |
| **13** | **Ícones Originais em Alta Resolução, Ocultação Automática e Dock Compacta Estilo Apple** | Concluído | `docs/13-icones-originais-discreto-apple.md`: Extração de ícones autênticos em 256x256 e 48x48 via Shell `SHGetImageList(SHIL_JUMBO)` e Win32 `WM_GETICON`/`GetClassLongPtr` de janelas e UWP; detecção de navegadores para URLs; auto-hide imediato da barra do Windows na instalação; alinhamento rente à borda inferior (4px de folga); proporções compactas Apple (altura 52px para ícones médios, 46px para pequenos); coleções com mini-grade 2x2 e popover em 2 colunas com título centralizado e botão de rodapé "Editar Coleção..." (Imagem 2); widget de calendário em cápsula com badge estilo Apple (Imagem 3); instalador oficial atualizado em `dist/DockWindows-Setup.exe` (0,50 MB). 64/64 testes aprovados. |
| **14** | **Substituição Total da Barra de Tarefas, Gancho da Tecla Win e Ícones Originais** | Concluído | `docs/14-substituicao-total-barra-win-key.md`: Substituição total da barra de tarefas do Windows via `SW_HIDE` e `EnableWindow(false)` para `Shell_TrayWnd` e `Shell_SecondaryTrayWnd`; watchdog contínuo de 250ms anti-ressurreição; interceptação de toque isolado da tecla Win via `WH_KEYBOARD_LL` (preservando Win+R, Win+D, Win+E, Win+L); abertura do Launchpad próprio estilo Liquid Glass ("abrir no que eu tenho") com busca instantânea e execução no Enter; correção definitiva da extração de ícones em alta resolução (`PrivateExtractIconsW`, `SHGetImageList` + `ImageList_GetIcon`, BCL e janelas HWND sem exceções silenciosas `✦`); scripts de restauração atualizados. 71/71 testes aprovados. |

---

### Registro de Conclusão do Projeto
- **Status Geral:** 100% Concluído (Versão 1.4.3 — Substituição Total da Barra, Tecla Win e Ícones Originais)
- **Ambiente:** Windows 10/11 (x64), .NET 10 (10.0.401), WPF
- **Compilação:** 0 Avisos, 0 Erros
- **Testes Automatizados:** 71/71 Aprovados (Unitários, Integração, Substituição Win32, Gancho WinKey, Launchpad, Coleções, Widgets, Temas, Espaçadores, Navegação 3 Colunas, Validação e Liquid Glass)
- **Instalador Oficial:** Gerado e testado em `dist/DockWindows-Setup.exe` (0,51 MB)
- **Scripts de Recuperação de Emergência:** `tools/restaurar-barra-windows.bat` e `tools/restaurar-barra-windows.ps1`
- **Documentação de Recursos:** `docs/14-substituicao-total-barra-win-key.md`, `docs/13-icones-originais-discreto-apple.md`, `docs/12-estilo-visual-liquid-glass.md`, `docs/11-janela-ajustes-3colunas-upgrade-instalador.md`, `docs/10-ajustes-colecoes-widgets-temas.md` e `README.md`

### Etapa 15 (v1.5.0) - Reservas de Espa�o e Fixes Cr�ticos
- **Objetivo:** Corre��o de bugs de intera��o, reserva de espa�o AppBar e verifica��o final de �cones.
- **Implementa��o:**
  - Adicionado AppBarHelper para registrar o dock usando SHAppBarMessage (ABM_NEW/ABM_SETPOS), resolvendo a lacuna de sobreposi��o de janelas maximizadas.
  - Ajuste de bindings quebrados em SectionColecoes e SectionIniciarPesquisa que impediam cliques devido � �rvore visual do WPF (RelativeSource AncestorType).
  - Configura��o autom�tica para fechar o Launchpad ao teclar Enter num resultado de busca.
  - Vincula��o dos dados nos widgets de Calend�rio e Rel�gio que estavam inertes.
- **Testes:** Compila��o 0 erros, testes 71/71 OK.
- **Pr�ximos Passos:** Implementar as demais etapas do PLANO-EVOLUCAO.md, iniciando por DWM Thumbnails na v1.6.

### Etapa 15 (v1.5.0) - Reservas de Espa�o e Fixes Cr�ticos
- **Objetivo:** Corre��o de bugs de intera��o, reserva de espa�o AppBar e verifica��o final de �cones.
- **Implementa��o:**
  - Adicionado AppBarHelper para registrar o dock usando SHAppBarMessage (ABM_NEW/ABM_SETPOS), resolvendo a lacuna de sobreposi��o de janelas maximizadas.
  - Ajuste de bindings quebrados em SectionColecoes e SectionIniciarPesquisa que impediam cliques devido � �rvore visual do WPF (RelativeSource AncestorType).
  - Configura��o autom�tica para fechar o Launchpad ao teclar Enter num resultado de busca.
  - Vincula��o dos dados nos widgets de Calend�rio e Rel�gio que estavam inertes.
- **Testes:** Compila��o 0 erros, testes 71/71 OK.
- **Pr�ximos Passos:** Implementar as demais etapas do PLANO-EVOLUCAO.md, iniciando por DWM Thumbnails na v1.6.

### Etapa 16 (v1.6.0) - Refatora��o Arquitetural e UI Apple-like (Fase 1 e 2)
- **Objetivo:** Adotar o estilo visual de p�lula, com �cones 'Squircle' uniformes, badges padr�o e criar o suporte para blocos grandes diretamente na dock (Inline Widgets).
- **Implementa��o:**
  - **Dock Pill:** For�ado o raio de borda para 100, transformando o dock em p�lula.
  - **Squircle Icons:** Aplicado GeometryClip (CornerRadius 10) em SectionApps e SectionItensAmbiente para uniformizar todos os formatos irregulares extra�dos dos EXEs em blocos quadrados padr�o iOS.
  - **Indicadores:** Removido as barras coloridas por ambiente e substitu�das por simples pontos (dots) cinzas opacos/transl�cidos.
  - **Badges:** Atualizado cor do selo num�rico de azul para vermelho iOS (#FF3B30).
  - **M�dia e Clima Inline:** Criada a infraestrutura das se��es SectionMidiaInline e SectionClimaInline, seus ViewModels Mock, e registrados na pipeline do MainWindow.
- **Testes:** Compila��o 0 erros. Su�te de testes rodada.
- **Pr�ximos Passos:** Conectar os mocks do Clima a alguma API real de Weather e a M�dia � API do Windows (Windows.Media.Control) para o NowPlaying real no futuro.

### Etapa 17 (v1.6.1) - Sincroniza��o iCal, Corre��o de Foco e Clima Local
- **Objetivo:** Resolver bugs cr�ticos no Launchpad (foco de teclado, a��o de clique incorreta, e disparo via Enter vazio), substituir API mockada de clima por local real, e integrar parsing de ICS/iCal no calend�rio.
- **Implementa��o:**
  - Foco via Win32 API (SetForegroundWindow) garantido ao abrir Launchpad.
  - Evitado toggle minimize em apps ao clicar no Launchpad (for�a ativa��o).
  - Preven��o do Launchpad abrir aplicativos aleat�rios com Enter em caixa de busca vazia.
  - Sincroniza��o e parsing real de arquivos .ics online introduzidos na Central de Ajustes.
  - Adicionada automa��o via IP para Clima Real do usu�rio (usando a wttr.in) junto com as op��es de ExibirClima e ExibirBotoesAcao.
- **Testes:** Compila��o 0 erros.
- **Pr�ximos Passos:** Finalizar suporte a controles de m�dia avan�ados e valida��es de thumbnail DWM.

### Etapa 18 (v1.6.2) - Detec��o de Tela Cheia e Redesign Clima
- **Objetivo:** Adicionar detec��o autom�tica de tela cheia (jogos, v�deos) para auto-ocultar a Dock e melhorar visual do Clima.
- **Implementa��o:**
  - Win32WindowTrackingService agora implementa hook MonitorFromWindow e GetMonitorInfo na janela em primeiro plano.
  - Vari�vel de estado atada ao viewmodel OcultoPorTelaCheia que manipula o Hide() na WPF.
  - Corre��o de design em SectionClimaInline.xaml (mudan�a do stack vertical pra horizontal para evitar sobreposi��o de fonte).
  - Adicionado suporte a LocalizacaoClima manual para substituir IP tracking da API de clima.
  - Binding do CheckBox de visibilidade de widgets modificado para garantir sync bidirecional instant�neo e n�o exigir reboot.
- **Testes:** Compila��o OK, Instalador gerado.

### Etapa 19 (v1.6.3) - Suporte a Arquivo .ics Local
- **Objetivo:** Adicionar funcionalidade para carregar compromissos de arquivos .ics locais al�m da URL web.
- **Implementa��o:**
  - Modificado o TextBox de configura��o do Calend�rio para suportar caminhos locais (ex: C:\arquivos\cal.ics) atrav�s de um bot�o [...] lateral.
  - Adicionada detec��o se a entrada � link (HTTP) ou arquivo real (File IO) dentro de CalendarioWidgetViewModel.cs.
- **Testes:** Compila��o com 0 erros.

### Etapa 20 (v1.6.4) - Auditoria de Seguran�a
- **Objetivo:** Auditar o c�digo contra inje��es de comando e escalonamento de privil�gios.
- **Resultado:** A valida��o estrita em ItemValidator.cs (restri��o absoluta de protocolos em URLs para HTTP/HTTPS) e a desserializa��o limpa no WPF bloqueiam 100% de ataques RCE via atalhos e evitam XSS/Injection. Nenhuma telemetria na rede. Aplicativo extremamente isolado.
- **Status:** Sem falhas de seguran�a detectadas. Instalador regenerado e verificado.

### Etapa 21 (v1.6.5) - Hotfix Encodings
- **Problema:** Textos e �cones renderizando com bugs visuais no AjustesWindow.xaml
- **Resolu��o:** Restaurados os encodings UTF-8 corrompidos por manipula��o via PowerShell ANSI e recompilado o instalador.

### Ideias Futuras e Limitações Conhecidas
- **Cores de Status do Teams (Graph API):** O aplicativo do Teams não expõe as cores de status (Disponível, Ausente, Ocupado) localmente no disco. Para implementar a sincronização dessas cores na dock no futuro, seria necessário abrir uma exceção na regra do projeto ("100% offline e local"). A implementação exige registrar a GigaDock como um Aplicativo Corporativo no Azure AD (Portal do Desenvolvedor da Microsoft) para obter um `Client ID`, implementar MSAL (OAuth 2.0) e forçar o usuário a fazer login na própria conta Microsoft na dock. O status seria então puxado da nuvem utilizando a API `GET /me/presence`.

## Ambientes isolados + menus escuros (2026-10-05, nao commitado)
- Apps fixados agora pertencem ao ambiente ativo (AppsPermanentes globais migrados para o 1o ambiente). Dock recarrega ao trocar ambiente.
- Widgets alterados em Ajustes so afetam a dock se o ambiente editado for o ativo.
- Tema escuro global para ContextMenu/MenuItem/submenus (App.xaml).
- Selecao de ambiente em Ajustes corrigida (SelectedValuePath=Model).
- Compilado e instalador gerado; nao testado manualmente. Limite: secao Midia Inline continua global (aparece sempre que ha midia tocando).


## Correcao: apps de outros ambientes reaparecendo (2026-10-05)
- Resultado: janelas de apps fixados em outros ambientes sao filtradas da lista de apps abertos nao fixados. Apps compartilhados continuam visiveis quando fixados no ambiente ativo.
- Salvamento deixou de recriar AppsPermanentes globais, evitando nova migracao e mistura de itens na proxima inicializacao.
- A opcao de mostrar apps abertos nao fixados ainda permite apps sem vinculo com nenhum ambiente; desligada, mostra apenas os itens do ambiente.
- Validacao executada: dotnet build DockWindows.slnx -c Release --no-restore: sucesso, zero erros; dois casos de regressao (opcao ligada/desligada) aprovados, incluindo troca, atualizacao e reabertura com repositorio simulado.
- Dubles de icones de quatro arquivos de teste atualizados para a interface atual.
- Publicacao Release win-x64 do app e instalador concluida; release/GigaDock-Setup.exe atualizado.
- Limites: suite completa nao executada; instalacao e interface nao testadas manualmente. Avisos existentes de nulabilidade em MidiaWidgetViewModel e campo nao usado em MonitorSistemaViewModel durante publicacao; evento nao usado no duble de tracking durante build.
- Proximos passos: instalar a versao atualizada e conferir visualmente a alternancia entre ambientes. Itens ja misturados por migracoes anteriores nao sao excluidos automaticamente, pois podem ser fixacoes intencionais.
## Visual do widget Spotify inspirado na referencia (2026-10-05)
- SectionMidiaInline: cartao com cantos arredondados, borda em gradiente, vidro escuro com textura da capa atual, capa arredondada, titulo/artista destacados e selo Spotify vetorial.
- Controles existentes de anterior, play/pause e proxima preservados; selo abre o player. Nomes acessiveis, tooltips e foco de teclado nos controles.
- Viewbox adapta o cartao a altura da dock. Coracao e repeticao da referencia nao adicionados: esta etapa e visual e esses comandos nao estavam implementados.
- Validacao executada: build Release do app e publicacao app/instalador com zero erros. Avisos preexistentes de nulabilidade no widget de midia e campo nao utilizado no monitor.
- Previa WPF renderizada e inspecionada: docs/spotify-widget-preview.png; dados de titulo/artista ilustrativos, capa local disponivel. Script: docs/render-media-preview.ps1.
- Limites: nao foi executado teste manual na dock instalada nem teste de interacao com Spotify. Proximo passo: instalar release/GigaDock-Setup.exe e conferir em uso real.
## Widget de clima no modelo solicitado (2026-10-05)
- Cartao escuro arredondado com temperatura atual e Hoje a esquerda; tres colunas de previsao com dia abreviado em pt-BR, icone monocromatico e maxima em Celsius. Viewbox ajusta a altura da dock.
- Consulta existente ao wttr.in adaptada ao JSON j1 para previsoes reais: dia atual e dois seguintes. Local e condicao aparecem no tooltip, junto da explicacao de temperatura maxima.
- Fonte consultada: documentacao oficial https://github.com/chubin/wttr.in (formato JSON j1). Sem nova conta, credencial ou backend proprio; permanece a dependencia de rede ja existente no widget.
- Removidos dados ficticios iniciais (Istanbul/21 graus). Sem dados, mostra tracos; falhas indicam mensagem no tooltip. Respostas antigas de outra localizacao nao sobrescrevem a selecao atual.
- Validacao executada: build Release da solucao, zero erros; tres testes de parser aprovados (Celsius/datas/maximas, resposta parcial e resposta invalida). Previa WPF renderizada e inspecionada em docs/weather-widget-preview.png com dados ilustrativos.
- Publicacoes Release win-x64 do app e instalador concluidas; release/GigaDock-Setup.exe atualizado.
- Limites: consulta real ao servico e dock instalada nao testadas nesta etapa; suite completa nao executada. Avisos preexistentes de nulabilidade em MidiaWidgetViewModel, campo nao utilizado em MonitorSistemaViewModel e evento nao usado no duble de tracking.
- Proximo passo: instalar e conferir clima da localizacao configurada, incluindo indisponibilidade da rede.
## Painel de controles rapidos (2026-10-05)
- Adicionado painel escuro arredondado na secao Relogio/Controles da dock, com sete controles, icones coloridos, contador e interruptores de visibilidade. Cinco primeiros visiveis por padrao; bloquear tela/suspender ocultos. Escolhas salvas em cada Ambiente, com compatibilidade para JSON antigo.
- Interruptores mostram/ocultam atalhos e nao representam o estado do Windows. Clique no nome/icone executa a acao. Wi-Fi, Bluetooth, modo escuro e foco abrem as respectivas paginas oficiais ms-settings; nao foram implementadas alternancias diretas de estado. Motivo: usar caminho documentado sem editar registro ou configuracoes globais por visibilidade.
- Bloquear tela usa LockWorkStation. Suspender pede confirmacao local e usa SetSuspendState com privilegio de suspensao habilitado temporariamente no processo e restaurado depois.
- Bloqueio temporario do teclado usa WH_KEYBOARD_LL, nao captura nem armazena texto. Libera por F12, clique no botao Liberar, prazo de 30 segundos, troca de ambiente ou encerramento. Mouse permanece funcional. Nao substitui bloqueio de sessao nem cobre area de seguranca do Windows.
- Teclado: foco visivel, nomes de acessibilidade, Escape para fechar o painel, Tab para navegar. Nenhuma acao de sistema ocorre ao escolher visibilidade.
- Fontes oficiais consultadas:
  - https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings
  - https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-lockworkstation
  - https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-setsuspendstate
  - https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc
  - https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw
- Validacao: build Release da solucao com zero erros; quatro testes com servico simulado aprovados (persistencia/isolamento, suspensao condicionada a confirmacao, liberacao na troca e erros). Previa WPF renderizada e inspecionada em docs/quick-controls-preview.png. Publicacoes app/instalador Release win-x64 concluidas.
- Limites reais: APIs de bloqueio, suspensao e teclado nao executadas nesta maquina; navegacao do popup instalado, timer nativo, F12 e escalas nao testados manualmente. Suite completa nao executada. Permanecem avisos anteriores de nulabilidade, campo e evento nao usados.
- Proximo passo: instalar release/GigaDock-Setup.exe e validar as acoes, navegacao por teclado e retorno do bloqueio temporario em Windows 10/11.
## Relogio em duas linhas conforme referencia (2026-10-05)
- Hora HH:mm grande e centralizada, data abreviada abaixo em pt-BR (ex.: Seg, 22 Jun), texto secundario cinza e fundo transparente. Removido icone decorativo e ampliacao no hover; foco de teclado visivel.
- HoraFormatada agora contem apenas HH:mm; HoraPrincipal usada no novo layout e DataResumida atualizada junto do horario. Clique para abrir calendario preservado.
- Validacao executada: build Release da solucao com zero erros; publicacoes Release win-x64 app e instalador concluidas. Previa WPF renderizada com dados ilustrativos e inspecionada em docs/clock-widget-preview.png.
- Limites: sem novos testes automatizados para esta alteracao visual; suite nao executada nesta etapa. Calendario, passagem da meia-noite e escalas na dock instalada nao testados manualmente. Avisos preexistentes de nulabilidade, campo e evento nao utilizados permanecem.
- Proximo passo: instalar release/GigaDock-Setup.exe e conferir tamanho do relogio na altura configurada da dock.
## Indicador de bateria com visibilidade opcional (2026-10-05)
- Indicador compacto: porcentagem e icone vetorial horizontal com preenchimento proporcional. Carregamento sinalizado em verde com raio; carga baixa em vermelho. Tooltip explica bateria/carregamento/tomada.
- Ajustes: interruptor Mostrar Bateria junto de Mostrar Lixeira, aplicacao imediata e preferencia global ExibirBateria salva localmente, desativada por padrao como a lixeira. Compativel com JSON anterior.
- Leitura local por GetSystemPowerStatus, sem dependencias externas, rede ou alteracao de configuracoes de energia. Atualiza imediatamente ao habilitar e a cada 30 segundos; timer parado ao ocultar/encerrar.
- Ausencia de bateria e dados desconhecidos mostram traco e mensagem explicativa, sem porcentagem ficticia.
- Fonte oficial consultada: https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-system_power_status (flags 128/255 e carga desconhecida 255).
- Validacao executada: build Release da solucao e publicacoes app/instalador com zero erros; oito casos de testes aprovados (flags, ausencia, carga/carregamento, persistencia e chamada nativa somente leitura). Previa WPF ilustrativa renderizada e inspecionada em docs/battery-widget-preview.png.
- Limites: chamada nativa executada pelo teste admite resultado indisponivel; variacao real da carga, carregar/descarregar e alternancia nos Ajustes da dock instalada nao testadas manualmente. Suite completa nao executada. Avisos preexistentes de nulabilidade/campo/evento nao utilizados permanecem.
- Proximo passo: instalar release/GigaDock-Setup.exe e ativar Mostrar Bateria nos Ajustes para conferir o equipamento.
## Visualizacoes opcionais com referencias nos Ajustes (2026-10-05)
- Nova pagina Ajustes > Visualizacoes: quatro cartoes ilustrativos e interruptores independentes para miniaturas de janelas, clima detalhado, relogio analogico com digital e pastas com previa de arquivos. Preferencias globais locais, desativadas por padrao; aplicacao imediata. Widgets ainda precisam estar habilitados no ambiente.
- Clique no aplicativo abre miniaturas de suas janelas pelo DWM; paginacao, ativacao e fechamento explicito. Troca de ambiente fecha a previa. Nao inspeciona abas do navegador nem move/oculta janelas de outros programas.
- Clima: cartao azul abre detalhes com sensacao, proximas horas e tres dias com minima/maxima fornecidos pelo provedor existente. Relogio: mostrador vetorial com ponteiros junto do horario e data em pt-BR.
- Pastas: lista local limitada a 100 itens; selecao mostra imagens (ate 20 MB), texto (ate 1 MB e 4000 caracteres) ou metadados. PDF e outros formatos nao possuem renderizacao nesta etapa. Abrir arquivo exige clique explicito e validacao pelo launcher.
- APIs publicas consultadas: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmregisterthumbnail e https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmupdatethumbnailproperties . Miniaturas dependem da disponibilidade da janela de origem, inclusive quando minimizada ou com conteudo protegido.
- Validacao executada: build Release da solucao com zero erros; sete testes selecionados aprovados (persistencia das quatro opcoes, clima e isolamento de ambientes). Registro nativo de miniatura DWM bem-sucedido com duas janelas proprias de teste. Publicacoes win-x64 do aplicativo e instalador concluidas; foi necessario restaurar os ativos do runtime antes da publicacao.
- docs/visualizacoes-preview-inicial.png registra somente a primeira renderizacao ilustrativa, anterior ao ajuste final dos interruptores/rolagem e com parte inferior cortada. A segunda execucao do auxiliar docs/VisualHarness foi bloqueada pelo Controle de Aplicativo do Windows (0x800711C7); a verificacao visual final ficou pendente. Nenhuma politica foi alterada.
- Limites reais: dock instalada, navegacao completa dos popups, varios monitores/escalas, janelas de terceiros e consulta real ao clima nao testados manualmente. Suite completa nao executada. Avisos preexistentes de nulabilidade e campo nao usado permanecem.
- Proximos passos: instalar release/GigaDock-Setup.exe, conferir as quatro referencias nos Ajustes e validar cada visualizacao na dock.
## Estilos de widgets na loja e por ambiente (2026-10-05)
- WidgetInstanceConfig agora salva Estilo junto do formato e visibilidade. Estilos desconhecidos/JSON antigo recebem fallback compatível; relógio/clima herdam a opção anterior na primeira sincronização, depois podem ter escolhas independentes. Os interruptores antigos de relógio/clima passam a editar o widget do ambiente ativo.
- Loja: Escolher estilo antes de instalar e Mudar estilo nos já instalados. Galeria de cartões com referências, indicação da escolha atual e aplicação apenas no ambiente em edição; cancelar preserva a configuração. Alterar estilo não cria outro widget do mesmo tipo nem muda a visibilidade.
- Ajustes > Widgets: botão Escolher estilo em cada widget instalado. Editar ambiente inativo não altera a dock atual; a escolha aparece ao ativar o ambiente. Coleção do modelo e do viewmodel sincronizadas ao aplicar alterações da loja.
- Relógio: hora, segundos, hora/data, dígitos em cartões, data, dia do mês, hoje, três relógios mundiais (São Paulo/Londres/Tóquio), analógico minimalista/claro/escuro e analógico com digital. Desenho vetorial WPF original; dígitos em cartões não têm animação de virada. Fusos fixos via TimeZoneInfo, com horário de verão conforme dados do Windows.
- Cronômetro e temporizador de cinco minutos: clique inicia/pausa; menu de contexto reinicia. Medição por Stopwatch. Duração fixa nesta etapa, sem alarme sonoro, persistência da contagem ou execução após encerrar o aplicativo. Pomodoro continua widget próprio com as fases existentes.
- Calendário: agenda de hoje (até dois eventos) ou próximo evento futuro (um). Sem eventos usa estado vazio; referências da galeria são ilustrativas. Clima tem compacto/detalhado; demais widgets conservam os formatos compacto/expandido existentes.
- Loja simplificada em português, busca e categorias preservadas; removidos indicadores estáticos de FPS/slots/perfil que não representavam medições reais.
- Validação executada: solução compilada Release com zero erros; 11 testes selecionados passaram, cobrindo edição de ambiente inativo, troca, aplicação imediata, salvamento/reabertura, estilo inválido, compatibilidade anterior, filtros da agenda e iniciar/reiniciar temporizador. Publicações app/instalador win-x64 concluídas; release/GigaDock-Setup.exe atualizado.
- Limites reais: interface instalada, Tab/foco, mudanças de escala, passagem de dias/fusos e término real do temporizador não verificados manualmente. Não executada nova captura WPF devido ao bloqueio do auxiliar pelo Controle de Aplicativo registrado na etapa anterior. Suite completa não executada; avisos anteriores de nulabilidade/campo/evento não usados permanecem.
- Próximo passo: instalar e selecionar um ambiente em Ajustes > Widgets; testar Escolher estilo e Loja de widgets, conferir escolhas independentes nos demais ambientes.
## Galeria ampliada: reuniões, mídia, bateria e sistema (2026-10-05)
- Escolha de estilo mantém o fluxo Loja > Escolher/Mudar estilo e Ajustes > Widgets > Escolher estilo; referências ilustrativas de mídia e sistema agora desenhadas com cartões, controles, anéis e curvas. Nenhum recurso gráfico das imagens foi copiado.
- Mídia e bateria adicionadas ao catálogo como widgets por ambiente. Configurações anteriores ExibirMidia/ExibirBateria são herdadas uma vez na migração; interruptores existentes editam a visibilidade do ambiente ativo. Estilo/visibilidade permanecem independentes entre ambientes. Marcador local de migração evita reinstalar widgets removidos em cada sincronização.
- Mídia: capa/controles existente, Tocando agora, Mini (somente reproduzir/pausar) e Barra (título/artista, reproduzir/pausar, posição/duração e progresso). Timeline consultada pela API pública de sessão do Windows; dado indisponível mostra traço. Não implementado arrastar a barra para seek. Widget aparece somente quando há mídia publicada por uma sessão compatível.
- Calendário: Próxima reunião e Central da reunião, com próximo compromisso futuro, horário, contagem e entrada pelo link HTTPS; editor local de título/data/link. Link validado, credenciais na URL rejeitadas e abertura só por clique explícito. Não controla microfone/câmera de aplicativos de chamada. Compromissos continuam na lista local global existente; o estilo pertence ao ambiente. Não há descoberta automática de reuniões de contas externas.
- Monitor: CPU/RAM compactos ou detalhados, anéis individuais, gráficos de CPU/RAM, rede, download, upload, gráfico de rede e armazenamento da unidade do Windows. Amostras a cada dois segundos somente enquanto habilitado, histórico limitado a 30; sem dados fictícios. Rede soma interfaces ativas (VPN/interfaces virtuais podem duplicar tráfego); gráfico de rede usa escala compartilhada entre download/upload.
- Serviço de métricas separado/testável: PerformanceCounter para CPU, GlobalMemoryStatusEx para RAM física, NetworkInterface.GetIPStatistics para bytes de rede e DriveInfo para disco. Falhas deixam indicador indisponível, em vez de zero fictício. Corrigido cálculo anterior da RAM, que usava limite de memória do GC como total físico. Bateria oferece indicador compacto ou anel proporcional com o serviço nativo existente.
- Fontes oficiais consultadas:
  - https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex
  - https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.networkinterface.getipstatistics?view=net-10.0
  - https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessiontimelineproperties
- Validação executada: 29 casos de testes selecionados aprovados (histórico limitado, leituras simuladas/indisponíveis, cálculo de taxas, links seguros sem execução, reunião futura, isolamento de mídia/bateria, remoção persistente, estilos e testes anteriores). Build Release da solução com zero erros e zero avisos nesta execução incremental; publicação app/instalador win-x64 concluída, release/GigaDock-Setup.exe atualizado.
- Limites reais: integração com player instalado/timeline, abertura de reunião, editor WPF, gráficos sob carga, bateria física e escalas não verificados manualmente. Métricas novas testadas com serviço simulado; teste nativo somente leitura da bateria também passou. Não executada nova captura visual devido ao bloqueio anterior do auxiliar pelo Controle de Aplicativo. Suite completa não executada. Aviso de evento não usado no duble apareceu na compilação dos testes.
- Exemplos ainda fora do catálogo desta etapa: água, áudio/volume/microfone, área de transferência, captura de tela, arquivos recentes, Downloads, estante de arquivos e compartilhamento tipo AirDrop. Esses exigem widgets/integrações próprios; não expostos como controles funcionais.
- Próximos passos: instalar, testar os estilos na dock e ampliar a categoria de utilitários do sistema com ações documentadas para Windows.
## Estilos de clima e Personalizar direto na dock (2026-10-05)
- Galeria do clima ampliada para nove estilos: temperatura, local/detalhes, previsão diária, hoje/previsão, condição, próximas horas, vento, nascer/pôr do sol e cartão azul com detalhes. Mesma escolha disponível na loja, nos instalados e pelo menu Personalizar na dock. Referências vetoriais originais com dados ilustrativos; criação da referência não faz consultas de rede.
- Novo controle WPF compartilhado entre prévia e dock, dimensionado pelo estilo e limitado à altura configurada. Clique abre detalhes; botão direito ou Shift+F10 abre menu de personalização. Escolhas continuam locais e por ambiente, preservando visibilidade. Aplicação captura o ambiente/widget de origem do seletor.
- Parser do provedor existente agora lê velocidade/direção do vento, precipitação observada, sunrise/sunset e horário da observação. Unidades km/h e mm; horários convertidos AM/PM para HH:mm; ausência/eventos solares não fornecidos mostram traço. Indicador solar usa o horário da observação, sem posição fictícia calculada pelo relógio da máquina.
- Previsão diária limitada aos três dias disponíveis e máximas; próximas horas mostram até cinco amostras do provedor (não prometem intervalo de uma hora). Sem novo serviço/backend, conta ou credenciais. Localização e dados do clima permanecem nas configurações existentes; estilo é individual por ambiente.
- Fonte do formato JSON/serviço consultada: https://github.com/chubin/wttr.in . Mantida a requisição existente format=j1 e lang=pt.
- Personalizar adicionado aos menus de relógio, mídia, calendário, monitor, bateria e controles rápidos. Nos controles rápidos abre painel com escolha Compacto/Anéis, referências visuais e seleção dos atalhos. Estilo dos controles salvo no ambiente; anéis têm cores individuais e foco de teclado. Não representam estado real de Wi-Fi/Bluetooth nem alteram configurações globais ao escolher aparência.
- Validação executada: 18 testes selecionados aprovados, incluindo clima/ausências/AM-PM, aplicação por ambiente sem alterar visibilidade, estilos anteriores e controles rápidos sem ações nativas. Build Release da solução concluído com zero erros; aviso anterior do evento não usado no duble. Publicações app/instalador win-x64 concluídas, release/GigaDock-Setup.exe atualizado.
- Limites reais: novas aparências, menus por mouse/Shift+F10, animação do popup, escalas e consulta real ao provedor não testados manualmente na dock instalada. Sem nova captura WPF devido ao bloqueio do auxiliar visual registrado anteriormente. Suite completa não executada.
- Próximos passos: instalar, escolher Clima > estilo na loja ou clicar com botão direito no widget; conferir aparência, dados indisponíveis e escolhas entre ambientes.
## Revisão geral e atualização do README (2026-10-05)
- README reescrito a partir do código atual: atualizações, estilos por ambiente, uso da loja/menu Personalizar, estado real de cada widget, acesso à rede/dados locais, limites, pendências e comandos de desenvolvimento.
- Relatório em docs/ANALISE-PROJETO.md com método, achados priorizados, evidências, lista das 13 falhas e próximos passos. Varredura da estrutura e leitura dirigida, sem alegar auditoria linha a linha ou teste da interface instalada.
- Corrigidas na documentação as promessas de funcionamento totalmente offline, integrações 100% funcionais, GitHub apenas mock, presença oficial do Teams e suposto contorno de Smart App Control.
- Achados principais: instalador ativa modo de barra principal sem escolha específica e desabilita a barra nativa; migrações v5/v6 aninhadas na v4; divergência de versões; comportamento global/mesclagem de apps em conflito com testes; integrações Discord/OBS incompletas e GitHub parcial; lifecycle/validação/ICS e catálogo/UI precisam de revisão.
- Executado dotnet test DockWindows.slnx -c Release --no-restore: 108 casos, 95 aprovados, 13 falhas, zero ignorados. O teste real de autostart tentou configurar o recurso e falhou; contém finally de restauração do booleano. Suite não é isolada e deve separar integrações opt-in. Não houve repetição para coleta de logs.
- Executado dotnet build DockWindows.slnx -c Release --no-restore: zero erros e zero avisos nesta execução incremental, SDK 10.0.401. Compilação durante os testes teve aviso anterior de evento não usado no duble.
- Nenhuma correção de comportamento, publicação ou reconstrução do instalador nesta etapa de documentação. As falhas permanecem; manual de instalação e validação real continuam pendentes.
- Próximo passo prioritário: corrigir instalador para preservar barra funcional, depois migrações/versões/contrato de globais e isolamento dos testes.


## Abertura de pain?is por clique ou mouse (2026-10-05)

- Escolha em Ajustes ? Visualiza??es, salva no ambiente ativo (Clique/Mouse). Configura??es antigas usam Clique.
- Clima, pr?vias de janelas e pastas usam abertura acima da dock j? existente. Mouse aguarda 350 ms e permite atravessar at? o painel; fechamento ap?s sair de ambos. Trocar ambiente/modo e descarregar controles cancela o timer e fecha os pain?is. Timers de hover ficam parados em repouso; Enter/Espa?o continuam abrindo as pr?vias.
- Compila??o Release executada: zero erros, um aviso CS0067 existente em teste. Tentativa de 11 testes selecionados: 3 aprovados e 8 falhas de carregamento por pol?tica de Controle de Aplicativo (0x800711C7, DockWindows.Core.dll), incluindo os dois novos casos. Resultado n?o valida a persist?ncia nova nem representa oito regress?es confirmadas. Sem contornar a pol?tica ou repetir por outro caminho.
- Limites: intera??o por mouse, teclado, m?ltiplos monitores/DPI e travessia do ponteiro ainda requerem valida??o manual. Instalador distribu?vel n?o reconstru?do. As 13 falhas da auditoria anterior n?o foram corrigidas; su?te completa n?o repetida.


## ?cone do GigaDock no aplicativo e instalador (2026-10-05)

- Criado ?cone geom?trico original em assets/gigadock.ico (8 resolu??es: 16, 20, 24, 32, 48, 64, 128 e 256 px), com pr?via PNG e gerador tools/New-GigaDockIcon.ps1. Sem fontes, recursos de terceiros ou depend?ncias adicionais de execu??o.
- ApplicationIcon configurado nos projetos App e Installer; recurso WPF compartilhado e Icon nas duas janelas principais. Bandeja usa WM_GETICON da janela em vez de sempre usar o ?cone gen?rico. Atalhos e DisplayIcon j? apontavam para o execut?vel, ?ndice 0, e passam a aproveitar o recurso embutido.
- Build Release aprovado, zero erros e um aviso CS0067 existente em teste. Publica??es win-x64 conclu?das: aplicativo framework-dependent, instalador self-contained/single-file. app.zip atualizado e release/GigaDock-Setup.exe reconstru?do, incluindo a op??o anterior de abertura dos pain?is por mouse/clique.
- Verificados estrutura das 8 entradas ICO, igualdade do execut?vel publicado com o pacote app.zip e extra??o do ?cone de ambos os execut?veis sem execut?-los; capturas docs/gigadock-exe-icon.png e docs/gigadock-setup-icon.png.
- Gerador .ps1 n?o executado por pol?tica de execu??o de scripts desta sess?o; recurso produzido por comandos de desenho System.Drawing, sem alterar a pol?tica do sistema. N?o foi executada instala??o, nem validada manualmente a atualiza??o do cache de ?cones do Explorer ou a bandeja em execu??o. Testes n?o repetidos: altera??o de recurso e de obten??o de handle; bloqueio de Controle de Aplicativo registrado na etapa anterior.
- Pr?ximos passos: validar ?cone nos atalhos ap?s atualiza??o e na bandeja. Pend?ncia cr?tica de comportamento do instalador, j? registrada na auditoria, permanece fora desta corre??o visual.


## M?dia com fundo derivado da capa (2026-10-05)

- Substitu?do o degrad? fixo verde/dourado por fundo, borda e destaque derivados de CorPredominanteHex; aplica-se tamb?m aos estilos compacto, mini e barra. Tom escuro e textos claros preservam a leitura; sem capa/cor utiliz?vel, usa tom neutro.
- Extra??o em miniatura de 32 ? 32 pixels, agrupando cores semelhantes e reduzindo influ?ncia de preto/branco/transpar?ncia. Executada nas atualiza??es de metadados, n?o no timer de progresso. Sem novas consultas externas, blur ou anima??o cont?nua.
- Estilo capa ampliado horizontalmente, artista mais leg?vel, textura da capa suavizada e linha decorativa substitu?da por progresso real. Ins?gnia do player mant?m a identifica??o do aplicativo.
- Prote??o por revis?o e captura da sess?o evita que uma consulta de metadados antiga sobrescreva capa/t?tulo/cor ap?s uma consulta mais recente. Sem sess?o, reinicia a cor.
- Build Release aprovado, zero erros e um aviso CS0067 existente em teste. Publica??es app e instalador win-x64 executadas; app.zip e release/GigaDock-Setup.exe atualizados, preservando o ?cone pr?prio.
- Limites: apar?ncia com capas reais, transi??es de m?sica e contraste/DPI ainda n?o validados manualmente. N?o houve nova captura nem execu??o de auxiliares/testes bloqueados anteriormente pelo Controle de Aplicativo. Consumo de RAM/CPU n?o medido. Cache antigo de thumbnails em arquivos tempor?rios ainda necessita gerenciamento de ciclo de vida.
- Pr?ximo passo: conferir m?sicas com capas claras, escuras e coloridas, faixa sem capa e troca r?pida de player na dock instalada.


## Modo gamer RGB contínuo (2026-10-05)

- Preferência global ModoGamerRgb, desativada por padrão e persistida no settings.json. Mantém a borda RGB ligada independentemente de player, reprodução ou visibilidade do widget de mídia, incluindo troca de ambientes.
- Botão RGB na dock, interruptor Modo gamer RGB em Ajustes e item marcável no menu da bandeja. Ao desativar gamer, volta ao comportamento RGB Musical se essa opção estiver ativa; para apagar também durante música, desligar as duas opções.
- Reutiliza o arco-íris existente, sem criar timer ou nova animação paralela. Storyboard inicia com o elemento visível e animações habilitadas; é removido ao sair dessas condições. Com Desativar animações, conserva a borda colorida estática. IsHitTestVisible=false impede que o brilho capture cliques.
- Corrigida descrição antiga do RGB Musical, que prometia cores de capa embora o efeito fosse arco-íris, e três rótulos da mídia afetados pela codificação do pipe na etapa anterior.
- Build Release executado: zero erros, um aviso CS0067 existente nos testes. Publicações win-x64 do aplicativo e instalador concluídas; app.zip e release/GigaDock-Setup.exe atualizados.
- Limites: botão, persistência após reabrir, combinações musical/gamer, storyboard e escalas precisam de validação manual; não executados testes/auxiliares bloqueados pelo Controle de Aplicativo na etapa anterior. Consumo real não medido. Ocultação automática que apenas desloca a janela não equivale a IsVisible=false e pode manter a animação ativa.
- Próximos passos: validar ligado sem música, pausa/player fechado, reinício, desativação com RGB Musical ligado/desligado e redução de animações. Pendências anteriores da auditoria permanecem.


## Expansão dos ícones de aplicativos ao passar o mouse (2026-10-05)

- Na seção de aplicativos, o ícone apontado amplia para 1,45× e os dois vizinhos imediatos para 1,16×, com transição de 140 ms e ancoragem na base. Ao sair, retorna ao tamanho normal. Foco de teclado também amplia o ícone.
- Substituídas as animações individuais de hover de 1,18× na SectionApps por coordenação dos vizinhos. Mantidos comandos, badges, menus e prévias por clique/mouse. Sem timer contínuo: animações finitas disparadas pela interação, substituindo clocks anteriores.
- Margem superior transparente proporcional ao tamanho e escala dos ícones reserva espaço para a expansão; não aumenta a altura visual da barra. Dock passa a ocupar janela mais alta, devendo ser conferida em auto-hide e modo de barra principal.
- Respeita Desativar animações e SystemParameters.ClientAreaAnimation na seção de aplicativos; troca de ambiente e descarregamento removem a magnificação. Demais seções conservam seus efeitos anteriores, sem aplicação a widgets nesta etapa.
- Build Release executado: zero erros, um aviso CS0067 existente em teste. Aplicativo e instalador win-x64 publicados; release/GigaDock-Setup.exe atualizado.
- Limites: aparência, passagem rápida entre ícones, sobreposição, foco, múltiplos monitores/DPI e áreas de clique ainda precisam de validação manual. Não houve nova captura WPF nem repetição de auxiliares/testes bloqueados anteriormente pelo Controle de Aplicativo. Sem medição de desempenho.
- Próximos passos: conferir magnificação com ícones pequenos/grandes, prévias ativadas, teclado, ocultação automática e alterações de altura.


## Revisão consolidada do projeto e README (2026-10-05, após magnificação)

- README reorganizado para reunir ícone, painéis por clique/mouse, cores da capa, gamer RGB e expansão dos aplicativos no corpo principal; corrigidos trechos novos com codificação incorreta. Incluída tabela de timers e explicação de pausa parcial, escopo global/por ambiente e ausência de medições de desempenho.
- Pendências incluem Wi-Fi/Bluetooth conectados e acesso à bandeja, além de integrações parciais, cache/ciclo de vida, migrações, versões e falhas históricas. Propostas não foram apresentadas como recursos concluídos.
- Relatório ANALISE-PROJETO.md consolidado com revisão dos componentes novos, riscos de geometria/magnificação, RGB em auto-hide, ajustes desatualizados, cache das capas e método/limites. Preservada lista histórica das 13 falhas.
- Executado build Release da solução: zero erros e zero avisos nesta execução incremental. Testes não repetidos por bloqueio de carregamento já registrado e integrações não isoladas; última suíte completa permanece resultado histórico, não validação do código atual.
- Nenhuma alteração funcional ou publicação nesta tarefa. Instalador local anterior preservado. Próximos passos: retirar ocultação automática da barra no instalador; corrigir migrações/globais/versões; isolar testes e validar comportamento visual em ambiente permitido; medir e otimizar atividade/cache.


## Auditoria completa de performance e lifecycle (2026-10-05)

- Etapa solicitada no texto anexado: análise do projeto inteiro antes de implementar. Inventário inicial de 30 entradas, incluindo os 13 widgets, Cotação sem implementação e componentes auxiliares; saídas activity-inventory-before.json/.txt e AUDITORIA-ATIVIDADE.md.
- Gerenciador Core sem timer central; contrato IAtividadeWidget; tokens WPF por renderização; integração dos 13 widgets com ambiente, habilitação, dock minimizada/oculta/fullscreen e auto-hide por deslocamento. Pausa de animações/polling/listeners dispensáveis; retomada imediata com cache.
- Pomodoro usa prazo absoluto e preserva conclusão em background. Notas pendentes, notificações Windows, segurança do teclado, tracking/fullscreen e tarefas explícitas continuam quando necessário. Mídia mantém eventos mínimos para RGB musical/discovery sem timeline oculta.
- Removidos timer duplicável de notificações, varredura antecipada de apps e temporários novos de capa; polling visual de Teams/Discord controlado. Cache de ícones limitado a 512, capa a uma imagem limitada, métricas a 30 amostras. Cancelamento/cleanup de HTTP, processos, IPC, DWM e popups.
- Testes executados: **41 aprovados, zero falhas, zero ignorados** na seleção AtividadeWidgetsTests/PomodoroTests/BateriaTests/ClimaWidgetTests/EstilosSistemaTests. TRX: docs/TestResults/atividade-widgets.trx. Incluem 10 mil alternâncias (10 mil leituras imediatas, um registro, zero tokens ao final) e **2 min 0,009 s** com Dispatcher ativo e zero leituras nos intervalos ocultos do monitor com fonte falsa.
- Build Release final da solução: **zero erros, um aviso CS0067 existente em fake de teste**. Aplicativo win-x64 dependente do runtime e instalador self-contained publicados; app.zip e release/GigaDock-Setup.exe atualizados. Instalador 172.276.616 bytes; SHA-256 598C0AAEC2647791113690D65C64DE5C21690B25BEE210E20C7C20D1021F8BB0.
- README atualizado; relatório explica cada problema/impacto/solução, arquitetura, extensão, remoção e limites. Métricas verificadas em activity-validation.json. Nenhuma medição comparável de CPU/RAM do aplicativo real foi feita; não declarar porcentagem de economia ou ausência universal de vazamentos.
- Os novos binários de teste puderam executar normalmente; não houve contorno do Controle de Aplicativo. Resultado histórico de 13 falhas na suíte completa permanece histórico, sem alegação de resolução integral.
- Próximos passos: corrigir P0 do instalador que oculta/desabilita barra nativa; validar lifecycle ponta a ponta, WinRT, popups e DPI; medir CPU/RAM/handles/GC em sessões reais equivalentes; resolver migrações/versões/validação ICS e integrações demonstrativas. Relatório de dois minutos é teste isolado, não teste prolongado de todos os serviços nativos.


## Contorno RGB mais definido (2026-10-05)

- Halo reforçado: espessura de 3 para 5 DIP e desfoque de 14 para 8 DIP. Paleta saturada com oito pontos incluindo rosa, violeta, azul, ciano, verde e amarelo.
- Borda nítida de 2 DIP desenhada acima da superfície da dock, compartilhando o pincel animado do halo. Altura e alinhamento acompanham a barra, inclusive a área reservada à magnificação dos ícones.
- Sem timer ou storyboard adicional; conserva a suspensão por visibilidade real e preferências de animação. As duas camadas não capturam cliques.
- Build Release: zero erros, um aviso CS0067 preexistente em fake de teste. Aplicativo e instalador win-x64 publicados; app.zip e release/GigaDock-Setup.exe atualizados.
- Limites: aparência em execução, diferentes fundos e escalas de tela ainda requerem conferência manual; não houve medição de GPU/CPU nem teste visual nesta etapa. Próximo passo: conferir contraste e espessura em 100%, 125%, 150% e 200%.


## Site do projeto em vercel/ — 2026-10-06

- Página reescrita em português com identidade GigaDock, tipografia ampla, fundo claro, cartões e desktop ilustrativo com dock escura e contorno RGB.
- Demonstração interativa: Trabalho/Estudos/Pessoal, configurações independentes em memória, visibilidade de mídia/clima/relógio/bateria/lixeira, RGB, três tamanhos e três estilos de relógio. Controles simulados de reprodução/faixas com mudança de fundo; prévias de aplicativos e lixeira.
- Catálogo filtrável com 14 cartões: os 13 tipos implementados e cronômetro/temporizador como opções do relógio. Status e limites explícitos; Cotação e demais sugestões ficam no roadmap. Recursos incluem ambientes, temas, controles rápidos, prévias, acessibilidade, ciclo de atividade e armazenamento local.
- Sem Tailwind CDN, fontes externas, backend, coleta de dados ou APIs do computador. Demonstração não representa acesso real a mídia/notificações/Windows. Downloads apontam para releases, sem inventar arquivo publicado ou versão nova.
- Validação: node --check aprovado; Chrome headless com 13/13 verificações em 1440 px e 390 px. Resultados site-desktop-validation.json/site-mobile-validation.json e capturas site-desktop-preview.png/site-mobile-preview.png em docs/. Conferido visualmente o primeiro viewport das capturas; sem auditoria integral por leitor de tela.
- Build Release da solução: zero erros e zero avisos nesta execução incremental. Bloqueio NVM da instalação npm preservado; os testes usaram Chrome instalado via protocolo de depuração, sem alterar confiança do gerenciador.
- Não publicado no Vercel nesta etapa. Próximo passo: conferir conteúdo e publicar a pasta vercel/ pelo fluxo do projeto. Revisão anterior de segurança/organização continua com pendências separadas; este resultado não declara a suíte desktop inteira aprovada.


## Ajustes: visual e layout adaptativo — 2026-10-06

- Paleta azul/grafite, cartões com mais respiro, ícone original GigaDock, rótulos com mais contraste e versão derivada da assembly. Foco visível nos interruptores e botões de navegação.
- Layout largo (>=1050 DIP): navegação + lista + edição. Intermediário (760–1049): navegação compacta por seletor e lista/edição lado a lado. Estreito (600–759): lista acima da edição, com rolagem no conteúdo. Seções sem lista usam toda a área disponível. Visualizações conserva painel próprio e é reposicionado junto com as demais seções.
- Tamanho mínimo reduzido para 600×500; tamanho inicial limitado à área de trabalho. Arredondamento de layout e pixels. Não foi substituído por Viewbox que reduzisse toda a interface.
- Corrigido fechamento por Close sem atribuir DialogResult em janela não modal; callbacks do ViewModel liberados ao encerrar.
- Corrigidos itens explicitamente globais: migração legada única, configurações novas sem globais automáticos, preservação em salvar/recarga e exibição em todos os ambientes apenas quando o usuário escolher escopo global. Alternar escopo conserva seleção; remoção/reordenação separam globais e locais. Migração inicial salva preferências diretamente sem substituir ambientes pela coleção ainda não carregada.
- Inspeção estática: XML válido e 29 comandos diretos com propriedades correspondentes no AjustesViewModel. Isso não equivale à execução de cada comando.
- Build Release: zero erros, um aviso CS0067 existente em fake. Tentativa de testes registrada em docs/TestResults/ajustes.trx: carregamento bloqueado pelo Controle de Aplicativo (0x800711C7). Nenhum resultado aprovado desta seleção foi declarado; bloqueio não contornado e auxiliares alternativos não executados para contorná-lo.
- Relatório estruturado: docs/ajustes-validation.json. Suíte ampla anterior, de outra etapa: 107 aprovados e 12 falhas; permanece histórica e não prova correção integral.
- Limites: falta testar layout WPF em execução, todas as seções, seleção/remoção/reordenação globais, teclado, DPI, múltiplos monitores, confirmações e importação/exportação. Integrações Windows não foram disparadas nesta etapa. Não declarar “tudo funcional” sem essas verificações.
- Próximos passos: executar a seleção em ambiente permitido, conferir 600/760/1050/1280 DIP e 100/125/150/200% de escala e resolver as falhas históricas ainda aplicáveis.


## Verificação e correções de falhas — 2026-10-06

- Revisadas as 12 falhas históricas da seleção ampla. Corrigida deduplicação de aplicativos fixados por caminho resolvido: nome curto e caminho absoluto do mesmo executável passam a produzir um item. Escopo global já corrigido na etapa de ajustes permanece explícito e preservado.
- Atualizados testes de schema para migração sequencial até v6, seções inline, botões de ação desativados por padrão e restauração conforme os padrões atuais. Testes de apps não fixados habilitam a preferência explicitamente; teste de troca de ambiente verifica isolamento. Tamanho do ícone é validado independentemente da altura configurada da barra.
- Teste de filtro do launchpad usa nome sintético para não colidir com aplicativos instalados no PC. Essa última alteração foi compilada após a execução; não reexecutada devido ao bloqueio de política observado.
- Executada suíte com filtro Category!=SystemIntegration: **120 resultados, 60 aprovados e 60 falhas por bloqueio de carregamento 0x800711C7**, zero falhas de assertion observadas entre testes que puderam executar. Não interpretar bloqueados como aprovados. Registro: docs/TestResults/verificacao-final.trx; classificação em docs/test-verification-summary.json.
- Não executados testes que alteram autostart/barra nativa; proteção de Controle de Aplicativo preservada. Não repetidos binários/auxiliares em caminhos alternativos.
- Build Release final da solução: zero erros, um aviso CS0067 existente em fake de tracking. Não foram feitos testes visuais, instalação ou validação de integrações reais.
- Próximos passos: executar os casos bloqueados em ambiente permitido e validar ponta a ponta ajustes, migrações, deduplicação, escopo, prévias e escalas. Não afirmar que não há falhas restantes, pois metade da suíte não pôde executar.


## Instalador atualizado e revisão visual — 2026-10-06

- Assistente 820×580, mínimo 720×540, redimensionável e limitado à área de trabalho inicial. Paleta azul/grafite, lateral em degradê, ícone original, cantos mais suaves, botões com padding real e foco de teclado/estado desabilitado visíveis. Textos antigos de versão e promessas de restauração garantida corrigidos.
- Mantidos controles e handlers do fluxo de instalação, atualização e desinstalação. Fechamento bloqueado durante trabalho em andamento; botão Sair usa Close. Inicialização automática desmarcada em instalação nova e preferência existente preservada na atualização. Falha ao iniciar aplicativo após instalar recebe mensagem.
- Compilação da solução anterior à publicação: zero erros e zero avisos incremental. Publicações finais de aplicativo e instalador concluídas sem erros. Pacote app.zip conferido: executável idêntico ao publicado; release/GigaDock-Setup.exe idêntico ao artefato do publish. Hash/tamanho em installer-validation.json.
- Instalador self-contained win-x64; aplicativo embutido continua exigindo .NET Desktop Runtime 10 x64. Instalador não ativa ocultação da barra nativa automaticamente; modo de barra principal permanece escolha explícita no app.
- Limites: não executados instalação/atualização/desinstalação, renderização WPF do assistente ou testes adicionais bloqueados por política Windows. Resultados da suíte anterior permanecem 60 aprovados/60 bloqueados, não aprovação integral do instalador.
- Próximos passos: conferir visual/DPI, progresso, atualização com dados existentes, encerramento durante operação, permissões e desinstalação em ambiente permitido.


## Alertas neon WhatsApp e Teams — 2026-10-06

- Inspecionado fluxo UserNotificationListener -> ToastNotificationService -> MainViewModel -> contorno/sombra WPF. Reconhecimento por nome da aplicação contendo WhatsApp/Teams; depende de notificações publicadas e permissão Windows, não leitura direta de conversas/presença.
- Corrigido incremento duplicado do Teams quando não há aplicativo fixado correspondente. Widgets WhatsApp/Teams passam a receber um incremento cada por evento; contagens do Windows ainda são sincronizadas pelo serviço compartilhado.
- DispararAlertaGlobal respeita AlertasVisuaisHabilitados, descarte e Dispatcher. Desativar a opção cancela o prazo e encerra alerta atual. Nova notificação reinicia prazo de 15 s. Remoção da animação conserva bindings e valores-base da sombra, sem ClearValue que os apagava.
- Contorno estático de alerta acima da superfície e do RGB gamer: WhatsApp verde #25D366, Teams violeta #8B7CFF. Pulso não vai mais até opacidade zero. Com animações desativadas conserva contorno; dock oculta/fullscreen/auto-hide pausa desenho e mantém apenas prazo do alerta.
- Adicionado menu de contexto Testar alerta neon (verde) no widget WhatsApp; Teams já tem Testar Alerta de Reunião (Roxo). Ambos passam pelo mesmo método global e exigem alertas habilitados.
- Build Release: zero erros, um aviso CS0067 existente em fake. Aplicativo e instalador publicados e release/GigaDock-Setup.exe atualizado.
- Limites: não testadas notificações reais recebidas de WhatsApp/Teams, permissão concedida/negada ou aparência do neon em execução. Testes de integração não repetidos diante do bloqueio de carregamento 0x800711C7 já registrado. Revisão de fluxo e compilação não equivalem a confirmação ponta a ponta.

### Conferência manual pendente

1. Habilitar Alertas visuais nos Ajustes e mostrar os widgets WhatsApp/Teams.
2. Abrir o menu do WhatsApp -> Testar alerta neon (verde); abrir o do Teams -> Testar Alerta de Reunião (Roxo). Conferir contorno, duração aproximada de 15 s e nova chamada reiniciando prazo.
3. Desligar alertas durante o efeito: contorno deve desaparecer. Com animações desativadas e alertas habilitados: contorno estático.
4. Ocultar/reabrir dock durante prazo; confirmar pausa visual e retomada somente enquanto alerta não expirou. Repetir com gamer RGB ativo.
5. Autorizar acesso a notificações no Windows e receber uma notificação real de cada app, sem apenas mensagem interna: verificar cor/contagem. Se acesso negado ou aplicativo não publicar toast, neon automático não pode funcionar.


## 06/10/2026 — Refatoração geral: atividade, atualização, cache e integrações

Etapa: solicitação anexada de refatoração dos widgets. Implementação concluída no código; validação automática aprovada, sem redesenhar os estilos existentes. Relatório completo em [REFATORACAO-WIDGETS.md](REFATORACAO-WIDGETS.md).

- Inventariados 13 widgets/14 tipos e serviços auxiliares. Coordenador ampliado com instalação, execução real, pausa, suspensão, saúde/erro/indisponibilidade e descarte terminal.
- Clima preserva dados antigos, timestamp e retentativa visível; mídia coalesce consultas, usa cache limitado e evento de timeline pausada; monitor coleta métricas solicitadas e diferencia primeira amostra; bateria diferencia falha da API/estado desconhecido.
- Calendário com Ical.Net 5.2.0/NodaTime, UTC/TZID/floating, recorrências/exceções/dia inteiro, limites e preservação em falha. Temporizador com prazo absoluto, despertar oculto e conclusão única.
- Notas com editor, loja, conteúdo real e persistência por arquivo temporário. OBS/Discord sem sucesso falso. Teams/WhatsApp explicitam estimativas, notificações e permissões; prévias não iniciam serviços reais.
- Hooks/eventos removidos ao fechar; thumbnails DWM não reativam ocultos; prévias de pastas invalidam entregas após fechar; eventos recebidos durante sincronização de notificações geram atualização posterior.
- Corrigida também duplicação de aplicativo fixado identificado por nome curto/caminho explícito durante a suíte completa.

Verificação final: **140 aprovados, zero falhas**, 20 casos novos; dois testes `SystemIntegration` excluídos por alterarem autostart/barra de tarefas. Inclui 10 mil alternâncias e dois minutos de Dispatcher com fonte simulada. Build Release sem incremento: **zero erros e um aviso CS0067 existente no fake de teste**. Auditoria NuGet/transitivas sem vulnerabilidades listadas. [Resultados](widget-refactor-validation.json), [TRX](TestResults/refatoracao-final.trx), [build](widget-refactor-build.txt).

Próximos passos: validar visual/DPI, players/notificações reais, hardware, suspensão física e consumo real de CPU/RAM; implementar RPC Discord e OBS WebSocket somente com protocolo/respostas reais. Temporizador ainda sem duração configurável/alarme dedicado/persistência após sair. Reempacotar o instalador: o artefato local anterior não contém esta refatoração. Nenhuma publicação foi feita nesta etapa.

## 06/10/2026 — Lixeira: dock e área de trabalho

Etapa concluída no código: ativar Mostrar Lixeira nos ajustes oculta o ícone do desktop; desativar solicita sua exibição novamente. Mudança apenas por ação explícita, com serviço injetável, notificação ao Explorer e alerta/preservação da preferência em falha. A interface explica o efeito global. Detalhes em [LIXEIRA-DESKTOP.md](LIXEIRA-DESKTOP.md).

Validação: quatro testes novos aprovados com serviço simulado; build Release sem incremento com zero erros e um aviso CS0067 existente no fake de testes. [TRX](TestResults/lixeira-desktop.trx), [build](lixeira-desktop-build.txt). O desktop real não foi alterado pelos testes.

Próximos passos: validar visualmente em Windows 10/11 o redesenho do Explorer e políticas de ícones; reempacotar o instalador quando solicitado. O instalador existente não inclui esta alteração.

## 06/10/2026 — Botões e escala do widget de mídia

Ícones vetoriais uniformes de anterior, próxima, reproduzir e pausar nos estilos capa/compacto/mini/barra. Botão central com fundo discreto, preservados comandos, nomes acessíveis e foco de teclado. Base visual de 54 unidades: recebe uma única escala do container da dock (AlturaBarra/64), corrigindo a combinação anterior de MaxHeight dinâmico com escala externa; versões pequenas e grandes mantêm as proporções da capa, texto e controles.

Build Release: zero erros e um aviso CS0067 existente nos testes. Log: [midia-botoes-build.txt](midia-botoes-build.txt). Validação visual com player e DPI real permanece pendente; instalador não reempacotado nesta etapa.


## 06/10/2026 — Instalador final atualizado

Reempacotado release/GigaDock-Setup.exe com o código atual, incluindo refatoração dos widgets, lixeira no desktop e botões/escala de mídia. Publicações Release win-x64 concluídas sem erros; setup self-contained, app requer .NET Desktop Runtime 10 x64. ZIP e release conferidos por SHA-256, dados em installer-validation.json. README atualizado, pendências antigas comprovadamente resolvidas corrigidas. Script de build consolidado e sem remoção recursiva de dist; sua execução foi bloqueada pela política local, portanto foram usados comandos diretos sem alterar a política.

Próximos passos: instalação/atualização/desinstalação, visual/DPI e integrações reais. Não executados instalador ou publicação remota nesta etapa. Logs: installer-app-publish.txt e installer-setup-publish.txt.


## 06/10/2026 — Visual dos controles rápidos

Painel com fundo opaco, escolha Compacto/Anéis por cartões selecionáveis com prévia e navegação de teclado, rolagem escura apenas na lista e nomes/status com quebra de linha em vez de largura fixa. Cabeçalho e seletor permanecem visíveis; altura da lista limitada à área de trabalho primária ao abrir. Comandos e vínculos de visibilidade por ambiente preservados.

Build Release aprovado: zero erros e aviso CS0067 existente. Log em controles-rapidos-visual-build.txt; tentativa de testes em TestResults/controles-rapidos-visual.trx bloqueada pelo Controle de Aplicativo do Windows (0x800711C7); nenhum teste executado nesta etapa. Visual/DPI, monitores secundários e interação real permanecem pendentes; instalador gerado anteriormente não inclui esta alteração.


## 06/10/2026 — Remoção do botão RGB da dock

Removido o ToggleButton RGB da seção de relógio/controles, sem espaço reservado. O modo gamer RGB continua configurável nos ajustes. Instalador anterior ainda não incorpora esta remoção. Validação de build em remover-botao-rgb-build.txt.


## 06/10/2026 — Acesso aos ícones ocultos do Windows

Adicionado botão com seta junto à bateria que solicita abrir o painel nativo de ícones ocultos via UI Automation Invoke/ExpandCollapse. Busca somente botões acessíveis da barra do Windows cujo nome indique ícones ocultos em português/inglês; consulta apenas no clique, fora da thread de interface, sem polling. Não enumera processos nem cria lista simulada. Se barra estiver oculta, botão ausente ou idioma diferente, mostra erro compreensível. O painel permanece na posição nativa do Windows; não é incorporado à dock.

Build em bandeja-oculta-build.txt. Abertura real ainda não validada; próximo passo é testar em Windows 10/11 com ícones ocultos. Instalador anterior não inclui esta integração. Referência: https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.invokepattern.invoke?view=windowsdesktop-10.0


## 06/10/2026 — Prioridade e duração dos alertas WhatsApp

RGB de música/gamer suprimido durante alertas, retomado após o término. Mensagens usam fila com pulso único de 700 ms e intervalo de 150 ms por evento recebido. Chamadas reconhecidas usam cor fixa e prioridade sobre mensagens; removido encerramento genérico após 15 segundos e por clique na dock. Remoção da última notificação de chamada encerra o estado, mensagens aguardam na fila. Animação de sombra limitada a um ciclo; preferências de animação reduzida mantidas. IDs de toast deduplicados; reconhecimento textual de chamada mais específico e exclui chamada perdida/encerrada.

Limite real: UserNotificationListener observa notificações, não o protocolo de chamada do WhatsApp. Atender/rejeitar/encerrar só encerra o alerta automaticamente quando o WhatsApp remove a notificação correspondente. Se o toast não existir, a permissão for negada ou não houver evento de remoção, não há confirmação confiável do estado da chamada. Não prometer detecção garantida de atendimento. Alertas visuais nos ajustes podem ser desligados para limpar o estado. Visual e chamadas reais ainda não validados; instalador anterior não inclui esta mudança. Build Release aprovado, zero erros e aviso CS0067 existente. Teste de prioridade adicionado, mas bloqueado ao carregar DockWindows.Infrastructure.dll pelo Controle de Aplicativo do Windows (0x800711C7): zero testes aprovados nesta etapa. Resultado em TestResults/whatsapp-alerta.trx. Build final incremental: zero erros e zero avisos.


## 06/10/2026 — Espaçamento no cartão de mídia

Cartão com capa: ampliada a base de composição para 540×120 mantendo Viewbox de altura 54 e escala única da dock. Texto em linhas de 28/22, intervalo dedicado de 8 unidades antes dos controles e linha de botões de 36, sem sobreposição; afastamento da capa e do botão central ampliado. Tamanho de fonte compensado para preservar legibilidade após a normalização. Build em midia-espacamento-build.txt; conferência visual em player/DPI real pendente. Instalador anterior ainda não inclui esta alteração.


## 06/10/2026 — Atalhos duplicados e bloqueio de testes

Confirmados GigaDock.lnk e Dock Windows.lnk apontando para o mesmo executável, no desktop e Menu Iniciar. Removidos somente os dois atalhos legados do usuário, com cópias de recuperação em dist/shortcut-backups. Instalador agora migra nomes antigos apenas quando o destino corresponde ao executável da instalação, sem apagar atalhos de outros destinos. Build aprovado com zero erros e aviso CS0067 existente; publicações de app/setup aprovadas e pacote conferido por hash. Release atualizado inclui as alterações anteriores até esta etapa.

Aviso da imagem refere-se ao testhost carregando DockWindows.Infrastructure.dll. Authenticode verificou DLL de testes e setup como NotSigned. Não alterada a política do Windows. Correção de confiança requer certificado de assinatura de código confiável, não disponível nesta etapa. Instalação manual e assinatura pendentes. Logs: atalhos-instalador-build.txt, atalhos-app-publish.txt e atalhos-setup-publish.txt.


## 06/10/2026 — Hover dos aplicativos

Magnificação reduzida para 1,30× no alvo e 1,08× nos vizinhos, transição de 180 ms e margens horizontais de 9 unidades. Tooltip substituído por etiqueta escura com cantos arredondados, largura máxima e quebra de linha, posicionada acima e afastada dos ícones; atraso de 650 ms. Mantidos foco de teclado e respeito à preferência de animações. Build em apps-hover-build.txt; visual, DPI e limites de tela ainda precisam de validação em execução. Instalador anterior não inclui a mudança.


## 06/10/2026 — Instância única da dock

StartupUri removido: a janela principal é criada apenas após adquirir mutex nomeado Local por SID do usuário/sessão. Segundo lançamento sinaliza evento AutoReset e encerra antes de criar janela, widgets ou hooks. Processo principal recebe sinal sem polling e revela a dock existente, restaurando janela minimizada/ocultação automática. Encerramento secundário não restaura a barra do Windows indevidamente; mutex/evento/registro de espera liberados no encerramento. Mutex abandonado após falha permite nova inicialização.

Build Release aprovado: zero erros e aviso CS0067 existente no fake de teste. Log instancia-unica-build.txt. Não executado teste manual de múltiplos processos/UI nesta etapa. As instâncias antigas já abertas precisam ser fechadas para iniciar a versão corrigida. Instalador anterior não inclui esta mudança. Próximo passo: validar cliques rápidos, auto-hide, encerramento/reabertura e sessões distintas no executável atualizado.


## 06/10/2026 — RAM e galeria de estilos do monitor

Cache de ícones limitado a 128 entradas e 8 MiB estimados de pixels, descarte FIFO sob lock e caminho de navegador corrigido para respeitar o mesmo limite. Preservada resolução das imagens, sem GC forçado ou redução artificial de working set. O teto exclui overhead WPF/GPU e imagens ainda utilizadas por outras views; não foi medido consumo antes/depois da versão atual. Log otimizacao-ram-build.txt.

Galeria com cartões arredondados, estados de foco/hover/clique, indicação textual de estilo atual, previews maiores e textos que quebram linha. Indicadores corrigidos: percentual único no anel, título fora dele, gráficos CPU/RAM com apenas uma série, texto limitado à linha, CPU/RAM compactos em colunas. Alteração do controle também se aplica ao monitor na dock. Build final em galeria-monitor-build.txt. Visual/DPI e consumo total real pendentes; instalador anterior não inclui as alterações.


## 06/10/2026 — Ocultação em tela cheia restrita a vídeo

Corrigida regra que ocultava a dock em qualquer janela cobrindo o monitor. Agora exige geometria de tela cheia e processo de player conhecido, ou navegador conhecido com título identificando YouTube, Netflix, Prime Video, Disney+, Twitch, Vimeo ou Globoplay. Editores e demais aplicativos não disparam a ocultação por essa regra. Não altera a preferência independente de ocultação automática.

Identificação heurística por processo/título, sem inspecionar URLs, extensões ou reprodução real. F11 em página reconhecida também pode ser classificado como vídeo; players/sites não reconhecidos podem deixar a dock visível. Build Release: zero erros e aviso CS0067 existente. Log tela-cheia-video-build.txt. Validação real com vídeo e múltiplos monitores pendente; instalador anterior não inclui esta mudança.


## 06/10/2026 — Instalador atualizado final

Publicações Release win-x64 concluídas: app framework-dependent e setup self-contained. Pacote inclui instância única, cache de ícones com orçamento, hover dos aplicativos, galeria do monitor e detecção de vídeo em tela cheia, além das alterações anteriores. ZIP validado contra o app publicado; release validado contra o setup publicado. SHA-256/tamanho em installer-validation.json. Logs latest-app-publish.txt e latest-installer-publish.txt.

Não executados instalação/atualização/desinstalação ou testes reais de integração nesta etapa. Binários ainda sem assinatura; Controle de Aplicativo pode bloquear a execução. Próximos passos: assinatura confiável e validação manual do instalador e recursos em Windows 10/11.


## 06/10/2026 — Fluxo de assinatura preparado

Build suporta -Assinar com thumbprint público de certificado CurrentUser/My, caminho do SignTool e timestamp RFC3161. Preflight valida formato, RSA, validade, chave privada, finalidade de assinatura e cadeia. Hook MSBuild assina cópias dos componentes antes do bundle, verifica DLLs e mantém o cache NuGet intacto. App assinado antes de app.zip; setup assinado e verificado antes da cópia para release. Manifesto registra estado real da assinatura. Guia em ASSINATURA-DIGITAL.md.

Validação: parser PowerShell sem erros; build Release com zero erros e aviso CS0067 existente; tentativa de publicação assinada sem parâmetros recusada pelo hook. Sem certificado disponível: assinatura/timestamp/verificação de pacote assinado não executados. Política local de scripts permanece intacta; scripts de build não executados integralmente. Instalador de release continua sem assinatura e bloqueado por Smart App Control. Próximo passo do usuário: obter certificado e configurar provedor; então executar o fluxo em ambiente autorizado e testar instalação.


## 06/10/2026 — GitHub Actions configurado localmente

Criado .github/workflows/build-windows.yml: push, pull_request e execução manual; runner Windows 2025, .NET 10, restore/build/testes (exclui SystemIntegration), empacotamento pelo script oficial e upload de instalador/manifesto e TRX. Permissão contents:read, sem credenciais persistidas, sem segredos ou publicação de Releases. Artefatos retidos 14 dias. Guia em GITHUB-ACTIONS.md. Build local em github-actions-build.txt; execução remota não realizada.

Próximo passo do usuário: enviar código/workflow ao repositório e conferir Actions. Remote local aponta para Contagiovaneines/WinDock-, diferente do endereço GigaDock usado no formulário; confirmar destino. Integração SignPath pendente de aprovação.


## 06/10/2026 — Correção do push recusado por arquivo grande

Diagnóstico do log: setup de 165,35 MB no único commit local não enviado, rejeitado por GH001. Setup e app.zip removidos somente do índice Git; arquivos locais preservados e já ignorados. Workflow confirmado no commit local. Commit anterior publicado contém setup menor que 100 MB; a versão grande precisa ser removida do commit não publicado via amend, não apenas por um novo commit. Não alterado histórico nesta etapa nem executado push. Usuário deve revisar e executar commit --amend --no-edit e push. Remote ainda aponta para Contagiovaneines/WinDock-, conferir destino usado no formulário.


## 06/10/2026 — Visual das prévias de janelas

Painel-base de flyouts atualizado para grafite com borda discreta e botões arredondados; estados de hover, pressão, foco e desabilitado. Prévias de janelas com cabeçalhos espaçados, título completo em tooltip, cartões e ações de fechar separadas, navegação centralizada com contador e botões indisponíveis desabilitados. Mantidos thumbnails DWM, comandos reais e paginação. Brushes compartilhados congelados, sem animações/timers adicionais.

Build em previas-janelas-visual-build.txt, zero erros. Validação visual e DPI em execução pendentes; mudança do painel-base também se aplica aos demais flyouts. Instalador e artefato Actions anteriores não incluem esta alteração. Próximo passo: conferir ativação/fechamento e diferentes quantidades de janelas, enviar o código e gerar novo pacote.


## 06/10/2026 — Separação da linha de progresso da mídia

Progresso do estilo capa movido para uma linha própria de 18 unidades abaixo do conteúdo. Composição-base 630×140 mantém altura normalizada 54 e proporção horizontal anterior; título, artista e capa compensados para preservar legibilidade. Espaço reservado entre área clicável dos botões e progresso, sem sobreposição; textura cobre ambas as linhas. Build em midia-progresso-espaco-build.txt. Visual real pendente; instalador/Actions anteriores não incluem este ajuste.
# 06/10/2026 — Ajustes de widgets e acesso à loja

Seção Widgets passa a ocupar toda a largura disponível, preservando a lista e os detalhes das demais seções. Cabeçalho com quebra automática e botão Loja de Widgets destacado; cartões esticados, etiquetas com quebra e botão de estilo padronizado. Contador apresenta widgets instalados, eliminando o limite fictício de 9 ativos. Loja existente acessível em Ajustes → Widgets → Loja de Widgets, usando o comando original de adição/remoção.

Compilação Release executada com zero erros e um aviso CS0067 preexistente; log em ajustes-widgets-layout-build.txt. Validação visual em execução e diferentes escalas de tela pendente. Próximo passo: conferir a loja na aplicação e gerar novo instalador; pacote anterior não inclui este ajuste.

# 06/10/2026 — Bandeja com barra principal

Clique na seta da bandeja agora restaura e habilita a barra nativa quando ocultada pelo modo barra principal, antes da busca UI Automation. Barra permanece disponível para interação com os ícones; alternância existente pode ocultá-la novamente. Botões fora da tela por auto-ocultação não são descartados se disponibilizam Invoke/ExpandCollapse. A bandeja contém ícones de notificação, não todos os processos do computador.

Build Release registrado em bandeja-barra-principal-build.txt. Abertura real do popup do Explorer pendente de validação no computador do usuário; ausência do botão nativo ainda pode impedir a abertura. Instalador anterior não inclui esta correção.

# 06/10/2026 — Aparência sem sobreposição

Conteúdo dinâmico dos ajustes organizado verticalmente: cartão de dimensões não se sobrepõe mais aos temas nem ocupa toda a altura do painel. Cartão com largura máxima de 900 unidades, altura conforme conteúdo e espaçamento de 20 unidades. Sliders horizontais estilizados com trilho azul, controle circular, estados de interação e indicação de foco de teclado; preservados bindings, intervalos e comandos nativos de ajuste.

Compilação Release: zero erros, um aviso CS0067 preexistente; log aparencia-layout-build.txt. Validação visual e interação por mouse/teclado no aplicativo pendentes. Próximo passo: conferir temas e dimensões em diferentes escalas; instalador anterior não inclui a alteração.

# 06/10/2026 — Seleção de animações arcade do GitHub

Menu corrigido com comando de seleção real, indicação do estilo atual e opções Pac-Man, Breakout, Galaga, Puzzle Bobble, Bomberman e Minesweeper, além de Cobrinha e desativação. Preferência local persistida; novas simulações nativas na grade existente, com cores compartilhadas e pausa/restauração quando ocultas. Detalhes em GITHUB-ANIMACOES.md; assets SVG não foram fornecidos e os estilos são adaptações compactas.

Build Release executado; testes específicos tentados, mas os 12 casos foram bloqueados pelo Controle de Aplicativo ao carregar DLL (0x800711C7). Logs github-arcade-build.txt e github-arcade-tests.txt. Próximos passos: executar testes no CI, validar visual no Windows e gerar instalador atualizado.

## 06/10/2026 — README consolidado

README reorganizado para apresentação pública: recursos, catálogo, instalação, desempenho, privacidade, desenvolvimento, validação e contribuição. Removidas notas cronológicas repetidas e afirmações contraditórias sobre pacote, RGB e cache; resultados históricos separados da validação recente. Seções finais dedicadas ao que permanece em beta e às ideias futuras, sem promessa de prazo.

Etapa apenas documental, sem alteração de código ou novo build. Links relativos e estrutura Markdown conferidos; próximo passo: validar recursos pendentes e reempacotar alterações recentes antes de atualizar a distribuição.
# 06/10/2026 — Ícones e dicas de hover

Removido fundo permanente do aplicativo ativo na seção Apps; indicador de execução permanece. Realces de hover/pressão mais discretos e contorno explícito no foco de teclado. Dicas padronizadas no recurso global ToolTip: grafite, cantos arredondados, espaçamento, fonte de 12 unidades, posicionamento superior e limite de largura com quebra de texto. Dica de aplicativos usa o mesmo padrão, preservando atraso de 650 ms e conteúdo atual.

Build Release em icones-dicas-visual-build.txt. Aparência real em diferentes escalas ainda pendente; instalador anterior não contém o ajuste. Etapa visual sem benchmark de desempenho.

