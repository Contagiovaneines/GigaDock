$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$root='C:\Users\giovane\Documents\dockwindows'
$xaml=[IO.File]::ReadAllText("$root\src\DockWindows.App\Views\Sections\SectionRelogioControles.xaml")
$button=[regex]::Match($xaml,'(?s)<Button x:Name="RelogioButton".*?</Button>').Value
$button=$button.Replace('Visibility="{Binding Clock.Habilitado, Converter={StaticResource BoolToVis}}"','Visibility="Visible"')
$standalone='<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Background="#1A2114" Padding="8">'+$button+'</Border>'
$control=[Windows.Markup.XamlReader]::Parse($standalone)
$control.DataContext=[pscustomobject]@{AlturaBarra=80; Clock=[pscustomobject]@{HoraPrincipal='12:26'; DataResumida='Seg, 22 Jun'; DataFormatada='Dados ilustrativos'}}
$window=[Windows.Window]::new()
$window.WindowStyle='None'
$window.ShowInTaskbar=$false
$window.Left=-10000
$window.Top=-10000
$window.Width=160
$window.Height=90
$window.Content=$control
$window.Show()
$control.UpdateLayout()
$control.Dispatcher.Invoke([Action]{}, [Windows.Threading.DispatcherPriority]::ApplicationIdle)
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int]($control.ActualWidth*2),[int]($control.ActualHeight*2),192,192,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($control)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream=[IO.File]::Create("$root\docs\clock-widget-preview.png")
$encoder.Save($stream)
$stream.Dispose()
$window.Close()