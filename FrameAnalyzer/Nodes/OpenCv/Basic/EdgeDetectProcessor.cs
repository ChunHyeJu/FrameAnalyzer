using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class EdgeDetectProcessor : INodeProcessor, IDisposable
{
    public string Name => "Edge Detect";

    private Mat? _edgeImage;

    public Mat? EdgeImage => _edgeImage;

    public double Threshold1 { get; set; } = 100;

    public double Threshold2 { get; set; } = 200;

    public event Action<Mat?>? ImageUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            ClearImage();

            return new Dictionary<string, object?>
            {
                ["Edge"] = null
            };
        }

        Mat result = ApplyCanny(source);

        _edgeImage?.Dispose();
        _edgeImage = result;

        ImageUpdated?.Invoke(_edgeImage);

        return new Dictionary<string, object?>
        {
            ["Edge"] = _edgeImage
        };
    }

    private Mat ApplyCanny(Mat source)
    {
        using Mat gray = ConvertToGray8U(source);

        Mat edge = new();

        Cv2.Canny(
            gray,
            edge,
            Threshold1,
            Threshold2);

        return edge;
    }

    private static Mat ConvertToGray8U(Mat source)
    {
        Mat gray = new();

        if (source.Channels() == 1)
        {
            source.CopyTo(gray);
        }
        else if (source.Channels() == 3)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            throw new InvalidOperationException(
                $"지원하지 않는 이미지 채널 수입니다. Channels: {source.Channels()}");
        }

        if (gray.Depth() == MatType.CV_8U)
            return gray;

        Mat gray8 = new();

        Cv2.Normalize(
            gray,
            gray8,
            0,
            255,
            NormTypes.MinMax,
            MatType.CV_8U);

        gray.Dispose();

        return gray8;
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
        _edgeImage?.Dispose();
        _edgeImage = null;

        ImageUpdated?.Invoke(null);
    }

    public void Dispose()
    {
        _edgeImage?.Dispose();
        _edgeImage = null;
    }
}