path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\AjustesViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """                var wgtRemover = WidgetsAmbiente.FirstOrDefault(w => w.Tipo == janelaLoja.WidgetParaRemover);
                if (wgtRemover != null)
                {
                    WidgetsAmbiente.Remove(wgtRemover);
                    AmbienteSelecionado.WidgetsInstalados.Remove(wgtRemover);
                    AlternarVisibilidadeWidget(wgtRemover); // Disable in MainVM"""

good = """                var wgtRemover = WidgetsAmbiente.FirstOrDefault(w => w.Tipo == janelaLoja.WidgetParaRemover);
                if (wgtRemover != null)
                {
                    wgtRemover.Visivel = false; // <<< OBRIGATORIO: desativa antes de atualizar o MainViewModel
                    WidgetsAmbiente.Remove(wgtRemover);
                    AmbienteSelecionado.WidgetsInstalados.Remove(wgtRemover);
                    AlternarVisibilidadeWidget(wgtRemover); // Disable in MainVM"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
