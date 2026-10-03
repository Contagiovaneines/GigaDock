path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionWidgets.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        <!-- WIDGET: DISCORD -->
        <local:SectionDiscordInline Visibility="{Binding Discord.Habilitado, Converter={StaticResource BoolToVis}}" Margin="2,0"/>
    </StackPanel>
</UserControl>"""

good = """        <!-- WIDGET: DISCORD -->
        <local:SectionDiscordInline Visibility="{Binding Discord.Habilitado, Converter={StaticResource BoolToVis}}" Margin="2,0"/>

        <!-- WIDGET: OBS STUDIO -->
        <local:SectionObsInline Visibility="{Binding Obs.Habilitado, Converter={StaticResource BoolToVis}}" Margin="2,0"/>
    </StackPanel>
</UserControl>"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
