using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class MorphologyControl : OpenCvPreviewNodeControl
{
    private readonly Label _operationLabel = new();
    private readonly ComboBox _operationComboBox = new();

    private readonly Label _kernelLabel = new();
    private readonly NumericUpDown _kernelNumeric = new();

    private readonly Label _iterationsLabel = new();
    private readonly NumericUpDown _iterationsNumeric = new();

    private readonly MorphologyProcessor _processor;

    public MorphologyControl(MorphologyProcessor processor)
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

        InitOperationControls();
        InitKernelControls();
        InitIterationsControls();

        settingsLayout.Controls.Add(_operationLabel, 0, 0);
        settingsLayout.Controls.Add(_operationComboBox, 1, 0);

        settingsLayout.Controls.Add(_kernelLabel, 0, 1);
        settingsLayout.Controls.Add(_kernelNumeric, 1, 1);

        settingsLayout.Controls.Add(_iterationsLabel, 0, 2);
        settingsLayout.Controls.Add(_iterationsNumeric, 1, 2);

        SettingsPanel.Controls.Add(settingsLayout);

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void InitOperationControls()
    {
        _operationLabel.Text = "Operation";
        _operationLabel.Dock = DockStyle.Fill;
        _operationLabel.TextAlign = ContentAlignment.MiddleLeft;
        _operationLabel.Margin = Padding.Empty;

        _operationComboBox.Dock = DockStyle.Fill;
        _operationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _operationComboBox.Margin = new Padding(0, 0, 0, 4);
        _operationComboBox.Items.AddRange(Enum.GetNames(typeof(MorphologyOperation)));
        _operationComboBox.SelectedItem = _processor.Operation.ToString();

        _operationComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_operationComboBox.SelectedItem == null)
                return;

            _processor.Operation = Enum.Parse<MorphologyOperation>(
                _operationComboBox.SelectedItem.ToString()!);
        };
    }

    private void InitKernelControls()
    {
        _kernelLabel.Text = "Kernel";
        _kernelLabel.Dock = DockStyle.Fill;
        _kernelLabel.TextAlign = ContentAlignment.MiddleLeft;
        _kernelLabel.Margin = Padding.Empty;

        _kernelNumeric.Dock = DockStyle.Fill;
        _kernelNumeric.Minimum = 1;
        _kernelNumeric.Maximum = 99;
        _kernelNumeric.Increment = 2;
        _kernelNumeric.Value = _processor.KernelSize;
        _kernelNumeric.Margin = new Padding(0, 0, 0, 4);

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
    }

    private void InitIterationsControls()
    {
        _iterationsLabel.Text = "Iterations";
        _iterationsLabel.Dock = DockStyle.Fill;
        _iterationsLabel.TextAlign = ContentAlignment.MiddleLeft;
        _iterationsLabel.Margin = Padding.Empty;

        _iterationsNumeric.Dock = DockStyle.Fill;
        _iterationsNumeric.Minimum = 1;
        _iterationsNumeric.Maximum = 20;
        _iterationsNumeric.Value = _processor.Iterations;
        _iterationsNumeric.Margin = Padding.Empty;

        _iterationsNumeric.ValueChanged += (_, _) =>
        {
            _processor.Iterations = (int)_iterationsNumeric.Value;
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
            $"{_processor.Operation} / K={_processor.KernelSize} / I={_processor.Iterations}";
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        ApplyLabelTheme(_operationLabel, theme);
        ApplyLabelTheme(_kernelLabel, theme);
        ApplyLabelTheme(_iterationsLabel, theme);

        _operationComboBox.BackColor = Color.White;
        _operationComboBox.ForeColor = Color.Black;

        _kernelNumeric.BackColor = Color.White;
        _kernelNumeric.ForeColor = Color.Black;

        _iterationsNumeric.BackColor = Color.White;
        _iterationsNumeric.ForeColor = Color.Black;
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