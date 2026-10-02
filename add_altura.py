import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

altura_xaml = """
                                <Border Height="1" Background="#2A2A33" Margin="0,0,0,16"/>

                                <Grid Margin="0,0,0,16">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <Border Width="28" Height="28" CornerRadius="6" Background="#2C2C36" Margin="0,0,12,0">
                                        <TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE90A;" Foreground="#A0A0A8" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                    </Border>
                                    <TextBlock Grid.Column="1" Text="Altura da Barra" FontSize="13" Foreground="#E5E7EB" VerticalAlignment="Center"/>
                                    <TextBlock Grid.Column="2" Text="{Binding AlturaBarra, StringFormat='{}{0:N0} px'}" FontSize="13" Foreground="#8E8E93" VerticalAlignment="Center"/>
                                </Grid>
                                <Slider Minimum="48" Maximum="128" Value="{Binding AlturaBarra, Mode=TwoWay}" Margin="40,0,0,24"/>
"""

# Find where to insert it. We can insert it before "Fundo Translúcido (Blur)"
target_pattern = r'(\s*<Border Height="1" Background="#2A2A33" Margin="0,0,0,16"/>\s*<Grid Margin="0,0,0,8">\s*<Grid\.ColumnDefinitions>\s*<ColumnDefinition Width="Auto"/>\s*<ColumnDefinition Width="\*"/>\s*<ColumnDefinition Width="Auto"/>\s*</Grid\.ColumnDefinitions>\s*<Border Width="28" Height="28" CornerRadius="6" Background="#2C2C36" Margin="0,0,12,0">\s*<TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE756;")'

content = re.sub(target_pattern, altura_xaml + r'\1', content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
