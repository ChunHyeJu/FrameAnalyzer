using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class EdgeDetectControl : OpenCvPreviewNodeControl
{
    private readonly Label _threshold1Label = new();
    private readonly NumericUpDown _threshold1Numeric = new();

    private readonly Label _threshold2Label = new();
    private readonly NumericUpDown _threshold2Numeric = new();

    private readonly EdgeDetectProcessor _processor;

    public EdgeDetectControl(EdgeDetectProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 250, settingsHeight: 58)
    {
        _processor = processor;

        TableLayoutPanel settingsLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        InitThreshold1Controls();
        InitThreshold2Controls();

        settingsLayout.Controls.Add(_threshold1Label, 0, 0);
        settingsLayout.Controls.Add(_threshold1Numeric, 1, 0);

        settingsLayout.Controls.Add(_threshold2Label, 0, 1);
        settingsLayout.Controls.Add(_threshold2Numeric, 1, 1);

        SettingsPanel.Controls.Add(settingsLayout);

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void InitThreshold1Controls()
    {
        _threshold1Label.Text = "Threshold1";
        _threshold1Label.Dock = DockStyle.Fill;
        _threshold1Label.TextAlign = ContentAlignment.MiddleLeft;
        _threshold1Label.Margin = Padding.Empty;

        _threshold1Numeric.Dock = DockStyle.Fill;
        _threshold1Numeric.Minimum = 0;
        _threshold1Numeric.Maximum = 255;
        _threshold1Numeric.DecimalPlaces = 0;
        _threshold1Numeric.Increment = 1;
        _threshold1Numeric.Value = (decimal)_processor.Threshold1;
        _threshold1Numeric.Margin = new Padding(0, 0, 0, 4);

        _threshold1Numeric.ValueChanged += (_, _) =>
        {
            _processor.Threshold1 = (double)_threshold1Numeric.Value;
        };
    }

    private void InitThreshold2Controls()
    {
        _threshold2Label.Text = "Threshold2";
        _threshold2Label.Dock = DockStyle.Fill;
        _threshold2Label.TextAlign = ContentAlignment.MiddleLeft;
        _threshold2Label.Margin = Padding.Empty;

        _threshold2Numeric.Dock = DockStyle.Fill;
        _threshold2Numeric.Minimum = 0;
        _threshold2Numeric.Maximum = 255;
        _threshold2Numeric.DecimalPlaces = 0;
        _threshold2Numeric.Increment = 1;
        _threshold2Numeric.Value = (decimal)_processor.Threshold2;
        _threshold2Numeric.Margin = Padding.Empty;

        _threshold2Numeric.ValueChanged += (_, _) =>
        {
            _processor.Threshold2 = (double)_threshold2Numeric.Value;
        };
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
        return
            $"{image.Width} x {image.Height} / {image.Channels()}ch / " +
            $"T1={_processor.Threshold1:F0} / T2={_processor.Threshold2:F0}";
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_threshold1Label, theme);
        ApplyLabelTheme(_threshold2Label, theme);

        _threshold1Numeric.BackColor = Color.White;
        _threshold1Numeric.ForeColor = Color.Black;

        _threshold2Numeric.BackColor = Color.White;
        _threshold2Numeric.ForeColor = Color.Black;
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