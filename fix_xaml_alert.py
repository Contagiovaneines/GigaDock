path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Insert the toggle for Neon Alerts under Geral > Elementos da Barra
insert = """                                <!-- Alertas Visuais -->
                                <Border Background="#25252C" CornerRadius="8" Padding="12,8" Margin="0,0,0,8">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <StackPanel Grid.Column="0">
                                            <TextBlock Text="Alertas Visuais Neon" FontSize="13" Foreground="#E5E7EB" FontWeight="SemiBold"/>
                                            <TextBlock Text="Faz a dock brilhar na cor do aplicativo quando recebe mensagem." FontSize="11" Foreground="#8E8E93" Margin="0,2,0,0"/>
                                        </StackPanel>
                                        <ToggleButton Grid.Column="1" Style="{StaticResource ModernToggleStyle}" IsChecked="{Binding AlertasVisuaisHabilitados}" VerticalAlignment="Center"/>
                                    </Grid>
                                </Border>

"""
c = c.replace('<!-- Lixeira -->', insert + '<!-- Lixeira -->')

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
