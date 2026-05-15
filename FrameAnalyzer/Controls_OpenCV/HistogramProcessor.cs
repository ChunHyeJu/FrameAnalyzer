using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using System.Runtime.InteropServices;

namespace FrameAnalyzer.Controls_OpenCV;

public class HistogramProcessor : INodeProcessor
{
    private const int HistogramBins = 256;

    public string Name => "Histogram";

    public HistogramData? CurrentHistogram { get; private set; }

    public event Action<HistogramData?>? HistogramUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            CurrentHistogram = null;
            HistogramUpdated?.Invoke(null);

            return new Dictionary<string, object?>
            {
                ["Image"] = null,
                ["HistogramData"] = null
            };
        }

        HistogramData histogram = CalculateHistogram(source);

        CurrentHistogram = histogram;
        HistogramUpdated?.Invoke(histogram);

        return new Dictionary<string, object?>
        {
            // 들어온 이미지를 그대로 다음 노드로 전달
            ["Image"] = source,

            // 히스토그램 데이터도 같이 출력
            ["HistogramData"] = histogram
        };
    }

    private static HistogramData CalculateHistogram(Mat source)
    {
        using Mat gray = ConvertToGray8U(source);

        double[] counts = new double[HistogramBins];

        // Mat 메모리를 안전하게 읽기 위해 연속 메모리 형태로 복사
        using Mat continuous = gray.Clone();

        int byteLength = checked((int)(continuous.Total() * continuous.ElemSize()));

        byte[] pixels = new byte[byteLength];

        Marshal.Copy(
            continuous.Data,
            pixels,
            0,
            byteLength);

        foreach (byte pixel in pixels)
        {
            counts[pixel]++;
        }

        return new HistogramData(counts);
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
}

public class HistogramData
{
    public double[] Counts { get; }

    public int BinCount => Counts.Length;

    public long TotalPixels { get; }

    public double MaxCount { get; }

    public int PeakBin { get; }

    public double MeanIntensity { get; }

    public HistogramData(double[] counts)
    {
        Counts = counts;

        double maxCount = 0;
        int peakBin = 0;
        double weightedSum = 0;
        long totalPixels = 0;

        for (int i = 0; i < counts.Length; i++)
        {
            double count = counts[i];

            totalPixels += (long)count;
            weightedSum += i * count;

            if (count > maxCount)
            {
                maxCount = count;
                peakBin = i;
            }
        }

        TotalPixels = totalPixels;
        MaxCount = maxCount;
        PeakBin = peakBin;

        MeanIntensity = totalPixels > 0
            ? weightedSum / totalPixels
            : 0;
    }
}