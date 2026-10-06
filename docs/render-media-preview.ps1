Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$root='C:\Users\giovane\Documents\dockwindows'
$xaml=[IO.File]::ReadAllText("$root\src\DockWindows.App\Views\Sections\SectionMidiaInline.xaml")
$xaml=$xaml -replace 'x:Class="[^"]+"','' -replace 'xmlns:conv="[^"]+"',''
$xaml=$xaml -replace '<conv:BooleanToVisibilityConverter x:Key="BoolToVis"/>',''
$xaml=$xaml.Replace('Visibility="{Binding Midia.TemMidia, Converter={StaticResource BoolToVis}}"','Visibility="Visible"')
$control=[Windows.Markup.XamlReader]::Parse($xaml)
$cover=Get-ChildItem "$env:TEMP\dockwindows_media_thumb_*.jpg" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$control.DataContext=[pscustomobject]@{
    AlturaBarra=112
    Midia=[pscustomobject]@{Titulo="Wanna Be Startin' Somethin'"; Artista='Michael Jackson'; EstaTocando=$true; FonteNome='Spotify'; FonteCor='#1ED760'; FonteIcone='♫'; CapaAlbumUrl=if($cover){$cover.FullName}else{$null}}
}
$control.Measure([Windows.Size]::new(460,124))
$control.Arrange([Windows.Rect]::new(0,0,460,124))
$control.UpdateLayout()
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(920,248,192,192,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($control)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream=[IO.File]::Create("$root\docs\spotify-widget-preview.png")
$encoder.Save($stream)
$stream.Dispose()