path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\LojaWidgetsWindow.xaml.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """            DialogResult = true;
            Close();
        }
    private void BtnRemover_Click"""

good = """            DialogResult = true;
            Close();
        }
    }

    private void BtnRemover_Click"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
