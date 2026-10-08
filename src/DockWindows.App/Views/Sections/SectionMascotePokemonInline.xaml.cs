using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DockWindows.App.ViewModels;
using DockWindows.App.Common;

namespace DockWindows.App.Views.Sections;

public partial class SectionMascotePokemonInline : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Stopwatch _clock = new();
    private readonly Stopwatch _evolutionClock = new();
    private System.Windows.Media.Imaging.BitmapSource? _evolutionSource;
    private MainViewModel? _main;
    private double _position = 16, _direction = 1, _frameTime;
    private double _walkDistance, _turnPause;
    private double _airTime;
    private double _restTime;
    private double _untilFrontPause = Random.Shared.Next(90000, 180001), _frontPause;
    private string? _lastState;
    private int _frame;
    public SectionMascotePokemonInline()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        DataContextChanged += (_, _) => { if (IsLoaded) Attach(); };
        IsVisibleChanged += (_, _) => Update();
        SizeChanged += (_, _) => { Clamp(); };
    }
    private void Attach()
    {
        Detach();
        _main = DataContext as MainViewModel;
        if (_main != null)
        {
            _main.PropertyChanged += Changed;
            _main.MascotePokemon.PropertyChanged += Changed;
        }
        Update();
    }
    private void Detach()
    {
        _timer.Stop(); _clock.Reset();
        if (_main != null)
        {
            _main.PropertyChanged -= Changed;
            _main.MascotePokemon.PropertyChanged -= Changed;
        }
        _main = null;
    }
    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MascotePokemonViewModel.Evoluindo))
        {
            if (_main?.MascotePokemon.Evoluindo == true)
            {
                _evolutionSource = _main.MascotePokemon.WalkAnimation?.Front[0].Image;
                _evolutionClock.Restart();
            }
            else _evolutionClock.Reset();
        }
        if (e.PropertyName is nameof(MascotePokemonViewModel.Frames) or nameof(MascotePokemonViewModel.WalkAnimation))
        { _frame = 0; _frameTime = 0; _walkDistance = 0; _turnPause = 0; _airTime = 0; }
        if (e.PropertyName == nameof(MascotePokemonViewModel.WalkAnimation))
        { _frontPause = 0; _untilFrontPause = Random.Shared.Next(90000, 180001); }
        Update();
    }
    private void Update()
    {
        var walk = _main?.MascotePokemon.WalkAnimation;
        Pet.Height = 44;
        Pet.Source = walk?.IsAirborne == true ? walk.FrameAtTime(_airTime, _direction > 0)
            : walk?.FrameAt(_walkDistance, _direction > 0) ?? _main?.MascotePokemon.Sprite;
        if (walk != null && _turnPause > 175) Pet.Source = walk.Front[0].Image;
        ApplyRestPose();
        Height = walk?.IsAirborne == true || _main?.MascotePokemon.Evoluindo == true ? 60 : 44;
        Canvas.SetBottom(Pet, PokemonLocomotion.Altitude(walk?.MovementKind ?? PokemonMovementKind.Ground,
            _airTime, _main?.MascotePokemon.Estado == "Dormindo", _main?.AnimacoesAtivas == true));
        if (_main?.MascotePokemon.Estado is "Descansando" or "Dormindo") Canvas.SetBottom(Pet, 0);
        Facing.ScaleX = walk != null ? 1 : _direction;
        if (Pet.Source is { Width: > 0, Height: > 0 } image)
            Pet.Width = Pet.Height * image.Width / image.Height;
        if (IsLoaded && IsVisible && _main is { AnimacoesAtivas: true } && _main.MascotePokemon.Habilitado)
        { if (!_timer.IsEnabled) { _clock.Restart(); _timer.Start(); } }
        else { _timer.Stop(); _clock.Reset(); }
        Clamp();
    }
    private void Clamp()
    {
        _position = Math.Clamp(_position, 0, Math.Max(0, ActualWidth - Pet.Width));
        Canvas.SetLeft(Pet, _position);
        UpdateEvolutionEffect();
    }

    private void UpdateEvolutionEffect()
    {
        var pet = _main?.MascotePokemon;
        var show = pet?.Evoluindo == true && _main?.AnimacoesAtivas == true;
        EvolutionEffect.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        Pet.Visibility = show ? Visibility.Hidden : Visibility.Visible;
        if (!show) return;
        Canvas.SetLeft(EvolutionEffect, Math.Clamp(_position + Pet.Width / 2 - EvolutionEffect.Width / 2,
            0, Math.Max(0, ActualWidth - EvolutionEffect.Width)));
        EvolutionEffect.SetFrame(_evolutionSource, pet!.EvolutionPreview,
            _evolutionClock.Elapsed.TotalMilliseconds / Controls.PokemonEvolutionEffect.DurationMilliseconds);
    }
    private void Tick()
    {
        var elapsed = Math.Min(100, _clock.Elapsed.TotalMilliseconds); _clock.Restart();
        var pet = _main?.MascotePokemon;
        if (pet == null) return;
        UpdateEvolutionEffect();
        if (_lastState != pet.Estado) { _lastState = pet.Estado; _restTime = 0; }
        _restTime += elapsed;
        var walking = !pet.Evoluindo && pet.Estado is not ("Dormindo" or "Descansando");
        if (walking)
        {
            if (_frontPause > 0) _frontPause = Math.Max(0, _frontPause - elapsed);
        }
        else _frontPause = 0;
        walking &= _frontPause == 0;
        _turnPause = Math.Max(0, _turnPause - elapsed);
        if (walking && _turnPause == 0)
        {
            var previous = _position;
            _position += _direction * 18 * elapsed / 1000;
            var right = Math.Max(0, ActualWidth - Pet.Width - 16);
            var left = Math.Min(16, right);
            if (_position >= right) { _position = right; _direction = -1; _turnPause = 350; }
            else if (_position <= left) { _position = left; _direction = 1; _turnPause = 350; }
            _untilFrontPause -= Math.Abs(_position - previous) / 18 * 1000;
            var middle = (left + right) / 2;
            var crossedMiddle = previous < middle && _position >= middle
                || previous > middle && _position <= middle;
            if (_untilFrontPause <= 0 && right - left > 1 && crossedMiddle)
            {
                _position = middle;
                _frontPause = Random.Shared.Next(2000, 3001);
                _untilFrontPause = Random.Shared.Next(90000, 180001);
            }
            _walkDistance += Math.Abs(_position - previous);
            Facing.ScaleX = pet.WalkAnimation != null ? 1 : _direction;
            Clamp();
        }
        if (pet.WalkAnimation is { } walk)
        {
            var sleeping = pet.Estado == "Dormindo";
            if (walk.IsAirborne && !sleeping) _airTime += elapsed;
            Pet.Source = walk.IsAirborne ? walk.FrameAtTime(_airTime, _direction > 0) : walk.FrameAt(_walkDistance, _direction > 0);
            // Vira de frente durante a pausa e assume o novo lado antes de voltar a se mover.
            if (_turnPause > 175) Pet.Source = walk.Front[0].Image;
            Canvas.SetBottom(Pet, PokemonLocomotion.Altitude(walk.MovementKind, _airTime, sleeping, true));
            ApplyRestPose();
            return;
        }
        var frames = pet.Frames;
        if (frames.Count == 0) return;
        _frame %= frames.Count;
        if (walking && _turnPause == 0)
        {
            _frameTime += elapsed;
            while (_frameTime >= frames[_frame].DelayMilliseconds)
            { _frameTime -= frames[_frame].DelayMilliseconds; _frame = (_frame + 1) % frames.Count; }
        }
        Pet.Source = frames[_frame].Image;
    }

    private void ApplyRestPose()
    {
        var pet = _main?.MascotePokemon;
        if (pet is { Evoluindo: true, WalkAnimation: { } evolving })
        {
            Pet.Source = evolving.Front[0].Image;
            Canvas.SetBottom(Pet, 0);
            return;
        }
        if (_frontPause > 0 && pet is { Estado: "Acompanhando você", WalkAnimation: { } looking })
        {
            Pet.Source = looking.IsAirborne ? looking.FrontAtTime(_airTime) : looking.Front[0].Image;
            Facing.ScaleX = 1;
            return;
        }
        var pose = pet?.Estado == "Dormindo" ? pet.SleepAnimation
            : pet?.Estado == "Descansando" ? pet.RestAnimation : null;
        if (pose == null) return;
        Pet.Source = pose.AnimationName == "Sit" ? pose.Front[^1].Image : pose.FrontAtTime(_restTime);
        // Mantém a escala dos pixels da caminhada: sentado não fica artificialmente maior.
        Pet.Height = pet!.WalkAnimation is { } walk ? Math.Min(44, 44d * Pet.Source.Height / walk.Front[0].Image.Height) : 44;
        Pet.Width = Pet.Height * Pet.Source.Width / Pet.Source.Height;
        Canvas.SetBottom(Pet, 0);
    }
}
