using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public enum ColorFormat
{
    RGB,
    HSV,
    LAB
}

public class ROIColorSamplerProcessor : INodeProcessor
{
    public string Name => "ROI Color Sampler";

    private Mat? _originImage;
    private Mat? _roiImage;
    private RoiData? _roiData;
    private ColorFormat _colorFormat = ColorFormat.RGB;

    public Mat? OriginImage => _originImage;
    public Mat? RoiImage => _roiImage;
    public RoiData? RoiData
    {
        get => _roiData;
        set
        {
            _roiData = value;

            RoiDataUpdated?.Invoke(_roiData);
            UpdateColorFromCurrentImage();
        }
    }

    public ColorFormat ColorFormat
    {
        get => _colorFormat;
        set
        {
            if (_colorFormat == value)
                return;

            _colorFormat = value;
            UpdateColorFromCurrentImage();
        }
    }

    /// <summary>
    /// ROI 영역의 평균 색상값이 계산되었을 때 외부로 전달한다.
    /// </summary>
    public event Action<Scalar?, Mat?>? ColorUpdated;

    public event Action<RoiData?>? RoiDataUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMat(inputs);

        if (source == null || source.Empty())
        {
            ClearOriginImage();
            ClearRoiImage();

            return new Dictionary<string, object?>
            {
                ["Image"] = null
            };
        }

        UpdateOriginImage(source);
        UpdateColorFromCurrentImage();

        return new Dictionary<string, object?>
        {
            ["Image"] = source
        };
    }

    public static Scalar GetColorAverage(
        Mat source,
        RoiData roiData,
        ColorFormat colorFormat)
    {
        if (source == null || source.Empty())
            throw new ArgumentException("source 이미지가 비어 있습니다.", nameof(source));

        Rect roiRect = CreateSafeRect(source, roiData);

        using Mat roiMat = new(source, roiRect);
        using Mat converted = ConvertColor(roiMat, colorFormat);

        return Cv2.Mean(converted);
    }

    private static Mat ConvertColor(Mat source, ColorFormat colorFormat)
    {
        return colorFormat switch
        {
            ColorFormat.RGB => ConvertToRgb(source),
            ColorFormat.HSV => ConvertToHsv(source),
            ColorFormat.LAB => ConvertToLab(source),

            _ => throw new ArgumentOutOfRangeException(nameof(colorFormat), colorFormat, null)
        };
    }

    private static Mat ConvertToRgb(Mat source)
    {
        Mat output = new();

        switch (source.Channels())
        {
            case 1:
                Cv2.CvtColor(source, output, ColorConversionCodes.GRAY2RGB);
                break;

            case 3:
                // OpenCV Mat 기본 순서는 BGR이므로 RGB로 변환한다.
                Cv2.CvtColor(source, output, ColorConversionCodes.BGR2RGB);
                break;

            case 4:
                Cv2.CvtColor(source, output, ColorConversionCodes.BGRA2RGB);
                break;

            default:
                output.Dispose();
                throw new NotSupportedException($"지원하지 않는 채널 수입니다. Channels={source.Channels()}");
        }

        return output;
    }

    private static Mat ConvertToHsv(Mat source)
    {
        Mat output = new();

        switch (source.Channels())
        {
            case 1:
                using (Mat bgr = new())
                {
                    Cv2.CvtColor(source, bgr, ColorConversionCodes.GRAY2BGR);
                    Cv2.CvtColor(bgr, output, ColorConversionCodes.BGR2HSV);
                }
                break;

            case 3:
                Cv2.CvtColor(source, output, ColorConversionCodes.BGR2HSV);
                break;

            case 4:
                using (Mat bgr = new())
                {
                    Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
                    Cv2.CvtColor(bgr, output, ColorConversionCodes.BGR2HSV);
                }
                break;

            default:
                output.Dispose();
                throw new NotSupportedException($"지원하지 않는 채널 수입니다. Channels={source.Channels()}");
        }

        return output;
    }

    private static Mat ConvertToLab(Mat source)
    {
        Mat output = new();

        switch (source.Channels())
        {
            case 1:
                using (Mat bgr = new())
                {
                    Cv2.CvtColor(source, bgr, ColorConversionCodes.GRAY2BGR);
                    Cv2.CvtColor(bgr, output, ColorConversionCodes.BGR2Lab);
                }
                break;

            case 3:
                Cv2.CvtColor(source, output, ColorConversionCodes.BGR2Lab);
                break;

            case 4:
                using (Mat bgr = new())
                {
                    Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
                    Cv2.CvtColor(bgr, output, ColorConversionCodes.BGR2Lab);
                }
                break;

            default:
                output.Dispose();
                throw new NotSupportedException($"지원하지 않는 채널 수입니다. Channels={source.Channels()}");
        }

        return output;
    }


    private static Rect CreateSafeRect(Mat source, RoiData roiData)
    {
        int x = Math.Max(0, roiData.OffsetX);
        int y = Math.Max(0, roiData.OffsetY);

        int width = roiData.X;
        int height = roiData.Y;

        if (width <= 0 || height <= 0)
            throw new ArgumentException("ROI의 Width 또는 Height가 0 이하입니다.", nameof(roiData));

        if (x >= source.Width || y >= source.Height)
            throw new ArgumentException("ROI 시작 위치가 이미지 범위를 벗어났습니다.", nameof(roiData));

        width = Math.Min(width, source.Width - x);
        height = Math.Min(height, source.Height - y);

        if (width <= 0 || height <= 0)
            throw new ArgumentException("ROI 영역이 이미지 범위를 벗어났습니다.", nameof(roiData));

        return new Rect(x, y, width, height);
    }

    private static Mat? FindMat(Dictionary<string, object?> inputs)
    {
        return inputs.TryGetValue("Image", out object? value) && value is Mat mat && !mat.Empty()
            ? mat
            : null;
    }

    private void UpdateOriginImage(Mat source)
    {
        _originImage?.Dispose();
        _originImage = source.Clone();
    }

    private void UpdateColorFromCurrentImage()
    {
        if (_originImage == null || _originImage.Empty() || _roiData == null)
        {
            ClearRoiImage();
            return;
        }

        try
        {
            Scalar averageColor = GetColorAverage(_originImage, _roiData, ColorFormat);

            UpdateRoiImage(_originImage, _roiData);

            ColorUpdated?.Invoke(averageColor, _roiImage);
        }
        catch (ArgumentException)
        {
            ClearRoiImage();
        }
    }

    private void UpdateRoiImage(Mat source, RoiData roiData)
    {
        Rect roiRect = CreateSafeRect(source, roiData);

        _roiImage?.Dispose();

        using Mat roi = new(source, roiRect);
        _roiImage = roi.Clone();
    }

    private void ClearOriginImage()
    {
        _originImage?.Dispose();
        _originImage = null;
    }

    private void ClearRoiImage()
    {
        _roiImage?.Dispose();
        _roiImage = null;

        ColorUpdated?.Invoke(null, null);
    }

    public void Dispose()
    {
        ClearOriginImage();

        _roiImage?.Dispose();
        _roiImage = null;
    }
}

public class RoiData
{
    public int OffsetX { get; private set; }

    public int OffsetY { get; private set; }
    public int X { get; private set; }

    public int Y { get; private set; }

    public RoiData(int offsetX, int offsetY, int x, int y)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        X = x;
        Y = y;
    }
}
