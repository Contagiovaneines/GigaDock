path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionMidiaInline.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add a blurred background image based on the album art
target = """            <Grid>
                
                <Grid Margin="6,0">"""

replacement = """            <Grid>
                <!-- Fundo da capa do album borrado e invisivel -->
                <Border CornerRadius="8" ClipToBounds="True" Margin="2,2" Background="#101015">
                    <Image Source="{Binding Midia.CapaAlbumUrl}" Stretch="UniformToFill" Opacity="0.25">
                        <Image.Effect>
                            <BlurEffect Radius="25" RenderingBias="Quality"/>
                        </Image.Effect>
                    </Image>
                </Border>
                
                <Grid Margin="6,0">"""

c = c.replace(target, replacement)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
