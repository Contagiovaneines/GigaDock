namespace DockWindows.Core.Widgets;

public sealed class TemporizadorEngine(TimeSpan duracao, TimeProvider? relogio = null)
{
    private readonly TimeProvider _relogio = relogio ?? TimeProvider.System;
    private readonly TimeSpan _duracao = duracao;
    private TimeSpan _restante = duracao;
    private DateTimeOffset _fim;
    public bool EstaExecutando { get; private set; }
    public TimeSpan Restante => EstaExecutando ? Maximo(_fim - _relogio.GetUtcNow()) : _restante;
    public event Action? Concluido;
    public void Alternar()
    {
        if (EstaExecutando) { _restante = Restante; EstaExecutando = false; }
        else { if (_restante <= TimeSpan.Zero) _restante = _duracao; _fim = _relogio.GetUtcNow() + _restante; EstaExecutando = true; }
    }
    public void Reiniciar() { EstaExecutando = false; _restante = _duracao; }
    public void Tick()
    {
        if (!EstaExecutando || Restante > TimeSpan.Zero) return;
        EstaExecutando = false; _restante = TimeSpan.Zero; Concluido?.Invoke();
    }
    private static TimeSpan Maximo(TimeSpan valor) => valor < TimeSpan.Zero ? TimeSpan.Zero : valor;
}
