using System;
using System.IO;
using System.Text.RegularExpressions;

var f = @"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\GitHubWidgetViewModel.cs";
var c = File.ReadAllText(f);
var pattern = @"(?s)private async Task CarregarContribuicoesAsync\(\).*?Application\.Current\?\.Dispatcher\.Invoke";
var replacement = @"    private async Task CarregarContribuicoesAsync()
    {
        if (string.IsNullOrWhiteSpace(_nomeUsuario)) return;
        Carregando = true;
        try
        {
            var url = $""https://github.com/users/{_nomeUsuario}/contributions"";
            var html = await _http.GetStringAsync(url);
            var dias = new System.Collections.Generic.List<ContribuicaoDia>();
            int total = 0;

            var matches = System.Text.RegularExpressions.Regex.Matches(html, @""data-date=\""""(\d{4}-\d{2}-\d{2})\""""[^>]*data-level=\""""(\d+)\"""""");
            if (matches.Count == 0)
            {
                var matches2 = System.Text.RegularExpressions.Regex.Matches(html, @""data-level=\""""(\d+)\""""[^>]*data-date=\""""(\d{4}-\d{2}-\d{2})\""""""");
                if (matches2.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match m in matches2)
                    {
                        dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[2].Value), Nivel = int.Parse(m.Groups[1].Value) });
                    }
                }
                else
                {
                    matches = System.Text.RegularExpressions.Regex.Matches(html, @""data-date=\""""(\d{4}-\d{2}-\d{2})\""""[^>]*>\s*(\d+)\s+contribution"");
                }
            }

            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[1].Value), Nivel = int.Parse(m.Groups[2].Value) });
            }

            var matchesTooltip = System.Text.RegularExpressions.Regex.Matches(html, @""(\d+)\s+contributions?\s+on"");
            if (matchesTooltip.Count > 0)
            {
                foreach (System.Text.RegularExpressions.Match mt in matchesTooltip) total += int.Parse(mt.Groups[1].Value);
            }
            else total = System.Linq.Enumerable.Count(dias, d => d.Nivel > 0);

            System.Windows.Application.Current?.Dispatcher.Invoke";
c = Regex.Replace(c, pattern, replacement);
File.WriteAllText(f, c);
