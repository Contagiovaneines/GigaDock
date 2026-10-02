import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

m = re.search(r'<!-- 4\. APAR.*?E CUSTOMIZ.*?-->\s*<StackPanel.*?Visibility="\{Binding EhSecaoAparencia, Converter=\{StaticResource BoolToVis\}\}".*?(<!-- 5\. PAINEL DE EDI)', content, flags=re.DOTALL)
if m:
    with open("c:\\Users\\giovane\\Documents\\dockwindows\\aparencia.txt", "w", encoding="utf-8") as out:
        out.write(m.group(0))
