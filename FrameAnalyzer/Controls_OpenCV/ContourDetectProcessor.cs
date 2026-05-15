using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace FrameAnalyzer.Controls_OpenCV;

public class ContourDetectProcessor : INodeProcessor, IDisposable
{
    public string Name => "Contour Detect";

    private Mat? _resultImage;

    public Mat? ResultImage => _resultImage;

    public List<ContourInfo> Contours { get; private set; } = new();

    public double MinArea { get; set; } = 10;

    public double MaxArea { get; set; } = 1000000;

    public event Action<Mat?, IReadOnlyList<ContourInfo>>? ResultUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            ClearResult();

            return new Dictionary<string, object?>
            {
                ["Contours"] = Contours,
                ["ResultImage"] = null
            };
        }

        Mat result = DetectContours(source, out List<ContourInfo> contours);

        _resultImage?.Dispose();
        _resultImage = result;
        Contours = contours;

        ResultUpdated?.Invoke(_resultImage, Contours);

        return new Dictionary<string, object?>
        {
            ["Contours"] = Contours,
            ["ResultImage"] = _resultImage
        };
    }

    private Mat DetectContours(Mat source, out List<ContourInfo> contours)
    {
        using Mat binary = ConvertToBinary8U(source);

        Mat result = CreateDisplayImage(binary);

        Cv2.FindContours(
            binary,
            out OpenCvSharp.Point[][] rawContours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        contours = new List<ContourInfo>();

        int index = 1;

        foreach (Point[] contour in rawContours)
        {
            double area = Cv2.ContourArea(contour);

            if (area < MinArea)
                continue;

            if (area > MaxArea)
                continue;

            double perimeter = Cv2.ArcLength(contour, true);
            Rect rect = Cv2.BoundingRect(contour);
            Point2d center = GetContourCenter(contour, rect);

            double circularity = 0;

            if (perimeter > 0)
            {
                circularity = 4.0 * Math.PI * area / (perimeter * perimeter);
            }

            ContourInfo info = new()
            {
                Index = index,
                Points = contour,
                Area = area,
                Perimeter = perimeter,
                BoundingRect = rect,
                Center = center,
                Circularity = circularity
            };

            contours.Add(info);

            DrawContour(result, info);

            index++;
        }

        return result;
    }

    private static Mat ConvertToBinary8U(Mat source)
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

        if (gray.Depth() != MatType.CV_8U)
        {
            Mat gray8 = new();

            Cv2.Normalize(
                gray,
                gray8,
                0,
                255,
                NormTypes.MinMax,
                MatType.CV_8U);

            gray.Dispose();
            gray = gray8;
        }

        Mat binary = new();

        // 이진 이미지가 아니어도 동작하게 한번 더 이진화
        Cv2.Threshold(
            gray,
            binary,
            0,
            255,
            ThresholdTypes.Binary | ThresholdTypes.Otsu);

        gray.Dispose();

        return binary;
    }

    private static Mat CreateDisplayImage(Mat binary)
    {
        Mat result = new();

        Cv2.CvtColor(
            binary,
            result,
            ColorConversionCodes.GRAY2BGR);

        return result;
    }

    private static Point2d GetContourCenter(Point[] contour, Rect rect)
    {
        Moments moments = Cv2.Moments(contour);

        if (Math.Abs(moments.M00) < double.Epsilon)
        {
            return new Point2d(
                rect.X + rect.Width / 2.0,
                rect.Y + rect.Height / 2.0);
        }

        return new Point2d(
            moments.M10 / moments.M00,
            moments.M01 / moments.M00);
    }

    private static void DrawContour(Mat result, ContourInfo contour)
    {
        Point center = new(
            (int)Math.Round(contour.Center.X),
            (int)Math.Round(contour.Center.Y));

        // 윤곽선
        Cv2.Polylines(
            result,
            new[] { contour.Points },
            true,
            new Scalar(0, 255, 0),
            2);

        // 외접 사각형
        Cv2.Rectangle(
            result,
            contour.BoundingRect,
            new Scalar(255, 0, 0),
            1);

        // 중심점
        Cv2.DrawMarker(
            result,
            center,
            new Scalar(0, 0, 255),
            MarkerTypes.Cross,
            12,
            2);

        // 번호
        Cv2.PutText(
            result,
            contour.Index.ToString(),
            new Point(
                contour.BoundingRect.X,
                Math.Max(0, contour.BoundingRect.Y - 4)),
            HersheyFonts.HersheySimplex,
            0.5,
            new Scalar(0, 255, 255),
            1);
    }

    private static Mat? FindMatInput(Dictionary<string, object?> inputs)
    {
        if (inputs.TryGetValue("Binary", out object? binaryValue) &&
            binaryValue is Mat binary &&
            !binary.Empty())
        {
            return binary;
        }

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

    private void ClearResult()
    {
        _resultImage?.Dispose();
        _resultImage = null;
        Contours.Clear();

        ResultUpdated?.Invoke(null, Contours);
    }

    public void Dispose()
    {
        _resultImage?.Dispose();
        _resultImage = null;
    }
}

public class ContourInfo
{
    public int Index { get; set; }

    public Point[] Points { get; set; } = Array.Empty<Point>();

    public double Area { get; set; }

    public double Perimeter { get; set; }

    public Rect BoundingRect { get; set; }

    public Point2d Center { get; set; }

    public double Circularity { get; set; }

    public double Width => BoundingRect.Width;

    public double Height => BoundingRect.Height;
}