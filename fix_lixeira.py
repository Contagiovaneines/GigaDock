import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionRelogioControles.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

lixeira_xaml = """
        <!-- Separador Lixeira -->
        <Rectangle Width="1" Height="24"
                   Fill="{Binding SeparadorColor, Converter={StaticResource ColorToBrushConverter}}"
                   Visibility="{Binding ExibirLixeira, Converter={StaticResource BoolToVis}}"
                   Margin="6,0"/>

        <!-- Lixeira -->
        <Button Command="{Binding AbrirLixeiraCommand}"
                Visibility="{Binding ExibirLixeira, Converter={StaticResource BoolToVis}}"
                Width="32" Height="32" Margin="2,0"
                Background="Transparent" BorderThickness="0"
                Foreground="#FFFFFF" FontSize="16"
                Cursor="Hand" ToolTip="Lixeira">
            <Button.Content>
                <TextBlock FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets" Text="&#xE74D;" VerticalAlignment="Center" HorizontalAlignment="Center"/>
            </Button.Content>
            <Button.Template>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Border" CornerRadius="10" Background="{TemplateBinding Background}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Border" Property="Background" Value="#30FFFFFF"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Button.Template>
        </Button>
"""

content = content.replace("</StackPanel>\n</UserControl>", f"{lixeira_xaml}    </StackPanel>\n</UserControl>")

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
