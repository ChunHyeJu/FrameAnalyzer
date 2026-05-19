using System.Drawing.Drawing2D;
using System.Text.Json;
using static FrameAnalyzer.NodeEditor.NodeEnums;

namespace FrameAnalyzer.NodeEditor;

/// <summary>
/// 노드 기반 이미지 처리 그래프를 표시하고 조작하는 캔버스 컨트롤입니다.
///
/// 주요 기능:
/// - 노드 추가 / 삭제 / 이동 / 크기 조절
/// - 포트 연결
/// - 줌 / 패닝
/// - 우클릭 노드 생성 메뉴
/// - 그래프 저장 / 불러오기
/// - 노드 내부 HostedControl 배치
/// </summary>
public class NodeCanvasControl : Control
{
    #region Constants

    // 노드가 너무 작아져서 UI가 깨지는 것을 막기 위한 최소 크기입니다.
    private const float MinNodeWidth = 160;
    private const float MinNodeHeight = 110;

    // LeftRight 포트 배치일 때 제목 영역 높이입니다.
    private const float LeftRightTitleHeight = 28;

    // TopBottom 포트 배치일 때 영역 높이입니다.
    private const float TopBottomInputAreaHeight = 30;
    private const float TopBottomTitleHeight = 24;
    private const float TopBottomOutputAreaHeight = 38;

    // 노드 내부 UserControl과 노드 테두리 사이 여백입니다.
    private const float HostedPadding = 6;

    // 노드 우하단 크기 조절 핸들 크기입니다.
    private const float ResizeGripSize = 14;

    #endregion

    #region Fields

    // 현재 캔버스에 존재하는 노드 목록입니다.
    private readonly List<NodeView> _nodes = new();

    // 노드 포트 간 연결선 목록입니다.
    private readonly List<NodeConnection> _connections = new();

    // 우클릭 메뉴에 등록될 노드 생성 함수 목록입니다.
    private readonly List<NodeFactoryRegistration> _nodeFactories = new();

    // 캔버스 우클릭 메뉴입니다.
    private readonly ContextMenuStrip _contextMenu = new();

    // 현재 선택된 노드 또는 연결선입니다.
    private NodeView? _selectedNode;
    private NodeConnection? _selectedConnection;

    // 노드 드래그 상태입니다.
    private NodeView? _draggingNode;
    private PointF _dragOffsetWorld;

    // 노드 크기 조절 상태입니다.
    private NodeView? _resizingNode;
    private RectangleF _resizeStartBounds;
    private PointF _resizeStartWorld;

    // 빈 캔버스 드래그 상태입니다.
    // 현재 구조에서는 빈 캔버스를 드래그하면 전체 노드가 같이 이동합니다.
    private bool _isCanvasDragging;
    private PointF _lastCanvasDragWorld;

    // 포트 연결 중인 상태입니다.
    private NodePort? _connectingPort;
    private PointF _mouseWorld;

    // 마우스 휠 버튼 패닝 상태입니다.
    private bool _isPanning;
    private Point _lastMouseScreen;

    // 캔버스 줌 / 패닝 값입니다.
    private float _zoom = 1.0f;
    private PointF _pan = new(0, 0);

    // 우클릭 메뉴가 열린 월드 좌표입니다.
    // 노드 생성 시 이 위치에 생성합니다.
    private PointF _contextWorldPoint;

    private NodeCanvasTheme _theme = NodeCanvasTheme.Dark;
    private NodePortLayout _portLayout = NodePortLayout.LeftRight;

    // Saved graphs from older builds did not include NodeType. This table keeps
    // those files loadable by mapping the visible node title back to a factory.
    private static readonly Dictionary<string, string> LegacyNodeTypeByTitle = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["Image Input"] = "OpenCV/Image Input",
        ["External Mat Input"] = "OpenCV/External Mat Input",
        ["GrayScale"] = "OpenCV/GrayScale",
        ["Blur"] = "OpenCV/Blur",
        ["Heatmap"] = "OpenCV/Heatmap",
        ["Threshold"] = "OpenCV/Threshold",
        ["Edge Detect"] = "OpenCV/Edge Detect",
        ["Histogram"] = "OpenCV/Histogram",
        ["Brightness / Contrast"] = "OpenCV/Brightness Contrast",
        ["Morphology"] = "OpenCV/Morphology",
        ["Blob Detect"] = "OpenCV/Blob Detect",
        ["Contour Detect"] = "OpenCV/Contour Detect",
        ["Resize"] = "OpenCV/Resize",
        ["Channel Split"] = "OpenCV/Channel Split",
        ["ROI Color Sampler"] = "OpenCV/ROI Color Sampler",
        ["Image Viewer"] = "OpenCV/Image Viewer",
        ["Background Subtraction"] = "OpenCV_Detection/Background Subtraction"
    };

    #endregion

    #region Properties

    public IReadOnlyList<NodeView> Nodes => _nodes;

    public IReadOnlyList<NodeConnection> Connections => _connections;

    /// <summary>
    /// 캔버스와 노드의 색상 테마입니다.
    /// </summary>
    public NodeCanvasTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            BackColor = value.BackgroundColor;

            ApplyThemeToHostedControls();
            Invalidate();
        }
    }

    /// <summary>
    /// 포트 배치 방식입니다.
    /// LeftRight: 입력 왼쪽, 출력 오른쪽
    /// TopBottom: 입력 위쪽, 출력 아래쪽
    /// </summary>
    public NodePortLayout PortLayout
    {
        get => _portLayout;
        set
        {
            _portLayout = value;

            foreach (NodeView node in _nodes)
                node.PortLayout = value;

            SyncHostedControls();
            Invalidate();
        }
    }

    #endregion

    #region Types

    public enum NodePortLayout
    {
        LeftRight,
        TopBottom
    }

    /// <summary>
    /// 우클릭 메뉴에 표시할 노드 생성 항목입니다.
    /// </summary>
    private sealed class NodeFactoryRegistration
    {
        public string MenuPath { get; }

        public Func<NodeCanvasControl, PointF, NodeView> Factory { get; }

        public NodeFactoryRegistration(
            string menuPath,
            Func<NodeCanvasControl, PointF, NodeView> factory)
        {
            MenuPath = menuPath;
            Factory = factory;
        }
    }

    #endregion

    #region Constructor

    public NodeCanvasControl()
    {
        DoubleBuffered = true;
        TabStop = true;

        // 키보드 입력을 받기 위해 선택 가능한 컨트롤로 설정합니다.
        SetStyle(ControlStyles.Selectable, true);

        Theme = NodeCanvasTheme.Dark;

        MouseDown += OnCanvasMouseDown;
        MouseMove += OnCanvasMouseMove;
        MouseUp += OnCanvasMouseUp;
        MouseWheel += OnCanvasMouseWheel;
        KeyDown += OnCanvasKeyDown;
    }

    #endregion

    #region Node Factory Menu

    /// <summary>
    /// 우클릭 메뉴에 노드 생성 항목을 등록합니다.
    ///
    /// 예:
    /// RegisterNodeFactory("OpenCV/Blur", ...)
    /// → 우클릭 메뉴에 OpenCV > Blur 형태로 표시됩니다.
    /// </summary>
    public void RegisterNodeFactory(
        string menuPath,
        Func<NodeCanvasControl, PointF, NodeView> factory)
    {
        if (string.IsNullOrWhiteSpace(menuPath))
            throw new ArgumentException("메뉴 이름이 비어 있습니다.", nameof(menuPath));

        _nodeFactories.Add(new NodeFactoryRegistration(menuPath, factory));
    }

    /// <summary>
    /// 등록된 노드 생성 메뉴를 모두 제거합니다.
    /// </summary>
    public void ClearNodeFactories()
    {
        _nodeFactories.Clear();
    }

    /// <summary>
    /// 현재 마우스 위치에 우클릭 메뉴를 표시합니다.
    /// </summary>
    private void ShowContextMenu(Point screenPoint, PointF worldPoint)
    {
        _contextWorldPoint = worldPoint;

        BuildContextMenu();

        _contextMenu.Show(this, screenPoint);
    }

    /// <summary>
    /// 우클릭 메뉴를 현재 등록 상태 기준으로 다시 구성합니다.
    /// </summary>
    private void BuildContextMenu()
    {
        _contextMenu.Items.Clear();

        AddRegisteredNodeMenus();
        AddCanvasOptionMenus();
    }

    /// <summary>
    /// 등록된 노드 팩토리 목록을 메뉴에 추가합니다.
    /// </summary>
    private void AddRegisteredNodeMenus()
    {
        if (_nodeFactories.Count == 0)
        {
            _contextMenu.Items.Add(new ToolStripMenuItem("등록된 노드 없음")
            {
                Enabled = false
            });

            return;
        }

        foreach (NodeFactoryRegistration registration in _nodeFactories)
            AddFactoryMenuItem(registration);
    }

    /// <summary>
    /// "OpenCV/Blur" 같은 경로 문자열을 실제 하위 메뉴로 변환합니다.
    /// </summary>
    private void AddFactoryMenuItem(NodeFactoryRegistration registration)
    {
        string[] parts = registration.MenuPath.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            return;

        ToolStripItemCollection items = _contextMenu.Items;

        for (int i = 0; i < parts.Length; i++)
        {
            string text = parts[i];
            bool isLast = i == parts.Length - 1;

            ToolStripMenuItem menuItem = GetOrCreateMenuItem(items, text);

            if (isLast)
            {
                menuItem.Click += (_, _) =>
                {
                    CreateNodeFromFactory(registration, _contextWorldPoint);
                    SyncHostedControls();
                    Invalidate();
                };
            }
            else
            {
                items = menuItem.DropDownItems;
            }
        }
    }

    /// <summary>
    /// 같은 이름의 메뉴가 있으면 재사용하고, 없으면 새로 생성합니다.
    /// </summary>
    private static ToolStripMenuItem GetOrCreateMenuItem(
        ToolStripItemCollection items,
        string text)
    {
        ToolStripMenuItem? existing = items
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(x => x.Text == text);

        if (existing != null)
            return existing;

        ToolStripMenuItem created = new(text);
        items.Add(created);

        return created;
    }

    private NodeView CreateNodeFromFactory(
        NodeFactoryRegistration registration,
        PointF worldPoint)
    {
        NodeView node = registration.Factory(this, worldPoint);
        node.NodeType = registration.MenuPath;

        return node;
    }

    /// <summary>
    /// 테마 전환, 저장, 불러오기 같은 캔버스 기본 메뉴를 추가합니다.
    /// </summary>
    private void AddCanvasOptionMenus()
    {
        _contextMenu.Items.Add(new ToolStripSeparator());

        _contextMenu.Items.Add("Toggle Theme", null, (_, _) =>
        {
            Theme = IsDarkTheme()
                ? NodeCanvasTheme.Light
                : NodeCanvasTheme.Dark;
        });

        _contextMenu.Items.Add(new ToolStripSeparator());

        _contextMenu.Items.Add("Save Graph", null, (_, _) => ShowSaveGraphDialog());
        _contextMenu.Items.Add("Load Graph", null, (_, _) => ShowLoadGraphDialog());
    }

    private void ShowSaveGraphDialog()
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "Node Graph (*.json)|*.json",
            FileName = "node_graph.json"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            SaveGraph(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Graph Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ShowLoadGraphDialog()
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "Node Graph (*.json)|*.json"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            LoadGraph(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Graph Load Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private bool IsDarkTheme()
    {
        return _theme.BackgroundColor == NodeCanvasTheme.Dark.BackgroundColor;
    }

    #endregion

    #region Add Node

    public NodeView AddNode(
        string title,
        string[] inputs,
        string[] outputs,
        float x,
        float y)
    {
        return AddNode(title, inputs, outputs, x, y, null);
    }

    /// <summary>
    /// 새 노드를 캔버스에 추가합니다.
    /// hostedControl이 있으면 노드 내부에 표시됩니다.
    /// </summary>
    public NodeView AddNode(
        string title,
        string[] inputs,
        string[] outputs,
        float x,
        float y,
        Control? hostedControl)
    {
        SizeF nodeSize = CalculateInitialNodeSize(inputs, outputs, hostedControl);

        NodeView node = new()
        {
            Title = title,
            Bounds = new RectangleF(x, y, nodeSize.Width, nodeSize.Height),
            HostedControl = hostedControl,
            PortLayout = PortLayout
        };

        AddPorts(node, inputs, outputs);

        _nodes.Add(node);

        if (hostedControl != null)
            AttachHostedControl(node, hostedControl);

        SyncHostedControls();
        Invalidate();

        return node;
    }

    /// <summary>
    /// 입력/출력 포트를 노드에 추가합니다.
    /// </summary>
    private void AddPorts(NodeView node, string[] inputs, string[] outputs)
    {
        foreach (string input in inputs)
        {
            node.Inputs.Add(new NodePort
            {
                Name = input,
                Direction = PortDirection.Input,
                Owner = node
            });
        }

        foreach (string output in outputs)
        {
            node.Outputs.Add(new NodePort
            {
                Name = output,
                Direction = PortDirection.Output,
                Owner = node
            });
        }
    }

    /// <summary>
    /// 포트 개수와 내부 컨트롤 크기를 기준으로 초기 노드 크기를 계산합니다.
    /// </summary>
    private SizeF CalculateInitialNodeSize(
        string[] inputs,
        string[] outputs,
        Control? hostedControl)
    {
        float controlWidth = hostedControl?.Width ?? 220;
        float controlHeight = hostedControl?.Height ?? 160;

        return PortLayout == NodePortLayout.TopBottom
            ? CalculateTopBottomNodeSize(inputs, outputs, controlWidth, controlHeight)
            : CalculateLeftRightNodeSize(inputs, outputs, controlWidth, controlHeight);
    }

    private static SizeF CalculateTopBottomNodeSize(
        string[] inputs,
        string[] outputs,
        float controlWidth,
        float controlHeight)
    {
        float minWidthByInputs = Math.Max(1, inputs.Length) * 85;
        float minWidthByOutputs = Math.Max(1, outputs.Length) * 85;

        float width = Math.Max(
            MinNodeWidth,
            Math.Max(
                Math.Max(minWidthByInputs, minWidthByOutputs),
                controlWidth + HostedPadding * 2));

        float height =
            TopBottomInputAreaHeight +
            TopBottomTitleHeight +
            controlHeight +
            TopBottomOutputAreaHeight +
            HostedPadding * 2;

        return new SizeF(
            Math.Max(MinNodeWidth, width),
            Math.Max(MinNodeHeight, height));
    }

    private static SizeF CalculateLeftRightNodeSize(
        string[] inputs,
        string[] outputs,
        float controlWidth,
        float controlHeight)
    {
        float portHeight = 44 + Math.Max(inputs.Length, outputs.Length) * 24;

        float width = Math.Max(
            MinNodeWidth,
            controlWidth + HostedPadding * 2);

        float height = Math.Max(
            portHeight,
            LeftRightTitleHeight + controlHeight + HostedPadding * 2);

        return new SizeF(
            Math.Max(MinNodeWidth, width),
            Math.Max(MinNodeHeight, height));
    }

    /// <summary>
    /// 노드 내부 UserControl을 캔버스에 붙입니다.
    /// 실제 위치와 크기는 SyncHostedControls에서 맞춥니다.
    /// </summary>
    private void AttachHostedControl(NodeView node, Control hostedControl)
    {
        hostedControl.Parent = this;
        hostedControl.Visible = true;
        hostedControl.TabStop = false;
        hostedControl.Margin = Padding.Empty;

        ApplyThemeToHostedControl(hostedControl);

        hostedControl.MouseDown += (_, _) =>
        {
            Focus();
            _selectedNode = node;
            _selectedConnection = null;
            Invalidate();
        };

        // HostedControl 위에서 휠을 굴려도 캔버스 줌이 되도록 전달합니다.
        hostedControl.MouseWheel += (_, e) =>
        {
            OnCanvasMouseWheel(this, e);
        };

        Controls.Add(hostedControl);
        hostedControl.BringToFront();
    }

    #endregion

    #region Save / Load

    /// <summary>
    /// Saves graph layout and wiring. Runtime-only objects such as controls and
    /// processors are recreated from NodeType when the graph is loaded.
    /// </summary>
    public void SaveGraph(string path)
    {
        NodeGraphData data = new()
        {
            Nodes = _nodes.Select(CreateSerializableNode).ToList(),
            Connections = CreateSerializableConnections()
        };

        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        File.WriteAllText(path, JsonSerializer.Serialize(data, options));
    }

    /// <summary>
    /// Loads a graph and recreates runtime controls/processors through the
    /// registered factories. The existing graph is only cleared after the file
    /// has been parsed successfully.
    /// </summary>
    public void LoadGraph(string path)
    {
        string json = File.ReadAllText(path);
        NodeGraphData? data = JsonSerializer.Deserialize<NodeGraphData>(json);

        if (data == null)
            return;

        ClearGraph();

        foreach (NodeView savedNode in data.Nodes)
            RestoreRuntimeNode(savedNode);

        RestoreConnections(data.Connections);

        ResetInteractionState();

        SyncHostedControls();
        Invalidate();
    }

    private static NodeView CreateSerializableNode(NodeView node)
    {
        return new NodeView
        {
            Id = node.Id,
            NodeType = node.NodeType,
            Title = node.Title,
            Bounds = node.Bounds,
            Inputs = ClonePorts(node.Inputs),
            Outputs = ClonePorts(node.Outputs)
        };
    }

    private static List<NodePort> ClonePorts(IEnumerable<NodePort> ports)
    {
        return ports.Select(port => new NodePort
        {
            Id = port.Id,
            Name = port.Name,
            Direction = port.Direction
        }).ToList();
    }

    /// <summary>
    /// JSON 저장용 연결 정보를 생성합니다.
    /// 실제 포트 참조 대신 노드/포트 Id를 저장합니다.
    /// </summary>
    private List<NodeConnection> CreateSerializableConnections()
    {
        return _connections.Select(c => new NodeConnection
        {
            FromNodeId = c.From?.Owner?.Id ?? c.FromNodeId,
            FromPortId = c.From?.Id ?? c.FromPortId,
            ToNodeId = c.To?.Owner?.Id ?? c.ToNodeId,
            ToPortId = c.To?.Id ?? c.ToPortId
        }).ToList();
    }

    private NodeView RestoreRuntimeNode(NodeView savedNode)
    {
        string? nodeType = ResolveNodeType(savedNode);

        NodeView node = TryFindNodeFactory(nodeType, out NodeFactoryRegistration? registration)
            ? CreateNodeFromFactory(
                registration,
                new PointF(savedNode.Bounds.X, savedNode.Bounds.Y))
            : AddNode(
                savedNode.Title,
                savedNode.Inputs.Select(p => p.Name).ToArray(),
                savedNode.Outputs.Select(p => p.Name).ToArray(),
                savedNode.Bounds.X,
                savedNode.Bounds.Y,
                null);

        node.Id = savedNode.Id;
        node.NodeType = nodeType;
        node.Title = savedNode.Title;
        node.Bounds = savedNode.Bounds;
        node.PortLayout = PortLayout;

        RestorePortIdentity(savedNode.Inputs, node.Inputs);
        RestorePortIdentity(savedNode.Outputs, node.Outputs);

        return node;
    }

    private string? ResolveNodeType(NodeView savedNode)
    {
        if (!string.IsNullOrWhiteSpace(savedNode.NodeType))
            return savedNode.NodeType;

        return LegacyNodeTypeByTitle.TryGetValue(savedNode.Title, out string? nodeType)
            ? nodeType
            : null;
    }

    private bool TryFindNodeFactory(
        string? nodeType,
        out NodeFactoryRegistration registration)
    {
        registration = null!;

        if (string.IsNullOrWhiteSpace(nodeType))
            return false;

        NodeFactoryRegistration? match = _nodeFactories.FirstOrDefault(
            factory => string.Equals(
                factory.MenuPath,
                nodeType,
                StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return false;

        registration = match;
        return true;
    }

    private static void RestorePortIdentity(
        IReadOnlyList<NodePort> savedPorts,
        IReadOnlyList<NodePort> runtimePorts)
    {
        int count = Math.Min(savedPorts.Count, runtimePorts.Count);

        for (int i = 0; i < count; i++)
        {
            runtimePorts[i].Id = savedPorts[i].Id;
            runtimePorts[i].Name = savedPorts[i].Name;
            runtimePorts[i].Direction = savedPorts[i].Direction;
        }
    }

    /// <summary>
    /// 저장된 Id 정보를 실제 포트 참조로 복원합니다.
    /// </summary>
    private void RestoreConnections(IEnumerable<NodeConnection> connections)
    {
        foreach (NodeConnection connection in connections)
        {
            NodeView? fromNode = _nodes.FirstOrDefault(n => n.Id == connection.FromNodeId);
            NodeView? toNode = _nodes.FirstOrDefault(n => n.Id == connection.ToNodeId);

            NodePort? fromPort = fromNode?.Outputs.FirstOrDefault(p => p.Id == connection.FromPortId);
            NodePort? toPort = toNode?.Inputs.FirstOrDefault(p => p.Id == connection.ToPortId);

            if (fromPort == null || toPort == null)
                continue;

            connection.From = fromPort;
            connection.To = toPort;

            _connections.Add(connection);
        }
    }

    private void ClearGraph()
    {
        foreach (NodeView node in _nodes.ToList())
            DisposeNodeResources(node);

        _nodes.Clear();
        _connections.Clear();

        ResetInteractionState();
    }

    private void DisposeNodeResources(NodeView node)
    {
        if (node.HostedControl != null)
        {
            Controls.Remove(node.HostedControl);
            node.HostedControl.Dispose();
            node.HostedControl = null;
        }

        if (node.Processor is IDisposable disposable)
            disposable.Dispose();

        node.Processor = null;
    }

    private void ResetInteractionState()
    {
        _selectedNode = null;
        _selectedConnection = null;
        _draggingNode = null;
        _resizingNode = null;
        _connectingPort = null;
        _isCanvasDragging = false;
        _isPanning = false;
        Cursor = Cursors.Default;
    }

    #endregion

    #region Paint

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using Matrix transform = CreateCanvasTransform();
        e.Graphics.Transform = transform;

        DrawGrid(e.Graphics);
        DrawConnections(e.Graphics);
        DrawTempConnectionIfNeeded(e.Graphics);
        DrawNodes(e.Graphics);

        e.Graphics.ResetTransform();

        // HostedControl은 일반 WinForms 컨트롤이라 Graphics Transform을 받지 않습니다.
        // 그래서 Paint 이후 화면 좌표로 따로 동기화합니다.
        SyncHostedControls();
    }

    private Matrix CreateCanvasTransform()
    {
        Matrix transform = new();

        transform.Translate(_pan.X, _pan.Y);
        transform.Scale(_zoom, _zoom);

        return transform;
    }

    /// <summary>
    /// 현재 화면에 보이는 월드 영역 기준으로 격자를 그립니다.
    /// </summary>
    private void DrawGrid(Graphics g)
    {
        const float gridSize = 40;

        using Pen gridPen = new(_theme.GridColor, 1 / _zoom);

        RectangleF visible = ScreenToWorld(ClientRectangle);

        float startX = MathF.Floor(visible.Left / gridSize) * gridSize;
        float startY = MathF.Floor(visible.Top / gridSize) * gridSize;

        for (float x = startX; x < visible.Right; x += gridSize)
            g.DrawLine(gridPen, x, visible.Top, x, visible.Bottom);

        for (float y = startY; y < visible.Bottom; y += gridSize)
            g.DrawLine(gridPen, visible.Left, y, visible.Right, y);
    }

    private void DrawConnections(Graphics g)
    {
        foreach (NodeConnection connection in _connections)
            DrawConnection(g, connection);
    }

    private void DrawTempConnectionIfNeeded(Graphics g)
    {
        if (_connectingPort != null)
            DrawTempConnection(g, _connectingPort, _mouseWorld);
    }

    private void DrawNodes(Graphics g)
    {
        foreach (NodeView node in _nodes)
            DrawNode(g, node);
    }

    /// <summary>
    /// 노드 본체, 제목, 포트, 리사이즈 핸들을 그립니다.
    /// </summary>
    private void DrawNode(Graphics g, NodeView node)
    {
        LayoutPorts(node);

        bool selected = node == _selectedNode;

        using SolidBrush bodyBrush = new(_theme.NodeBodyColor);
        using SolidBrush titleBrush = new(_theme.NodeTitleColor);
        using SolidBrush textBrush = new(_theme.TextColor);
        using SolidBrush portTextBrush = new(_theme.PortTextColor);

        using Pen borderPen = new(
            selected ? _theme.SelectedBorderColor : _theme.NodeBorderColor,
            selected ? 3 / _zoom : 1 / _zoom);

        g.FillRectangle(bodyBrush, node.Bounds);

        DrawNodeTitle(g, node, titleBrush, textBrush);
        DrawNodeBorder(g, node, borderPen);
        DrawNodePorts(g, node, portTextBrush);

        if (selected)
            DrawResizeGrip(g, node);
    }

    private void DrawNodeTitle(
        Graphics g,
        NodeView node,
        Brush titleBrush,
        Brush textBrush)
    {
        RectangleF titleRect = GetNodeTitleBounds(node);

        g.FillRectangle(titleBrush, titleRect);

        g.DrawString(
            node.Title,
            Font,
            textBrush,
            titleRect.X + 8,
            titleRect.Y + 5);
    }

    private RectangleF GetNodeTitleBounds(NodeView node)
    {
        if (PortLayout == NodePortLayout.TopBottom)
        {
            return new RectangleF(
                node.Bounds.X,
                node.Bounds.Y + TopBottomInputAreaHeight,
                node.Bounds.Width,
                TopBottomTitleHeight);
        }

        return new RectangleF(
            node.Bounds.X,
            node.Bounds.Y,
            node.Bounds.Width,
            LeftRightTitleHeight);
    }

    private static void DrawNodeBorder(Graphics g, NodeView node, Pen borderPen)
    {
        g.DrawRectangle(
            borderPen,
            node.Bounds.X,
            node.Bounds.Y,
            node.Bounds.Width,
            node.Bounds.Height);
    }

    private void DrawNodePorts(Graphics g, NodeView node, Brush portTextBrush)
    {
        foreach (NodePort port in node.Inputs)
        {
            DrawPort(g, port);
            DrawPortName(g, port, portTextBrush);
        }

        foreach (NodePort port in node.Outputs)
        {
            DrawPort(g, port);
            DrawPortName(g, port, portTextBrush);
        }
    }

    private void DrawPort(Graphics g, NodePort port)
    {
        using SolidBrush brush = new(
            port.Direction == PortDirection.Input
                ? _theme.InputPortColor
                : _theme.OutputPortColor);

        g.FillEllipse(brush, port.Bounds);
        g.DrawEllipse(Pens.Black, port.Bounds);
    }

    private void DrawPortName(Graphics g, NodePort port, Brush brush)
    {
        if (port.Owner == null)
            return;

        if (PortLayout == NodePortLayout.LeftRight)
        {
            DrawLeftRightPortName(g, port, brush);
            return;
        }

        DrawTopBottomPortName(g, port, brush);
    }

    private void DrawLeftRightPortName(Graphics g, NodePort port, Brush brush)
    {
        SizeF size = g.MeasureString(port.Name, Font);

        if (port.Direction == PortDirection.Input)
        {
            g.DrawString(
                port.Name,
                Font,
                brush,
                port.Bounds.Right + 6,
                port.Bounds.Y - 2);
        }
        else
        {
            g.DrawString(
                port.Name,
                Font,
                brush,
                port.Bounds.Left - size.Width - 6,
                port.Bounds.Y - 2);
        }
    }

    private void DrawTopBottomPortName(Graphics g, NodePort port, Brush brush)
    {
        if (port.Owner == null)
            return;

        SizeF textSize = g.MeasureString(port.Name, Font);

        float x = port.Bounds.X - textSize.Width / 2 + port.Bounds.Width / 2;

        float y = port.Direction == PortDirection.Input
            ? port.Bounds.Bottom + 4
            : port.Owner.Bounds.Bottom - 34;

        g.DrawString(port.Name, Font, brush, x, y);
    }

    private void DrawResizeGrip(Graphics g, NodeView node)
    {
        RectangleF grip = GetResizeGripBounds(node);

        using SolidBrush brush = new(_theme.SelectedBorderColor);
        using Pen pen = new(_theme.NodeBorderColor, 1 / _zoom);

        g.FillRectangle(brush, grip);
        g.DrawRectangle(pen, grip.X, grip.Y, grip.Width, grip.Height);
    }

    private void DrawConnection(Graphics g, NodeConnection connection)
    {
        if (connection.From == null || connection.To == null)
            return;

        PointF p1 = Center(connection.From.Bounds);
        PointF p2 = Center(connection.To.Bounds);

        bool selected = connection == _selectedConnection;

        using Pen pen = new(
            selected ? _theme.SelectedConnectionColor : _theme.ConnectionColor,
            selected ? 3 / _zoom : 2 / _zoom);

        DrawBezierConnection(g, pen, p1, p2);
    }

    private void DrawTempConnection(Graphics g, NodePort from, PointF to)
    {
        PointF p1 = Center(from.Bounds);

        using Pen pen = new(_theme.TempConnectionColor, 2 / _zoom)
        {
            DashStyle = DashStyle.Dash
        };

        DrawBezierConnection(g, pen, p1, to);
    }

    /// <summary>
    /// 포트 배치 방식에 따라 연결선을 베지어 곡선으로 그립니다.
    /// </summary>
    private void DrawBezierConnection(Graphics g, Pen pen, PointF p1, PointF p2)
    {
        if (PortLayout == NodePortLayout.LeftRight)
        {
            g.DrawBezier(
                pen,
                p1,
                new PointF(p1.X + 70, p1.Y),
                new PointF(p2.X - 70, p2.Y),
                p2);
        }
        else
        {
            g.DrawBezier(
                pen,
                p1,
                new PointF(p1.X, p1.Y + 70),
                new PointF(p2.X, p2.Y - 70),
                p2);
        }
    }

    #endregion

    #region Port Layout

    /// <summary>
    /// 노드의 현재 포트 배치 방식에 따라 포트 위치를 계산합니다.
    /// </summary>
    private void LayoutPorts(NodeView node)
    {
        if (PortLayout == NodePortLayout.LeftRight)
            LayoutPortsLeftRight(node);
        else
            LayoutPortsTopBottom(node);
    }

    private void LayoutPortsLeftRight(NodeView node)
    {
        for (int i = 0; i < node.Inputs.Count; i++)
        {
            node.Inputs[i].Bounds = new RectangleF(
                node.Bounds.Left - 6,
                node.Bounds.Top + 44 + i * 24,
                12,
                12);
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            node.Outputs[i].Bounds = new RectangleF(
                node.Bounds.Right - 6,
                node.Bounds.Top + 44 + i * 24,
                12,
                12);
        }
    }

    private void LayoutPortsTopBottom(NodeView node)
    {
        const float portSize = 12;
        const float inputPortY = -6;
        const float outputPortBottomOffset = 6;

        float inputGap = node.Bounds.Width / (node.Inputs.Count + 1);
        float outputGap = node.Bounds.Width / (node.Outputs.Count + 1);

        for (int i = 0; i < node.Inputs.Count; i++)
        {
            float x = node.Bounds.Left + inputGap * (i + 1) - portSize / 2;

            node.Inputs[i].Bounds = new RectangleF(
                x,
                node.Bounds.Top + inputPortY,
                portSize,
                portSize);
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            float x = node.Bounds.Left + outputGap * (i + 1) - portSize / 2;

            node.Outputs[i].Bounds = new RectangleF(
                x,
                node.Bounds.Bottom - outputPortBottomOffset,
                portSize,
                portSize);
        }
    }

    #endregion

    #region Mouse / Keyboard

    private void OnCanvasMouseDown(object? sender, MouseEventArgs e)
    {
        Focus();

        PointF world = ScreenToWorld(e.Location);
        _mouseWorld = world;

        if (e.Button == MouseButtons.Middle)
        {
            StartPanning(e.Location);
            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            ShowContextMenu(e.Location, world);
            return;
        }

        if (e.Button != MouseButtons.Left)
            return;

        if (TryStartConnection(world))
            return;

        if (TrySelectConnection(world))
            return;

        if (TrySelectNode(world))
            return;

        StartCanvasDragging(world);
    }

    private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
    {
        PointF world = ScreenToWorld(e.Location);
        _mouseWorld = world;

        if (_isPanning)
        {
            UpdatePanning(e.Location);
            return;
        }

        if (_resizingNode != null)
        {
            UpdateNodeResize(world);
            return;
        }

        if (_draggingNode != null)
        {
            UpdateNodeDragging(world);
            return;
        }

        if (_isCanvasDragging)
        {
            UpdateCanvasDragging(world);
            return;
        }

        UpdateCursor(world);

        if (_connectingPort != null)
            Invalidate();
    }

    private void OnCanvasMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            StopPanning();
            return;
        }

        PointF world = ScreenToWorld(e.Location);

        if (_connectingPort != null)
            FinishConnection(world);

        _draggingNode = null;
        _resizingNode = null;
        _isCanvasDragging = false;
    }

    private void OnCanvasMouseWheel(object? sender, MouseEventArgs e)
    {
        ZoomAt(e.Location, e.Delta);
    }

    private void OnCanvasKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete)
            return;

        DeleteSelectedItem();
    }

    #endregion

    #region Mouse Operations

    private void StartPanning(Point screenPoint)
    {
        _isPanning = true;
        _lastMouseScreen = screenPoint;
        Cursor = Cursors.Hand;
    }

    private void UpdatePanning(Point screenPoint)
    {
        _pan.X += screenPoint.X - _lastMouseScreen.X;
        _pan.Y += screenPoint.Y - _lastMouseScreen.Y;

        _lastMouseScreen = screenPoint;

        SyncHostedControls();
        Invalidate();
    }

    private void StopPanning()
    {
        _isPanning = false;
        Cursor = Cursors.Default;
    }

    /// <summary>
    /// 출력 포트를 클릭했으면 임시 연결선을 시작합니다.
    /// </summary>
    private bool TryStartConnection(PointF world)
    {
        NodePort? port = HitTestPort(world);

        if (port == null || port.Direction != PortDirection.Output)
            return false;

        _connectingPort = port;
        _selectedNode = null;
        _selectedConnection = null;
        _isCanvasDragging = false;

        Invalidate();

        return true;
    }

    private bool TrySelectConnection(PointF world)
    {
        NodeConnection? connection = HitTestConnection(world);

        if (connection == null)
            return false;

        _selectedConnection = connection;
        _selectedNode = null;
        _isCanvasDragging = false;

        Invalidate();

        return true;
    }

    private bool TrySelectNode(PointF world)
    {
        NodeView? node = HitTestNode(world);

        if (node == null)
            return false;

        SelectNode(node);
        BringNodeToFront(node);

        if (HitTestResizeGrip(node, world))
        {
            StartNodeResize(node, world);
            return true;
        }

        StartNodeDrag(node, world);

        return true;
    }

    private void SelectNode(NodeView node)
    {
        _selectedNode = node;
        _selectedConnection = null;
        _isCanvasDragging = false;
    }

    private void BringNodeToFront(NodeView node)
    {
        _nodes.Remove(node);
        _nodes.Add(node);

        node.HostedControl?.BringToFront();
    }

    private void StartNodeDrag(NodeView node, PointF world)
    {
        _draggingNode = node;

        _dragOffsetWorld = new PointF(
            world.X - node.Bounds.X,
            world.Y - node.Bounds.Y);

        Invalidate();
    }

    private void UpdateNodeDragging(PointF world)
    {
        if (_draggingNode == null)
            return;

        _draggingNode.Bounds = new RectangleF(
            world.X - _dragOffsetWorld.X,
            world.Y - _dragOffsetWorld.Y,
            _draggingNode.Bounds.Width,
            _draggingNode.Bounds.Height);

        SyncHostedControls();
        Invalidate();
    }

    private void StartNodeResize(NodeView node, PointF world)
    {
        _resizingNode = node;
        _resizeStartBounds = node.Bounds;
        _resizeStartWorld = world;

        Invalidate();
    }

    private void UpdateNodeResize(PointF world)
    {
        if (_resizingNode == null)
            return;

        float dx = world.X - _resizeStartWorld.X;
        float dy = world.Y - _resizeStartWorld.Y;

        float newWidth = Math.Max(MinNodeWidth, _resizeStartBounds.Width + dx);
        float newHeight = Math.Max(MinNodeHeight, _resizeStartBounds.Height + dy);

        _resizingNode.Bounds = new RectangleF(
            _resizeStartBounds.X,
            _resizeStartBounds.Y,
            newWidth,
            newHeight);

        SyncHostedControls();
        Invalidate();
    }

    /// <summary>
    /// 빈 캔버스를 드래그하면 전체 노드를 이동합니다.
    /// </summary>
    private void StartCanvasDragging(PointF world)
    {
        _selectedNode = null;
        _selectedConnection = null;
        _draggingNode = null;
        _resizingNode = null;

        _isCanvasDragging = true;
        _lastCanvasDragWorld = world;

        Invalidate();
    }

    private void UpdateCanvasDragging(PointF world)
    {
        float dx = world.X - _lastCanvasDragWorld.X;
        float dy = world.Y - _lastCanvasDragWorld.Y;

        foreach (NodeView node in _nodes)
        {
            node.Bounds = new RectangleF(
                node.Bounds.X + dx,
                node.Bounds.Y + dy,
                node.Bounds.Width,
                node.Bounds.Height);
        }

        _lastCanvasDragWorld = world;

        SyncHostedControls();
        Invalidate();
    }

    /// <summary>
    /// 임시 연결선을 실제 연결로 확정합니다.
    /// </summary>
    private void FinishConnection(PointF world)
    {
        NodePort? targetPort = HitTestPort(world);

        if (CanConnectTo(targetPort))
            ConnectPorts(_connectingPort!, targetPort!);

        _connectingPort = null;
        Invalidate();
    }

    private bool CanConnectTo(NodePort? targetPort)
    {
        return targetPort != null &&
               targetPort.Direction == PortDirection.Input &&
               targetPort.Owner != _connectingPort?.Owner;
    }

    /// <summary>
    /// 출력 포트에서 입력 포트로 연결합니다.
    /// 입력 포트는 하나의 연결만 허용하고, 출력 포트는 여러 곳으로 연결 가능합니다.
    /// </summary>
    private void ConnectPorts(NodePort fromPort, NodePort toPort)
    {
        _connections.RemoveAll(c => c.To == toPort);

        _connections.Add(new NodeConnection
        {
            From = fromPort,
            To = toPort,
            FromNodeId = fromPort.Owner!.Id,
            FromPortId = fromPort.Id,
            ToNodeId = toPort.Owner!.Id,
            ToPortId = toPort.Id
        });
    }

    /// <summary>
    /// 마우스 위치를 기준으로 확대/축소합니다.
    /// </summary>
    private void ZoomAt(Point screenPoint, int delta)
    {
        float oldZoom = _zoom;

        _zoom = delta > 0
            ? _zoom * 1.1f
            : _zoom / 1.1f;

        _zoom = Math.Clamp(_zoom, 0.25f, 3.0f);

        PointF mouseWorldBefore = new(
            (screenPoint.X - _pan.X) / oldZoom,
            (screenPoint.Y - _pan.Y) / oldZoom);

        _pan.X = screenPoint.X - mouseWorldBefore.X * _zoom;
        _pan.Y = screenPoint.Y - mouseWorldBefore.Y * _zoom;

        SyncHostedControls();
        Invalidate();
    }

    private void UpdateCursor(PointF world)
    {
        if (_selectedNode != null && HitTestResizeGrip(_selectedNode, world))
            Cursor = Cursors.SizeNWSE;
        else if (!_isPanning)
            Cursor = Cursors.Default;
    }

    private void DeleteSelectedItem()
    {
        if (_selectedConnection != null)
        {
            _connections.Remove(_selectedConnection);
            _selectedConnection = null;
            Invalidate();
            return;
        }

        if (_selectedNode == null)
            return;

        _connections.RemoveAll(c =>
            c.From != null && c.From.Owner == _selectedNode ||
            c.To != null && c.To.Owner == _selectedNode);

        DisposeNodeResources(_selectedNode);

        _nodes.Remove(_selectedNode);
        _selectedNode = null;

        Invalidate();
    }

    #endregion

    #region Hosted Control Layout

    /// <summary>
    /// 각 노드 내부 HostedControl의 화면 위치와 크기를 노드 Bounds에 맞춥니다.
    /// </summary>
    private void SyncHostedControls()
    {
        foreach (NodeView node in _nodes)
            SyncHostedControl(node);

        foreach (NodeView node in _nodes)
            node.HostedControl?.BringToFront();
    }

    private void SyncHostedControl(NodeView node)
    {
        if (node.HostedControl == null)
            return;

        node.PortLayout = PortLayout;

        RectangleF hostedWorldBounds = GetHostedWorldBounds(node);
        Rectangle screenRect = WorldToScreen(hostedWorldBounds);

        node.HostedControl.Bounds = screenRect;
        node.HostedControl.Visible = IsVisibleOnScreen(screenRect);
    }

    private bool IsVisibleOnScreen(Rectangle screenRect)
    {
        return screenRect.Right >= 0 &&
               screenRect.Bottom >= 0 &&
               screenRect.Left <= Width &&
               screenRect.Top <= Height;
    }

    private RectangleF GetHostedWorldBounds(NodeView node)
    {
        if (PortLayout == NodePortLayout.TopBottom)
            return GetTopBottomHostedWorldBounds(node);

        return GetLeftRightHostedWorldBounds(node);
    }

    private static RectangleF GetTopBottomHostedWorldBounds(NodeView node)
    {
        float x = node.Bounds.X + HostedPadding;

        float y =
            node.Bounds.Y +
            TopBottomInputAreaHeight +
            TopBottomTitleHeight +
            HostedPadding;

        float width = node.Bounds.Width - HostedPadding * 2;

        float height =
            node.Bounds.Height -
            TopBottomInputAreaHeight -
            TopBottomTitleHeight -
            TopBottomOutputAreaHeight -
            HostedPadding * 2;

        return NormalizeRect(x, y, width, height);
    }

    private static RectangleF GetLeftRightHostedWorldBounds(NodeView node)
    {
        float x = node.Bounds.X + HostedPadding;
        float y = node.Bounds.Y + LeftRightTitleHeight + HostedPadding;

        float width = node.Bounds.Width - HostedPadding * 2;

        float height =
            node.Bounds.Height -
            LeftRightTitleHeight -
            HostedPadding * 2;

        return NormalizeRect(x, y, width, height);
    }

    private static RectangleF NormalizeRect(
        float x,
        float y,
        float width,
        float height)
    {
        return new RectangleF(
            x,
            y,
            Math.Max(1, width),
            Math.Max(1, height));
    }

    #endregion

    #region Hit Test

    private NodeView? HitTestNode(PointF point)
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            if (_nodes[i].Bounds.Contains(point))
                return _nodes[i];
        }

        return null;
    }

    private NodePort? HitTestPort(PointF point)
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            NodeView node = _nodes[i];

            LayoutPorts(node);

            foreach (NodePort port in node.Inputs)
            {
                if (port.Bounds.Contains(point))
                    return port;
            }

            foreach (NodePort port in node.Outputs)
            {
                if (port.Bounds.Contains(point))
                    return port;
            }
        }

        return null;
    }

    /// <summary>
    /// 연결선 클릭 선택을 위한 간단한 hit test입니다.
    /// 현재는 베지어 곡선을 정확히 검사하지 않고 시작점-끝점 직선 거리로 근사합니다.
    /// </summary>
    private NodeConnection? HitTestConnection(PointF point)
    {
        const float tolerance = 8f;

        foreach (NodeConnection connection in _connections.AsEnumerable().Reverse())
        {
            if (connection.From == null || connection.To == null)
                continue;

            PointF p1 = Center(connection.From.Bounds);
            PointF p2 = Center(connection.To.Bounds);

            if (DistancePointToLine(point, p1, p2) <= tolerance)
                return connection;
        }

        return null;
    }

    private bool HitTestResizeGrip(NodeView node, PointF point)
    {
        return GetResizeGripBounds(node).Contains(point);
    }

    private static RectangleF GetResizeGripBounds(NodeView node)
    {
        return new RectangleF(
            node.Bounds.Right - ResizeGripSize,
            node.Bounds.Bottom - ResizeGripSize,
            ResizeGripSize,
            ResizeGripSize);
    }

    #endregion

    #region Coordinate

    /// <summary>
    /// 화면 좌표를 캔버스 월드 좌표로 변환합니다.
    /// </summary>
    private PointF ScreenToWorld(Point point)
    {
        return new PointF(
            (point.X - _pan.X) / _zoom,
            (point.Y - _pan.Y) / _zoom);
    }

    private RectangleF ScreenToWorld(Rectangle rect)
    {
        PointF topLeft = ScreenToWorld(new Point(rect.Left, rect.Top));
        PointF bottomRight = ScreenToWorld(new Point(rect.Right, rect.Bottom));

        return RectangleF.FromLTRB(
            topLeft.X,
            topLeft.Y,
            bottomRight.X,
            bottomRight.Y);
    }

    /// <summary>
    /// 월드 좌표를 실제 WinForms 화면 좌표로 변환합니다.
    /// HostedControl 배치에 사용합니다.
    /// </summary>
    private Rectangle WorldToScreen(RectangleF world)
    {
        return new Rectangle(
            (int)(world.X * _zoom + _pan.X),
            (int)(world.Y * _zoom + _pan.Y),
            Math.Max(1, (int)(world.Width * _zoom)),
            Math.Max(1, (int)(world.Height * _zoom)));
    }

    private static PointF Center(RectangleF rect)
    {
        return new PointF(
            rect.Left + rect.Width / 2,
            rect.Top + rect.Height / 2);
    }

    /// <summary>
    /// 점과 선분 사이의 거리를 계산합니다.
    /// 연결선 선택 hit test에 사용합니다.
    /// </summary>
    private static float DistancePointToLine(PointF p, PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;

        if (dx == 0 && dy == 0)
            return Distance(p, a);

        float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);

        PointF closest = new(
            a.X + t * dx,
            a.Y + t * dy);

        return Distance(p, closest);
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }

    #endregion

    #region Runtime Options

    /// <summary>
    /// 모든 노드의 이미지 미리보기 표시 여부를 변경합니다.
    /// </summary>
    public void SetAllPreviewEnabled(bool enabled)
    {
        foreach (NodeView node in _nodes)
        {
            if (node.HostedControl is INodeRuntimeAware runtimeAware)
                runtimeAware.PreviewEnabled = enabled;
        }
    }

    /// <summary>
    /// 모든 노드의 처리 시간 표시 여부를 변경합니다.
    /// </summary>
    public void SetAllProcessingTimeVisible(bool visible)
    {
        foreach (NodeView node in _nodes)
        {
            if (node.HostedControl is INodeRuntimeAware runtimeAware)
                runtimeAware.ShowProcessingTime = visible;
        }
    }

    #endregion

    #region Theme

    private void ApplyThemeToHostedControls()
    {
        foreach (NodeView node in _nodes)
        {
            if (node.HostedControl != null)
                ApplyThemeToHostedControl(node.HostedControl);
        }
    }

    private void ApplyThemeToHostedControl(Control control)
    {
        if (control is INodeThemeAware themeAware)
            themeAware.ApplyTheme(_theme);
    }

    #endregion

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearGraph();
            _contextMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion
}
