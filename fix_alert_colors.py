path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

import re

old_logic = """            if (AlertasVisuaisHabilitados)
            {
                string proc = (app.Titulo ?? string.Empty).ToLowerInvariant();
                string cor = "#0A84FF"; // Blue default
                
                if (proc.Contains("teams")) cor = "#4A448C"; // Roxo
                else if (proc.Contains("whatsapp")) cor = "#25D366"; // Verde
                else if (proc.Contains("discord")) cor = "#5865F2"; // Azul discord
                else if (proc.Contains("slack")) cor = "#E01E5A"; // Rosa slack
                
                CorAlerta = cor;
                EstaEmAlerta = true;
            }"""

new_logic = """            if (AlertasVisuaisHabilitados)
            {
                string proc = (app.Titulo ?? string.Empty).ToLowerInvariant();
                string cor = string.Empty;
                
                if (proc.Contains("teams") || proc.Contains("msteams")) cor = "#4A448C"; // Roxo
                else if (proc.Contains("whatsapp")) cor = "#25D366"; // Verde
                else if (proc.Contains("discord")) cor = "#5865F2"; // Azul discord
                
                if (!string.IsNullOrEmpty(cor))
                {
                    CorAlerta = cor;
                    EstaEmAlerta = true;
                }
            }"""

c = c.replace(old_logic, new_logic)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
