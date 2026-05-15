using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ImageViewerProcessor : INodeProcessor
{
    public string Name => "Image Viewer";

    public Mat? CurrentImage { get; private set; }

    public event Action<Mat?>? ImageUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? image = FindMatInput(inputs);

        CurrentImage = image;
        ImageUpdated?.Invoke(image);

        return new Dictionary<string, object?>
        {
            ["Image"] = image
        };
    }

    private static Mat? FindMatInput(Dictionary<string, object?> inputs)
    {
        if (inputs.TryGetValue("Image", out object? imageValue) &&
            imageValue is Mat image &&
            !image.Empty())
        {
            return image;
        }

        if (inputs.TryGetValue("Heatmap", out object? heatmapValue) &&
            heatmapValue is Mat heatmap &&
            !heatmap.Empty())
        {
            return heatmap;
        }

        foreach (object? value in inputs.Values)
        {
            if (value is Mat mat && !mat.Empty())
                return mat;
        }

        return null;
    }
}
