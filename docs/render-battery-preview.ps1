$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$root='C:\Users\giovane\Documents\dockwindows'
$xaml=[IO.File]::ReadAllText("$root\src\DockWindows.App\Views\Sections\SectionBateria.xaml")
$xaml=$xaml -replace 'x:Class="[^"]+"','' -replace 'xmlns:conv="[^"]+"',''
$xaml=$xaml -replace '<conv:BooleanToVisibilityConverter x:Key="BoolToVis"/>',''
$xaml=$xaml.Replace('Visibility="{Binding Bateria.Habilitado, Converter={StaticResource BoolToVis}}"','Visibility="Visible"')
$xaml=$xaml.Replace('Visibility="{Binding Bateria.Carregando, Converter={StaticResource BoolToVis}}"','Visibility="Collapsed"')
$control=[Windows.Markup.XamlReader]::Parse($xaml)
$control.DataContext=[pscustomobject]@{Bateria=[pscustomobject]@{Porcentagem='61%'; LarguraCarga=10.98; CorCarga='#E1E4E8'; Descricao='Dados ilustrativos'}}
$background=[Windows.Controls.Border]::new()
$background.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#10131B')
$background.Padding=[Windows.Thickness]::new(8)
$background.Child=$control
$window=[Windows.Window]::new()
$window.WindowStyle='None'
$window.ShowInTaskbar=$false
$window.Left=-10000
$window.Top=-10000
$window.SizeToContent='WidthAndHeight'
$window.Content=$background
$window.Show()
$background.UpdateLayout()
$background.Dispatcher.Invoke([Action]{}, [Windows.Threading.DispatcherPriority]::ApplicationIdle)
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int]($background.ActualWidth*3),[int]($background.ActualHeight*3),288,288,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($background)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream=[IO.File]::Create("$root\docs\battery-widget-preview.png")
$encoder.Save($stream)
$stream.Dispose()
$window.Close()