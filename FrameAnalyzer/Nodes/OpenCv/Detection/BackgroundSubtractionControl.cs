using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using System.Diagnostics;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Detection;

public class BackgroundSubtractionControl : OpenCvPreviewNodeControl
{
    #region Fields

    private readonly BackgroundSubtractionProcessor _processor;

    private readonly Label _historyLabel = new();
    private readonly NumericUpDown _historyNumeric = new();

    private readonly Label _varThresholdLabel = new();
    private readonly NumericUpDown _varThresholdNumeric = new();

    private readonly CheckBox _detectShadowsCheckBox = new();

    private readonly Label _thresholdLabel = new();
    private readonly NumericUpDown _thresholdNumeric = new();

    private readonly Label _minAreaLabel = new();
    private readonly NumericUpDown _minAreaNumeric = new();

    private readonly Label _fillMinAreaLabel = new();
    private readonly NumericUpDown _fillMinAreaNumeric = new();

    private readonly Label _openKernelLabel = new();
    private readonly NumericUpDown _openKernelNumeric = new();

    private readonly Label _closeKernelLabel = new();
    private readonly NumericUpDown _closeKernelNumeric = new();

    private readonly Label _warmUpLabel = new();
    private readonly NumericUpDown _warmUpNumeric = new();

    private readonly Label _viewLabel = new();
    private readonly ComboBox _viewComboBox = new();

    private readonly Button _resetButton = new();
    private readonly Button _fixButton = new();

    private bool _updatingUi;

    #endregion

    #region Constructor

    public BackgroundSubtractionControl(BackgroundSubtractionProcessor processor)
        : base(defaultWidth: 280, defaultHeight: 430, settingsHeight: 248)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));

        InitializeSettingsLayout();
        BindProcessorValues();

        _processor.ResultUpdated += OnResultUpdated;
    }

    #endregion

    #region Initialize

    private void InitializeSettingsLayout()
    {
        TableLayoutPanel settingsLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 11,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < settingsLayout.RowCount; i++)
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / settingsLayout.RowCount));

        InitializeHistoryControls();
        InitializeVarThresholdControls();
        InitializeDetectShadowsControls();

        InitializeThresholdControls();
        InitializeMinAreaControls();
        InitializeFillMinAreaControls();

        InitializeOpenKernelControls();
        InitializeCloseKernelControls();

        InitializeWarmUpControls();
        InitializeViewControls();

        InitializeResetButton();
        InitializeFixButton();

        AddRow(settingsLayout, 0, _historyLabel, _historyNumeric);
        AddRow(settingsLayout, 1, _varThresholdLabel, _varThresholdNumeric);

        settingsLayout.Controls.Add(_detectShadowsCheckBox, 0, 2);
        settingsLayout.SetColumnSpan(_detectShadowsCheckBox, 2);

        AddRow(settingsLayout, 3, _thresholdLabel, _thresholdNumeric);
        AddRow(settingsLayout, 4, _minAreaLabel, _minAreaNumeric);
        AddRow(settingsLayout, 5, _fillMinAreaLabel, _fillMinAreaNumeric);

        AddRow(settingsLayout, 6, _openKernelLabel, _openKernelNumeric);
        AddRow(settingsLayout, 7, _closeKernelLabel, _closeKernelNumeric);

        AddRow(settingsLayout, 8, _warmUpLabel, _warmUpNumeric);
        AddRow(settingsLayout, 9, _viewLabel, _viewComboBox);

        TableLayoutPanel buttonLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        buttonLayout.Controls.Add(_resetButton, 0, 0);
        buttonLayout.Controls.Add(_fixButton, 1, 0);

        settingsLayout.Controls.Add(buttonLayout, 0, 10);
        settingsLayout.SetColumnSpan(buttonLayout, 2);

        SettingsPanel.Controls.Add(settingsLayout);
    }

    private static void AddRow(
        TableLayoutPanel layout,
        int row,
        Control label,
        Control input)
    {
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(input, 1, row);
    }

    private void InitializeHistoryControls()
    {
        InitializeLabel(_historyLabel, "History");

        InitializeNumeric(
            _historyNumeric,
            minimum: 1,
            maximum: 10000,
            increment: 10,
            decimalPlaces: 0);

        _historyNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.History = (int)_historyNumeric.Value;
            SetStatusText("Background model reset");
        };
    }

    private void InitializeVarThresholdControls()
    {
        InitializeLabel(_varThresholdLabel, "VarThres");

        InitializeNumeric(
            _varThresholdNumeric,
            minimum: 1,
            maximum: 255,
            increment: 1,
            decimalPlaces: 1);

        _varThresholdNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.VarThreshold = (double)_varThresholdNumeric.Value;
            SetStatusText("Background model reset");
        };
    }

    private void InitializeDetectShadowsControls()
    {
        _detectShadowsCheckBox.Text = "Detect Shadows";
        _detectShadowsCheckBox.Dock = DockStyle.Fill;
        _detectShadowsCheckBox.Margin = new Padding(0, 0, 0, 4);

        _detectShadowsCheckBox.CheckedChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.DetectShadows = _detectShadowsCheckBox.Checked;
            SetStatusText("Background model reset");
        };
    }

    private void InitializeThresholdControls()
    {
        InitializeLabel(_thresholdLabel, "Threshold");

        InitializeNumeric(
            _thresholdNumeric,
            minimum: 0,
            maximum: 255,
            increment: 1,
            decimalPlaces: 0);

        _thresholdNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.ThresholdValue = (int)_thresholdNumeric.Value;
        };
    }

    private void InitializeMinAreaControls()
    {
        InitializeLabel(_minAreaLabel, "MinArea");

        InitializeNumeric(
            _minAreaNumeric,
            minimum: 0,
            maximum: 100000000,
            increment: 10,
            decimalPlaces: 0);

        _minAreaNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.MinArea = (int)_minAreaNumeric.Value;
        };
    }

    private void InitializeFillMinAreaControls()
    {
        InitializeLabel(_fillMinAreaLabel, "FillArea");

        InitializeNumeric(
            _fillMinAreaNumeric,
            minimum: 0,
            maximum: 100000000,
            increment: 10,
            decimalPlaces: 0);

        _fillMinAreaNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.FillMinArea = (int)_fillMinAreaNumeric.Value;
        };
    }

    private void InitializeOpenKernelControls()
    {
        InitializeLabel(_openKernelLabel, "Open");

        InitializeNumeric(
            _openKernelNumeric,
            minimum: 0,
            maximum: 99,
            increment: 1,
            decimalPlaces: 0);

        _openKernelNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.OpenKernelSize = (int)_openKernelNumeric.Value;
        };
    }

    private void InitializeCloseKernelControls()
    {
        InitializeLabel(_closeKernelLabel, "Close");

        InitializeNumeric(
            _closeKernelNumeric,
            minimum: 0,
            maximum: 99,
            increment: 1,
            decimalPlaces: 0);

        _closeKernelNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.CloseKernelSize = (int)_closeKernelNumeric.Value;
        };
    }

    private void InitializeWarmUpControls()
    {
        InitializeLabel(_warmUpLabel, "WarmUp");

        InitializeNumeric(
            _warmUpNumeric,
            minimum: 0,
            maximum: 1000,
            increment: 1,
            decimalPlaces: 0);

        _warmUpNumeric.ValueChanged += (_, _) =>
        {
            if (_updatingUi)
                return;

            _processor.WarmUpFrames = (int)_warmUpNumeric.Value;
        };
    }

    private void InitializeViewControls()
    {
        InitializeLabel(_viewLabel, "View");

        _viewComboBox.Dock = DockStyle.Fill;
        _viewComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _viewComboBox.Margin = new Padding(0, 0, 0, 4);

        _viewComboBox.Items.AddRange(new object[]
        {
            "Image",
            "Mask"
        });

        _viewComboBox.SelectedIndex = 0;

        _viewComboBox.SelectedIndexChanged += (_, _) =>
        {
            SetStatusText("Waiting next frame...");
        };
    }

    private void InitializeResetButton()
    {
        _resetButton.Text = "Reset";
        _resetButton.Dock = DockStyle.Fill;
        _resetButton.Margin = new Padding(0, 0, 4, 0);

        _resetButton.Click += (_, _) =>
        {
            _processor.ResetModel();

            _updatingUi = true;

            try
            {
                UpdateFixButtonText();
            }
            finally
            {
                _updatingUi = false;
            }

            SetPreviewImage(null);
            SetStatusText("Background reset");
        };
    }

    private void InitializeFixButton()
    {
        _fixButton.Dock = DockStyle.Fill;
        _fixButton.Margin = Padding.Empty;

        UpdateFixButtonText();

        _fixButton.Click += (_, _) =>
        {
            bool nextFixedState = !_processor.IsBackgroundFixed;

            _processor.SetBackgroundFixed(nextFixedState);

            UpdateFixButtonText();

            SetStatusText(nextFixedState
                ? "Background fixed"
                : "Background learning");
        };
    }

    private static void InitializeLabel(Label label, string text)
    {
        label.Text = text;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = Padding.Empty;
    }

    private static void InitializeNumeric(
        NumericUpDown numeric,
        decimal minimum,
        decimal maximum,
        decimal increment,
        int decimalPlaces)
    {
        numeric.Dock = DockStyle.Fill;
        numeric.Minimum = minimum;
        numeric.Maximum = maximum;
        numeric.Increment = increment;
        numeric.DecimalPlaces = decimalPlaces;
        numeric.Margin = new Padding(0, 0, 0, 4);
    }

    #endregion

    #region Bind

    private void BindProcessorValues()
    {
        _updatingUi = true;

        try
        {
            SetNumericValue(_historyNumeric, _processor.History);
            SetNumericValue(_varThresholdNumeric, (decimal)_processor.VarThreshold);

            _detectShadowsCheckBox.Checked = _processor.DetectShadows;

            SetNumericValue(_thresholdNumeric, _processor.ThresholdValue);
            SetNumericValue(_minAreaNumeric, _processor.MinArea);
            SetNumericValue(_fillMinAreaNumeric, _processor.FillMinArea);

            SetNumericValue(_openKernelNumeric, _processor.OpenKernelSize);
            SetNumericValue(_closeKernelNumeric, _processor.CloseKernelSize);

            SetNumericValue(_warmUpNumeric, _processor.WarmUpFrames);

            UpdateFixButtonText();
        }
        finally
        {
            _updatingUi = false;
        }
    }

    private static void SetNumericValue(NumericUpDown numeric, decimal value)
    {
        if (value < numeric.Minimum)
            value = numeric.Minimum;

        if (value > numeric.Maximum)
            value = numeric.Maximum;

        numeric.Value = value;
    }

    private void UpdateFixButtonText()
    {
        _fixButton.Text = _processor.IsBackgroundFixed
            ? "Resume Learning"
            : "Fix Background";
    }

    #endregion

    #region Update

    private void OnResultUpdated(Mat? mask, Mat? resultImage)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            Mat? maskCopy = mask != null && !mask.Empty() ? mask.Clone() : null;
            Mat? resultCopy = resultImage != null && !resultImage.Empty() ? resultImage.Clone() : null;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UpdatePreview(maskCopy, resultCopy);
                    }
                    finally
                    {
                        maskCopy?.Dispose();
                        resultCopy?.Dispose();
                    }
                }));
            }
            catch (ObjectDisposedException)
            {
                maskCopy?.Dispose();
                resultCopy?.Dispose();
            }
            catch (InvalidOperationException)
            {
                maskCopy?.Dispose();
                resultCopy?.Dispose();
            }

            return;
        }

        UpdatePreview(mask, resultImage);
    }

    private void UpdatePreview(Mat? mask, Mat? resultImage)
    {
        try
        {
            Mat? image = GetSelectedPreviewImage(mask, resultImage);

            SetPreviewImage(
                image,
                image == null || image.Empty()
                    ? "No Image"
                    : CreateStatusText(image));
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private Mat? GetSelectedPreviewImage(Mat? mask, Mat? resultImage)
    {
        return _viewComboBox.SelectedIndex switch
        {
            0 => resultImage,
            1 => mask,
            _ => resultImage
        };
    }

    private string CreateStatusText(Mat image)
    {
        string viewName = _viewComboBox.SelectedIndex == 0
            ? "Image"
            : "Mask";

        string backgroundState = _processor.IsBackgroundFixed
            ? "Fixed"
            : "Learning";

        return $"{viewName} / {image.Width} x {image.Height} / {backgroundState} / Obj {_processor.LastObjectCount}";
    }

    #endregion

    #region Theme

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_historyLabel, theme);
        ApplyLabelTheme(_varThresholdLabel, theme);
        ApplyLabelTheme(_thresholdLabel, theme);
        ApplyLabelTheme(_minAreaLabel, theme);
        ApplyLabelTheme(_fillMinAreaLabel, theme);
        ApplyLabelTheme(_openKernelLabel, theme);
        ApplyLabelTheme(_closeKernelLabel, theme);
        ApplyLabelTheme(_warmUpLabel, theme);
        ApplyLabelTheme(_viewLabel, theme);

        _detectShadowsCheckBox.ForeColor = theme.TextColor;
        _detectShadowsCheckBox.BackColor = theme.NodeBodyColor;

        ApplyInputTheme(_historyNumeric);
        ApplyInputTheme(_varThresholdNumeric);
        ApplyInputTheme(_thresholdNumeric);
        ApplyInputTheme(_minAreaNumeric);
        ApplyInputTheme(_fillMinAreaNumeric);
        ApplyInputTheme(_openKernelNumeric);
        ApplyInputTheme(_closeKernelNumeric);
        ApplyInputTheme(_warmUpNumeric);
        ApplyInputTheme(_viewComboBox);

        ApplyButtonTheme(_resetButton);
        ApplyButtonTheme(_fixButton);
    }

    private static void ApplyLabelTheme(Label label, NodeCanvasTheme theme)
    {
        label.ForeColor = theme.TextColor;
        label.BackColor = theme.NodeBodyColor;
    }

    private static void ApplyInputTheme(Control control)
    {
        control.BackColor = Color.White;
        control.ForeColor = Color.Black;
    }

    private static void ApplyButtonTheme(Button button)
    {
        button.BackColor = Color.White;
        button.ForeColor = Color.Black;
    }

    #endregion

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processor.ResultUpdated -= OnResultUpdated;
        }

        base.Dispose(disposing);
    }

    #endregion
}