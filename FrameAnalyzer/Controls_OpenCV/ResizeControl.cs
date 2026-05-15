using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ResizeControl : OpenCvPreviewNodeControl
{
    private readonly CheckBox _useScaleCheckBox = new();

    private readonly Label _scaleLabel = new();
    private readonly NumericUpDown _scaleNumeric = new();

    private readonly Label _widthLabel = new();
    private readonly NumericUpDown _widthNumeric = new();

    private readonly Label _heightLabel = new();
    private readonly NumericUpDown _heightNumeric = new();

    private readonly ResizeProcessor _processor;

    public ResizeControl(ResizeProcessor processor)
        : base(defaultWidth: 260, defaultHeight: 300, settingsHeight: 118)
    {
        _processor = processor;

        TableLayoutPanel settingsLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

        InitUseScaleControl();
        InitScaleControls();
        InitWidthControls();
        InitHeightControls();

        settingsLayout.Controls.Add(_useScaleCheckBox, 0, 0);
        settingsLayout.SetColumnSpan(_useScaleCheckBox, 2);

        settingsLayout.Controls.Add(_scaleLabel, 0, 1);
        settingsLayout.Controls.Add(_scaleNumeric, 1, 1);

        settingsLayout.Controls.Add(_widthLabel, 0, 2);
        settingsLayout.Controls.Add(_widthNumeric, 1, 2);

        settingsLayout.Controls.Add(_heightLabel, 0, 3);
        settingsLayout.Controls.Add(_heightNumeric, 1, 3);

        SettingsPanel.Controls.Add(settingsLayout);

        UpdateInputEnabledState();

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void InitUseScaleControl()
    {
        _useScaleCheckBox.Text = "Use Scale";
        _useScaleCheckBox.Dock = DockStyle.Fill;
        _useScaleCheckBox.Margin = new Padding(0, 0, 0, 4);
        _useScaleCheckBox.Checked = _processor.UseScale;

        _useScaleCheckBox.CheckedChanged += (_, _) =>
        {
            _processor.UseScale = _useScaleCheckBox.Checked;
            UpdateInputEnabledState();
        };
    }

    private void InitScaleControls()
    {
        _scaleLabel.Text = "Scale";
        _scaleLabel.Dock = DockStyle.Fill;
        _scaleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _scaleLabel.Margin = Padding.Empty;

        _scaleNumeric.Dock = DockStyle.Fill;
        _scaleNumeric.Minimum = 0.01M;
        _scaleNumeric.Maximum = 10.0M;
        _scaleNumeric.DecimalPlaces = 2;
        _scaleNumeric.Increment = 0.1M;
        _scaleNumeric.Value = (decimal)_processor.Scale;
        _scaleNumeric.Margin = new Padding(0, 0, 0, 4);

        _scaleNumeric.ValueChanged += (_, _) =>
        {
            _processor.Scale = (double)_scaleNumeric.Value;
        };
    }

    private void InitWidthControls()
    {
        _widthLabel.Text = "Width";
        _widthLabel.Dock = DockStyle.Fill;
        _widthLabel.TextAlign = ContentAlignment.MiddleLeft;
        _widthLabel.Margin = Padding.Empty;

        _widthNumeric.Dock = DockStyle.Fill;
        _widthNumeric.Minimum = 1;
        _widthNumeric.Maximum = 100000;
        _widthNumeric.DecimalPlaces = 0;
        _widthNumeric.Increment = 10;
        _widthNumeric.Value = _processor.TargetWidth;
        _widthNumeric.Margin = new Padding(0, 0, 0, 4);

        _widthNumeric.ValueChanged += (_, _) =>
        {
            _processor.TargetWidth = (int)_widthNumeric.Value;
        };
    }

    private void InitHeightControls()
    {
        _heightLabel.Text = "Height";
        _heightLabel.Dock = DockStyle.Fill;
        _heightLabel.TextAlign = ContentAlignment.MiddleLeft;
        _heightLabel.Margin = Padding.Empty;

        _heightNumeric.Dock = DockStyle.Fill;
        _heightNumeric.Minimum = 1;
        _heightNumeric.Maximum = 100000;
        _heightNumeric.DecimalPlaces = 0;
        _heightNumeric.Increment = 10;
        _heightNumeric.Value = _processor.TargetHeight;
        _heightNumeric.Margin = Padding.Empty;

        _heightNumeric.ValueChanged += (_, _) =>
        {
            _processor.TargetHeight = (int)_heightNumeric.Value;
        };
    }

    private void UpdateInputEnabledState()
    {
        bool useScale = _processor.UseScale;

        _scaleNumeric.Enabled = useScale;

        _widthNumeric.Enabled = !useScale;
        _heightNumeric.Enabled = !useScale;
    }

    private void OnImageUpdated(Mat? image)
    {
        SetPreviewImage(
            image,
            image == null || image.Empty()
                ? "No Image"
                : CreateStatusText(image));
    }

    private string CreateStatusText(Mat image)
    {
        string mode = _processor.UseScale
            ? $"Scale={_processor.Scale:F2}"
            : $"{_processor.TargetWidth}x{_processor.TargetHeight}";

        return $"{image.Width} x {image.Height} / {image.Channels()}ch / {mode}";
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_scaleLabel, theme);
        ApplyLabelTheme(_widthLabel, theme);
        ApplyLabelTheme(_heightLabel, theme);

        _useScaleCheckBox.ForeColor = theme.TextColor;
        _useScaleCheckBox.BackColor = theme.NodeBodyColor;

        _scaleNumeric.BackColor = Color.White;
        _scaleNumeric.ForeColor = Color.Black;

        _widthNumeric.BackColor = Color.White;
        _widthNumeric.ForeColor = Color.Black;

        _heightNumeric.BackColor = Color.White;
        _heightNumeric.ForeColor = Color.Black;
    }

    private static void ApplyLabelTheme(Label label, NodeCanvasTheme theme)
    {
        label.ForeColor = theme.TextColor;
        label.BackColor = theme.NodeBodyColor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processor.ImageUpdated -= OnImageUpdated;
        }

        base.Dispose(disposing);
    }
}