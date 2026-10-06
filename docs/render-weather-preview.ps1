Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$root='C:\Users\giovane\Documents\dockwindows'
$xaml=[IO.File]::ReadAllText("$root\src\DockWindows.App\Views\Sections\SectionClimaInline.xaml")
$xaml=$xaml -replace 'x:Class="[^"]+"','' -replace 'xmlns:conv="[^"]+"',''
$xaml=$xaml -replace '<conv:BooleanToVisibilityConverter x:Key="BoolToVis"/>',''
$xaml=$xaml.Replace('Visibility="{Binding Clima.Habilitado, Converter={StaticResource BoolToVis}}"','Visibility="Visible"')
$control=[Windows.Markup.XamlReader]::Parse($xaml)
$control.DataContext=[pscustomobject]@{
    AlturaBarra=100
    Clima=[pscustomobject]@{Temperatura='26°'; Local='Dados ilustrativos'; Condicao='Chuva'; Previsoes=@(
        [pscustomobject]@{Dia='seg'; Temperatura='30°'; Condicao='Chuva'; TipoIcone='Chuva'},
        [pscustomobject]@{Dia='ter'; Temperatura='30°'; Condicao='Chuva'; TipoIcone='Chuva'},
        [pscustomobject]@{Dia='qua'; Temperatura='30°'; Condicao='Chuva'; TipoIcone='Chuva'}
    )}
}
$control.Measure([Windows.Size]::new(290,106))
$control.Arrange([Windows.Rect]::new(0,0,290,106))
$control.UpdateLayout()
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(580,212,192,192,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($control)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream=[IO.File]::Create("$root\docs\weather-widget-preview.png")
$encoder.Save($stream)
$stream.Dispose()