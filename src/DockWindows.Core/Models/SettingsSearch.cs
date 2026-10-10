using System.Globalization;
using System.Text;

namespace DockWindows.Core.Models;

public sealed record SettingsSearchEntry(string Title, string WindowsSection, string LinuxSection, string Keywords);

public static class SettingsSearch
{
    private static readonly SettingsSearchEntry[] Entries =
    [
        new("Aplicativos e ambientes", "Ambientes", "Ambientes", "apps buscar fixar arquivos URLs duplicar ordem cor trabalho estudos pessoal"),
        new("Widgets e tarefas", "Widgets", "Widgets", "notas checklist calendário agenda relógio pomodoro clima música bateria rede som água"),
        new("Pokédex", "Mascotes", "Pokédex", "pokemon mascote espécie animação"),
        new("Divisores", "Espacadores", "Divisores", "separador espaçamento"),
        new("Aparência", "Aparencia", "Aparência", "tema cores opacidade transparência cantos altura tamanho reduzir movimento efeitos"),
        new("Visualizações", "Visualizacoes", "Visualizações", "monitor tela janelas prévia seletor acima topo"),
        new("Geral", "Geral", "Geral", "inicialização iniciar autostart atualização instalação atalhos"),
        new("Utilitários", "Utilitarios", "Utilitários", "diagnóstico backup restaurar exportar importar"),
        new("Sobre", "Sobre", "Sobre", "versão créditos licença"),
    ];

    public static IReadOnlyList<SettingsSearchEntry> Find(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var terms = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Entries.Where(entry => terms.All(term => Normalize(entry.Title + " " + entry.Keywords).Contains(term, StringComparison.Ordinal))).ToArray();
    }

    private static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
}
