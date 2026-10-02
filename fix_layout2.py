import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update ColumnDefinition
content = re.sub(
    r'<ColumnDefinition Width="360" MinWidth="300" MaxWidth="500"/>',
    r'<ColumnDefinition Width="Auto" MinWidth="0" MaxWidth="500"/>',
    content
)

# 2. Add Style to Border Grid.Column="1"
border_pattern = r'<Border Grid\.Column="1" Background="#19191D" BorderBrush="#282830" BorderThickness="0,0,1,0">'
border_replacement = r"""<Border Grid.Column="1" Background="#19191D" BorderBrush="#282830" BorderThickness="0,0,1,0">
            <Border.Style>
                <Style TargetType="Border">
                    <Setter Property="Visibility" Value="Collapsed"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding EhSecaoAmbientes}" Value="True">
                            <Setter Property="Visibility" Value="Visible"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding EhSecaoEspacadores}" Value="True">
                            <Setter Property="Visibility" Value="Visible"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>"""
content = content.replace(border_pattern, border_replacement)

# 3. Move Widgets from Column 1 to Column 2
widgets_pattern = r'<!-- 2\. WIDGETS -->\s*<StackPanel Grid\.Row="0" Visibility="\{Binding EhSecaoWidgets, Converter=\{StaticResource BoolToVis\}\}">.*?</Grid>\s*</Grid>'
widgets_match = re.search(widgets_pattern, content, flags=re.DOTALL)
if widgets_match:
    widgets_code = widgets_match.group(0)
    content = content.replace(widgets_code, "")
    
    col2_pattern = r'(<!-- COLUNA 3: EDIÇÃO DO ITEM SELECIONADO.*?<Border Grid\.Column="2" Background="#141416">\s*<ScrollViewer VerticalScrollBarVisibility="Auto">\s*<Grid Margin="24">)'
    widgets_clean = re.sub(r'<Grid Grid\.Row="1" Visibility="\{Binding EhSecaoWidgets, Converter=\{StaticResource BoolToVis\}\}">', r'<Grid Visibility="{Binding EhSecaoWidgets, Converter={StaticResource BoolToVis}}" Margin="0,16,0,0">', widgets_code)
    
    widgets_wrapped = f"""
                    <!-- 2. WIDGETS (MOVIDO PARA COLUNA DE DETALHES) -->
                    <StackPanel Visibility="{{Binding EhSecaoWidgets, Converter={{StaticResource BoolToVis}}}}">
                        {widgets_clean}
                    </StackPanel>
"""
    content = re.sub(col2_pattern, r'\1\n' + widgets_wrapped, content, flags=re.DOTALL)

# 4. Remove dummy lists carefully
content = re.sub(r'<!-- 4\. APAR[A-Z0-9&#;ÇÊ]* \(TEMAS PR[A-Z0-9&#;ÇÊ]*\).*?</ListBox>', '', content, flags=re.DOTALL)
content = re.sub(r'<!-- 5\. GERAL \(LISTA DE CATEGORIAS\).*?</StackPanel>\s*</StackPanel>', '', content, flags=re.DOTALL)
content = re.sub(r'<!-- 6\. UTILIT[A-Z0-9&#;ÇÊ]* \(LISTA DE.*?</StackPanel>\s*</StackPanel>', '', content, flags=re.DOTALL)
content = re.sub(r'<!-- 7\. SOBRE \(LISTA DE.*?</StackPanel>\s*</StackPanel>', '', content, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
