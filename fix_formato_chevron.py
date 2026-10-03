path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """                                        <StackPanel Orientation="Horizontal">
                                            <TextBlock Text="{Binding Formato, StringFormat='Formato: {0}'}" FontSize="10" Foreground="#C8C8CE" VerticalAlignment="Center"/>
                                            <TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE70D;" FontSize="8" Foreground="#8E8E93" VerticalAlignment="Center" Margin="6,0,0,0"/>
                                        </StackPanel>"""

good = """                                        <StackPanel Orientation="Horizontal">
                                            <TextBlock Text="{Binding Formato, StringFormat='Formato: {0}'}" FontSize="10" Foreground="#C8C8CE" VerticalAlignment="Center"/>
                                        </StackPanel>"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)

# Now check LojaWidgetsWindow.xaml
path2 = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\LojaWidgetsWindow.xaml"
with open(path2, "r", encoding="utf-8") as f2:
    c2 = f2.read()

bad2 = """                                        <StackPanel Orientation="Horizontal">
                                            <TextBlock Text="{Binding Formato, StringFormat='Formato: {0}'}" FontSize="10" Foreground="#C8C8CE" VerticalAlignment="Center"/>
                                            <TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE70D;" FontSize="8" Foreground="#8E8E93" VerticalAlignment="Center" Margin="6,0,0,0"/>
                                        </StackPanel>"""

c2 = c2.replace(bad2, good)

with open(path2, "w", encoding="utf-8") as f2:
    f2.write(c2)
