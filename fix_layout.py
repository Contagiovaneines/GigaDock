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
# First, extract the Widgets section from Column 1
widgets_pattern = r'<!-- 2\. WIDGETS -->\s*<StackPanel Grid\.Row="0" Visibility="\{Binding EhSecaoWidgets, Converter=\{StaticResource BoolToVis\}\}">.*?</Grid>\s*</Grid>'
widgets_match = re.search(widgets_pattern, content, flags=re.DOTALL)
if widgets_match:
    widgets_code = widgets_match.group(0)
    # Remove from Column 1
    content = content.replace(widgets_code, "")
    
    # Insert at the beginning of Column 2's Grid
    col2_pattern = r'(<!-- COLUNA 3: EDIÇÃO DO ITEM SELECIONADO.*?<Border Grid\.Column="2" Background="#141416">\s*<ScrollViewer VerticalScrollBarVisibility="Auto">\s*<Grid Margin="24">)'
    
    # We need to wrap it so it doesn't conflict. We can just drop it in.
    # Wait, the sections in Column 2 are just StackPanels. So we can just paste widgets_code there.
    # Also, Widgets had `Grid.Row="0"` and `Grid.Row="1"`. Let's wrap Widgets in a single StackPanel instead of using Grid.Rows.
    widgets_clean = re.sub(r'<Grid Grid\.Row="1" Visibility="\{Binding EhSecaoWidgets, Converter=\{StaticResource BoolToVis\}\}">', r'<Grid Visibility="{Binding EhSecaoWidgets, Converter={StaticResource BoolToVis}}" Margin="0,16,0,0">', widgets_code)
    
    # Let's wrap both StackPanel and Grid in a parent StackPanel for Column 2
    widgets_wrapped = f"""
                    <!-- 2. WIDGETS (MOVIDO PARA COLUNA DE DETALHES) -->
                    <StackPanel Visibility="{{Binding EhSecaoWidgets, Converter={{StaticResource BoolToVis}}}}">
                        {widgets_clean}
                    </StackPanel>
"""
    # Insert into Column 2
    content = re.sub(col2_pattern, r'\1\n' + widgets_wrapped, content, flags=re.DOTALL)

# 4. Remove dummy lists from Column 1
# 4. APARÊNCIA
content = re.sub(r'<!-- 4\. APARNCIA.*?</ListBox>', '', content, flags=re.DOTALL)
# 5. GERAL
content = re.sub(r'<!-- 5\. GERAL.*?</StackPanel>\s*</StackPanel>', '', content, flags=re.DOTALL)
# 6. UTILITÁRIOS
content = re.sub(r'<!-- 6\. UTILITRIOS.*?</StackPanel>\s*</StackPanel>', '', content, flags=re.DOTALL)
# 7. SOBRE
content = re.sub(r'<!-- 7\. SOBRE.*?</StackPanel>\s*</StackPanel>', '', content, flags=re.DOTALL)


with open(path, "w", encoding="utf-8") as f:
    f.write(content)
