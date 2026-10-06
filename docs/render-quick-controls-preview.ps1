$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
[Windows.Media.RenderOptions]::ProcessRenderMode=[Windows.Interop.RenderMode]::SoftwareOnly
$root='C:\Users\giovane\Documents\dockwindows'
$xaml=[IO.File]::ReadAllText("$root\src\DockWindows.App\Views\Sections\SectionControlesRapidos.xaml")
$xaml=$xaml -replace 'x:Class="[^"]+"','' -replace 'xmlns:conv="[^"]+"',''
$xaml=$xaml -replace '<conv:BooleanToVisibilityConverter x:Key="QuickBoolToVis"/>',''
$xaml=$xaml -replace ' Opened="PanelPopup_Opened"','' -replace 'PreviewKeyDown="Panel_PreviewKeyDown"',''
$xaml=$xaml.Replace('Visibility="{Binding TecladoBloqueado, Converter={StaticResource QuickBoolToVis}}"','Visibility="Collapsed"')
$document=[xml]$xaml
$resource=[regex]::Match($xaml,'(?s)<UserControl.Resources>.*?</UserControl.Resources>').Value
$border=[regex]::Match($xaml,'(?s)<Border x:Name="Panel".*?</Popup>').Value.Replace('</Popup>','').Trim()
$standalone='<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">'+$resource+$border+'</UserControl>'

$panel=[Windows.Markup.XamlReader]::Parse($standalone)
$names=@('Wi-Fi','Bluetooth','Modo escuro','Foco','Bloquear teclado','Bloquear tela','Suspender')
$icons=@([char]0xE701,[char]0xE702,[char]0xE708,[char]0xE708,[char]0xE765,[char]0xE72E,[char]0xE708)
$colors=@('#3488E7','#3488E7','#A675EE','#ECA342','#54C5B2','#829DA9','#9894E5')
$items=for($i=0;$i -lt 7;$i++){
    [pscustomobject]@{Nome=$names[$i]; Icone=[string]$icons[$i]; Cor=$colors[$i]; Ajuda=''; Status=if($i -lt 5){'Na dock'}else{'Oculto da dock'}; MostrarNaDock=($i -lt 5)}
}
$panel.DataContext=[pscustomobject]@{Quantidade=5; Itens=@($items)}
$window=[Windows.Window]::new()
$window.WindowStyle='None'
$window.ShowInTaskbar=$false
$window.Left=-10000
$window.Top=-10000
$window.SizeToContent='WidthAndHeight'
$window.Content=$panel
$window.Show()
$panel.Measure([Windows.Size]::new(340,620))
$panel.Arrange([Windows.Rect]::new(0,0,340,$panel.DesiredSize.Height))
$panel.UpdateLayout()
$panel.Dispatcher.Invoke([Action]{}, [Windows.Threading.DispatcherPriority]::ApplicationIdle)
$panel.UpdateLayout()
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(680,[int]($panel.ActualHeight*2),192,192,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($panel)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream=[IO.File]::Create("$root\docs\quick-controls-preview.png")
$encoder.Save($stream)
$stream.Dispose()
$window.Close()