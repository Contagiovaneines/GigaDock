path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\Views\Sections\SectionWhatsAppInline.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """                <TextBlock Text="{Binding WhatsApp.MensagensNaoLidas, StringFormat='{}{0} mensagens'}" FontSize="12" FontWeight="SemiBold" Foreground="#FFFFFF" VerticalAlignment="Center" Margin="0,0,4,0"/>"""
good = """                <TextBlock Text="{Binding WhatsApp.MensagensNaoLidas, StringFormat='{}{0} mensagens'}" FontSize="12" FontWeight="SemiBold" Foreground="#FFFFFF" VerticalAlignment="Center" Margin="0,0,4,0" Visibility="{Binding WhatsApp.TemMensagens, Converter={StaticResource BoolToVis}}"/>"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
