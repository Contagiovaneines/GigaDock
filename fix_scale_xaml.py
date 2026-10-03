path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

old = '<StackPanel x:Name="SectionsContainer" Orientation="Horizontal" VerticalAlignment="Center"/>'
new = """<StackPanel x:Name="SectionsContainer" Orientation="Horizontal" VerticalAlignment="Center"
                          RenderTransformOrigin="0.5,0.5">
                        <StackPanel.LayoutTransform>
                            <ScaleTransform ScaleX="{Binding EscalaUI}" ScaleY="{Binding EscalaUI}"/>
                        </StackPanel.LayoutTransform>
                    </StackPanel>"""
c = c.replace(old, new)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
