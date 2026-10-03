path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Core\Models\Preferencias.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

import re

c = c.replace("public bool AlertasVisuaisHabilitados { get; set; } = true;", "public bool AlertasVisuaisHabilitados { get; set; } = true;\n    public bool ModoRgbMedia { get; set; } = false;")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
