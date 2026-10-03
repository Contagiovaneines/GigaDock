path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

import re
pattern = r"(if\s*\(app\s*!=\s*null\)\s*\{\s*app\.NumeroNotificacoes\+\+;\s*\})"
replacement = r"""\1
        else
        {
            if (proc.Contains("teams") || proc.Contains("msteams"))
                Teams.MensagensNaoLidas++;
            else if (proc.Contains("whatsapp"))
                WhatsApp.MensagensNaoLidas++;
        }"""

c = re.sub(pattern, replacement, c)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
