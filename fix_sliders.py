path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Find the Aparencia panel in Column 3 and inject the sliders block after the ListBox closing tag
# The anchor we have: "</ListBox>" inside EhSecaoAparencia section before next section

slider_block = """
                        <!-- BLOCO: DIMENSOES E APARENCIA -->
                        <Border Background="#1C1C22" BorderBrush="#2A2A33" BorderThickness="1" CornerRadius="12"
                                Padding="16" Margin="0,16,0,0"
                                Visibility="{Binding EhSecaoAparencia, Converter={StaticResource BoolToVis}}">
                            <StackPanel>
                                <TextBlock Text="DIMENSOES DA BARRA" FontSize="11" FontWeight="Bold" Foreground="#8E8E93" Margin="0,0,0,12"/>

                                <!-- Altura da Barra -->
                                <Grid Margin="0,0,0,16">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="Altura da Barra" FontSize="13" Foreground="#E5E7EB"/>
                                        <TextBlock Text="Altura total do dock em pixels." FontSize="11" Foreground="#8E8E93"/>
                                        <Slider Minimum="48" Maximum="120" SmallChange="2" LargeChange="8"
                                                Value="{Binding AlturaBarra, Mode=TwoWay}"
                                                Margin="0,8,0,0"/>
                                    </StackPanel>
                                    <Border Grid.Column="1" Background="#2C2C36" CornerRadius="8" Padding="8,4" Margin="12,0,0,0" VerticalAlignment="Top">
                                        <TextBlock FontSize="13" FontWeight="Bold" Foreground="#FFFFFF">
                                            <Run Text="{Binding AlturaBarra, StringFormat='{}{0:0}'}"/>
                                            <Run Text="px"/>
                                        </TextBlock>
                                    </Border>
                                </Grid>

                                <!-- Opacidade -->
                                <Grid Margin="0,0,0,16">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="Opacidade do Dock" FontSize="13" Foreground="#E5E7EB"/>
                                        <TextBlock Text="Transparencia do fundo da barra." FontSize="11" Foreground="#8E8E93"/>
                                        <Slider Minimum="0.3" Maximum="1.0" SmallChange="0.05" LargeChange="0.1"
                                                Value="{Binding OpacidadeDock, Mode=TwoWay}"
                                                Margin="0,8,0,0"/>
                                    </StackPanel>
                                    <Border Grid.Column="1" Background="#2C2C36" CornerRadius="8" Padding="8,4" Margin="12,0,0,0" VerticalAlignment="Top">
                                        <TextBlock FontSize="13" FontWeight="Bold" Foreground="#FFFFFF">
                                            <Run Text="{Binding OpacidadeDock, StringFormat='{}{0:P0}'}"/>
                                        </TextBlock>
                                    </Border>
                                </Grid>

                                <!-- Raio dos Cantos -->
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="Raio dos Cantos" FontSize="13" Foreground="#E5E7EB"/>
                                        <TextBlock Text="Arredondamento das bordas do dock." FontSize="11" Foreground="#8E8E93"/>
                                        <Slider Minimum="0" Maximum="40" SmallChange="2" LargeChange="4"
                                                Value="{Binding RaioCantosDock, Mode=TwoWay}"
                                                Margin="0,8,0,0"/>
                                    </StackPanel>
                                    <Border Grid.Column="1" Background="#2C2C36" CornerRadius="8" Padding="8,4" Margin="12,0,0,0" VerticalAlignment="Top">
                                        <TextBlock FontSize="13" FontWeight="Bold" Foreground="#FFFFFF">
                                            <Run Text="{Binding RaioCantosDock, StringFormat='{}{0:0}'}"/>
                                        </TextBlock>
                                    </Border>
                                </Grid>
                            </StackPanel>
                        </Border>
"""

# Inject just before closing tag of the Aparencia StackPanel in Column 3
# The StackPanel has: EhSecaoAparencia and Margin="0,0,0,20"
# It ends with... let's find a unique anchor: the "<!-- 5. PAINEL DE EDICAO GERAL" comment
anchor = "<!-- 5. PAINEL DE EDI"
c = c.replace(anchor, slider_block + "\n                    " + anchor)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
