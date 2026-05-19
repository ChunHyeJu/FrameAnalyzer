using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class GrayScaleProcessor : INodeProcessor, IDisposable
{
    public string Name => "GrayScale";

    private Mat? _grayImage;

    public Mat? GrayImage => _grayImage;

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

        Mat result = ConvertToGray(source);

        _grayImage?.Dispose();
        _grayImage = result;

        ImageUpdated?.Invoke(_grayImage);

        return new Dictionary<string, object?>
        {
            // 다음 노드와 연결 편하게 하려고 Output 이름을 Image로 유지
            ["Image"] = _grayImage
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

        foreach (object? value in inputs.Values)
        {
            if (value is Mat mat && !mat.Empty())
                return mat;
        }

        return null;
    }

    private static Mat ConvertToGray(Mat source)
    {
        Mat gray = new();

        if (source.Channels() == 1)
        {
            source.CopyTo(gray);
            return gray;
        }

        if (source.Channels() == 3)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
            return gray;
        }

        gray.Dispose();

        throw new InvalidOperationException(
            $"지원하지 않는 이미지 채널 수입니다. Channels: {source.Channels()}");
    }

    private void ClearImage()
    {
        _grayImage?.Dispose();
        _grayImage = null;

        ImageUpdated?.Invoke(null);
    }

    public void Dispose()
    {
        _grayImage?.Dispose();
        _grayImage = null;
    }
}