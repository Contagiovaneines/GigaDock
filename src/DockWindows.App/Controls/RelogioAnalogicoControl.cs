using System.Windows;
using System.Windows.Media;

namespace DockWindows.App.Controls;

public class RelogioAnalogicoControl : FrameworkElement
{
    public static readonly DependencyProperty HorarioProperty = DependencyProperty.Register(
        nameof(Horario), typeof(DateTime), typeof(RelogioAnalogicoControl),
        new FrameworkPropertyMetadata(DateTime.Today.AddHours(10).AddMinutes(16), FrameworkPropertyMetadataOptions.AffectsRender));
    public DateTime Horario { get => (DateTime)GetValue(HorarioProperty); set => SetValue(HorarioProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var centro = new Point(ActualWidth / 2, ActualHeight / 2);
        var raio = Math.Max(0, Math.Min(ActualWidth, ActualHeight) / 2 - 1);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(12, 16, 24)), new Pen(Brushes.SlateGray, 1), centro, raio, raio);
        Point P(double graus, double comprimento) => new(centro.X + Math.Sin(graus * Math.PI / 180) * comprimento,
            centro.Y - Math.Cos(graus * Math.PI / 180) * comprimento);
        for (int i = 0; i < 12; i++)
            dc.DrawLine(new Pen(Brushes.Gray, 1), P(i * 30, raio * .79), P(i * 30, raio * .9));
        void Ponteiro(double angulo, double tamanho, Brush cor, double largura)
            => dc.DrawLine(new Pen(cor, largura) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                centro, P(angulo, raio * tamanho));
        Ponteiro((Horario.Hour % 12 + Horario.Minute / 60.0) * 30, .5, Brushes.White, 2.8);
        Ponteiro((Horario.Minute + Horario.Second / 60.0) * 6, .73, Brushes.White, 2);
        Ponteiro(Horario.Second * 6, .8, Brushes.IndianRed, 1);
        dc.DrawEllipse(Brushes.IndianRed, null, centro, 2, 2);
    }
}