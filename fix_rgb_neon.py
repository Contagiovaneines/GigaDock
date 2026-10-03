path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """            <!-- RAINBOW GLOW BORDER -->
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
            </Border>"""

good = """            <!-- RAINBOW GLOW BORDER (BLURRED HALO) -->
            <Border x:Name="RainbowGlowBorderOuter" CornerRadius="{Binding RaioCantosDock}"
                    Margin="-1"
                    BorderThickness="3"
                    Visibility="{Binding GlowRgbVisivel, Converter={StaticResource BoolToVis}}">
                <Border.BorderBrush>
                    <LinearGradientBrush StartPoint="0,0.5" EndPoint="1,0.5">
                        <LinearGradientBrush.RelativeTransform>
                            <RotateTransform x:Name="RainbowRotateOuter" CenterX="0.5" CenterY="0.5" Angle="0"/>
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
                    <BlurEffect Radius="8" KernelType="Gaussian"/>
                </Border.Effect>
                <Border.Triggers>
                    <EventTrigger RoutedEvent="Loaded">
                        <BeginStoryboard>
                            <Storyboard RepeatBehavior="Forever">
                                <DoubleAnimation Storyboard.TargetName="RainbowRotateOuter" Storyboard.TargetProperty="Angle" From="0" To="360" Duration="0:0:3"/>
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                </Border.Triggers>
            </Border>

            <!-- RAINBOW GLOW BORDER (CRISP INNER LINE) -->
            <Border x:Name="RainbowGlowBorderInner" CornerRadius="{Binding RaioCantosDock}"
                    Margin="0"
                    BorderThickness="1.5"
                    Visibility="{Binding GlowRgbVisivel, Converter={StaticResource BoolToVis}}">
                <Border.BorderBrush>
                    <LinearGradientBrush StartPoint="0,0.5" EndPoint="1,0.5">
                        <LinearGradientBrush.RelativeTransform>
                            <RotateTransform x:Name="RainbowRotateInner" CenterX="0.5" CenterY="0.5" Angle="0"/>
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
                <Border.Triggers>
                    <EventTrigger RoutedEvent="Loaded">
                        <BeginStoryboard>
                            <Storyboard RepeatBehavior="Forever">
                                <DoubleAnimation Storyboard.TargetName="RainbowRotateInner" Storyboard.TargetProperty="Angle" From="0" To="360" Duration="0:0:3"/>
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                </Border.Triggers>
            </Border>"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
