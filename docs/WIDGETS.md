# Widgets: funcionamento, disponibilidade e auditoria

Auditoria de implementação realizada em 10 de outubro de 2026, para Windows e Linux. Os 26 tipos têm descrição, modo de uso e requisitos na loja. Os estados descrevem o mínimo implementado; não significam que todos os dispositivos, aplicativos ou provedores foram testados ao vivo.

## Estados na loja

- **Disponível:** há implementação do mínimo descrito, com estados vazios/erros quando não há dados.
- **Beta funcional:** o mínimo está implementado, mas há integração experimental, estimativas ou limites adicionais.
- **Beta · em desenvolvimento:** instalação bloqueada; o recurso principal está incompleto.
- **Indisponível nesta plataforma:** instalação bloqueada; por exemplo, widgets exclusivos do Linux no Windows.
- **Requisito ausente:** no Linux, a ativação é bloqueada se faltar `playerctl`, `pactl`, `flatpak` ou uma sessão Sway/Hyprland com sua ferramenta. Reabra os Ajustes após resolver o requisito. A loja não instala ferramentas automaticamente.

Instalações antigas não são apagadas. A loja Windows mantém a opção de desinstalar; a interface Linux permite desativar itens antigos sem permitir uma nova ativação bloqueada. Configurações e dados locais são preservados.

## Matriz do mínimo implementado

| Widget | Windows | Linux | Mínimo e ressalva principal |
| --- | --- | --- | --- |
| Relógio | Disponível | Disponível | Mostra hora e data e permite escolher o mostrador. Linux: Mostra a hora local do computador. |
| Pomodoro | Disponível | Disponível | Cronometra períodos de foco e descanso. |
| Calendário | Disponível | Disponível | Consulta e organiza compromissos locais. |
| Notas | Disponível | Disponível | Guarda texto local para o ambiente. |
| Monitor do sistema | Disponível | Disponível | Exibe uso real de CPU e memória. |
| Cotação de moedas | Disponível | Disponível | Consulta taxas de referência entre moedas. |
| GitHub contribuições | Beta funcional | Disponível | Exibe contribuições públicas de um usuário do GitHub. Linux: Consulta nome público, quantidade de repositórios e seguidores. |
| Clima | Disponível | Disponível | Consulta clima e previsão para uma cidade informada. |
| WhatsApp | Beta · em desenvolvimento | Beta · em desenvolvimento | Integração de notificações do WhatsApp ainda indisponível neste instalador. Linux: Este widget ainda não possui integração funcional no Linux. |
| Teams · estado estimado | Beta funcional | Beta · em desenvolvimento | Estima se o Teams está aberto ou em reunião por processos e títulos de janela. Linux: Este widget ainda não possui integração funcional no Linux. |
| Discord · voz | Beta · em desenvolvimento | Beta · em desenvolvimento | Canal, participantes e estado de voz ainda não estão integrados. Linux: Este widget ainda não possui integração funcional no Linux. |
| OBS Studio | Beta funcional | Beta funcional | Consulta gravação/transmissão e inicia ou para gravação no OBS local. |
| Mídia | Disponível | Disponível | Consulta a faixa atual e oferece controles de reprodução. |
| Bateria | Disponível | Disponível | Mostra carga e estado da bateria detectada pelo sistema. |
| Lembrete de água | Disponível | Beta · em desenvolvimento | Lembra de beber água e permite contar copos consumidos nesta sessão. Linux: O registro manual existe, mas os lembretes automáticos ainda não foram implementados no Linux. |
| Área de transferência | Disponível | Beta · em desenvolvimento | Mantém histórico dos textos copiados nesta sessão. Linux: Este widget ainda não possui integração funcional no Linux. |
| Downloads e capturas | Disponível | Beta · em desenvolvimento | Lista arquivos recentes de Downloads e Imagens. Linux: Este widget ainda não possui integração funcional no Linux. |
| Conectividade | Disponível | Disponível | Mostra informações da conexão local. |
| Áudio do sistema | Disponível | Disponível | Consulta e ajusta o volume. |
| Estante de arquivos | Disponível | Beta · em desenvolvimento | Organiza referências locais com prévias seguras. Linux: Este widget ainda não possui integração funcional no Linux. |
| Pokédex | Disponível | Disponível | Exibe um mascote animado com sprites locais. |
| Tarefas | Disponível | Disponível | Mantém uma lista local de tarefas por ambiente. |
| Temperatura e ventoinhas | Indisponível nesta plataforma | Beta funcional | Lê temperaturas e rotações disponibilizadas pelo Linux. |
| Aplicativos Flatpak | Indisponível nesta plataforma | Beta funcional | Lista aplicativos Flatpak e permite abri-los. |
| Áreas de trabalho Linux | Indisponível nesta plataforma | Beta funcional | Lista e troca áreas de trabalho do Sway ou Hyprland. |
| Script local | Indisponível nesta plataforma | Beta funcional | Executa manualmente um script local e mostra seu resultado. |

## Como usar e requisitos

O conteúdo abaixo corresponde ao contrato publicado na loja. A versão Linux pode ter um mínimo diferente do Windows; por exemplo, GitHub consulta perfil público, e o calendário usa compromissos locais.

### Relógio

**Windows:** Clique para abrir o calendário; personalize o estilo nos Ajustes. Usa o horário do computador. No Linux o painel mostra hora local.

**Linux:** Clique para ver a hora com segundos e a data. A opção expandida mostra a hora na barra. Usa o horário do computador. Mostradores e fusos personalizados ainda não fazem parte da interface Linux.

### Pomodoro

**Windows:** Abra o widget e use Iniciar/Pausar, Reiniciar e Próxima fase. Funciona localmente enquanto o GigaDock estiver aberto.

**Linux:** Abra o widget e use Iniciar/Pausar, Reiniciar e Próxima fase. Funciona localmente enquanto o GigaDock estiver aberto.

### Calendário

**Windows:** Abra o painel e adicione título, data e horário. No Windows também importa iCalendar. Links iCalendar exigem internet; no Linux o mínimo atual é a agenda local.

**Linux:** Abra o painel e adicione título, data e horário. No Windows também importa iCalendar. Links iCalendar exigem internet; no Linux o mínimo atual é a agenda local.

### Notas

**Windows:** Clique para editar. Windows salva automaticamente; Linux exige clicar em Salvar nota. Sem conta ou internet. Os dados ficam neste computador.

**Linux:** Clique para editar. Windows salva automaticamente; Linux exige clicar em Salvar nota. Sem conta ou internet. Os dados ficam neste computador.

### Monitor do sistema

**Windows:** Abra o painel para acompanhar as leituras. A primeira amostra de CPU pode levar alguns segundos. Windows oferece também rede/disco; Linux lê CPU/RAM em /proc.

**Linux:** Abra o painel para acompanhar as leituras. A primeira amostra de CPU pode levar alguns segundos. Windows oferece também rede/disco; Linux lê CPU/RAM em /proc.

### Cotação de moedas

**Windows:** Escolha origem e destino. No Linux abra o painel e clique em Consultar câmbio. Internet para atualizar. Windows usa referência BCE com cache; Linux usa Frankfurter. Sem negociação.

**Linux:** Escolha origem e destino. No Linux abra o painel e clique em Consultar câmbio. Internet para atualizar. Windows usa referência BCE com cache; Linux usa Frankfurter. Sem negociação.

### GitHub contribuições

**Windows:** Informe o usuário nas configurações do widget e atualize a consulta. Requer internet. Leitura de HTML pode mudar; não acessa repositórios privados, Actions ou PRs.

**Linux:** Abra o painel, informe um usuário público e clique em Consultar perfil. Requer internet. Não exibe gráfico de contribuições nem dados privados.

### Clima

**Windows:** Informe a cidade. No Linux clique em Consultar previsão de 7 dias no painel. Requer internet e envia a cidade ao provedor: wttr.in no Windows, Open-Meteo no Linux.

**Linux:** Informe a cidade. No Linux clique em Consultar previsão de 7 dias no painel. Requer internet e envia a cidade ao provedor: wttr.in no Windows, Open-Meteo no Linux.

### WhatsApp

**Windows:** Instalação bloqueada até concluir e validar a leitura de notificações. O instalador atual não declara a capacidade necessária do Windows; abrir o WhatsApp não equivale a ler mensagens.

**Linux:** Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento. Sem runtime Linux implementado para este recurso.

### Teams · estado estimado

**Windows:** Abra o Teams e consulte o indicador na dock; clique para abrir o aplicativo. Estimativa local: não confirma presença oficial, microfone, calendário ou notificações.

**Linux:** Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento. Sem runtime Linux implementado para este recurso.

### Discord · voz

**Windows:** Instalação bloqueada enquanto a integração de voz estiver em desenvolvimento. Detectar ou abrir o Discord não confirma uma chamada.

**Linux:** Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento. Sem runtime Linux implementado para este recurso.

### OBS Studio

**Windows:** Ative o WebSocket nas ferramentas do OBS; configure porta e senha e conecte antes de usar os controles. Requer OBS aberto com WebSocket v5. Linux usa 127.0.0.1 e senha somente no painel; Windows usa configuração protegida local.

**Linux:** Ative o WebSocket nas ferramentas do OBS; configure porta e senha e conecte antes de usar os controles. Requer OBS aberto com WebSocket v5. Linux usa 127.0.0.1 e senha somente no painel; Windows usa configuração protegida local.

### Mídia

**Windows:** Inicie um player compatível e use Anterior, Tocar/Pausar e Próxima. Windows exige sessão de mídia compatível; Linux exige playerctl e um player MPRIS.

**Linux:** Abra um player MPRIS, clique em Atualizar faixa e use Anterior, Tocar/Pausar e Próxima. Requer playerctl; a instalação fica bloqueada enquanto essa ferramenta não estiver disponível.

### Bateria

**Windows:** Instale para acompanhar a carga; abra o painel para detalhes quando disponível. Requer bateria exposta pelo sistema. Um computador sem bateria mostra esse estado, sem valores simulados.

**Linux:** Instale para acompanhar a carga; abra o painel para detalhes quando disponível. Requer bateria exposta pelo sistema. Um computador sem bateria mostra esse estado, sem valores simulados.

### Lembrete de água

**Windows:** Configure intervalo e horário ativo nos Ajustes e registre quando beber água. No Windows os lembretes dependem do GigaDock em execução.

**Linux:** Instalação bloqueada até concluir os lembretes. Registro manual não equivale a um lembrete automático.

### Área de transferência

**Windows:** Copie um texto, abra o widget e selecione o item que deseja copiar novamente. Windows: até 20 textos; sem histórico permanente. Não há implementação Linux.

**Linux:** Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento. Sem runtime Linux implementado para este recurso.

### Downloads e capturas

**Windows:** Abra o painel, atualize a lista e clique em um arquivo para abrir. Capturar abre a ferramenta do Windows. Até 20 arquivos das pastas acessíveis. Não há implementação Linux.

**Linux:** Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento. Sem runtime Linux implementado para este recurso.

### Conectividade

**Windows:** Abra o painel para consultar o estado da rede. Windows consulta Wi-Fi/Bluetooth; Linux mostra interfaces de rede ativas, sem controle Bluetooth.

**Linux:** Abra o painel para consultar o estado da rede. Windows consulta Wi-Fi/Bluetooth; Linux mostra interfaces de rede ativas, sem controle Bluetooth.

### Áudio do sistema

**Windows:** Abra o painel. No Linux consulte o volume e clique em Aplicar volume depois de ajustar o controle. Windows oferece mixer por aplicativo; Linux exige pactl e servidor compatível, com volume da saída padrão.

**Linux:** Abra o painel. No Linux consulte o volume e clique em Aplicar volume depois de ajustar o controle. Windows oferece mixer por aplicativo; Linux exige pactl e servidor compatível, com volume da saída padrão.

### Estante de arquivos

**Windows:** Abra o painel, adicione um arquivo e use as ações de prévia, abrir ou remover. Windows: até 30 referências; não executa conteúdo incorporado nas prévias. Não há implementação Linux.

**Linux:** Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento. Sem runtime Linux implementado para este recurso.

### Pokédex

**Windows:** Ative e escolha a espécie nos Ajustes da Pokédex. Sem internet para os sprites incluídos. Créditos e direitos dos recursos gráficos preservados.

**Linux:** Ative e escolha a espécie nos Ajustes da Pokédex. O mascote percorre a borda superior da dock em qualquer tema; voadores/flutuantes usam as regras visuais do Windows. Reduzir animações pausa o movimento. Sem internet para os sprites incluídos. Créditos preservados. [Comportamento e limites de paridade](POKEMON-LINUX.md).

### Tarefas

**Windows:** Abra o painel, escreva uma tarefa e adicione. Marque para concluir ou exclua pela ação correspondente. Sem conta ou internet; a lista fica salva neste computador.

**Linux:** Abra o painel, escreva uma tarefa e adicione. Marque para concluir ou exclua pela ação correspondente. Sem conta ou internet; a lista fica salva neste computador.

### Temperatura e ventoinhas

**Windows:** Disponível na versão Linux, com os requisitos indicados. Widget exclusivo do Linux.

**Linux:** Abra o painel para atualizar as leituras; use o formato expandido para exibi-las na barra. Linux: sensores acessíveis em /sys/class/hwmon; máquinas virtuais podem não expor sensores.

### Aplicativos Flatpak

**Windows:** Disponível na versão Linux, com os requisitos indicados. Widget exclusivo do Linux.

**Linux:** Abra o painel, atualize a lista e escolha um aplicativo já instalado. Linux: requer flatpak. Não instala nem atualiza aplicativos por este widget.

### Áreas de trabalho Linux

**Windows:** Disponível na versão Linux, com os requisitos indicados. Widget exclusivo do Linux.

**Linux:** Abra o painel, atualize a lista e selecione a área desejada. Linux: sessão Sway com swaymsg ou Hyprland com hyprctl. GNOME/KDE não possuem integração aqui.

### Script local

**Windows:** Disponível na versão Linux, com os requisitos indicados. Widget exclusivo do Linux.

**Linux:** Informe um caminho absoluto, salve e clique em Executar. Não há execução automática. Linux: arquivo executável escolhido pelo usuário; saída limitada em texto ou JSON text/tooltip.

## Evidências executadas e limites

- Build Release da solução: zero erros e avisos.
- Windows: 74 testes aprovados, cobrindo bloqueio de instalação pela UI/handler, relógio, temporizador, Pomodoro, notas, calendário, clima online/offline com respostas simuladas, monitor CPU/RAM, bateria, instâncias, atividades e comportamento da Pokédex.
- Linux: 81 testes aprovados, incluindo serviços locais, consultas públicas com HTTP simulado, comandos com adaptadores controlados, WebSocket OBS local de teste, sensores em árvore de arquivos temporária, Flatpak, workspaces e scripts. Compartilhados: 54 testes aprovados.
- Loja WPF real, em processo isolado: 26 itens renderizados, seis instalações bloqueadas e instruções presentes; cliques forçados nos bloqueados foram recusados. A renderização também verificou um único botão de Tarefas com Pomodoro e Água habilitados, corrigindo três cópias indevidas.
- Avalonia real em Ubuntu/WSLg: texto de uso e bloqueios da loja, notas salvas, tarefas adicionadas/concluídas/excluídas, 151 sprites, tema, painel de sensores e execução manual de script verificados. Flatpak e workspaces ausentes foram bloqueados, em vez de apresentados como instaláveis.
- Nenhum controle de áudio/mídia, gravação real no OBS, chamada Teams/Discord ou leitura de WhatsApp foi validado com aplicativos pessoais. Consultas de internet foram testadas com respostas controladas; disponibilidade atual dos provedores não foi certificada. Histórico real da área de transferência, compartilhamento externo e sensores físicos não foram exercitados nesta etapa.
- Os estados sem bateria, sem player, sem rede ou sem sensores não são substituídos por valores fictícios. Instalar um widget não garante que uma fonte de dados exista neste computador.

Relatórios de execução e capturas de trabalho ficam em `docs/local/` e `docs/TestResults/`, ignorados pelo Git. Não foi instalada automaticamente a nova versão no computador pessoal nem testada outra distribuição/desktop Linux.

## Decisões de bloqueio

- **Discord voz:** o serviço atual observa o processo; canal e participantes ainda não estão integrados. Um atalho para abrir o aplicativo não satisfaz o mínimo de voz.
- **WhatsApp no Windows:** o instalador atual não possui o manifesto/capacidade de leitura de notificações. Essa exigência é documentada pela [Microsoft](https://learn.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/notification-listener); a ausência no empacotamento atual foi verificada no código. A integração permanece bloqueada até concluir o empacotamento e validar o acesso.
- **Água no Linux:** existe registro manual, mas nenhum agendamento de lembretes. Mantido em desenvolvimento até implementar o recurso anunciado.
- **Teams no Windows:** permitido como beta somente para estado estimado por processos/títulos. A loja deixou de anunciar presença, microfone e notificações como confirmados.
- **GitHub no Linux:** renomeado na loja para Perfil público do GitHub; não anuncia gráfico de contribuições que o painel ainda não possui.

O catálogo compartilhado está em [WidgetStoreCatalog.cs](../src/DockWindows.Core/Widgets/WidgetStoreCatalog.cs). A checagem local de ferramentas/sessão Linux está em [LinuxWidgetAvailability.cs](../src/GigaDock.Infrastructure.Linux/LinuxWidgetAvailability.cs).
