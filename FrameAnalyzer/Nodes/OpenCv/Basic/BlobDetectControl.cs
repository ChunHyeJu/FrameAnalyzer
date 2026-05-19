using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class BlobDetectControl : OpenCvPreviewNodeControl
{
    private readonly Label _minAreaLabel = new();
    private readonly NumericUpDown _minAreaNumeric = new();

    private readonly Label _maxAreaLabel = new();
    private readonly NumericUpDown _maxAreaNumeric = new();

    private readonly BlobDetectProcessor _processor;

    public BlobDetectControl(BlobDetectProcessor processor)
        : base(defaultWidth: 260, defaultHeight: 250, settingsHeight: 58)
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

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        InitMinAreaControls();
        InitMaxAreaControls();

        settingsLayout.Controls.Add(_minAreaLabel, 0, 0);
        settingsLayout.Controls.Add(_minAreaNumeric, 1, 0);

        settingsLayout.Controls.Add(_maxAreaLabel, 0, 1);
        settingsLayout.Controls.Add(_maxAreaNumeric, 1, 1);

        SettingsPanel.Controls.Add(settingsLayout);

        _processor.ResultUpdated += OnResultUpdated;
    }

    private void InitMinAreaControls()
    {
        _minAreaLabel.Text = "MinArea";
        _minAreaLabel.Dock = DockStyle.Fill;
        _minAreaLabel.TextAlign = ContentAlignment.MiddleLeft;
        _minAreaLabel.Margin = Padding.Empty;

        _minAreaNumeric.Dock = DockStyle.Fill;
        _minAreaNumeric.Minimum = 0;
        _minAreaNumeric.Maximum = 100000000;
        _minAreaNumeric.DecimalPlaces = 0;
        _minAreaNumeric.Increment = 10;
        _minAreaNumeric.Value = (decimal)_processor.MinArea;
        _minAreaNumeric.Margin = new Padding(0, 0, 0, 4);

        _minAreaNumeric.ValueChanged += (_, _) =>
        {
            _processor.MinArea = (double)_minAreaNumeric.Value;

            if (_processor.MinArea > _processor.MaxArea)
            {
                _processor.MaxArea = _processor.MinArea;
                _maxAreaNumeric.Value = (decimal)_processor.MaxArea;
            }
        };
    }

    private void InitMaxAreaControls()
    {
        _maxAreaLabel.Text = "MaxArea";
        _maxAreaLabel.Dock = DockStyle.Fill;
        _maxAreaLabel.TextAlign = ContentAlignment.MiddleLeft;
        _maxAreaLabel.Margin = Padding.Empty;

        _maxAreaNumeric.Dock = DockStyle.Fill;
        _maxAreaNumeric.Minimum = 0;
        _maxAreaNumeric.Maximum = 100000000;
        _maxAreaNumeric.DecimalPlaces = 0;
        _maxAreaNumeric.Increment = 100;
        _maxAreaNumeric.Value = (decimal)_processor.MaxArea;
        _maxAreaNumeric.Margin = Padding.Empty;

        _maxAreaNumeric.ValueChanged += (_, _) =>
        {
            _processor.MaxArea = (double)_maxAreaNumeric.Value;

            if (_processor.MaxArea < _processor.MinArea)
            {
                _processor.MinArea = _processor.MaxArea;
                _minAreaNumeric.Value = (decimal)_processor.MinArea;
            }
        };
    }

    private void OnResultUpdated(Mat? image, IReadOnlyList<BlobInfo> blobs)
    {
        SetPreviewImage(
            image,
            image == null || image.Empty()
                ? "No Image"
                : CreateStatusText(image, blobs));
    }

    private string CreateStatusText(Mat image, IReadOnlyList<BlobInfo> blobs)
    {
        return
            $"{image.Width} x {image.Height} / " +
            $"Count={blobs.Count} / " +
            $"Area={_processor.MinArea:F0}~{_processor.MaxArea:F0}";
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_minAreaLabel, theme);
        ApplyLabelTheme(_maxAreaLabel, theme);

        _minAreaNumeric.BackColor = Color.White;
        _minAreaNumeric.ForeColor = Color.Black;

        _maxAreaNumeric.BackColor = Color.White;
        _maxAreaNumeric.ForeColor = Color.Black;
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
            _processor.ResultUpdated -= OnResultUpdated;
        }

        base.Dispose(disposing);
    }
}