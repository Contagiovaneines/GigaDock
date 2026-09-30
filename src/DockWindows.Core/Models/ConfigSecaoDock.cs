using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DockWindows.Core.Models;

public class ConfigSecaoDock : INotifyPropertyChanged
{
    private TipoSecaoDock _tipo;
    private string _nome = string.Empty;
    private bool _visivel = true;
    private int _ordem;

    public TipoSecaoDock Tipo
    {
        get => _tipo;
        set { if (_tipo != value) { _tipo = value; OnPropertyChanged(); } }
    }

    public string Nome
    {
        get => _nome;
        set { if (_nome != value) { _nome = value; OnPropertyChanged(); } }
    }

    public bool Visivel
    {
        get => _visivel;
        set { if (_visivel != value) { _visivel = value; OnPropertyChanged(); } }
    }

    public int Ordem
    {
        get => _ordem;
        set { if (_ordem != value) { _ordem = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ConfigSecaoDock Clonar()
    {
        return new ConfigSecaoDock
        {
            Tipo = Tipo,
            Nome = Nome,
            Visivel = Visivel,
            Ordem = Ordem
        };
    }
}
