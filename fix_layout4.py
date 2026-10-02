import io
import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Fix Column Width
content = re.sub(
    r'<ColumnDefinition Width="360" MinWidth="300" MaxWidth="500"/>',
    r'<ColumnDefinition Width="Auto" MaxWidth="500"/>',
    content
)

# Apply Visibility Collapse to Border Grid.Column=1
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


# Move Widgets to Column 3 (which is Grid.Column=2)
# First we find Widgets in Column 1
widgets_pat = r'(<!-- 2\. WIDGETS -->\s*<StackPanel Grid\.Row="0" Visibility="\{Binding EhSecaoWidgets, Converter=\{StaticResource BoolToVis\}\}">.*?</Grid>\s*</Grid>)'
widgets_match = re.search(widgets_pat, content, flags=re.DOTALL)
if widgets_match:
    widgets_code = widgets_match.group(1)
    content = content.replace(widgets_code, "")
    
    # We clean it
    widgets_clean = re.sub(r'<Grid Grid\.Row="1" Visibility="\{Binding EhSecaoWidgets, Converter=\{StaticResource BoolToVis\}\}">', r'<Grid Visibility="{Binding EhSecaoWidgets, Converter={StaticResource BoolToVis}}" Margin="0,16,0,0">', widgets_code)
    
    # We inject it into Column 3 Grid
    col3_pat = r'(<!-- CONTE[A-Z0-9&#;ÇÊ]* DIN[A-Z0-9&#;ÇÊ]* CONFORME A SE[A-Z0-9&#;ÇÊ]* -->\s*<Grid>)'
    
    content = re.sub(col3_pat, r'\1\n' + widgets_clean, content)

# Now delete the other redundant blocks in Column 1
col1_pattern = r'(<Border Grid\.Column="1" Background="#19191D" BorderBrush="#282830" BorderThickness="0,0,1,0">.*?</Border>)'
# Wait, col1 has </Border>. But there are nested borders! So we shouldn't use .*?</Border> blindly.
# Since we know exactly where Column 3 starts, let's just slice it.

split_idx = content.find('<!-- COLUNA 3: EDI')
col1_part = content[:split_idx]
col3_part = content[split_idx:]

# In col1_part, remove blocks
col1_part = re.sub(r'<!-- 4\. APAR[A-Z0-9&#;ÇÊ]* \(TEMAS PR[A-Z0-9&#;ÇÊ]*-DEFINIDOS\) -->.*?<!-- 5\. GERAL \(LISTA DE CATEGORIAS\) -->', '<!-- 5. GERAL (LISTA DE CATEGORIAS) -->', col1_part, flags=re.DOTALL)
col1_part = re.sub(r'<!-- 5\. GERAL \(LISTA DE CATEGORIAS\) -->.*?</Grid>\s*</Border>', '</Grid>\n        </Border>', col1_part, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(col1_part + col3_part)
