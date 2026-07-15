using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BSDPI.Core.Models.ChainBuilder;
using Point = System.Windows.Point;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace BSDPI.Controls;

public sealed class NodeCanvas : Canvas
{
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly TranslateTransform _translate = new(0, 0);
    private readonly Dictionary<Guid, NodeControl> _nodeControls = [];
    private readonly List<ConnectionLine> _connectionLines = [];
    private Point _panStart;
    private bool _isPanning;
    private NodeControl? _selectedNodeControl;

    public NodeCanvas()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;

        var group = new TransformGroup();
        group.Children.Add(_scale);
        group.Children.Add(_translate);
        RenderTransform = group;

        MouseWheel += OnMouseWheel;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }

    public double Zoom => _scale.ScaleX;

    public event Action<ChainNode>? NodeSelected;

    public void AddNode(ChainNode node)
    {
        if (_nodeControls.ContainsKey(node.Id)) return;

        var control = new NodeControl
        {
            DataContext = node,
            Width = 160,
            Height = 70,
            Tag = node.Id
        };

        Canvas.SetLeft(control, node.X);
        Canvas.SetTop(control, node.Y);

        Children.Add(control);
        _nodeControls[node.Id] = control;
    }

    public void RemoveNode(Guid nodeId)
    {
        if (_nodeControls.TryGetValue(nodeId, out var control))
        {
            Children.Remove(control);
            _nodeControls.Remove(nodeId);
        }

        var toRemove = _connectionLines
            .Where(l => l.Tag is ValueTuple<Guid, Guid> pair &&
                        (pair.Item1 == nodeId || pair.Item2 == nodeId))
            .ToList();
        foreach (var line in toRemove)
        {
            Children.Remove(line);
            _connectionLines.Remove(line);
        }
    }

    public void ClearAll()
    {
        Children.Clear();
        _nodeControls.Clear();
        _connectionLines.Clear();
    }

    public void AddConnection(ChainNode source, string sourcePort, ChainNode target, string targetPort)
    {
        var sourceCtrl = _nodeControls.GetValueOrDefault(source.Id);
        var targetCtrl = _nodeControls.GetValueOrDefault(target.Id);
        if (sourceCtrl is null || targetCtrl is null) return;

        var exists = _connectionLines.Any(l =>
            l.Tag is ValueTuple<Guid, Guid> pair &&
            pair.Item1 == source.Id && pair.Item2 == target.Id);
        if (exists) return;

        var line = new ConnectionLine
        {
            StrokeThickness = 2,
            Tag = (source.Id, target.Id)
        };

        UpdateConnectionEndpoints(line, sourceCtrl, targetCtrl);

        Children.Insert(0, line);
        _connectionLines.Add(line);
    }

    public void RemoveConnection(Guid sourceId, Guid targetId)
    {
        var line = _connectionLines.FirstOrDefault(l =>
            l.Tag is ValueTuple<Guid, Guid> pair &&
            pair.Item1 == sourceId && pair.Item2 == targetId);
        if (line is null) return;

        Children.Remove(line);
        _connectionLines.Remove(line);
    }

    public void RefreshAll()
    {
        foreach (var line in _connectionLines)
        {
            if (line.Tag is ValueTuple<Guid, Guid> pair)
            {
                var source = _nodeControls.GetValueOrDefault(pair.Item1);
                var target = _nodeControls.GetValueOrDefault(pair.Item2);
                if (source is not null && target is not null)
                    UpdateConnectionEndpoints(line, source, target);
            }
        }
    }

    private static void UpdateConnectionEndpoints(ConnectionLine line, NodeControl source, NodeControl target)
    {
        var sx = Canvas.GetLeft(source) + source.ActualWidth;
        var sy = Canvas.GetTop(source) + source.ActualHeight / 2;
        var tx = Canvas.GetLeft(target);
        var ty = Canvas.GetTop(target) + target.ActualHeight / 2;

        line.Start = new Point(sx, sy);
        line.End = new Point(tx, ty);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(this);
        var delta = e.Delta > 0 ? 1.1 : 0.9;
        var newScale = Math.Clamp(_scale.ScaleX * delta, 0.2, 3.0);
        _scale.ScaleX = newScale;
        _scale.ScaleY = newScale;
        _scale.CenterX = pos.X;
        _scale.CenterY = pos.Y;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) ||
            (e.LeftButton == MouseButtonState.Pressed && e.OriginalSource == this))
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            CaptureMouse();
            e.Handled = true;
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            var node = FindChainNode(e.OriginalSource);
            if (node is not null)
            {
                SelectNode(node);
                NodeSelected?.Invoke(node);
            }
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(this);
        _translate.X += pos.X - _panStart.X;
        _translate.Y += pos.Y - _panStart.Y;
        _panStart = pos;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ReleaseMouseCapture();
        }
    }

    private static ChainNode? FindChainNode(object? source)
    {
        var element = source as DependencyObject;
        while (element is not null)
        {
            if (element is FrameworkElement { DataContext: ChainNode node })
                return node;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private void SelectNode(ChainNode? node)
    {
        _selectedNodeControl?.SetSelected(false);

        if (node is null || !_nodeControls.TryGetValue(node.Id, out var ctrl))
        {
            _selectedNodeControl = null;
            return;
        }

        _selectedNodeControl = ctrl;
        ctrl.SetSelected(true);
    }

    public void DeselectAll() => SelectNode(null);

    public void ResetView()
    {
        _scale.ScaleX = 1;
        _scale.ScaleY = 1;
        _translate.X = 0;
        _translate.Y = 0;
    }
}
