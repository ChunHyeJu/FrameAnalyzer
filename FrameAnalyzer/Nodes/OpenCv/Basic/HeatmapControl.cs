using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class HeatmapControl : OpenCvPreviewNodeControl
{
    private readonly HeatmapProcessor _processor;

    public HeatmapControl(HeatmapProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 220)
    {
        _processor = processor;
        _processor.HeatmapUpdated += OnHeatmapUpdated;
    }

    private void OnHeatmapUpdated(Mat? heatmap)
    {
        SetPreviewImage(
            heatmap,
            heatmap == null || heatmap.Empty()
                ? "No Image"
                : $"{heatmap.Width} x {heatmap.Height} / {heatmap.Channels()}ch");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processor.HeatmapUpdated -= OnHeatmapUpdated;
        }

        base.Dispose(disposing);
    }
}