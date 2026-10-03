path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionClimaInline.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        <Border CornerRadius="12" Padding="2" Background="#161B22" Cursor="Hand" ToolTip="Previsão do Tempo">
            <Border.Effect>
                <DropShadowEffect BlurRadius="8" ShadowDepth="2" Opacity="0.3" Color="Black"/>
            </Border.Effect>
            
            <Border CornerRadius="10" Background="#1A4E7A" Padding="12,4,16,4" VerticalAlignment="Stretch">"""

good = """        <Border CornerRadius="12" Padding="2" Background="Transparent" Cursor="Hand" ToolTip="Previsão do Tempo">
            
            <Border CornerRadius="10" Background="Transparent" Padding="12,4,16,4" VerticalAlignment="Stretch">"""

c = c.replace(bad, good)

# Also fix the inner text colors if they are not white
c = c.replace('Foreground="#FFFFFF"', 'Foreground="#E0E0E0"') # Keep it slightly dimmed to match media? Actually #FFFFFF is fine

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
