namespace FrameAnalyzer;

public partial class Form1 : Form
{
    private NodeCanvasControl _nodeCanvas;

    public Form1()
    {
        InitializeComponent();

        _nodeCanvas = new NodeCanvasControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(_nodeCanvas);

        _nodeCanvas.AddNode("Image Input", 50, 80);
        _nodeCanvas.AddNode("Edge Detect", 320, 120);
        _nodeCanvas.AddNode("Main Processor", 600, 100);
    }
}

public enum PortDirection
{
    Input,
    Output
}

public class NodePort
{
    public string Name { get; set; }
    public PortDirection Direction { get; set; }
    public Rectangle Bounds { get; set; }
    public NodeView Owner { get; set; }
}

public class NodeView
{
    public string Title { get; set; }
    public Rectangle Bounds { get; set; }

    public List<NodePort> Inputs { get; } = new();
    public List<NodePort> Outputs { get; } = new();
}

public class NodeConnection
{
    public NodePort From { get; set; }
    public NodePort To { get; set; }
}

public class NodeCanvasControl : Control
{
    private readonly List<NodeView> _nodes = new();
    private readonly List<NodeConnection> _connections = new();

    private NodeView _draggingNode;
    private Point _dragOffset;

    private NodePort _connectingPort;
    private Point _mousePosition;

    public NodeCanvasControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(35, 35, 35);

        MouseDown += OnCanvasMouseDown;
        MouseMove += OnCanvasMouseMove;
        MouseUp += OnCanvasMouseUp;
    }

    public void AddNode(string title, int x, int y)
    {
        var node = new NodeView
        {
            Title = title,
            Bounds = new Rectangle(x, y, 160, 100)
        };

        node.Inputs.Add(new NodePort
        {
            Name = "In",
            Direction = PortDirection.Input,
            Owner = node
        });

        node.Outputs.Add(new NodePort
        {
            Name = "Out",
            Direction = PortDirection.Output,
            Owner = node
        });

        _nodes.Add(node);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        foreach (var connection in _connections)
        {
            DrawConnection(e.Graphics, connection.From, connection.To);
        }

        if (_connectingPort != null)
        {
            DrawTempConnection(e.Graphics, _connectingPort, _mousePosition);
        }

        foreach (var node in _nodes)
        {
            DrawNode(e.Graphics, node);
        }
    }

    private void DrawNode(Graphics g, NodeView node)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(55, 55, 55));
        using var titleBrush = new SolidBrush(Color.FromArgb(75, 75, 75));
        using var borderPen = new Pen(Color.FromArgb(120, 120, 120));
        using var textBrush = new SolidBrush(Color.White);

        g.FillRectangle(bodyBrush, node.Bounds);
        g.FillRectangle(titleBrush, node.Bounds.X, node.Bounds.Y, node.Bounds.Width, 26);
        g.DrawRectangle(borderPen, node.Bounds);

        g.DrawString(node.Title, Font, textBrush, node.Bounds.X + 8, node.Bounds.Y + 6);

        LayoutPorts(node);

        foreach (var port in node.Inputs)
            DrawPort(g, port);

        foreach (var port in node.Outputs)
            DrawPort(g, port);
    }

    private void LayoutPorts(NodeView node)
    {
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            node.Inputs[i].Bounds = new Rectangle(
                node.Bounds.Left - 6,
                node.Bounds.Top + 40 + i * 22,
                12,
                12);
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            node.Outputs[i].Bounds = new Rectangle(
                node.Bounds.Right - 6,
                node.Bounds.Top + 40 + i * 22,
                12,
                12);
        }
    }

    private void DrawPort(Graphics g, NodePort port)
    {
        using var brush = new SolidBrush(
            port.Direction == PortDirection.Input
                ? Color.DeepSkyBlue
                : Color.Orange);

        g.FillEllipse(brush, port.Bounds);
        g.DrawEllipse(Pens.Black, port.Bounds);
    }

    private void DrawConnection(Graphics g, NodePort from, NodePort to)
    {
        Point p1 = Center(from.Bounds);
        Point p2 = Center(to.Bounds);

        using var pen = new Pen(Color.LightGreen, 2);

        g.DrawBezier(
            pen,
            p1,
            new Point(p1.X + 60, p1.Y),
            new Point(p2.X - 60, p2.Y),
            p2);
    }

    private void DrawTempConnection(Graphics g, NodePort from, Point to)
    {
        Point p1 = Center(from.Bounds);

        using var pen = new Pen(Color.Gray, 2);

        g.DrawBezier(
            pen,
            p1,
            new Point(p1.X + 60, p1.Y),
            new Point(to.X - 60, to.Y),
            to);
    }

    private void OnCanvasMouseDown(object sender, MouseEventArgs e)
    {
        var port = HitTestPort(e.Location);

        if (port != null && port.Direction == PortDirection.Output)
        {
            _connectingPort = port;
            _mousePosition = e.Location;
            Invalidate();
            return;
        }

        var node = HitTestNode(e.Location);

        if (node != null)
        {
            _draggingNode = node;
            _dragOffset = new Point(
                e.X - node.Bounds.X,
                e.Y - node.Bounds.Y);
        }
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        _mousePosition = e.Location;

        if (_draggingNode != null)
        {
            _draggingNode.Bounds = new Rectangle(
                e.X - _dragOffset.X,
                e.Y - _dragOffset.Y,
                _draggingNode.Bounds.Width,
                _draggingNode.Bounds.Height);

            Invalidate();
        }

        if (_connectingPort != null)
        {
            Invalidate();
        }
    }

    private void OnCanvasMouseUp(object sender, MouseEventArgs e)
    {
        if (_connectingPort != null)
        {
            var targetPort = HitTestPort(e.Location);

            if (targetPort != null &&
                targetPort.Direction == PortDirection.Input &&
                targetPort.Owner != _connectingPort.Owner)
            {
                _connections.Add(new NodeConnection
                {
                    From = _connectingPort,
                    To = targetPort
                });
            }

            _connectingPort = null;
            Invalidate();
        }

        _draggingNode = null;
    }

    private NodeView HitTestNode(Point point)
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            if (_nodes[i].Bounds.Contains(point))
                return _nodes[i];
        }

        return null;
    }

    private NodePort HitTestPort(Point point)
    {
        foreach (var node in _nodes)
        {
            LayoutPorts(node);

            foreach (var port in node.Inputs)
            {
                if (port.Bounds.Contains(point))
                    return port;
            }

            foreach (var port in node.Outputs)
            {
                if (port.Bounds.Contains(point))
                    return port;
            }
        }

        return null;
    }

    private static Point Center(Rectangle rect)
    {
        return new Point(
            rect.Left + rect.Width / 2,
            rect.Top + rect.Height / 2);
    }
}
