import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

tamanho_xaml = """
                                <Border Height="1" Background="#2A2A33" Margin="0,0,0,16"/>

                                <Grid Margin="0,0,0,16">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <Border Width="28" Height="28" CornerRadius="6" Background="#2C2C36" Margin="0,0,12,0">
                                        <TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE81E;" Foreground="#A0A0A8" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                    </Border>
                                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                                        <TextBlock Text="Tamanho dos Ícones" FontSize="13" Foreground="#E5E7EB"/>
                                        <TextBlock Text="Tamanho dos ícones dos aplicativos (32px, 40px, 48px)" FontSize="10" Foreground="#8E8E93"/>
                                    </StackPanel>
                                    <ComboBox Grid.Column="2" Width="120" Background="#2C2C36" Foreground="#FFFFFF" BorderBrush="#383844" BorderThickness="1" 
                                              SelectedValue="{Binding TamanhoIcones, Mode=TwoWay}" SelectedValuePath="Tag" VerticalAlignment="Center">
                                        <ComboBoxItem Content="Pequeno" Tag="Pequeno"/>
                                        <ComboBoxItem Content="Médio" Tag="Medio"/>
                                        <ComboBoxItem Content="Grande" Tag="Grande"/>
                                    </ComboBox>
                                </Grid>
"""

# Insert before "Altura da Barra"
target_pattern = r'(\s*<Border Height="1" Background="#2A2A33" Margin="0,0,0,16"/>\s*<Grid Margin="0,0,0,16">\s*<Grid\.ColumnDefinitions>\s*<ColumnDefinition Width="Auto"/>\s*<ColumnDefinition Width="\*"/>\s*<ColumnDefinition Width="Auto"/>\s*</Grid\.ColumnDefinitions>\s*<Border Width="28" Height="28" CornerRadius="6" Background="#2C2C36" Margin="0,0,12,0">\s*<TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE90A;")'

content = re.sub(target_pattern, tamanho_xaml + r'\1', content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
