path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add the UI toggle below the Neon Alerts toggle in the 'Geral' tab.
# We'll search for 'Alertas Visuais Neon' block and append another block.

block = """                                <!-- Alertas Visuais -->
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
                                        <CheckBox Grid.Column="1" Style="{StaticResource ToggleSwitchStyle}" IsChecked="{Binding AlertasVisuaisHabilitados, Mode=TwoWay}" VerticalAlignment="Center"/>
                                    </Grid>
                                </Border>"""

new_block = block + """

                                <!-- Modo RGB Mídia -->
                                <Border Background="#25252C" CornerRadius="8" Padding="12,8" Margin="0,0,0,8">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <StackPanel Grid.Column="0">
                                            <TextBlock Text="Modo RGB Musical" FontSize="13" Foreground="#E5E7EB" FontWeight="SemiBold"/>
                                            <TextBlock Text="Sombra da dock muda de cor acompanhando a capa da música atual." FontSize="11" Foreground="#8E8E93" Margin="0,2,0,0"/>
                                        </StackPanel>
                                        <CheckBox Grid.Column="1" Style="{StaticResource ToggleSwitchStyle}" IsChecked="{Binding ModoRgbMedia, Mode=TwoWay}" VerticalAlignment="Center"/>
                                    </Grid>
                                </Border>"""

c = c.replace(block, new_block)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
