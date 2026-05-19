using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class ResizeProcessor : INodeProcessor, IDisposable
{
    public string Name => "Resize";

    private Mat? _resultImage;

    public Mat? ResultImage => _resultImage;

    public bool UseScale { get; set; } = true;

    public double Scale { get; set; } = 0.5;

    public int TargetWidth { get; set; } = 640;

    public int TargetHeight { get; set; } = 480;

    public InterpolationFlags Interpolation { get; set; } = InterpolationFlags.Linear;

    public event Action<Mat?>? ImageUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            ClearImage();

            return new Dictionary<string, object?>
            {
                ["Image"] = null
            };
        }

        Mat result = ApplyResize(source);

        _resultImage?.Dispose();
        _resultImage = result;

        ImageUpdated?.Invoke(_resultImage);

        return new Dictionary<string, object?>
        {
            ["Image"] = _resultImage
        };
    }

    private Mat ApplyResize(Mat source)
    {
        Mat result = new();

        if (UseScale)
        {
            double scale = Math.Max(0.01, Scale);

            Cv2.Resize(
                source,
                result,
                new OpenCvSharp.Size(),
                scale,
                scale,
                Interpolation);
        }
        else
        {
            int width = Math.Max(1, TargetWidth);
            int height = Math.Max(1, TargetHeight);

            Cv2.Resize(
                source,
                result,
                new OpenCvSharp.Size(width, height),
                0,
                0,
                Interpolation);
        }

        return result;
    }

    private static Mat? FindMatInput(Dictionary<string, object?> inputs)
    {
        if (inputs.TryGetValue("Image", out object? imageValue) &&
            imageValue is Mat image &&
            !image.Empty())
        {
            return image;
        }

        foreach (object? value in inputs.Values)
        {
            if (value is Mat mat && !mat.Empty())
                return mat;
        }

        return null;
    }

    private void ClearImage()
    {
        _resultImage?.Dispose();
        _resultImage = null;

        ImageUpdated?.Invoke(null);
    }

    public void Dispose()
    {
        _resultImage?.Dispose();
        _resultImage = null;
    }
}