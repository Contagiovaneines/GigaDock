# Auditoria de atividade — inventário anterior às correções

Varredura de toda a árvore; exemplos de origem expandida em activity-inventory-before.txt. Estados abaixo são leitura estática, não medidas de CPU/RAM.

| Componente | Arquivo (src/ salvo indicação) | Timer/loop/frequência | Atualiza | Consulta externa | Coleta local | Animação | Histórico | Cache | Listeners | Invisível antes | Dock escondida antes | Background necessário |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Relógio | App/ViewModels/ClockWidgetViewModel.cs | DispatcherTimer 1 s | hora/data, Stopwatch | não | não | sim | não | não | não | sim | sim | Stopwatch absoluto; sem alarme existente |
| Pomodoro | App/ViewModels/PomodoroWidgetViewModel.cs | DispatcherTimer 1 s | contador decremental, som | não | não | não | ciclos | estado | engine events | sim | sim | fase em execução e alerta |
| Calendário/reuniões | App/ViewModels/CalendarioWidgetViewModel.cs | DispatcherTimer 1 min; async ICS ao configurar | datas/agenda/contagem | ICS HTTP ou arquivo | arquivo | não | não | lista ICS/local | não | sim | sim | sem alarmes implementados |
| Notas | App/ViewModels/NotasWidgetViewModel.cs | debounce 2 s após edição | salvar notas | não | arquivo | não | não | texto | não | save pendente | save pendente | preservar gravação pendente |
| CPU/RAM/rede/disco | App/ViewModels/MonitorSistemaViewModel.cs | DispatcherTimer 2 s | uma coleta compartilhada | não | sim | gráficos | 30 amostras | última leitura | não | para apenas desativado | sim | não |
| Bateria | App/ViewModels/BateriaViewModel.cs | DispatcherTimer 30 s | carga e energia | não | GetSystemPowerStatus | não | não | última leitura | não | para apenas desativado | sim | não |
| Mídia | App/ViewModels/MidiaWidgetViewModel.cs | DispatcherTimer 1 s + eventos GSMTC async | timeline, metadados, capa | sessão Windows | sim | progresso | não | arquivos GUID sem limite | 4 eventos manager/session | sim | sim | eventos mínimos só se RGB musical habilitado |
| Clima | App/ViewModels/ClimaWidgetViewModel.cs | DispatcherTimer 1 h + consulta inicial | JSON e previsão | wttr.in | não | não | não | sem TTL | não | sim | sim | não |
| GitHub | App/ViewModels/GitHubWidgetViewModel.cs | 30 min + 150 ms + Task.Delay 3 s | contribuições e jogos visuais | HTML público GitHub | não | sim | 91 células | sem TTL | não | sim | sim | não |
| WhatsApp | App/ViewModels/WhatsAppWidgetViewModel.cs | recebe contagens/eventos Main | mensagens e badges | notificações Windows | sim | piscar XAML | contagem | estado | Main shared toast | sim | sim | notificações da aplicação devem continuar |
| Teams | App/ViewModels/TeamsWidgetViewModel.cs | serviço 5 s + Task.Delay 10 s | processos/títulos e mensagem temporária | não | sim | XAML | contagem | estado | 2 eventos serviço | sim | sim | notificações shared; polling de títulos não é essencial |
| Discord | App/ViewModels/DiscordWidgetViewModel.cs | Task.Run + Connect(1000) | pipe e dados demonstrativos | IPC local | sim | falante | usuários | lista | 2 eventos | sim | sim | não; integração ainda demonstrativa |
| OBS | App/ViewModels/ObsWidgetViewModel.cs | nenhum | estado visual e abertura explícita | não | não | efeito XAML | não | estado | não | sem tarefa | sem tarefa | não; gravação real ausente |
| Cotação | Core/Models/TipoWidget.cs | sem implementação | apenas enum | não | não | não | não | não | não | não | não | não |
| Notificações globais | App/ViewModels/MainViewModel.cs | 3 s recriado na recarga + listener Toast | contagens e alertas | Windows notification API | sim | alerta RGB/sombra | contagem | estado | Toast + window tracking | sim | sim | eventos de notificações e estado, sem redraw oculto |
| Toast | Infrastructure/Windows/ToastNotificationService.cs | eventos + consulta assíncrona | notificações e permissão repetida | API Windows | sim | não | não | não | NotificationChanged sem cleanup | sim | sim | preservar eventos |
| WhatsAppNotificationService (não instanciado) | Infrastructure/Windows/WhatsAppNotificationService.cs | 5 s após permissão | contagem local | API Windows | sim | não | não | não | event | sim se iniciado | sim se iniciado | redundante com Toast; cleanup necessário |
| Window tracking | Infrastructure/Windows/Win32WindowTrackingService.cs | 600 ms + WinEvent hooks | janelas, ativação, fullscreen | não | Win32 | não | snapshot | lista de janelas | hooks e timer | sim | sim | fullscreen/ativação/notificações necessários; snapshot UI pode coalescer |
| Watchdog barra nativa | Infrastructure/Windows/Win32TaskbarService.cs | timer enquanto modo substituição | estado Shell | não | Win32 | não | não | estado anterior | timer | sim | sim | não alterar no lifecycle; P0 de instalação já registrado |
| Controles rápidos | Infrastructure/Windows/ControlesRapidosService.cs | 30 s só quando teclado bloqueado | liberação de teclado | não | hook | não | não | handle | hook | sim quando bloqueado | sim | liberação de segurança obrigatória |
| Auto-hide | App/MainWindow.xaml.cs | 1,5 s após mouse sair | posição da dock | não | não | 220 ms | não | não | eventos mouse/display | sim | sim | mecanismo para reabrir dock |
| RGB gamer/musical e alertas | App/MainWindow.xaml | storyboard 3 s / pulsação + storyboard code | borda e sombra | não | não | Forever | não | brush | bindings | até IsVisible false | sim auto-hide | suspender desenho; preservar preferência e alerta |
| Badges e coleções | App/Views/Sections | storyboards e comandos | piscar, escala e popup | não | não | sim | não | ícones | bindings e handlers | alguns continuam | sim auto-hide | não |
| HoverFlyout | App/Views/HoverFlyout.cs | 350 ms entrada/saída | abrir e fechar popup | não | não | não | não | window ref | eventos | parado em repouso | pode ficar aberto | não |
| DWM/folders | App/Views/JanelasPreviewWindow.cs | eventos e leitura ao abrir | miniaturas/listagem | não | Win32 e arquivos | DWM | 100 itens | imagens até 20 MB/textos | loaded/unloaded | cleanup parcial | popup pode continuar | não |
| Ícones | Infrastructure/Windows/IconExtractionService.cs | sob demanda | extração e decode | não | Shell | não | não | ConcurrentDictionary sem limite | não | não periódico | não periódico | limitar cache sem destruir ícones emprestados |
| Busca/galeria apps | Infrastructure/Windows/AppSearchService.cs | scan Task.Run na recarga/galeria | índice de atalhos/apps | não | arquivos/Shell | não | não | lista estática | tarefas finitas | scan pode continuar | scan pode continuar | uma carga reutilizada; cancelar entrega a janela fechada |
| Instalador/persistência | Installer/MainWindow.xaml.cs | Task.Run finita por ação | instalar/salvar | não | arquivos/registro | progresso | não | ZIP | eventos de progresso | trabalho explícito | independente | preservar tarefa explícita |
| Demonstração web | vercel/script.js | setTimeout finito por ação; CSS | demonstração separada | não | não | CSS | não | DOM | DOM listeners | até concluir ação | independente do desktop | sem widget scheduler desktop |
| Probes docs/test_notif | docs/VisualHarness e test_notif | auxiliares separados | render/test notification | Windows | sim | não contínua | não | artefatos | auxiliares | fora runtime | fora runtime | não são parte do app publicado |

Arquitetura identificada: instâncias únicas de ViewModels no Main, configuração e visibilidade por ambiente, seções WPF recriadas, dock ocultada por Hide/fullscreen ou deslocamento de auto-hide. Não existia coordenador de atividade. A implementação abaixo usa um gerenciador Core com tokens de renderização WPF e estados visual/background, sem criar timer central redundante.


## Implementação final — 2026-10-05

A tabela inicial é o inventário **anterior à alteração**, com 30 entradas: 13 widgets implementados, Cotação somente declarada e componentes auxiliares. Foram pesquisados também Task.Run/Delay, loops, async, eventos, HTTP, animações, cache e recursos nativos; o inventário bruto está em activity-inventory-before.txt e a versão estruturada em activity-inventory-before.json. A demonstração web e os probes foram separados do runtime desktop.

### Arquitetura

`Core/Widgets/GerenciadorAtividade.cs` não cria timer. Cada componente registra callbacks de aplicação de estado e descarte. `EstadoAtividade` distingue Habilitado, Visual, SegundoPlano e Animacoes; estados iguais não reaplicam callbacks. O Main registra os 13 widgets existentes uma vez. `Common/AtividadeVisual.cs` agrega tokens Loaded/Unloaded/IsVisible/DataContext por elemento; um componente com várias representações permanece ativo enquanto uma estiver visível.

`MainWindow` informa visibilidade real, minimização e auto-hide por deslocamento; Main combina isso com ambiente ativo, DockVisivel e fullscreen. O fechamento remove subscriptions e recursos. Os gatilhos de animação contínua exigem atividade da dock e visibilidade do elemento; removem storyboards ao ocultar. Magnificação e popups são encerrados quando a dock fica inativa. Preferências gamer/musical permanecem salvas.

Background é explícito: Pomodoro iniciado conserva prazo absoluto mesmo em outro ambiente; quando oculto agenda apenas a conclusão. Salvar notas pendentes, notificações Windows, detecção de fullscreen/ativação, liberação do teclado e instalação/persistência explícitas continuam. Mídia mantém apenas eventos necessários se RGB musical exige estado; quando o controle está montado mas vazio, observa sessões para descobrir reprodução. Não mantém polling de timeline oculto. Um temporizador central de polling não foi adicionado.

Caches: clima 1 h, ICS 15 min e GitHub 30 min, com cancelamento de consultas dispensáveis, proteção contra duplicação e resultados obsoletos. Falhas de clima/ICS usam validade curta de 30 s para evitar repetição imediata; isso não promete retentativa automática após 30 s, pois o timer normal é mais espaçado. Reabrir atualiza imediatamente o estado, consultando a rede apenas se o cache expirou. Histórico de métricas mantém até 30 amostras; cache de ícones até 512 entradas; mídia conserva uma capa decodificada com até 256×256 e rejeita thumbnail acima de 8 MB. Sem novos arquivos temporários de capa.

### Problemas, impacto e solução

Caminhos abaixo relativos a src/, salvo indicação.

| Arquivo / componente | Problema e impacto anterior | Solução aplicada |
| --- | --- | --- |
| App/ViewModels/MainViewModel.cs | Timer de notificações recriado nas preferências; polling periódico e subscriptions sem dono claro | Um debounce de 250 ms por evento/retomada; guarda de concorrência; handlers removidos em Dispose; registro único dos widgets |
| App/MainWindow.xaml.cs; Common/AtividadeVisual.cs | Auto-hide deslocava janela ainda IsVisible; ambiente habilitado confundido com renderização | Sinal de visibilidade real + tokens; pausa mesmo com janela fora da tela |
| App/ViewModels/ClockWidgetViewModel.cs | Tick de 1 s mesmo oculto/sem segundos | Intervalo conforme estilo: segundos, minuto ou virada do dia; cronômetro absoluto; pausa visual |
| Core/Widgets/PomodoroEngine.cs; App/ViewModels/PomodoroWidgetViewModel.cs | Contagem por ticks acumulava atraso; tick em repouso | TimeProvider e prazo absoluto; pausa congelada; background iniciado preservado, uma conclusão e um alarme |
| App/ViewModels/CalendarioWidgetViewModel.cs | Minuto contínuo e ICS fora da renderização | Agendamento por fronteira temporal, cache 15 min, cancelamento e resultado condicionado à atividade |
| App/ViewModels/NotasWidgetViewModel.cs | Salvamento e fechamento sem lifecycle | Preserva debounce pendente; flush ao desativar/descartar; fecha painel oculto |
| App/ViewModels/MonitorSistemaViewModel.cs; Infrastructure/Windows/MetricasSistemaService.cs | Habilitação insuficiente; contador CPU criado cedo | Coletor único CPU/RAM/rede/disco; leitura imediata na retomada; contador lazy e liberado na suspensão |
| App/ViewModels/BateriaViewModel.cs | Timer por habilitação, inclusive dock oculta | Leitura imediata e timer de 30 s somente visual |
| App/ViewModels/ClimaWidgetViewModel.cs | HTTP no construtor/oculto, sem TTL | Construtor inativo; cache e consultas canceláveis somente visuais; configuração oculta adia consulta |
| App/ViewModels/GitHubWidgetViewModel.cs | HTTP, animação 150 ms e reinício após Delay sobreviviam ocultação | TTL, cancelamento de fetch/reinício, animação condicionada, liberação ao desativar |
| App/ViewModels/MidiaWidgetViewModel.cs | Timeline sempre ativa, listeners e GUIDs de capa acumuláveis | Timeline só visual e reproduzindo; subscriptions por necessidade; capa congelada em memória, sem novos temporários |
| App/ViewModels/TeamsWidgetViewModel.cs; Infrastructure/Windows/TeamsIntegrationService.cs | Varredura de processos/títulos a cada 5 s mesmo oculto; mensagem via Delay | Serviço somente visual, processos descartados, prazo de mensagem sem tarefa solta; callbacks ocultos/descartados ignorados |
| App/ViewModels/DiscordWidgetViewModel.cs; Infrastructure/Windows/DiscordIpcService.cs | Conexão sem cancelamento/cleanup | Conexão async cancelável, pipe liberado ao ocultar, listeners/lista descartados; integração segue demonstrativa |
| App/ViewModels/WhatsAppWidgetViewModel.cs; ObsWidgetViewModel.cs | Componentes fora de contrato de lifecycle | Contrato comum sem inventar polling; WhatsApp recebe eventos compartilhados, OBS permanece controle visual |
| Infrastructure/Windows/ToastNotificationService.cs | Permissão/assinaturas repetidas e descarte ausente | Inicialização guardada, eventos removidos, callbacks após fechamento ignorados; notificações background preservadas |
| Infrastructure/Windows/Win32WindowTrackingService.cs | Process fallback sem Dispose; atualização visual em background | Process descartado e refresh visual condicionado; hooks e timer de fullscreen necessários preservados |
| Infrastructure/Windows/IconExtractionService.cs | Dicionário de imagens sem limite | Cache até 512 entradas, remoção de referências e respeito aos handles emprestados |
| App/Views/AppGalleryWindow.xaml.cs | Muitas entregas UI após fechar; scan antecipado Main | Cancelamento ao fechar, entrega sequencial e guarda; busca sob demanda |
| App/Controls/DwmThumbnailControl.cs; Views/HoverFlyout.cs | Miniatura oculta e handlers capturando popup fechado | Registro DWM liberado em IsVisible=false; handlers removidos e timer parado ao fechar |
| App/MainWindow.xaml; Views/Sections/*.xaml | Storyboards Forever em auto-hide e Stop sem liberar clocks | Condições globais/locais e RemoveStoryboard; alerta code-behind controlável e removido |
| Cotação; demonstração web; probes | Enum sem implementação / ferramentas fora runtime | Documentados como tais; nenhuma função inexistente foi declarada implementada |

### Extensão e remoção

Novo widget implementa IAtividadeWidget, registra-se uma vez via RegistrarWidget e marca o elemento com AtividadeVisual.Componente. DefinirAtividade é o único ponto que decide iniciar/parar trabalho; Dispose cancela consultas e libera subscriptions/recursos. Trabalho independente precisa de justificativa e flag explícita, como Pomodoro em andamento.

Remover uma configuração suspende seu trabalho no ambiente e libera o token visual; os 13 ViewModels compartilhados continuam como objetos limitados na arquitetura existente, permitindo reinstalar sem reutilizar objeto já descartado. Remover o último Pomodoro instalado cancela a contagem. Encerramento ou GerenciadorAtividade.Remover efetua Dispose definitivo. Não há múltiplas instâncias do mesmo tipo por ambiente nesta versão.

### Verificação e métricas

Resultado: **41 testes aprovados, zero falhas e zero ignorados**. Estabilidade Dispatcher: **00:02:00.0090978**, aprovada. Build Release final: **zero erros e um aviso CS0067** existente em fake de teste. Aplicativo e instalador win-x64 publicados; pacote local atualizado. Evidências em docs/TestResults/atividade-widgets.trx, activity-validation.json e docs/STATUS.md. Testes usam fontes falsas onde necessário, STA e Dispatcher WPF; não simulam sucesso de WinRT, instalação ou renderização de terceiros.

Cenários cobertos: habilitado sem renderizar; dock oculta; vários tokens do mesmo componente; pausa/retomada de monitor e bateria com leitura imediata; frequência por estilo do relógio; cronômetro parado; Pomodoro por tempo absoluto/pausa/conclusão; background em ambiente inativo; mídia criada inativa sem listeners/timer; descoberta de mídia montada sem conteúdo; remoção e lease antigo; cache HTTP por TTL e mudança de localização; 10 mil alternâncias com exatamente 10 mil leituras de retomada, um registro e zero tokens ao final; estabilidade com Dispatcher durante dois minutos e ausência de leituras nos intervalos ocultos.

São contagens em testes isolados, não medições de CPU/RAM do aplicativo completo. Não foi executada linha de base de consumo da versão anterior; não há porcentagem de economia, FPS ou tamanho de working set comparável. Os testes de nova compilação puderam executar nesta etapa, sem contornar a política do Windows; o bloqueio histórico de outro binário não foi tratado como resultado atual. A suíte completa histórica tinha 13 falhas e não foi repetida indiscriminadamente, pois inclui integrações com efeitos sobre a barra de tarefas.

### Limites e problemas restantes

- Validar manualmente troca de ambiente, auto-hide, fullscreen, popups, renderização das capas, WinRT/media/notificações e DPI em Windows 10/11. Testes do coordenador não substituem testes ponta a ponta das views.
- Medir CPU/RAM/handles/GC do aplicativo real por período prolongado, antes/depois sob carga equivalente. O teste de dois minutos é isolado e não prova ausência de todo vazamento.
- Instalador ainda pode esconder/desabilitar barra nativa automaticamente: P0 anterior fora desta alteração de performance. Não executar instalação para medir consumo antes de corrigir isso.
- Migrações, versionamento, apps globais, validação ICS, erros silenciosos e integrações demonstrativas permanecem conforme ANALISE-PROJETO.md.
- Falhas e permissão negada de serviços externos precisam de estados/retry mais consistentes; parsing ICS e GitHub HTML continuam sujeitos a limitações anteriores.
- Timers necessários de tracking/fullscreen, segurança do teclado e watchdog de barra principal permanecem. Não alegar zero timers globais quando a dock está oculta.
- Singletons preservam estado limitado entre ambientes; configurações são por ambiente, mas não existem motores independentes de Pomodoro/mídia por ambiente.
- Capas temporárias de versões antigas não são apagadas automaticamente; novos GUIDs deixaram de ser criados.
