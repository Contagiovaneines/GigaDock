import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\GitHubWidgetViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

content = content.replace('private string? _nomeUsuario = "Contagiovaneines";', 'private string? _nomeUsuario = string.Empty;')

sync_pattern = r"public void SincronizarUsuario\(string\? usuario\)\s*\{.*?\}(?=\s*private async Task CarregarContribuicoesAsync)"
sync_replacement = """public void SincronizarUsuario(string? usuario)
    {
        if (usuario != _nomeUsuario || Contribuicoes.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(usuario))
            {
                _nomeUsuario = usuario;
                OnPropertyChanged(nameof(NomeUsuario));
                _ = CarregarContribuicoesAsync();
            }
            else
            {
                _nomeUsuario = string.Empty;
                Contribuicoes.Clear();
                TotalContribuicoes = 0;
            }
        }
        if (!_timer.IsEnabled) _timer.Start();
    }
"""

content = re.sub(sync_pattern, lambda _: sync_replacement, content, flags=re.DOTALL)

pattern = r"private async Task CarregarContribuicoesAsync\(\).*?Application\.Current\?\.Dispatcher\.Invoke"
replacement = r"""private async Task CarregarContribuicoesAsync()
    {
        if (string.IsNullOrWhiteSpace(_nomeUsuario)) return;
        Carregando = true;
        try
        {
            var url = $"https://github.com/users/{_nomeUsuario}/contributions";
            var html = await _http.GetStringAsync(url);
            var dias = new List<ContribuicaoDia>();
            int total = 0;

            var matches = Regex.Matches(html, "data-date=\"([^\"]+)\"[^>]*data-level=\"(\\d+)\"");
            if (matches.Count == 0)
            {
                var matches2 = Regex.Matches(html, "data-level=\"(\\d+)\"[^>]*data-date=\"([^\"]+)\"");
                if (matches2.Count > 0)
                {
                    foreach (Match m in matches2)
                    {
                        dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[2].Value), Nivel = int.Parse(m.Groups[1].Value) });
                    }
                }
                else
                {
                    matches = Regex.Matches(html, "data-date=\"([^\"]+)\"[^>]*>\\s*(\\d+)\\s+contribution");
                }
            }

            foreach (Match m in matches)
            {
                dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[1].Value), Nivel = int.Parse(m.Groups[2].Value) });
            }

            var matchesTooltip = Regex.Matches(html, "(\\d+)\\s+contributions?\\s+on");
            if (matchesTooltip.Count > 0)
            {
                foreach (Match mt in matchesTooltip) total += int.Parse(mt.Groups[1].Value);
            }
            else total = dias.Count(d => d.Nivel > 0);

            Application.Current?.Dispatcher.Invoke"""

content = re.sub(pattern, lambda _: replacement, content, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
