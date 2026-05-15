using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class BrightnessContrastControl : OpenCvPreviewNodeControl
{
    private readonly Label _alphaLabel = new();
    private readonly NumericUpDown _alphaNumeric = new();

    private readonly Label _betaLabel = new();
    private readonly NumericUpDown _betaNumeric = new();

    private readonly BrightnessContrastProcessor _processor;

    public BrightnessContrastControl(BrightnessContrastProcessor processor)
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

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        InitAlphaControls();
        InitBetaControls();

        settingsLayout.Controls.Add(_alphaLabel, 0, 0);
        settingsLayout.Controls.Add(_alphaNumeric, 1, 0);

        settingsLayout.Controls.Add(_betaLabel, 0, 1);
        settingsLayout.Controls.Add(_betaNumeric, 1, 1);

        SettingsPanel.Controls.Add(settingsLayout);

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void InitAlphaControls()
    {
        _alphaLabel.Text = "Alpha";
        _alphaLabel.Dock = DockStyle.Fill;
        _alphaLabel.TextAlign = ContentAlignment.MiddleLeft;
        _alphaLabel.Margin = Padding.Empty;

        _alphaNumeric.Dock = DockStyle.Fill;
        _alphaNumeric.Minimum = 0;
        _alphaNumeric.Maximum = 10;
        _alphaNumeric.DecimalPlaces = 2;
        _alphaNumeric.Increment = 0.1M;
        _alphaNumeric.Value = (decimal)_processor.Alpha;
        _alphaNumeric.Margin = new Padding(0, 0, 0, 4);

        _alphaNumeric.ValueChanged += (_, _) =>
        {
            _processor.Alpha = (double)_alphaNumeric.Value;
        };
    }

    private void InitBetaControls()
    {
        _betaLabel.Text = "Beta";
        _betaLabel.Dock = DockStyle.Fill;
        _betaLabel.TextAlign = ContentAlignment.MiddleLeft;
        _betaLabel.Margin = Padding.Empty;

        _betaNumeric.Dock = DockStyle.Fill;
        _betaNumeric.Minimum = -255;
        _betaNumeric.Maximum = 255;
        _betaNumeric.DecimalPlaces = 0;
        _betaNumeric.Increment = 1;
        _betaNumeric.Value = (decimal)_processor.Beta;
        _betaNumeric.Margin = Padding.Empty;

        _betaNumeric.ValueChanged += (_, _) =>
        {
            _processor.Beta = (double)_betaNumeric.Value;
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
            $"A={_processor.Alpha:F2} / B={_processor.Beta:F0}";
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_alphaLabel, theme);
        ApplyLabelTheme(_betaLabel, theme);

        _alphaNumeric.BackColor = Color.White;
        _alphaNumeric.ForeColor = Color.Black;

        _betaNumeric.BackColor = Color.White;
        _betaNumeric.ForeColor = Color.Black;
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