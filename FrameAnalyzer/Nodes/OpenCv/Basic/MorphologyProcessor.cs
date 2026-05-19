using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public enum MorphologyOperation
{
    Erode,
    Dilate,
    Open,
    Close
}

public class MorphologyProcessor : INodeProcessor, IDisposable
{
    public string Name => "Morphology";

    private Mat? _binaryImage;

    public Mat? BinaryImage => _binaryImage;

    public MorphologyOperation Operation { get; set; } = MorphologyOperation.Open;

    public int KernelSize { get; set; } = 3;

    public int Iterations { get; set; } = 1;

    public event Action<Mat?>? ImageUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            ClearImage();

            return new Dictionary<string, object?>
            {
                ["Binary"] = null
            };
        }

        Mat result = ApplyMorphology(source);

        _binaryImage?.Dispose();
        _binaryImage = result;

        ImageUpdated?.Invoke(_binaryImage);

        return new Dictionary<string, object?>
        {
            ["Binary"] = _binaryImage
        };
    }

    private Mat ApplyMorphology(Mat source)
    {
        int kernelSize = NormalizeKernelSize(KernelSize);

        using Mat gray = ConvertToGray8U(source);

        using Mat kernel = Cv2.GetStructuringElement(
            MorphShapes.Rect,
            new OpenCvSharp.Size(kernelSize, kernelSize));

        Mat result = new();

        switch (Operation)
        {
            case MorphologyOperation.Erode:
                // 흰색 영역 축소, 작은 흰 노이즈 제거
                Cv2.Erode(gray, result, kernel, iterations: Iterations);
                break;

            case MorphologyOperation.Dilate:
                // 흰색 영역 확장, 끊어진 영역 연결
                Cv2.Dilate(gray, result, kernel, iterations: Iterations);
                break;

            case MorphologyOperation.Open:
                // Erode 후 Dilate, 작은 흰 노이즈 제거
                Cv2.MorphologyEx(
                    gray,
                    result,
                    MorphTypes.Open,
                    kernel,
                    iterations: Iterations);
                break;

            case MorphologyOperation.Close:
                // Dilate 후 Erode, 작은 검은 구멍 메우기
                Cv2.MorphologyEx(
                    gray,
                    result,
                    MorphTypes.Close,
                    kernel,
                    iterations: Iterations);
                break;

            default:
                gray.CopyTo(result);
                break;
        }

        return result;
    }

    private static int NormalizeKernelSize(int value)
    {
        if (value < 1)
            value = 1;

        if (value % 2 == 0)
            value += 1;

        return value;
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
        // Threshold 노드 출력명
        if (inputs.TryGetValue("Binary", out object? binaryValue) &&
            binaryValue is Mat binary &&
            !binary.Empty())
        {
            return binary;
        }

        // 다른 이미지 노드와도 연결 가능하게 처리
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
        _binaryImage?.Dispose();
        _binaryImage = null;

        ImageUpdated?.Invoke(null);
    }

    public void Dispose()
    {
        _binaryImage?.Dispose();
        _binaryImage = null;
    }
}