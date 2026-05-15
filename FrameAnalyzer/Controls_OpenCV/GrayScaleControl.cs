using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class GrayScaleControl : OpenCvPreviewNodeControl
{
    private readonly GrayScaleProcessor _processor;

    public GrayScaleControl(GrayScaleProcessor processor)
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