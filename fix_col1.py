import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove the blocks inside Column 1
# Since Column 1 is wrapped in <Border Grid.Column="1" ...>
# We can extract Column 1 content, process it, and put it back.

col1_pattern = r'(<Border Grid\.Column="1" Background="#19191D" BorderBrush="#282830" BorderThickness="0,0,1,0">.*?)(<!-- ========================================================= -->\s*<!-- COLUNA 3: EDIÇÃO)'
col1_match = re.search(col1_pattern, content, flags=re.DOTALL)

if col1_match:
    col1_content = col1_match.group(1)
    
    # Remove Aparência
    col1_content = re.sub(r'<!-- 4\..*?</ListBox>', '', col1_content, flags=re.DOTALL)
    
    # Remove Geral
    col1_content = re.sub(r'<!-- 5\..*?</StackPanel>\s*</StackPanel>', '', col1_content, flags=re.DOTALL)
    
    # Remove Utilitários
    col1_content = re.sub(r'<!-- 6\..*?</StackPanel>\s*</StackPanel>', '', col1_content, flags=re.DOTALL)
    
    # Remove Sobre
    col1_content = re.sub(r'<!-- 7\..*?</StackPanel>\s*</StackPanel>', '', col1_content, flags=re.DOTALL)
    
    # Update the whole content
    content = content[:col1_match.start(1)] + col1_content + content[col1_match.end(1):]

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
