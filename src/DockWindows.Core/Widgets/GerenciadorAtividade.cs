namespace DockWindows.Core.Widgets;

public readonly record struct EstadoAtividade(bool Habilitado, bool Visual, bool SegundoPlano, bool Animacoes);
public enum SaudeWidget { Disponivel, Erro, Indisponivel }
public readonly record struct DiagnosticoWidget(bool Instalado, bool Habilitado, bool Visivel, bool Executando, bool Pausado, bool Destruido, SaudeWidget Saude, string? Motivo);

/// <summary>Não cria timers: coordena os donos existentes de trabalho e tokens de renderização.</summary>
public sealed class GerenciadorAtividade : IDisposable
{
    private sealed class Entrada
    {
        public required Action<EstadoAtividade> Aplicar;
        public required Action Liberar;
        public bool Habilitado, Background, BackgroundIndependente, ObservarMontado;
        public readonly Dictionary<object, bool> Visuais = new();
        public EstadoAtividade Estado;
        public bool Instalado = true, Destruido;
        public SaudeWidget Saude;
        public string? Motivo;
        public Func<bool?>? EmExecucao;
    }
    private readonly Dictionary<string, Entrada> _entradas = new();
    private bool _disposed, _suspenso;
    public bool DockVisivel { get; private set; }
    public bool AnimacoesPermitidas { get; private set; } = true;
    public int Componentes => _entradas.Count;
    public int TokensVisuais => _entradas.Values.Sum(e => e.Visuais.Count);
    public int VisuaisAtivos => _entradas.Values.Count(e => e.Estado.Visual);
    public int BackgroundAtivos => _entradas.Values.Count(e => e.Estado.SegundoPlano);
    public void Registrar(string id, Action<EstadoAtividade> aplicar, Action liberar, bool backgroundIndependente = false, bool observarMontado = false, Func<bool?>? emExecucao = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entradas.TryAdd(id, new Entrada { Aplicar = aplicar, Liberar = liberar, BackgroundIndependente = backgroundIndependente, ObservarMontado = observarMontado, EmExecucao = emExecucao }))
            throw new InvalidOperationException($"Componente já registrado: {id}");
        aplicar(default);
    }
    public void DefinirDock(bool visivel, bool animacoes)
    {
        DockVisivel = visivel; AnimacoesPermitidas = animacoes;
        foreach (var e in _entradas.Values.ToArray()) Reavaliar(e);
    }
    public void Definir(string id, bool habilitado, bool segundoPlano = false)
    {
        if (!_entradas.TryGetValue(id, out var e)) return;
        e.Habilitado = habilitado; e.Background = segundoPlano; Reavaliar(e);
    }
    public IDisposable Renderizar(string id, object token, bool visivel)
    {
        if (!_entradas.TryGetValue(id, out var e)) return new Registro(() => { });
        e.Visuais[token] = visivel; Reavaliar(e);
        return new Registro(() => { if (e.Visuais.Remove(token)) Reavaliar(e); });
    }
    public void Visibilidade(string id, object token, bool visivel)
    {
        if (!_entradas.TryGetValue(id, out var e) || !e.Visuais.ContainsKey(token)) return;
        e.Visuais[token] = visivel; Reavaliar(e);
    }
    private void Reavaliar(Entrada e)
    {
        if (e.Destruido) return;
        var habilitado = e.Instalado && e.Habilitado;
        var visual = !_suspenso && habilitado && DockVisivel && e.Visuais.Values.Any(v => v);
        var estado = new EstadoAtividade(habilitado, visual,
            !_suspenso && e.Instalado && ((e.Background && (habilitado || e.BackgroundIndependente)) || (e.ObservarMontado && habilitado && DockVisivel && e.Visuais.Count > 0)), visual && AnimacoesPermitidas);
        if (estado == e.Estado) return;
        e.Estado = estado;
        e.Aplicar(estado);
    }
    public EstadoAtividade Estado(string id) => _entradas.TryGetValue(id, out var e) ? e.Estado : default;
    public void DefinirInstalado(string id, bool instalado)
    { if (_entradas.TryGetValue(id, out var e)) { e.Instalado = instalado; Reavaliar(e); } }
    public void InformarSaude(string id, SaudeWidget saude, string? motivo = null)
    { if (_entradas.TryGetValue(id, out var e)) { e.Saude = saude; e.Motivo = motivo; } }
    public DiagnosticoWidget Diagnostico(string id) => _entradas.TryGetValue(id, out var e)
        ? new(e.Instalado, e.Estado.Habilitado, e.Estado.Visual, Executando(e),
            e.Instalado && !Executando(e), false, e.Saude, e.Motivo)
        : new(false, false, false, false, false, true, SaudeWidget.Indisponivel, "Componente não registrado.");
    private bool Executando(Entrada e) => !_suspenso && !e.Destruido && e.Instalado && (e.EmExecucao?.Invoke() ?? (e.Estado.Visual || e.Estado.SegundoPlano));
    public void Suspender(bool suspenso)
    { _suspenso = suspenso; foreach (var e in _entradas.Values.ToArray()) Reavaliar(e); }
    public void Atualizar(string id)
    { if (!_disposed && _entradas.TryGetValue(id, out var e)) e.Aplicar(e.Estado); }
    public void Remover(string id)
    {
        if (!_entradas.Remove(id, out var e)) return;
        e.Destruido = true; e.Visuais.Clear();
        try { e.Aplicar(default); } finally { e.Liberar(); }
    }
    public void Dispose() { if (_disposed) return; _disposed = true; foreach (var id in _entradas.Keys.ToArray()) Remover(id); }
    private sealed class Registro(Action remover) : IDisposable
    {
        private Action? _remover = remover;
        public void Dispose() => Interlocked.Exchange(ref _remover, null)?.Invoke();
    }
}
