using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public enum ThresholdMode
{
    Binary,
    BinaryInv,
    Trunc,
    ToZero,
    ToZeroInv,
    Otsu,
    Triangle,
}

public class ThresholdProcessor : INodeProcessor, IDisposable
{
    public string Name => "Threshold";

    private Mat? _binaryImage;

    public Mat? BinaryImage => _binaryImage;

    public double Threshold { get; set; } = 128;

    public double MaxValue { get; set; } = 255;

    public ThresholdMode Mode { get; set; } = ThresholdMode.Binary;

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

        Mat result = ApplyThreshold(source);

        _binaryImage?.Dispose();
        _binaryImage = result;

        ImageUpdated?.Invoke(_binaryImage);

        return new Dictionary<string, object?>
        {
            ["Binary"] = _binaryImage
        };
    }

    private Mat ApplyThreshold(Mat source)
    {
        using Mat gray = ConvertToGray8U(source);

        Mat binary = new();

        ThresholdTypes thresholdType = ToOpenCvThresholdType(Mode);

        Cv2.Threshold(
            gray,
            binary,
            Threshold,
            MaxValue,
            thresholdType);

        return binary;
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

    private static ThresholdTypes ToOpenCvThresholdType(ThresholdMode mode)
    {
        return mode switch
        {
            // 기준값보다 밝으면 MaxValue, 아니면 0
            ThresholdMode.Binary => ThresholdTypes.Binary,

            // Binary 반대: 기준값보다 밝으면 0, 아니면 MaxValue
            ThresholdMode.BinaryInv => ThresholdTypes.BinaryInv,

            // 기준값보다 큰 픽셀을 기준값으로 제한
            ThresholdMode.Trunc => ThresholdTypes.Trunc,

            // 기준값보다 어두운 픽셀을 0으로 제거
            ThresholdMode.ToZero => ThresholdTypes.Tozero,

            // 기준값보다 밝은 픽셀을 0으로 제거
            ThresholdMode.ToZeroInv => ThresholdTypes.TozeroInv,

            // 히스토그램 기반 자동 임계값 계산
            ThresholdMode.Otsu => ThresholdTypes.Binary | ThresholdTypes.Otsu,

            // 히스토그램 형태 기반 자동 임계값 계산
            ThresholdMode.Triangle => ThresholdTypes.Binary | ThresholdTypes.Triangle,

            // 기본값
            _ => ThresholdTypes.Binary
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