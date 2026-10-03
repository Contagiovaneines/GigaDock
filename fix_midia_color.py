path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MidiaWidgetViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add _corPredominanteHex
c = c.replace("private string? _capaAlbumUrl = string.Empty;", "private string? _capaAlbumUrl = string.Empty;\n    private string _corPredominanteHex = \"#000000\";")
c = c.replace("public string? CapaAlbumUrl", "public string CorPredominanteHex { get => _corPredominanteHex; set => SetProperty(ref _corPredominanteHex, value); }\n\n    public string? CapaAlbumUrl")

# Find the block where RunOnUiAsync is called at the end of UpdateMediaPropertiesAsync
bad_block = """            await RunOnUiAsync(() =>
            {
                Titulo = titulo;
                Artista = artista;
                CapaAlbumUrl = capaPath;
                EstaTocando = estaTocando;
                FonteCor = cor;
                FonteIcone = icone;
                FonteNome = nome;
            });"""

good_block = """            await RunOnUiAsync(() =>
            {
                Titulo = titulo;
                Artista = artista;
                CapaAlbumUrl = capaPath;
                EstaTocando = estaTocando;
                FonteCor = cor;
                FonteIcone = icone;
                FonteNome = nome;
                
                string dominColor = "#000000";
                if (!string.IsNullOrEmpty(capaPath))
                {
                    try
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(capaPath);
                        bmp.DecodePixelWidth = 10;
                        bmp.DecodePixelHeight = 10;
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        var formatted = new System.Windows.Media.Imaging.FormatConvertedBitmap(bmp, System.Windows.Media.PixelFormats.Pbgra32, null, 0);
                        int bwidth = formatted.PixelWidth;
                        int bheight = formatted.PixelHeight;
                        int bytesPerPixel = 4;
                        byte[] pixels = new byte[bwidth * bheight * bytesPerPixel];
                        formatted.CopyPixels(pixels, bwidth * bytesPerPixel, 0);
                        long pr = 0, pg = 0, pb = 0;
                        for (int i = 0; i < pixels.Length; i += bytesPerPixel) { pb += pixels[i]; pg += pixels[i + 1]; pr += pixels[i + 2]; }
                        int count = pixels.Length / bytesPerPixel;
                        if (count > 0) dominColor = $"#{(byte)(pr/count):X2}{(byte)(pg/count):X2}{(byte)(pb/count):X2}";
                    }
                    catch { }
                }
                CorPredominanteHex = dominColor;
            });"""

c = c.replace(bad_block, good_block)

# Also fix the exception catch block
c = c.replace("FonteNome = string.Empty;\n            });\n        }", "FonteNome = string.Empty;\n                CorPredominanteHex = \"#000000\";\n            });\n        }")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
