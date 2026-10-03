path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionTeamsInline.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        </Border>
    </Grid>"""

good = """        </Border>
        
        <!-- Badge de Notificação -->
        <Border Background="#E02424" BorderBrush="#161B22" BorderThickness="1.5" 
                MinWidth="18" Height="18" CornerRadius="9" Padding="4,0"
                HorizontalAlignment="Right" VerticalAlignment="Top"
                Margin="0,-6,-6,0"
                Visibility="{Binding Teams.TemMensagem, Converter={StaticResource BoolToVis}}">
            <TextBlock Text="{Binding Teams.MensagensNaoLidas}" FontSize="10" FontWeight="Bold" Foreground="White" 
                       HorizontalAlignment="Center" VerticalAlignment="Center" Margin="0,-1,0,0"/>
        </Border>
    </Grid>"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
