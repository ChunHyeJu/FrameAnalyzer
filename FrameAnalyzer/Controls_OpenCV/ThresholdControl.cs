using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ThresholdControl : OpenCvPreviewNodeControl
{
    private readonly Label _thresholdLabel = new();
    private readonly NumericUpDown _thresholdNumeric = new();

    private readonly Label _maxValueLabel = new();
    private readonly NumericUpDown _maxValueNumeric = new();

    private readonly Label _typeLabel = new();
    private readonly ComboBox _typeComboBox = new();

    private readonly ThresholdProcessor _processor;

    public ThresholdControl(ThresholdProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 270, settingsHeight: 88)
    {
        _processor = processor;

        TableLayoutPanel settingsLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        InitThresholdControls();
        InitMaxValueControls();
        InitTypeControls();

        settingsLayout.Controls.Add(_thresholdLabel, 0, 0);
        settingsLayout.Controls.Add(_thresholdNumeric, 1, 0);

        settingsLayout.Controls.Add(_maxValueLabel, 0, 1);
        settingsLayout.Controls.Add(_maxValueNumeric, 1, 1);

        settingsLayout.Controls.Add(_typeLabel, 0, 2);
        settingsLayout.Controls.Add(_typeComboBox, 1, 2);

        SettingsPanel.Controls.Add(settingsLayout);

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void InitThresholdControls()
    {
        _thresholdLabel.Text = "Threshold";
        _thresholdLabel.Dock = DockStyle.Fill;
        _thresholdLabel.TextAlign = ContentAlignment.MiddleLeft;
        _thresholdLabel.Margin = Padding.Empty;

        _thresholdNumeric.Dock = DockStyle.Fill;
        _thresholdNumeric.Minimum = 0;
        _thresholdNumeric.Maximum = 255;
        _thresholdNumeric.DecimalPlaces = 0;
        _thresholdNumeric.Increment = 1;
        _thresholdNumeric.Value = (decimal)_processor.Threshold;
        _thresholdNumeric.Margin = new Padding(0, 0, 0, 4);

        _thresholdNumeric.ValueChanged += (_, _) =>
        {
            _processor.Threshold = (double)_thresholdNumeric.Value;
        };
    }

    private void InitMaxValueControls()
    {
        _maxValueLabel.Text = "Max";
        _maxValueLabel.Dock = DockStyle.Fill;
        _maxValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        _maxValueLabel.Margin = Padding.Empty;

        _maxValueNumeric.Dock = DockStyle.Fill;
        _maxValueNumeric.Minimum = 0;
        _maxValueNumeric.Maximum = 255;
        _maxValueNumeric.DecimalPlaces = 0;
        _maxValueNumeric.Increment = 1;
        _maxValueNumeric.Value = (decimal)_processor.MaxValue;
        _maxValueNumeric.Margin = new Padding(0, 0, 0, 4);

        _maxValueNumeric.ValueChanged += (_, _) =>
        {
            _processor.MaxValue = (double)_maxValueNumeric.Value;
        };
    }

    private void InitTypeControls()
    {
        _typeLabel.Text = "Type";
        _typeLabel.Dock = DockStyle.Fill;
        _typeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _typeLabel.Margin = Padding.Empty;

        _typeComboBox.Dock = DockStyle.Fill;
        _typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _typeComboBox.Margin = Padding.Empty;
        _typeComboBox.Items.AddRange(Enum.GetNames(typeof(ThresholdMode)));
        _typeComboBox.SelectedItem = _processor.Mode.ToString();

        _typeComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_typeComboBox.SelectedItem == null)
                return;

            _processor.Mode = Enum.Parse<ThresholdMode>(
                _typeComboBox.SelectedItem.ToString()!);

            UpdateInputEnabledState();
        };

        UpdateInputEnabledState();
    }

    private void UpdateInputEnabledState()
    {
        bool autoThreshold =
            _processor.Mode == ThresholdMode.Otsu ||
            _processor.Mode == ThresholdMode.Triangle;

        // Otsu, Triangle은 OpenCV가 임계값을 자동 계산하므로
        // 사용자가 입력한 Threshold 값은 사실상 사용되지 않습니다.
        _thresholdNumeric.Enabled = !autoThreshold;
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
            $"{_processor.Mode} / T={_processor.Threshold:F0} / Max={_processor.MaxValue:F0}";
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_thresholdLabel, theme);
        ApplyLabelTheme(_maxValueLabel, theme);
        ApplyLabelTheme(_typeLabel, theme);

        _thresholdNumeric.BackColor = Color.White;
        _thresholdNumeric.ForeColor = Color.Black;

        _maxValueNumeric.BackColor = Color.White;
        _maxValueNumeric.ForeColor = Color.Black;

        _typeComboBox.BackColor = Color.White;
        _typeComboBox.ForeColor = Color.Black;
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