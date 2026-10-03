path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Core\Models\Preferencias.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

import re
c = re.sub(r'Widgets = new WidgetConfig \{ RelogioHabilitado = true(.*?)\}', r'Widgets = new WidgetConfig { RelogioHabilitado = false\1}', c)
c = re.sub(r'PomodoroHabilitado = true', r'PomodoroHabilitado = false', c)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
