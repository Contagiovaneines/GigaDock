path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

import re

# We will just insert the RainbowGlowBorder right before the DockBorder using a regex or simple split
if "RainbowGlowBorder" not in c:
    parts = c.split("<!-- BARRA PRINCIPAL FLUTUANTE (DOCK) -->")
    
    good = """<!-- RAINBOW GLOW BORDER -->
            <Border x:Name="RainbowGlowBorder" CornerRadius="{Binding RaioCantosDock}"
                    Margin="-3"
                    BorderThickness="3"
                    Visibility="{Binding GlowRgbVisivel, Converter={StaticResource BoolToVis}}">
                <Border.Background>
                    <SolidColorBrush Color="#05FFFFFF"/>
                </Border.Background>
                <Border.BorderBrush>
                    <LinearGradientBrush StartPoint="0,0.5" EndPoint="1,0.5">
                        <LinearGradientBrush.RelativeTransform>
                            <RotateTransform x:Name="RainbowRotate" CenterX="0.5" CenterY="0.5" Angle="0"/>
                        </LinearGradientBrush.RelativeTransform>
                        <GradientStop Color="#FF0000" Offset="0.0"/>
                        <GradientStop Color="#FF00FF" Offset="0.16"/>
                        <GradientStop Color="#0000FF" Offset="0.33"/>
                        <GradientStop Color="#00FFFF" Offset="0.5"/>
                        <GradientStop Color="#00FF00" Offset="0.66"/>
                        <GradientStop Color="#FFFF00" Offset="0.83"/>
                        <GradientStop Color="#FF0000" Offset="1.0"/>
                    </LinearGradientBrush>
                </Border.BorderBrush>
                <Border.Effect>
                    <BlurEffect Radius="14" KernelType="Gaussian"/>
                </Border.Effect>
                <Border.Triggers>
                    <EventTrigger RoutedEvent="Loaded">
                        <BeginStoryboard>
                            <Storyboard RepeatBehavior="Forever">
                                <DoubleAnimation Storyboard.TargetName="RainbowRotate" Storyboard.TargetProperty="Angle" From="0" To="360" Duration="0:0:3"/>
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                </Border.Triggers>
            </Border>

            <!-- BARRA PRINCIPAL FLUTUANTE (DOCK) -->"""
            
    c = parts[0] + good + parts[1]

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
