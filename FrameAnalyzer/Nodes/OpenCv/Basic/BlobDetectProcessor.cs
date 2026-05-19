using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class BlobDetectProcessor : INodeProcessor, IDisposable
{
    public string Name => "Blob Detect";

    private Mat? _resultImage;

    public Mat? ResultImage => _resultImage;

    public List<BlobInfo> BlobList { get; private set; } = new();

    public double MinArea { get; set; } = 50;

    public double MaxArea { get; set; } = 1000000;

    public event Action<Mat?, IReadOnlyList<BlobInfo>>? ResultUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            ClearResult();

            return new Dictionary<string, object?>
            {
                ["BlobList"] = BlobList,
                ["ResultImage"] = null
            };
        }

        Mat result = DetectBlobs(source, out List<BlobInfo> blobs);

        _resultImage?.Dispose();
        _resultImage = result;
        BlobList = blobs;

        ResultUpdated?.Invoke(_resultImage, BlobList);

        return new Dictionary<string, object?>
        {
            ["BlobList"] = BlobList,
            ["ResultImage"] = _resultImage
        };
    }

    private Mat DetectBlobs(Mat source, out List<BlobInfo> blobs)
    {
        using Mat binary = ConvertToBinary8U(source);

        Mat result = CreateDisplayImage(binary);

        Cv2.FindContours(
            binary,
            out Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        blobs = new List<BlobInfo>();

        int index = 1;

        foreach (Point[] contour in contours)
        {
            double area = Cv2.ContourArea(contour);

            if (area < MinArea)
                continue;

            if (area > MaxArea)
                continue;

            Rect rect = Cv2.BoundingRect(contour);
            Point2d center = GetContourCenter(contour, rect);

            BlobInfo blob = new()
            {
                Index = index,
                Area = area,
                BoundingBox = rect,
                Center = center
            };

            blobs.Add(blob);

            DrawBlob(result, blob);

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

        // 입력이 완전한 이진 이미지가 아니어도 Blob 검출 가능하도록 한번 더 이진화
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

    private static void DrawBlob(Mat result, BlobInfo blob)
    {
        Rect rect = blob.BoundingBox;

        Point center = new(
            (int)Math.Round(blob.Center.X),
            (int)Math.Round(blob.Center.Y));

        // 검출 영역 사각형
        Cv2.Rectangle(
            result,
            rect,
            new Scalar(0, 255, 0),
            2);

        // 중심점
        Cv2.DrawMarker(
            result,
            center,
            new Scalar(0, 0, 255),
            MarkerTypes.Cross,
            12,
            2);

        // 번호 표시
        Cv2.PutText(
            result,
            blob.Index.ToString(),
            new Point(rect.X, Math.Max(0, rect.Y - 4)),
            HersheyFonts.HersheySimplex,
            0.5,
            new Scalar(0, 255, 255),
            1);
    }

    private static Mat? FindMatInput(Dictionary<string, object?> inputs)
    {
        // Threshold / Morphology 출력명
        if (inputs.TryGetValue("Binary", out object? binaryValue) &&
            binaryValue is Mat binary &&
            !binary.Empty())
        {
            return binary;
        }

        // 혹시 Image 포트로 들어와도 동작하게 처리
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
        BlobList.Clear();

        ResultUpdated?.Invoke(null, BlobList);
    }

    public void Dispose()
    {
        _resultImage?.Dispose();
        _resultImage = null;
    }
}

public class BlobInfo
{
    public int Index { get; set; }

    public double Area { get; set; }

    public Rect BoundingBox { get; set; }

    public Point2d Center { get; set; }

    public double Width => BoundingBox.Width;

    public double Height => BoundingBox.Height;
}