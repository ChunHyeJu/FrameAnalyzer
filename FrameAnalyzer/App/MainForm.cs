using FrameAnalyzer.Nodes.OpenCv;
using FrameAnalyzer.Nodes.OpenCv.Basic;
using FrameAnalyzer.NodeEditor;
using Point = System.Drawing.Point;

namespace FrameAnalyzer;

public partial class MainForm : Form
{
    private NodeCanvasControl _nodeCanvas = null!;
    private bool _darkTheme = true;

    public MainForm()
    {
        InitializeComponent();

        InitializeNodeCanvas();
        InitializeToolbar();

        // These registrars populate the canvas context menu with node factories.
        new OpenCvNodeRegistrar(_nodeCanvas);
        new OpenCvDetectionNodeRegistrar(_nodeCanvas);

        AddDefaultNodes();
    }

    #region UI setup

    private void InitializeNodeCanvas()
    {
        _nodeCanvas = new NodeCanvasControl
        {
            Dock = DockStyle.Fill,
            PortLayout = NodeCanvasControl.NodePortLayout.TopBottom,
            Theme = NodeCanvasTheme.Dark
        };

        Controls.Add(_nodeCanvas);
    }

    private void InitializeToolbar()
    {
        Button btnTheme = CreateButton("Theme", 10, ToggleTheme);
        Button btnRun = CreateButton("Run", 100, RunGraph);

        CheckBox chkPreview = CreateCheckBox(
            text: "Preview",
            x: 200,
            isChecked: true,
            onChanged: isChecked => _nodeCanvas.SetAllPreviewEnabled(isChecked));

        CheckBox chkTime = CreateCheckBox(
            text: "Time",
            x: 300,
            isChecked: true,
            onChanged: isChecked => _nodeCanvas.SetAllProcessingTimeVisible(isChecked));

        Controls.Add(btnTheme);
        Controls.Add(btnRun);
        Controls.Add(chkPreview);
        Controls.Add(chkTime);

        btnTheme.BringToFront();
        btnRun.BringToFront();
        chkPreview.BringToFront();
        chkTime.BringToFront();
    }

    private Button CreateButton(string text, int x, Action clickAction)
    {
        Button button = new()
        {
            Text = text,
            Width = 80,
            Height = 30,
            Location = new Point(x, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };

        button.Click += (_, _) => clickAction();

        return button;
    }

    private CheckBox CreateCheckBox(
        string text,
        int x,
        bool isChecked,
        Action<bool> onChanged)
    {
        CheckBox checkBox = new()
        {
            Text = text,
            Checked = isChecked,
            Width = 90,
            Height = 24,
            Location = new Point(x, 13),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };

        checkBox.CheckedChanged += (_, _) =>
        {
            onChanged(checkBox.Checked);
        };

        return checkBox;
    }

    private void ToggleTheme()
    {
        _darkTheme = !_darkTheme;

        _nodeCanvas.Theme = _darkTheme
            ? NodeCanvasTheme.Dark
            : NodeCanvasTheme.Light;
    }
    #endregion

    #region Graph execution

    private void RunGraph()
    {
        try
        {
            NodeGraphExecutor executor = new(
                _nodeCanvas.Nodes,
                _nodeCanvas.Connections);

            executor.ExecuteAllSinks();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Run Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void AddDefaultNodes()
    {
        var imageInputProcessor = new ImageInputProcessor();

        var imageInputNode = _nodeCanvas.AddNode(
            "Image Input",
            Array.Empty<string>(),
            new[] { "Image" },
            300,
            120,
            new ImageInputControl(imageInputProcessor));

        imageInputNode.NodeType = "OpenCV/Image Input";
        imageInputNode.Processor = imageInputProcessor;

        var imageViewerProcessor = new ImageViewerProcessor();

        var imageViewerNode = _nodeCanvas.AddNode(
            "Image Viewer",
            new[] { "Image" },
            Array.Empty<string>(),
            620,
            360,
            new ImageViewerControl(imageViewerProcessor));

        imageViewerNode.NodeType = "OpenCV/Image Viewer";
        imageViewerNode.Processor = imageViewerProcessor;
    }
    #endregion
}
