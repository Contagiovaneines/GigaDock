using System.Windows.Threading;
using System.Windows.Input;
using System.IO;
using System.Linq;
using DockWindows.App.Common;

namespace DockWindows.App.ViewModels;

public class NotasWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => _timerSalvar.IsEnabled || _pendenteSalvar;
    public DockWindows.Core.Widgets.SaudeWidget Saude => string.IsNullOrEmpty(ErroPersistencia) ? DockWindows.Core.Widgets.SaudeWidget.Disponivel : DockWindows.Core.Widgets.SaudeWidget.Erro;
    public string? MotivoEstado => Descricao;

    private string _textoNotas = string.Empty;
    private bool _painelAberto;
    private bool _habilitado;
    private DockWindows.Core.Models.FormatoWidget _formato = DockWindows.Core.Models.FormatoWidget.Expandido;
    private readonly DispatcherTimer _timerSalvar;
    private readonly string _arquivo;
    private bool _pendenteSalvar, _disposed;
    public string ErroPersistencia { get; private set; } = "";
    public int QuantidadeLinhas => _textoNotas.Split('\n', '\r').Count(l => !string.IsNullOrWhiteSpace(l));
    public string Descricao => string.IsNullOrEmpty(ErroPersistencia) ? "Clique para editar suas notas. Salvamento automático local." : ErroPersistencia;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public DockWindows.Core.Models.FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }

    public string TextoNotas
    {
        get => _textoNotas;
        set
        {
            if (!_disposed && SetProperty(ref _textoNotas, value))
            {
                _pendenteSalvar = true;
                _timerSalvar.Stop();
                _timerSalvar.Start(); // Salva após 2s sem digitar
                OnPropertyChanged(nameof(ResumoNotas));
                OnPropertyChanged(nameof(QuantidadeLinhas));
            }
        }
    }

    public string ResumoNotas
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_textoNotas)) return "📝 Sem anotações";
            var primeira = _textoNotas.Split('\n', '\r').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
            return primeira.Length > 25 ? "📝 " + primeira[..22] + "..." : "📝 " + primeira;
        }
    }

    public bool PainelAberto
    {
        get => _painelAberto;
        set => SetProperty(ref _painelAberto, value);
    }

    public ICommand AlternarPainelCommand { get; }

    public NotasWidgetViewModel(string? arquivo = null)
    {
        _arquivo = arquivo ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "notas.txt");
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);

        _timerSalvar = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timerSalvar.Tick += (_, _) =>
        {
            _timerSalvar.Stop();
            if (_pendenteSalvar)
            {
                SalvarNotas();
            }
        };

        CarregarNotas();
    }

    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        if (!estado.Visual) PainelAberto = false;
        if (!estado.Habilitado) { _timerSalvar.Stop(); if (_pendenteSalvar) { SalvarNotas(); } }
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _timerSalvar.Stop(); if (_pendenteSalvar) SalvarNotas(); }

    private void CarregarNotas()
    {
        try
        {
            var arquivo = _arquivo;
            if (File.Exists(arquivo))
            {
                _textoNotas = File.ReadAllText(arquivo);
                OnPropertyChanged(nameof(TextoNotas));
                OnPropertyChanged(nameof(ResumoNotas));
            }
        }
        catch { ErroPersistencia = "Não foi possível carregar as notas salvas."; OnPropertyChanged(nameof(ErroPersistencia)); OnPropertyChanged(nameof(Descricao)); }
    }

    private void SalvarNotas()
    {
        try
        {
            var arquivo = _arquivo;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivo))!);
            var temporario = arquivo + ".tmp";
            File.WriteAllText(temporario, _textoNotas);
            File.Move(temporario, arquivo, overwrite: true);
            _pendenteSalvar = false; ErroPersistencia = ""; OnPropertyChanged(nameof(ErroPersistencia)); OnPropertyChanged(nameof(Descricao));
        }
        catch { ErroPersistencia = "Não foi possível salvar as notas. O texto continua no editor."; OnPropertyChanged(nameof(ErroPersistencia)); OnPropertyChanged(nameof(Descricao)); }
    }
}



