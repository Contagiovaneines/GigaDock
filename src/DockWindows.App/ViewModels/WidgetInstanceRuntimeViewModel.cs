using System.IO;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class WidgetInstanceRuntimeViewModel : ObservableObject, IWidgetRuntime
{
    private bool _ativo;
    private bool _disposed;

    public WidgetInstanceRuntimeViewModel(WidgetInstanceConfig config, string? scopeId = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(Config.Id)) Config.Id = Guid.NewGuid().ToString("N");

        if (Config.Tipo == TipoWidget.Relogio)
        {
            Relogio = new ClockWidgetViewModel
            {
                Habilitado = Config.Visivel,
                Formato = Config.Formato,
                Estilo = EstilosWidget.Resolver(Config)
            };
            Relogio.FusoHorarioId = Config.ObterConfiguracao("fusoHorarioId");
        }
        else if (Config.Tipo == TipoWidget.Notas)
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DockWindows", "widgets", SanitizarId(scopeId ?? "global"), SanitizarId(Config.Id));
            Notas = new NotasWidgetViewModel(Path.Combine(pasta, "notas.txt"))
            {
                Habilitado = Config.Visivel,
                Formato = Config.Formato
            };
        }
        else
        {
            throw new NotSupportedException($"O widget {Config.Tipo} ainda não possui runtime por instância.");
        }
    }

    public WidgetInstanceConfig Config { get; }
    public string InstanceId => Config.Id;
    public TipoWidget Tipo => Config.Tipo;
    public string Nome => Config.Nome;
    public ClockWidgetViewModel? Relogio { get; }
    public NotasWidgetViewModel? Notas { get; }
    public bool EhRelogio => Relogio != null;
    public bool EhNotas => Notas != null;
    public bool Ativo { get => _ativo; private set => SetProperty(ref _ativo, value); }

    public void Ativar(bool visual, bool segundoPlano = false)
    {
        if (_disposed) return;
        Ativo = true;
        var estado = new EstadoAtividade(true, visual, segundoPlano, visual);
        Relogio?.DefinirAtividade(estado);
        Notas?.DefinirAtividade(estado);
    }

    public void Suspender()
    {
        if (_disposed) return;
        Ativo = false;
        var estado = new EstadoAtividade(false, false, false, false);
        Relogio?.DefinirAtividade(estado);
        Notas?.DefinirAtividade(estado);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Ativo = false;
        Relogio?.Dispose();
        Notas?.Dispose();
    }

    private static string SanitizarId(string id)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var seguro = new string(id.Where(c => !invalidos.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(seguro) ? Guid.NewGuid().ToString("N") : seguro;
    }
}
