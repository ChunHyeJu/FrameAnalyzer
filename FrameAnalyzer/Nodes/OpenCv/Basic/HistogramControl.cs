using FrameAnalyzer.NodeEditor;
using System.Drawing.Drawing2D;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class HistogramControl : OpenCvPreviewNodeControl
{
    private readonly HistogramProcessor _processor;

    private HistogramData? _lastHistogram;

    private Color _graphBackColor = Color.FromArgb(25, 25, 25);
    private Color _graphAxisColor = Color.FromArgb(130, 130, 130);
    private Color _graphBarColor = Color.DeepSkyBlue;
    private Color _graphTextColor = Color.White;

    public HistogramControl(HistogramProcessor processor)
        : base(defaultWidth: 260, defaultHeight: 220)
    {
        _processor = processor;

        PreviewBox.SizeMode = PictureBoxSizeMode.StretchImage;

        _processor.HistogramUpdated += OnHistogramUpdated;
    }

    private void OnHistogramUpdated(HistogramData? histogram)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnHistogramUpdated(histogram)));
            return;
        }

        _lastHistogram = histogram;

        UpdateGraph();
    }

    private void UpdateGraph()
    {
        Image? oldImage = PreviewBox.Image;
        PreviewBox.Image = null;
        oldImage?.Dispose();

        if (_lastHistogram == null || _lastHistogram.TotalPixels <= 0)
        {
            StatusLabel.Text = "No Image";
            return;
        }

        int width = Math.Max(1, PreviewBox.Width);
        int height = Math.Max(1, PreviewBox.Height);

        Bitmap graphBitmap = CreateHistogramBitmap(
            _lastHistogram,
            width,
            height);

        PreviewBox.Image = graphBitmap;

        StatusLabel.Text =
            $"Mean={_lastHistogram.MeanIntensity:F1} / " +
            $"Peak={_lastHistogram.PeakBin} / " +
            $"Pixels={_lastHistogram.TotalPixels:N0}";
    }

    private Bitmap CreateHistogramBitmap(
        HistogramData histogram,
        int width,
        int height)
    {
        Bitmap bitmap = new(width, height);

        using Graphics g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(_graphBackColor);

        Rectangle plotArea = new(
            x: 34,
            y: 10,
            width: Math.Max(1, width - 44),
            height: Math.Max(1, height - 34));

        using Pen axisPen = new(_graphAxisColor);
        using Pen barPen = new(_graphBarColor);

        // X축, Y축
        g.DrawLine(
            axisPen,
            plotArea.Left,
            plotArea.Top,
            plotArea.Left,
            plotArea.Bottom);

        g.DrawLine(
            axisPen,
            plotArea.Left,
            plotArea.Bottom,
            plotArea.Right,
            plotArea.Bottom);

        double maxCount = histogram.MaxCount;

        if (maxCount <= 0)
            return bitmap;

        for (int i = 0; i < histogram.Counts.Length; i++)
        {
            double count = histogram.Counts[i];

            float x =
                plotArea.Left +
                (float)i / (histogram.Counts.Length - 1) * plotArea.Width;

            float barHeight =
                (float)(count / maxCount * plotArea.Height);

            float y1 = plotArea.Bottom;
            float y2 = plotArea.Bottom - barHeight;

            g.DrawLine(barPen, x, y1, x, y2);
        }

        using Brush textBrush = new SolidBrush(_graphTextColor);

        g.DrawString(
            "0",
            Font,
            textBrush,
            plotArea.Left - 4,
            plotArea.Bottom + 2);

        g.DrawString(
            "255",
            Font,
            textBrush,
            plotArea.Right - 24,
            plotArea.Bottom + 2);

        g.DrawString(
            $"Peak {histogram.PeakBin}",
            Font,
            textBrush,
            plotArea.Left + 4,
            plotArea.Top + 2);

        return bitmap;
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        _graphBackColor = Color.FromArgb(25, 25, 25);
        _graphAxisColor = theme.NodeBorderColor;
        _graphBarColor = theme.OutputPortColor;
        _graphTextColor = theme.TextColor;

        UpdateGraph();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_lastHistogram != null)
            UpdateGraph();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processor.HistogramUpdated -= OnHistogramUpdated;
        }

        base.Dispose(disposing);
    }
}