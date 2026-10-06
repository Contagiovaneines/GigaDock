using System.Windows.Controls;

namespace DockWindows.App.Views.Sections;

public partial class SectionCalendarioInline : UserControl
{
    public SectionCalendarioInline()
    {
        InitializeComponent();
    }
    private void ConfigurarReuniao_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not DockWindows.App.ViewModels.MainViewModel main) return;
        var atual = main.Calendario.ProximaReuniao;
        var editor = new DockWindows.App.Views.ReuniaoEditorWindow(atual) { Owner = System.Windows.Window.GetWindow(this) };
        if (editor.ShowDialog() != true || editor.Resultado == null) return;
        var lista = main.Preferencias.CompromissosLocais;
        var index = atual == null ? -1 : lista.FindIndex(c => c.Id == atual.Id);
        if (index >= 0) lista[index] = editor.Resultado; else lista.Add(editor.Resultado);
        main.Calendario.SincronizarCompromissos(lista.ToList());
        main.SalvarPreferencias();
    }
}
