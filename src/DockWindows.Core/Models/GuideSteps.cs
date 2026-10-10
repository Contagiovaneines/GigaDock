namespace DockWindows.Core.Models;

public sealed record GuideStep(string Title, string Description, string WindowsSection, string LinuxSection);

public static class GuideSteps
{
    public static IReadOnlyList<GuideStep> All { get; } =
    [
        new("Seus ambientes", "Trabalho, Estudos e Pessoal guardam seus próprios itens e widgets. Você pode criar e duplicar ambientes sem alterar as janelas de outros programas.", "Ambientes", "Ambientes"),
        new("Aplicativos e arquivos", "Busque aplicativos ou adicione arquivos, pastas e sites. Organize a ordem dos itens e reúna-os em coleções. A abertura acontece ao clicar.", "Ambientes", "Ambientes"),
        new("Widgets e tarefas", "Escolha os widgets do ambiente. Clique na dock para abrir o painel de detalhes. Tarefas são listas com conclusão; notas continuam como texto livre.", "Widgets", "Widgets"),
        new("Sua aparência", "A aparência atual é o padrão. Temas e decorações são opcionais. No Linux, confira a prévia antes de aplicar; as opções disponíveis podem variar por sistema.", "Aparencia", "Aparência"),
        new("Tudo neste computador", "As configurações e tarefas ficam locais. Widgets públicos só consultam seus serviços quando você solicita. A dock mantém a barra de tarefas e os painéis do sistema funcionais.", "Geral", "Geral"),
    ];
}
