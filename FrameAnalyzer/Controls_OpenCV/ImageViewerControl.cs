using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ImageViewerControl : OpenCvPreviewNodeControl
{
    private readonly ImageViewerProcessor _processor;

    private Mat? _lastImage;

    public ImageViewerControl(ImageViewerProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 220)
    {
        _processor = processor;

        _processor.ImageUpdated += OnImageUpdated;
    }

    private void OnImageUpdated(Mat? image)
    {
        _lastImage = image;

        SetPreviewImage(image, CreateStatusText());
    }

    private string CreateStatusText()
    {
        if (_lastImage == null || _lastImage.Empty())
            return "No Image";

        return
            $"{_lastImage.Width} x {_lastImage.Height} / " +
            $"{_lastImage.Channels()}ch";
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
