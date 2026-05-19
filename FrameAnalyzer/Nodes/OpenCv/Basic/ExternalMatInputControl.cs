using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class ExternalMatInputControl : OpenCvPreviewNodeControl
{
    private readonly ExternalMatInputProcessor _processor;

    public ExternalMatInputControl(ExternalMatInputProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 220)
    {
        _processor = processor;
        _processor.ImageUpdated += OnImageUpdated;
    }

    private void OnImageUpdated(Mat? image)
    {
        SetPreviewImage(
            image,
            image == null || image.Empty()
                ? "No Image"
                : $"{image.Width} x {image.Height} / {image.Channels()}ch");
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