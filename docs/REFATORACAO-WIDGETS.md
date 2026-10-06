# Refatoração de atividade, dados e integrações — 06/10/2026

Escopo: solicitação anexada nesta etapa. Mantidos temas, estilos, dimensões e configurações; mudanças visuais limitadas a textos corretos, dependências/permissões e editor de notas. Os documentos anteriores descrevem etapas históricas e não substituem este estado.

## Inventário e arquitetura

Inspecionados os 13 ViewModels de widgets, os 14 tipos declarados, serviços Windows, seções WPF, prévias, persistência, testes, instalador e demonstração web. Cotação está somente no enum. Loja e galeria usam valores ilustrativos; não inicializam sessões de mídia, métricas ou integrações externas. O clima de referência usa `iniciarConsulta: false` e agora é descartado ao fechar a galeria.

O inventário estruturado está em [widget-refactor-inventory.json](widget-refactor-inventory.json). O levantamento anterior desta etapa está em [widget-refactor-inventory.txt](widget-refactor-inventory.txt); a varredura final está em [widget-refactor-inventory-after.txt](widget-refactor-inventory-after.txt). O inventário histórico anterior à primeira otimização continua em [AUDITORIA-ATIVIDADE.md](AUDITORIA-ATIVIDADE.md).

O coordenador existente foi ampliado, sem acrescentar timer central. `GerenciadorAtividade` reúne instalação, habilitação, tokens visuais, trabalho em segundo plano, suspensão, animações, descarte e saúde. `DiagnosticoWidget` distingue instalado, habilitado, visível, executando, pausado, destruído, erro e indisponibilidade. Widgets reportam trabalho real (timer, consulta, listener ou salvamento), separado da integração disponível: OBS/Discord não aparecem executando uma integração inexistente. Saúde não é sinônimo de visibilidade. Os estilos do mesmo tipo usam a instância existente do ViewModel, não coletores independentes.

Instalado significa presente em algum ambiente; habilitado corresponde à configuração atual. Trocar de ambiente não cancela Pomodoro/temporizador iniciados. Remover a última instalação encerra esses controles. Tokens antigos não reativam componentes removidos e não afetam um novo registro com o mesmo nome. Descarte é terminal para o gerenciador.

`MainWindow` assina suspensão/retomada, mudança de hora e preferências de animação do Windows; remove as assinaturas ao fechar. A suspensão interrompe timers/listeners dispensáveis, preservando prazos absolutos. Na retomada, reavalia os estados; uma conclusão atrasada é processada uma vez. Mudança de hora/fuso atualiza relógio e calendário. Não foi simulado o desligamento físico do computador.

## Widgets analisados e comportamento final

| Widget | Trabalho visível | Oculto / em segundo plano | Dados e confiabilidade |
| --- | --- | --- | --- |
| Relógio | Um timer, alinhado ao segundo, minuto ou dia conforme estilo | Sem redraw; temporizador iniciado mantém um único despertar para conclusão | Cronômetro usa Stopwatch; temporizador usa prazo absoluto, pausa e evento único; duração fixa de 5 min |
| Pomodoro | Um timer visual de 1 s somente em execução | Um despertar de conclusão; tempo transcorrido inclui suspensão | Motor absoluto existente preservado; fases e alerta sem decremento por frames |
| Calendário/reuniões | Atualização nas fronteiras relevantes; ICS com validade de 15 min | Timer para; consulta cancelada; dados anteriores preservados em erro | Importador Ical.Net 5.2.0: UTC, floating, TZID, recorrência, EXDATE, dia inteiro; sem conta Outlook/Google |
| Notas | Editor integrado, contagem de linhas reais e debounce de 2 s | Salvamento pendente pode concluir; desativar/fechar força tentativa de salvar | Arquivo temporário e substituição do arquivo; erro explícito, sem descartar texto pendente |
| Monitor | Uma coleta a cada 2 s; métricas selecionadas pelo consumidor; histórico de 30 amostras | Para e libera contador/baseline | CPU/rede diferenciam primeira amostra de indisponibilidade; zero real continua zero; CPU com erro usa espera de 30 s |
| Bateria | Leitura de energia a cada 30 s | Para; não reativa após Dispose | Carregando, descarregando, completa, sem bateria, desconhecida e API indisponível; sem porcentagem inventada |
| Mídia | Metadados por eventos; timeline de 1 s somente reproduzindo | Sem polling de timeline; observação mínima quando necessária à descoberta/RGB | Evento de timeline atualiza seek pausado; consultas de metadados coalescidas; resultado de sessão antiga descartado |
| Clima | Cache de 1 h; tentativa após erro em 30 s enquanto visível | Para timer e cancela consulta; retoma com cache/consulta válida | Timestamp, estado de rede/erro, sem dados/atuais/antigos; mantém previsão anterior se atualização falha |
| GitHub | HTML público com cache de 30 min; animação de 150 ms quando permitida | Para polling/animação/reinício e cancela consulta | HTTP limitado a 2 MB/10 s; erro preserva dados; sem fingir Actions/PRs; dias ativos não viram total de contribuições |
| WhatsApp | Contagem/eventos do serviço compartilhado | Listener global necessário a notificações continua; nenhum polling próprio do widget | Permissão informada; notificação detectada não confirma mensagem não lida |
| Teams | Polling de títulos/processos de 5 s, somente visível | Polling para; notificações globais continuam | Origem heurística, confiança baixa e texto “estimado”; libera cada Process, inclusive os encerrados |
| Discord | Atalho para abrir aplicativo; diagnóstico de indisponibilidade | Nenhum pipe/timer de demonstração | RPC autenticado não implementado; nenhuma sala ou participante inventado |
| OBS | Atalho para abrir aplicativo; diagnóstico de indisponibilidade | Nenhum timer | WebSocket não implementado; clicar no controle não confirma gravação |
| Cotação | Não implementado | Nenhum | Tipo reservado no modelo |

O importador limita o arquivo a 2 MB, 2.000 eventos e 10.000 ocorrências, com horizonte de 30 dias no widget. Calendários inválidos, fusos desconhecidos ou expansão excessiva apresentam erro; não são convertidos silenciosamente para UTC. Eventos de dia inteiro mantêm a data. O texto ICS é conservado em memória para reinterpretar o fuso sem consultar a rede novamente. O parser e as recorrências usam [Ical.Net](https://github.com/ical-org/ical.net/tree/v5.2.0), baseado no formato [RFC 5545](https://www.rfc-editor.org/rfc/rfc5545).

## Recursos compartilhados e vazamentos

- Mídia: cache de até oito capas, validade de uma hora, decodificação limitada a 256×256 e rejeição de thumbnail acima de 8 MB. Limpa ao desativar/destruir; sem arquivos temporários acumulados. Cinco listeners quando manager/sessão estão conectados, removidos na desconexão. O listener adicional de timeline substitui a necessidade de polling pausado.
- Notificações: permanece um listener global. Para a notificação nova, usa a API pública [GetNotification(id)](https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.management.usernotificationlistener.getnotification), evitando uma segunda enumeração completa. Contagens usam debounce de 250 ms; eventos durante consulta geram uma sincronização posterior, sem outro timer concorrente.
- `WhatsAppNotificationService` antigo permanece sem instância no aplicativo. Seu timer de 5 s não participa do runtime; seu start/stop/dispose foram revisados. Não foi criada uma segunda coleta no widget.
- Window tracking: 600 ms e hooks WinEvent continuam para ativação/fullscreen. A UI não recompõe listas continuamente quando oculta. Watchdog da barra nativa permanece restrito ao modo explicitamente configurado; nenhum teste desta etapa alterou a barra do Windows.
- Auto-hide e HoverFlyout usam espera finita. DWM descarta thumbnails ao ocultar/descarregar e agora não registra novamente no LayoutUpdated oculto. Prévias de arquivos/janelas são sob demanda, sem timer de widget. Fechar a prévia de pasta cancela a listagem e invalida resultados de imagens/textos pendentes; codecs já em execução podem concluir, sem entregar conteúdo à janela fechada.
- RGB, alertas e badges: quatro storyboards contínuos encontrados no XAML; todos condicionados à atividade/visibilidade e removidos ao sair do estado. Magnificação permanece uma animação finita. O alerta de 15 s e reinício de animação GitHub de 3 s têm cancelamento.
- Ícones continuam com cache limitado a 512 entradas; métricas com histórico de 30 pontos, GitHub com 91 células. Loops de parser, busca de caminho e jogo são finitos; não são coletores permanentes.

## Antes e depois desta etapa

Esta etapa começou com a primeira otimização de lifecycle já presente. Não é correto apresentar novamente as reduções históricas como se fossem novas.

| Item | Antes desta etapa | Depois |
| --- | --- | --- |
| Timers alocados dos widgets/serviço Teams | 11 | 11, com donos únicos; nenhum scheduler extra |
| Coleta de métricas por tick visível | CPU/RAM/rede/disco, mesmo em estilo individual | Somente o conjunto solicitado; CPU+RAM continua uma coleta |
| Coleta pesada de widgets ocultos, sem exceções necessárias | Já pausada pelo coordenador | Continua pausada; suspensão/descarte e diagnóstico reforçados |
| Temporizador oculto | Contagem consultada somente ao voltar, sem evento agendado | Prazo absoluto + um despertar de conclusão |
| Erro de clima | Cache curto, mas próximo timer podia esperar uma hora | Dados antigos identificados; retentativa agendada em 30 s visível |
| Capas de músicas anteriores | Apenas uma capa | Oito entradas limitadas; replay de faixa reutiliza a capa |
| Notificação adicionada | Enumeração inteira para localizar um ID, além das contagens | Busca por ID e enumeração de contagens com debounce |
| Discord/OBS | Sala fictícia / gravação por estado local | Indisponibilidade explícita, sem sucesso falso |

As contagens são inventário estático de donos e medições com serviços simulados nos testes; não são benchmark de CPU/RAM do aplicativo instalado. Durante 10.000 alternâncias, registros/tokens permanecem limitados. O teste com Dispatcher por dois minutos verifica ausência de leituras do coletor simulado durante ocultação. Não foram medidos FPS, uso real de RAM, bateria física ou tráfego dos serviços reais.

## Arquivos principais

- `Core/Widgets/GerenciadorAtividade.cs`, `TemporizadorEngine.cs`, `ImportadorCalendario.cs`, `Core/Models/CompromissoLocal.cs`, `Core/DockWindows.Core.csproj`.
- `App/Common/IAtividadeWidget.cs`, `App/MainWindow.xaml.cs`, `App/ViewModels/MainViewModel.cs` e os 13 ViewModels de widgets/monitor/bateria.
- `Infrastructure/Windows/MetricasSistemaService.cs`, `BateriaService.cs`, `TeamsIntegrationService.cs`, `DiscordIpcService.cs`, `ToastNotificationService.cs`.
- `App/Controls/DwmPreviewControl.cs`, `App/Views/PastaPreviewWindow.cs`, loja/galeria de estilos e seções Notas, OBS, Discord, Teams e WhatsApp.
- `tests/DockWindows.Tests/RefatoracaoWidgetsTests.cs`; documentação desta etapa e README.

Dependências novas: Ical.Net 5.2.0 (MIT) e NodaTime 3.2.2 transitivo (Apache-2.0), compatíveis com net10.0/WPF. A auditoria NuGet, incluindo transitivas, não encontrou vulnerabilidades listadas no momento da consulta: [widget-dependency-audit.json](widget-dependency-audit.json). Isso não constitui garantia de segurança do aplicativo inteiro.

## Validação e limites

Resultado final: **140 testes aprovados, zero falhas**, incluindo **20 casos novos**. Build Release com `--no-incremental`: **zero erros e um aviso CS0067 existente em um fake de teste**. Evidências em [widget-refactor-validation.json](widget-refactor-validation.json), [TRX](TestResults/refatoracao-final.trx) e [log do build](widget-refactor-build.txt). Os testes incluem lifecycle, 10 mil alternâncias, Dispatcher por dois minutos, cache online/offline/online, preview sem rede, descarte, métricas solicitadas, bateria e ausência de sucesso falso, UTC/TZID/floating/recorrência/exceções/dia inteiro/horário de verão, persistência de notas e prazo absoluto.

Dois testes marcados `SystemIntegration` ficam excluídos por alterarem autostart/barra de tarefas. Não foram executados instalação, desinstalação, notificações reais com permissões, OBS/Discord reais, renderização visual/DPI, suspensão física ou benchmarks. Integração real de gravação/voz permanece pendente e explicitamente indisponível. O temporizador emite evento de conclusão, mas ainda não tem duração configurável, alarme dedicado ou recuperação após encerrar o aplicativo. Não há múltiplas instâncias do mesmo tipo por ambiente.

O instalador local existente não foi reempacotado nesta etapa; o código e os testes compilados contêm esta refatoração.
