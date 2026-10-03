path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\LojaWidgetsWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add Click="BtnRemover_Click" Tag="{Binding}"
old_btn = """<Button Grid.Column="1" Visibility="{Binding JaAdicionado, Converter={StaticResource BoolToVis}}" 
                                                        Content="&#xE712;" FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" FontSize="14" 
                                                        Background="Transparent" Foreground="#8E8E93" BorderBrush="#252733" BorderThickness="1" 
                                                        Width="32" Height="32" Margin="8,0,0,0" Cursor="Hand">"""

new_btn = """<Button Grid.Column="1" Visibility="{Binding JaAdicionado, Converter={StaticResource BoolToVis}}" 
                                                        Content="&#xE74D;" FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" FontSize="14" 
                                                        Background="Transparent" Foreground="#8E8E93" BorderBrush="#252733" BorderThickness="1" 
                                                        Width="32" Height="32" Margin="8,0,0,0" Cursor="Hand" Click="BtnRemover_Click" Tag="{Binding}">"""

c = c.replace(old_btn, new_btn)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
