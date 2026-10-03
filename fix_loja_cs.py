path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\LojaWidgetsWindow.xaml.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

insert = """    private void BtnRemover_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ItemLoja item && item.JaAdicionado)
        {
            var w = _widgetsInstalados.FirstOrDefault(x => x.Tipo == item.Tipo);
            if (w != null)
            {
                // To tell the caller to remove it, we return it with a specific property or clear it
                // Instead of a new property, let's just return a "RemoveMe" dummy configuration
                WidgetSelecionado = new WidgetInstanceConfig
                {
                    Id = "REMOVE",
                    Tipo = item.Tipo
                };
                DialogResult = true;
                Close();
            }
        }
    }
}"""

c = c.replace("    }\n}", insert)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
