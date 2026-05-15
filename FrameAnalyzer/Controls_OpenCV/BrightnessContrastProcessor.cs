using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class BrightnessContrastProcessor : INodeProcessor, IDisposable
{
    public string Name => "Brightness / Contrast";

    private Mat? _resultImage;

    public Mat? ResultImage => _resultImage;

    // Alpha: 대비
    // 1.0 = 원본 대비
    // 1.5 = 대비 증가
    // 0.5 = 대비 감소
    public double Alpha { get; set; } = 1.0;

    // Beta: 밝기
    // 0 = 원본 밝기
    // 양수 = 밝게
    // 음수 = 어둡게
    public double Beta { get; set; } = 0.0;

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

        Mat result = ApplyBrightnessContrast(source);

        _resultImage?.Dispose();
        _resultImage = result;

        ImageUpdated?.Invoke(_resultImage);

        return new Dictionary<string, object?>
        {
            ["Image"] = _resultImage
        };
    }

    private Mat ApplyBrightnessContrast(Mat source)
    {
        Mat result = new();

        // result = source * Alpha + Beta
        source.ConvertTo(
            result,
            -1,
            Alpha,
            Beta);

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