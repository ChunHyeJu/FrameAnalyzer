using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class BlurControl : OpenCvPreviewNodeControl
{
    private readonly Label _typeLabel = new();
    private readonly ComboBox _typeComboBox = new();

    private readonly Label _kernelLabel = new();
    private readonly NumericUpDown _kernelNumeric = new();

    private readonly BlurProcessor _processor;

    public BlurControl(BlurProcessor processor)
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

        _typeLabel.Text = "Type";
        _typeLabel.Dock = DockStyle.Fill;
        _typeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _typeLabel.Margin = Padding.Empty;

        _typeComboBox.Dock = DockStyle.Fill;
        _typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _typeComboBox.Margin = new Padding(0, 0, 0, 4);
        _typeComboBox.Items.AddRange(Enum.GetNames(typeof(BlurType)));
        _typeComboBox.SelectedItem = _processor.BlurType.ToString();

        _typeComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_typeComboBox.SelectedItem == null)
                return;

            _processor.BlurType = Enum.Parse<BlurType>(
                _typeComboBox.SelectedItem.ToString()!);
        };

        _kernelLabel.Text = "Kernel";
        _kernelLabel.Dock = DockStyle.Fill;
        _kernelLabel.TextAlign = ContentAlignment.MiddleLeft;
        _kernelLabel.Margin = Padding.Empty;

        _kernelNumeric.Dock = DockStyle.Fill;
        _kernelNumeric.Minimum = 1;
        _kernelNumeric.Maximum = 99;
        _kernelNumeric.Increment = 2;
        _kernelNumeric.Value = _processor.KernelSize;
        _kernelNumeric.Margin = Padding.Empty;

        _kernelNumeric.ValueChanged += (_, _) =>
        {
            int kernel = (int)_kernelNumeric.Value;

            if (kernel % 2 == 0)
            {
                kernel = kernel < _kernelNumeric.Maximum
                    ? kernel + 1
                    : kernel - 1;

                _kernelNumeric.Value = kernel;
            }

            _processor.KernelSize = kernel;
        };

        settingsLayout.Controls.Add(_typeLabel, 0, 0);
        settingsLayout.Controls.Add(_typeComboBox, 1, 0);
        settingsLayout.Controls.Add(_kernelLabel, 0, 1);
        settingsLayout.Controls.Add(_kernelNumeric, 1, 1);

        SettingsPanel.Controls.Add(settingsLayout);

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void OnImageUpdated(Mat? image)
    {
        SetPreviewImage(
            image,
            image == null || image.Empty()
                ? "No Image"
                : $"{image.Width} x {image.Height} / {image.Channels()}ch / {_processor.BlurType} / K={_processor.KernelSize}");
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        _typeLabel.ForeColor = theme.TextColor;
        _typeLabel.BackColor = theme.NodeBodyColor;

        _kernelLabel.ForeColor = theme.TextColor;
        _kernelLabel.BackColor = theme.NodeBodyColor;

        _typeComboBox.BackColor = Color.White;
        _typeComboBox.ForeColor = Color.Black;

        _kernelNumeric.BackColor = Color.White;
        _kernelNumeric.ForeColor = Color.Black;
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