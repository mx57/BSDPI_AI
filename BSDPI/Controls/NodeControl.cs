using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BSDPI.Core.Models.ChainBuilder;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;
using FontFamily = System.Windows.Media.FontFamily;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace BSDPI.Controls;

public sealed class NodeControl : UserControl
{
    private Point _dragStart;
    private bool _isDragging;
    private Canvas? _parentCanvas;
    private Border? _border;

    public bool IsSelected { get; private set; }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (Content is Border b)
        {
            b.BorderThickness = new Thickness(selected ? 2 : 1);
            if (DataContext is ChainNode node && !selected)
                b.BorderBrush = NodeAppearance.GetNodeColor(node.NodeType);
            else if (selected)
                b.BorderBrush = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
        }
    }

    public NodeControl()
    {
        BuildVisualTree();
        Loaded += OnLoaded;
    }

    private void BuildVisualTree()
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D))
        };
        _border = border;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBlock = new TextBlock
        {
            Name = "IconText",
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = Brushes.White
        };
        Grid.SetColumn(iconBlock, 0);

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleBlock = new TextBlock
        {
            Name = "TitleText",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var subtitleBlock = new TextBlock
        {
            Name = "SubtitleText",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0)
        };

        stack.Children.Add(titleBlock);
        stack.Children.Add(subtitleBlock);
        Grid.SetColumn(stack, 1);

        grid.Children.Add(iconBlock);
        grid.Children.Add(stack);
        border.Child = grid;

        Content = border;
    }

    private void UpdateContent(ChainNode node)
    {
        if (Content is not Border border || border.Child is not Grid grid) return;

        var icon = grid.Children[0] as TextBlock;
        var stack = grid.Children[1] as StackPanel;
        var title = stack?.Children[0] as TextBlock;
        var subtitle = stack?.Children[1] as TextBlock;

        if (icon is not null)
            icon.Text = NodeAppearance.GetNodeIcon(node.NodeType);

        if (title is not null)
            title.Text = node.Label;

        if (subtitle is not null)
            subtitle.Text = GetSubtitle(node);

        border.BorderBrush = NodeAppearance.GetNodeColor(node.NodeType);
    }

    private static string GetSubtitle(ChainNode node) => node.NodeType switch
    {
        ChainNodeType.Program => !string.IsNullOrEmpty(node.ProgramPath) ? System.IO.Path.GetFileName(node.ProgramPath) : "выберите процесс",
        ChainNodeType.Probe => node.TargetSites is { Length: > 0 } ? string.Join(", ", node.TargetSites.Take(2)) : "цели не заданы",
        ChainNodeType.Zapret => !string.IsNullOrEmpty(node.ZapretArgs) ? Truncate(node.ZapretArgs, 28) : "параметры по умолчанию",
        ChainNodeType.ByeDpi => !string.IsNullOrEmpty(node.ByeDpiArgs) ? Truncate(node.ByeDpiArgs, 28) : "параметры по умолчанию",
        ChainNodeType.Warp => $" порт {node.WarpPort ?? 40000}",
        ChainNodeType.Delay => $" {node.DelayMs} мс",
        ChainNodeType.Log => !string.IsNullOrEmpty(node.LogMessage) ? Truncate(node.LogMessage, 28) : "сообщение",
        ChainNodeType.Internet => "выход в сеть",
        _ => ""
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
        MouseLeftButtonDown += OnMouseLeftDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftUp;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        MouseLeftButtonDown -= OnMouseLeftDown;
        MouseMove -= OnMouseMove;
        MouseLeftButtonUp -= OnMouseLeftUp;
        Unloaded -= OnUnloaded;
    }

    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
    {
        if (_parentCanvas is null) return;
        _dragStart = e.GetPosition(_parentCanvas);
        _isDragging = true;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _parentCanvas is null || DataContext is not ChainNode node) return;
        var pos = e.GetPosition(_parentCanvas);
        var dx = pos.X - _dragStart.X;
        var dy = pos.Y - _dragStart.Y;
        Canvas.SetLeft(this, Canvas.GetLeft(this) + dx);
        Canvas.SetTop(this, Canvas.GetTop(this) + dy);
        node.X = Canvas.GetLeft(this);
        node.Y = Canvas.GetTop(this);
        _dragStart = pos;
    }

    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }
}
