import io

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\AjustesWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    lines = f.readlines()

new_lines = []
in_col1 = False
skip_mode = False

for line in lines:
    if '<!-- COLUNA 2: ITENS DA' in line:
        in_col1 = True
    elif '<!-- COLUNA 3: EDI' in line:
        in_col1 = False
        skip_mode = False

    if in_col1:
        if '<!-- 4. APAR' in line:
            skip_mode = True
        
        if skip_mode:
            # We want to stop skipping after 7. SOBRE closes its StackPanels.
            # But the easiest way is to just skip until we hit </Grid> and </Border> that ends Column 1!
            if '</Grid>' in line and len(line) < 30: # Just to be safe, find the closing Grid of Column 1
                # Wait, Column 1 has <Grid Margin="12,14">
                pass

    if not skip_mode:
        new_lines.append(line)
        
    if skip_mode and '</Grid>' in line and line.strip() == '</Grid>':
        # End of Column 1 Grid! Wait, we shouldn't skip the </Grid>.
        skip_mode = False
        new_lines.append(line)

with open(path + ".test", "w", encoding="utf-8") as f:
    f.writelines(new_lines)
