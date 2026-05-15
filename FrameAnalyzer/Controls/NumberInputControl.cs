using FrameAnalyzer.NodeEditor;

namespace FrameAnalyzer.Controls;

public class NumberInputControl : UserControl, INodeThemeAware
{
    private readonly Label _label = new();
    private readonly NumericUpDown _numeric = new();

    private readonly NumberInputProcessor _processor;

    public NumberInputControl(NumberInputProcessor processor)
    {
        _processor = processor;

        Height = 70;
        Width = 160;

        _label.Text = "Number";
        _label.AutoSize = true;
        _label.Location = new Point(8, 8);

        _numeric.Minimum = -100000;
        _numeric.Maximum = 100000;
        _numeric.DecimalPlaces = 2;
        _numeric.Value = 10;
        _numeric.Location = new Point(8, 32);
        _numeric.Width = 140;

        _processor.Value = (double)_numeric.Value;

        _numeric.ValueChanged += (_, _) =>
        {
            _processor.Value = (double)_numeric.Value;
        };

        Controls.Add(_label);
        Controls.Add(_numeric);
    }

    public void ApplyTheme(NodeCanvasTheme theme)
    {
        BackColor = theme.NodeBodyColor;
        ForeColor = theme.TextColor;

        _label.ForeColor = theme.TextColor;
        _label.BackColor = theme.NodeBodyColor;

        _numeric.BackColor = Color.White;
        _numeric.ForeColor = Color.Black;
    }
}