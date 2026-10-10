using DockWindows.Core.Models;

namespace DockWindows.Core.Widgets;

public enum WidgetPlatform { Windows, Linux }
public enum WidgetStoreState { Available, Beta, InDevelopment, Unavailable }

public sealed record WidgetStoreEntry(TipoWidget Kind, string Name, string Description, string HowTo,
    string Requirements, WidgetStoreState State = WidgetStoreState.Available)
{
    public bool CanInstall => State is WidgetStoreState.Available or WidgetStoreState.Beta;
    public string Status => State switch
    {
        WidgetStoreState.Beta => "Beta funcional",
        WidgetStoreState.InDevelopment => "Beta · em desenvolvimento",
        WidgetStoreState.Unavailable => "Indisponível nesta plataforma",
        _ => "Disponível"
    };
}

/// <summary>O contrato mínimo da loja descreve o que está implementado, sem prometer paridade entre plataformas.</summary>
public static class WidgetStoreCatalog
{
    public static IReadOnlyList<WidgetStoreEntry> ForPlatform(WidgetPlatform platform) =>
        Enum.GetValues<TipoWidget>().Select(kind => Get(kind, platform)).ToArray();

    public static WidgetStoreEntry Get(TipoWidget kind, WidgetPlatform platform)
    {
        var entry = kind switch
        {
            TipoWidget.Relogio => Entry(kind, "Relógio", "Mostra hora e data e permite escolher o mostrador.", "Clique para abrir o calendário; personalize o estilo nos Ajustes.", "Usa o horário do computador. No Linux o painel mostra hora local."),
            TipoWidget.Pomodoro => Entry(kind, "Pomodoro", "Cronometra períodos de foco e descanso.", "Abra o widget e use Iniciar/Pausar, Reiniciar e Próxima fase.", "Funciona localmente enquanto o GigaDock estiver aberto."),
            TipoWidget.CalendarioCompromissos => Entry(kind, "Calendário", "Consulta e organiza compromissos locais.", "Abra o painel e adicione título, data e horário. No Windows também importa iCalendar.", "Links iCalendar exigem internet; no Linux o mínimo atual é a agenda local."),
            TipoWidget.Notas => Entry(kind, "Notas", "Guarda texto local para o ambiente.", "Clique para editar. Windows salva automaticamente; Linux exige clicar em Salvar nota.", "Sem conta ou internet. Os dados ficam neste computador."),
            TipoWidget.MonitorSistema => Entry(kind, "Monitor do sistema", "Exibe uso real de CPU e memória.", "Abra o painel para acompanhar as leituras. A primeira amostra de CPU pode levar alguns segundos.", "Windows oferece também rede/disco; Linux lê CPU/RAM em /proc."),
            TipoWidget.CotacaoMoedas => Entry(kind, "Cotação de moedas", "Consulta taxas de referência entre moedas.", "Escolha origem e destino. No Linux abra o painel e clique em Consultar câmbio.", "Internet para atualizar. Windows usa referência BCE com cache; Linux usa Frankfurter. Sem negociação."),
            TipoWidget.GitHubContribuicoes => Entry(kind, "GitHub contribuições", "Exibe contribuições públicas de um usuário do GitHub.", "Informe o usuário nas configurações do widget e atualize a consulta.", "Requer internet. Leitura de HTML pode mudar; não acessa repositórios privados, Actions ou PRs.", WidgetStoreState.Beta),
            TipoWidget.Clima => Entry(kind, "Clima", "Consulta clima e previsão para uma cidade informada.", "Informe a cidade. No Linux clique em Consultar previsão de 7 dias no painel.", "Requer internet e envia a cidade ao provedor: wttr.in no Windows, Open-Meteo no Linux."),
            TipoWidget.WhatsAppNotificacoes => Entry(kind, "WhatsApp", "Integração de notificações do WhatsApp ainda indisponível neste instalador.", "Instalação bloqueada até concluir e validar a leitura de notificações.", "A leitura precisa de identidade assinada e consentimento no Windows, e ainda não foi validada nesta distribuição; abrir o WhatsApp não equivale a ler mensagens.", WidgetStoreState.InDevelopment),
            TipoWidget.TeamsStatus => Entry(kind, "Teams · estado estimado", "Estima se o Teams está aberto ou em reunião por processos e títulos de janela.", "Abra o Teams e consulte o indicador na dock; clique para abrir o aplicativo.", "Estimativa local: não confirma presença oficial, microfone, calendário ou notificações.", WidgetStoreState.Beta),
            TipoWidget.DiscordVoz => Entry(kind, "Discord · voz", "Canal, participantes e estado de voz ainda não estão integrados.", "Instalação bloqueada enquanto a integração de voz estiver em desenvolvimento.", "Detectar ou abrir o Discord não confirma uma chamada.", WidgetStoreState.InDevelopment),
            TipoWidget.OBSStudio => Entry(kind, "OBS Studio", "Consulta gravação/transmissão e inicia ou para gravação no OBS local.", "Ative o WebSocket nas ferramentas do OBS; configure porta e senha e conecte antes de usar os controles.", "Requer OBS aberto com WebSocket v5. Linux usa 127.0.0.1 e senha somente no painel; Windows usa configuração protegida local.", WidgetStoreState.Beta),
            TipoWidget.Midia => Entry(kind, "Mídia", "Consulta a faixa atual e oferece controles de reprodução.", "Inicie um player compatível e use Anterior, Tocar/Pausar e Próxima.", "Windows exige sessão de mídia compatível; Linux exige playerctl e um player MPRIS."),
            TipoWidget.Bateria => Entry(kind, "Bateria", "Mostra carga e estado da bateria detectada pelo sistema.", "Instale para acompanhar a carga; abra o painel para detalhes quando disponível.", "Requer bateria exposta pelo sistema. Um computador sem bateria mostra esse estado, sem valores simulados."),
            TipoWidget.LembreteAgua => Entry(kind, "Lembrete de água", "Lembra de beber água e permite contar copos consumidos nesta sessão.", "Configure intervalo e horário ativo nos Ajustes e registre quando beber água.", "No Windows os lembretes dependem do GigaDock em execução."),
            TipoWidget.AreaTransferencia => Entry(kind, "Área de transferência", "Mantém histórico dos textos copiados nesta sessão.", "Copie um texto, abra o widget e selecione o item que deseja copiar novamente.", "Windows: até 20 textos; sem histórico permanente. Não há implementação Linux."),
            TipoWidget.ArquivosRecentes => Entry(kind, "Downloads e capturas", "Lista arquivos recentes de Downloads e Imagens.", "Abra o painel, atualize a lista e clique em um arquivo para abrir. Capturar abre a ferramenta do Windows.", "Até 20 arquivos das pastas acessíveis. Não há implementação Linux."),
            TipoWidget.Conectividade => Entry(kind, "Conectividade", "Mostra informações da conexão local.", "Abra o painel para consultar o estado da rede.", "Windows consulta Wi-Fi/Bluetooth; Linux mostra interfaces de rede ativas, sem controle Bluetooth."),
            TipoWidget.AudioSistema => Entry(kind, "Áudio do sistema", "Consulta e ajusta o volume.", "Abra o painel. No Linux consulte o volume e clique em Aplicar volume depois de ajustar o controle.", "Windows oferece mixer por aplicativo; Linux exige pactl e servidor compatível, com volume da saída padrão."),
            TipoWidget.EstanteArquivos => Entry(kind, "Estante de arquivos", "Organiza referências locais com prévias seguras.", "Abra o painel, adicione um arquivo e use as ações de prévia, abrir ou remover.", "Windows: até 30 referências; não executa conteúdo incorporado nas prévias. Não há implementação Linux."),
            TipoWidget.MascotePokemon => Entry(kind, "Pokédex", "Exibe um mascote animado com sprites locais.", "Ative e escolha a espécie nos Ajustes da Pokédex.", "Sem internet para os sprites incluídos. Créditos e direitos dos recursos gráficos preservados."),
            TipoWidget.Tarefas => Entry(kind, "Tarefas", "Mantém uma lista local de tarefas por ambiente.", "Abra o painel, escreva uma tarefa e adicione. Marque para concluir ou exclua pela ação correspondente.", "Sem conta ou internet; a lista fica salva neste computador."),
            TipoWidget.SensoresLinux => Entry(kind, "Temperatura e ventoinhas", "Lê temperaturas e rotações disponibilizadas pelo Linux.", "Abra o painel para atualizar as leituras; use o formato expandido para exibi-las na barra.", "Linux: sensores acessíveis em /sys/class/hwmon; máquinas virtuais podem não expor sensores.", WidgetStoreState.Beta),
            TipoWidget.AplicativosFlatpak => Entry(kind, "Aplicativos Flatpak", "Lista aplicativos Flatpak e permite abri-los.", "Abra o painel, atualize a lista e escolha um aplicativo já instalado.", "Linux: requer flatpak. Não instala nem atualiza aplicativos por este widget.", WidgetStoreState.Beta),
            TipoWidget.WorkspacesLinux => Entry(kind, "Áreas de trabalho Linux", "Lista e troca áreas de trabalho do Sway ou Hyprland.", "Abra o painel, atualize a lista e selecione a área desejada.", "Linux: sessão Sway com swaymsg ou Hyprland com hyprctl. GNOME/KDE não possuem integração aqui.", WidgetStoreState.Beta),
            TipoWidget.ScriptLocalLinux => Entry(kind, "Script local", "Executa manualmente um script local e mostra seu resultado.", "Informe um caminho absoluto, salve e clique em Executar. Não há execução automática.", "Linux: arquivo executável escolhido pelo usuário; saída limitada em texto ou JSON text/tooltip.", WidgetStoreState.Beta),
            _ => Entry(kind, "Widget desconhecido", "Implementação não reconhecida.", "Instalação bloqueada.", "Atualize o aplicativo para um catálogo compatível.", WidgetStoreState.InDevelopment)
        };
        if (platform == WidgetPlatform.Windows && kind is TipoWidget.SensoresLinux or TipoWidget.AplicativosFlatpak or TipoWidget.WorkspacesLinux or TipoWidget.ScriptLocalLinux)
            return entry with { State = WidgetStoreState.Unavailable, Requirements = "Widget exclusivo do Linux.", HowTo = "Disponível na versão Linux, com os requisitos indicados." };
        if (platform == WidgetPlatform.Linux)
        {
            if (kind == TipoWidget.Relogio)
                return entry with { Description = "Mostra a hora local do computador.", HowTo = "Clique para ver a hora com segundos e a data. A opção expandida mostra a hora na barra.", Requirements = "Usa o horário do computador. Mostradores e fusos personalizados ainda não fazem parte da interface Linux." };
            if (kind == TipoWidget.Midia)
                return entry with { HowTo = "Abra um player MPRIS, clique em Atualizar faixa e use Anterior, Tocar/Pausar e Próxima.", Requirements = "Requer playerctl; a instalação fica bloqueada enquanto essa ferramenta não estiver disponível." };
            if (kind is TipoWidget.WhatsAppNotificacoes or TipoWidget.TeamsStatus or TipoWidget.DiscordVoz or TipoWidget.AreaTransferencia or TipoWidget.ArquivosRecentes or TipoWidget.EstanteArquivos)
                return entry with { State = WidgetStoreState.InDevelopment, Description = "Este widget ainda não possui integração funcional no Linux.", HowTo = "Instalação bloqueada enquanto a versão Linux estiver em desenvolvimento.", Requirements = "Sem runtime Linux implementado para este recurso." };
            if (kind == TipoWidget.LembreteAgua)
                return entry with { State = WidgetStoreState.InDevelopment, Description = "O registro manual existe, mas os lembretes automáticos ainda não foram implementados no Linux.", HowTo = "Instalação bloqueada até concluir os lembretes.", Requirements = "Registro manual não equivale a um lembrete automático." };
            if (kind == TipoWidget.GitHubContribuicoes)
                return entry with { Name = "Perfil público do GitHub", Description = "Consulta nome público, quantidade de repositórios e seguidores.", HowTo = "Abra o painel, informe um usuário público e clique em Consultar perfil.", Requirements = "Requer internet. Não exibe gráfico de contribuições nem dados privados.", State = WidgetStoreState.Available };
        }
        return entry;
    }

    private static WidgetStoreEntry Entry(TipoWidget kind, string name, string description, string howTo,
        string requirements, WidgetStoreState state = WidgetStoreState.Available) => new(kind, name, description, howTo, requirements, state);
}
