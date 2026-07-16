using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace BSDPI.Controls;

public sealed class ConnectionLine : Shape
{
    public static readonly DependencyProperty StartProperty =
        DependencyProperty.Register(nameof(Start), typeof(Point), typeof(ConnectionLine),
            new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty EndProperty =
        DependencyProperty.Register(nameof(End), typeof(Point), typeof(ConnectionLine),
            new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty LineBrushProperty =
        DependencyProperty.Register(nameof(LineBrush), typeof(Brush), typeof(ConnectionLine),
            new FrameworkPropertyMetadata(Brushes.Cyan,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnLineBrushChanged));

    private static void OnLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConnectionLine line && e.NewValue is Brush brush)
            line.Stroke = brush;
    }

    public Point Start { get => (Point)GetValue(StartProperty); set => SetValue(StartProperty, value); }
    public Point End { get => (Point)GetValue(EndProperty); set => SetValue(EndProperty, value); }
    public Brush LineBrush { get => (Brush)GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }

    public ConnectionLine()
    {
        Stretch = Stretch.None;
        StrokeThickness = 2;
        Stroke = LineBrush;
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            var dx = Math.Abs(End.X - Start.X) * 0.5;
            dx = Math.Max(dx, 30);

            var figure = new PathFigure { StartPoint = Start };
            var segment = new BezierSegment(
                new Point(Start.X + dx, Start.Y),
                new Point(End.X - dx, End.Y),
                End,
                true);
            figure.Segments.Add(segment);

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        Stroke = LineBrush;
        base.OnRender(dc);
    }
}
