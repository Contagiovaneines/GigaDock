path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = '<ToggleButton Grid.Column="1" Style="{StaticResource ModernToggleStyle}" IsChecked="{Binding AlertasVisuaisHabilitados}" VerticalAlignment="Center"/>'
good = '<CheckBox Grid.Column="1" Style="{StaticResource ToggleSwitchStyle}" IsChecked="{Binding AlertasVisuaisHabilitados, Mode=TwoWay}" VerticalAlignment="Center"/>'

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
