path_github = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionGitHubInline.xaml"
with open(path_github, "r", encoding="utf-8") as f:
    c = f.read()

c = c.replace("<ContextMenu>", "<ContextMenu DataContext=\"{Binding PlacementTarget.DataContext, RelativeSource={RelativeSource Self}}\">")

with open(path_github, "w", encoding="utf-8") as f:
    f.write(c)

path_teams = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionTeamsInline.xaml"
with open(path_teams, "r", encoding="utf-8") as f:
    c2 = f.read()

c2 = c2.replace("<ContextMenu>", "<ContextMenu DataContext=\"{Binding PlacementTarget.DataContext, RelativeSource={RelativeSource Self}}\">")

with open(path_teams, "w", encoding="utf-8") as f:
    f.write(c2)
