path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\LojaWidgetsWindow.xaml.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad_class = """public class ItemLoja
{
    public TipoWidget Tipo { get; set; }"""
good_class = """public class ItemLoja
{
    public TipoWidget Tipo { get; set; }
    public string TagStatus { get; set; } = string.Empty;
    public string TagStatusBackground => string.IsNullOrEmpty(TagStatus) ? "Transparent" : "#4A2016";
    public string TagStatusForeground => string.IsNullOrEmpty(TagStatus) ? "Transparent" : "#FF795A";"""

c = c.replace(bad_class, good_class)

# Update Catalogo
bad_discord = """            new ItemLoja { 
                Tipo = TipoWidget.DiscordVoz, Formato = FormatoWidget.Expandido, Nome = "Discord", 
                Descricao = "Veja quem est falando no seu canal de voz e controle seu microfone e udio.",
                DescricaoFormato = "Canal de Voz", Icone = "\uE716", CorIcone = "#20242B", Categoria = "Comunicao","""
good_discord = """            new ItemLoja { 
                Tipo = TipoWidget.DiscordVoz, Formato = FormatoWidget.Expandido, Nome = "Discord", 
                Descricao = "Mock: Exibe interface visual de salas de voz. API Oficial em desenvolvimento.",
                DescricaoFormato = "Canal de Voz", Icone = "\uE716", CorIcone = "#20242B", Categoria = "Comunicao", TagStatus = "BETA", """
c = c.replace(bad_discord, good_discord)

bad_github = """            new ItemLoja { 
                Tipo = TipoWidget.GitHubContribuicoes, Formato = FormatoWidget.Expandido, Nome = "GitHub Actions", 
                Descricao = "Acompanhe PRs abertas, code reviews pendentes e status de pipelines do GitHub Actions.",
                DescricaoFormato = "Grfico de Commits", Icone = "\uE943", CorIcone = "#20242B", Categoria = "Dev Tools", Custo = "Pro","""
good_github = """            new ItemLoja { 
                Tipo = TipoWidget.GitHubContribuicoes, Formato = FormatoWidget.Expandido, Nome = "GitHub Actions", 
                Descricao = "Mock: Gráfico de contribuições. A conexão oficial com o GitHub está em construção.",
                DescricaoFormato = "Grfico de Commits", Icone = "\uE943", CorIcone = "#20242B", Categoria = "Dev Tools", Custo = "Pro", TagStatus = "BETA","""
c = c.replace(bad_github, good_github)

bad_obs = """            new ItemLoja { 
                Tipo = TipoWidget.OBSStudio, Formato = FormatoWidget.Expandido, Nome = "OBS Studio Control", 
                Descricao = "Inicie e pare gravaes diretamente da dock, abra o OBS com um clique. Minimalista.",
                DescricaoFormato = "Rec e Status", Icone = "\uE714", CorIcone = "#1C1C22", Categoria = "Mdia", Custo = "Gratuito","""
good_obs = """            new ItemLoja { 
                Tipo = TipoWidget.OBSStudio, Formato = FormatoWidget.Expandido, Nome = "OBS Studio Control", 
                Descricao = "Mock: Botões visuais de rec/stop. A integração com o WebSockets do OBS chegará em breve.",
                DescricaoFormato = "Rec e Status", Icone = "\uE714", CorIcone = "#1C1C22", Categoria = "Mdia", Custo = "Gratuito", TagStatus = "BETA","""
c = c.replace(bad_obs, good_obs)

bad_teams = """            new ItemLoja { 
                Tipo = TipoWidget.ReunioesTeams, Formato = FormatoWidget.Expandido, Nome = "Microsoft Teams", 
                Descricao = "Controle de microfone rpido, status de presena em chamada e deteco de reunies agendadas.",
                DescricaoFormato = "Status Detalhado", Icone = "\uE716", CorIcone = "#292138", Categoria = "Comunicao","""
good_teams = """            new ItemLoja { 
                Tipo = TipoWidget.ReunioesTeams, Formato = FormatoWidget.Expandido, Nome = "Microsoft Teams", 
                Descricao = "Exibe notificações de mensagens e status de reunião baseado nas notificações do sistema.",
                DescricaoFormato = "Status Detalhado", Icone = "\uE716", CorIcone = "#292138", Categoria = "Comunicao","""
c = c.replace(bad_teams, good_teams)

bad_midia = """            new ItemLoja { 
                Tipo = TipoWidget.Midia, Formato = FormatoWidget.Expandido, Nome = "Tocando Agora (Mdia)", 
                Descricao = "Conecta com Spotify e Apple Music. Exibe a capa do lbum, msica atual e botes de prxima/pausar.",
                DescricaoFormato = "Capa do lbum", Icone = "\uE189", CorIcone = "#1A3223", Categoria = "Mdia","""
good_midia = """            new ItemLoja { 
                Tipo = TipoWidget.Midia, Formato = FormatoWidget.Expandido, Nome = "Tocando Agora (Mdia)", 
                Descricao = "Lê informações de mídia globais (Spotify, Edge, etc). Toca, pausa e exibe o álbum (100% Funcional).",
                DescricaoFormato = "Capa do lbum", Icone = "\uE189", CorIcone = "#1A3223", Categoria = "Mdia","""
c = c.replace(bad_midia, good_midia)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
