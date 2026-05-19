using FrameAnalyzer.NodeEditor;
using FrameAnalyzer.Nodes.OpenCv;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class ROIColorSamplerControl : OpenCvPreviewNodeControl
{
    private const int CoordinateMaximum = 100000;

    private readonly Label _xLabel = new();
    private readonly NumericUpDown _xNumeric = new();

    private readonly Label _yLabel = new();
    private readonly NumericUpDown _yNumeric = new();

    private readonly Label _widthLabel = new();
    private readonly NumericUpDown _widthNumeric = new();

    private readonly Label _heightLabel = new();
    private readonly NumericUpDown _heightNumeric = new();

    private readonly Label _formatLabel = new();
    private readonly ComboBox _formatComboBox = new();

    private readonly Panel _colorPanel = new();
    private readonly Label _colorLabel = new();

    private readonly ROIColorSamplerProcessor _processor;
    private bool _updatingUi;

    public ROIColorSamplerControl(ROIColorSamplerProcessor processor)
        : base(defaultWidth: 280, defaultHeight: 350, settingsHeight: 124)
    {
        _processor = processor;

        InitializeSettings();
        BindInitialProcessorValues();

        _processor.RoiDataUpdated += OnRoiDataUpdated;
        _processor.ColorUpdated += OnColorUpdated;
    }

    private void InitializeSettings()
    {
        TableLayoutPanel settingsLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        settingsLayout.Controls.Add(CreateRoiInputLayout(), 0, 0);
        settingsLayout.Controls.Add(CreateFormatLayout(), 0, 1);
        settingsLayout.Controls.Add(CreateColorLayout(), 0, 2);

        SettingsPanel.Controls.Add(settingsLayout);
    }

    private TableLayoutPanel CreateRoiInputLayout()
    {
        TableLayoutPanel roiLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        for (int i = 0; i < 4; i++)
            roiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        roiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        roiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        InitializeLabel(_xLabel, "X");
        InitializeLabel(_yLabel, "Y");
        InitializeLabel(_widthLabel, "W");
        InitializeLabel(_heightLabel, "H");

        InitializeNumeric(_xNumeric, 0);
        InitializeNumeric(_yNumeric, 0);
        InitializeNumeric(_widthNumeric, 64, 1);
        InitializeNumeric(_heightNumeric, 64, 1);

        roiLayout.Controls.Add(_xLabel, 0, 0);
        roiLayout.Controls.Add(_yLabel, 1, 0);
        roiLayout.Controls.Add(_widthLabel, 2, 0);
        roiLayout.Controls.Add(_heightLabel, 3, 0);

        roiLayout.Controls.Add(_xNumeric, 0, 1);
        roiLayout.Controls.Add(_yNumeric, 1, 1);
        roiLayout.Controls.Add(_widthNumeric, 2, 1);
        roiLayout.Controls.Add(_heightNumeric, 3, 1);

        return roiLayout;
    }

    private TableLayoutPanel CreateFormatLayout()
    {
        TableLayoutPanel formatLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        formatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        formatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        InitializeLabel(_formatLabel, "Format");

        _formatComboBox.Dock = DockStyle.Fill;
        _formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _formatComboBox.Margin = new Padding(0, 0, 0, 4);
        _formatComboBox.Items.AddRange(Enum.GetNames(typeof(ColorFormat)));
        _formatComboBox.SelectedItem = _processor.ColorFormat.ToString();
        _formatComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            if (_formatComboBox.SelectedItem == null)
                return;

            _processor.ColorFormat = Enum.Parse<ColorFormat>(
                _formatComboBox.SelectedItem.ToString()!);
        };

        formatLayout.Controls.Add(_formatLabel, 0, 0);
        formatLayout.Controls.Add(_formatComboBox, 1, 0);

        return formatLayout;
    }

    private TableLayoutPanel CreateColorLayout()
    {
        TableLayoutPanel colorLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        colorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        colorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _colorPanel.Dock = DockStyle.Fill;
        _colorPanel.Margin = new Padding(0, 0, 8, 0);
        _colorPanel.BorderStyle = BorderStyle.FixedSingle;
        _colorPanel.BackColor = Color.FromArgb(35, 35, 35);

        _colorLabel.Dock = DockStyle.Fill;
        _colorLabel.Margin = Padding.Empty;
        _colorLabel.TextAlign = ContentAlignment.MiddleLeft;
        _colorLabel.AutoEllipsis = true;
        _colorLabel.Text = "No Color";

        colorLayout.Controls.Add(_colorPanel, 0, 0);
        colorLayout.Controls.Add(_colorLabel, 1, 0);

        return colorLayout;
    }

    private static void InitializeLabel(Label label, string text)
    {
        label.Text = text;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = Padding.Empty;
    }

    private void InitializeNumeric(NumericUpDown numeric, int value, int minimum = 0)
    {
        numeric.Dock = DockStyle.Fill;
        numeric.Minimum = minimum;
        numeric.Maximum = CoordinateMaximum;
        numeric.DecimalPlaces = 0;
        numeric.Increment = 1;
        numeric.Value = value;
        numeric.Margin = new Padding(0, 0, 6, 4);
        numeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            ApplyRoiSettings();
        };
    }

    private void BindInitialProcessorValues()
    {
        _updatingUi = true;

        try
        {
            if (_processor.RoiData != null)
                BindRoiData(_processor.RoiData);

            _formatComboBox.SelectedItem = _processor.ColorFormat.ToString();
        }
        finally
        {
            _updatingUi = false;
        }

        if (_processor.RoiData == null)
            ApplyRoiSettings();
    }

    private void ApplyRoiSettings()
    {
        _processor.RoiData = new RoiData(
            (int)_xNumeric.Value,
            (int)_yNumeric.Value,
            Math.Max(1, (int)_widthNumeric.Value),
            Math.Max(1, (int)_heightNumeric.Value));
    }

    private void OnRoiDataUpdated(RoiData? roiData)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnRoiDataUpdated(roiData)));
            return;
        }

        if (roiData == null)
            return;

        _updatingUi = true;

        try
        {
            BindRoiData(roiData);
        }
        finally
        {
            _updatingUi = false;
        }
    }

    private void BindRoiData(RoiData roiData)
    {
        SetNumericValue(_xNumeric, roiData.OffsetX);
        SetNumericValue(_yNumeric, roiData.OffsetY);
        SetNumericValue(_widthNumeric, roiData.X);
        SetNumericValue(_heightNumeric, roiData.Y);
    }

    private static void SetNumericValue(NumericUpDown numeric, int value)
    {
        decimal next = Math.Clamp(value, (int)numeric.Minimum, (int)numeric.Maximum);

        if (numeric.Value != next)
            numeric.Value = next;
    }

    private void OnColorUpdated(Scalar? averageColor, Mat? roiImage)
    {
        if (InvokeRequired)
        {
            Mat? roiImageCopy = null;

            if (roiImage != null && !roiImage.Empty())
                roiImageCopy = roiImage.Clone();

            BeginInvoke(new Action(() =>
            {
                try
                {
                    OnColorUpdated(averageColor, roiImageCopy);
                }
                finally
                {
                    roiImageCopy?.Dispose();
                }
            }));

            return;
        }

        UpdateColorDisplay(averageColor, roiImage);

        SetPreviewImage(
            roiImage,
            roiImage == null || roiImage.Empty()
                ? "No ROI"
                : CreateStatusText(averageColor, roiImage));
    }

    private void UpdateColorDisplay(Scalar? averageColor, Mat? roiImage)
    {
        if (averageColor == null || roiImage == null || roiImage.Empty())
        {
            _colorPanel.BackColor = Color.FromArgb(35, 35, 35);
            _colorLabel.Text = "No Color";
            return;
        }

        _colorPanel.BackColor = CreateSwatchColor(roiImage);
        _colorLabel.Text = FormatColor(averageColor.Value);
    }

    private string CreateStatusText(Scalar? averageColor, Mat roiImage)
    {
        string colorText = averageColor == null
            ? "No Color"
            : FormatColor(averageColor.Value);

        return
            $"{roiImage.Width} x {roiImage.Height} / " +
            $"{roiImage.Channels()}ch / {colorText}";
    }

    private string FormatColor(Scalar color)
    {
        return _processor.ColorFormat switch
        {
            ColorFormat.RGB =>
                $"RGB ({color.Val0:F0}, {color.Val1:F0}, {color.Val2:F0})",
            ColorFormat.HSV =>
                $"HSV ({color.Val0:F0}, {color.Val1:F0}, {color.Val2:F0})",
            ColorFormat.LAB =>
                $"LAB ({color.Val0:F0}, {color.Val1:F0}, {color.Val2:F0})",
            _ => $"{color.Val0:F0}, {color.Val1:F0}, {color.Val2:F0}"
        };
    }

    private static Color CreateSwatchColor(Mat roiImage)
    {
        using Mat displayMat = CreateDisplayMat(roiImage);
        Scalar mean = Cv2.Mean(displayMat);

        return displayMat.Channels() switch
        {
            1 => Color.FromArgb(
                ClampColor(mean.Val0),
                ClampColor(mean.Val0),
                ClampColor(mean.Val0)),
            3 => Color.FromArgb(
                ClampColor(mean.Val2),
                ClampColor(mean.Val1),
                ClampColor(mean.Val0)),
            4 => Color.FromArgb(
                ClampColor(mean.Val2),
                ClampColor(mean.Val1),
                ClampColor(mean.Val0)),
            _ => Color.Black
        };
    }

    private static int ClampColor(double value)
    {
        return Math.Clamp((int)Math.Round(value), 0, 255);
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_xLabel, theme);
        ApplyLabelTheme(_yLabel, theme);
        ApplyLabelTheme(_widthLabel, theme);
        ApplyLabelTheme(_heightLabel, theme);
        ApplyLabelTheme(_formatLabel, theme);
        ApplyLabelTheme(_colorLabel, theme);

        _xNumeric.BackColor = Color.White;
        _xNumeric.ForeColor = Color.Black;

        _yNumeric.BackColor = Color.White;
        _yNumeric.ForeColor = Color.Black;

        _widthNumeric.BackColor = Color.White;
        _widthNumeric.ForeColor = Color.Black;

        _heightNumeric.BackColor = Color.White;
        _heightNumeric.ForeColor = Color.Black;

        _formatComboBox.BackColor = Color.White;
        _formatComboBox.ForeColor = Color.Black;
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
            _processor.RoiDataUpdated -= OnRoiDataUpdated;
            _processor.ColorUpdated -= OnColorUpdated;
        }

        base.Dispose(disposing);
    }
}
