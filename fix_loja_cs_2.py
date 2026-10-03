path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\LojaWidgetsWindow.xaml.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Replace the dummy logic I just added with the real properties it expects
bad_logic = """                WidgetSelecionado = new WidgetInstanceConfig
                {
                    Id = "REMOVE",
                    Tipo = item.Tipo
                };"""
                
good_logic = """                Removeu = true;
                WidgetParaRemover = item.Tipo;"""
                
c = c.replace(bad_logic, good_logic)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
