using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class HeatmapProcessor : INodeProcessor, IDisposable
{
    public string Name => "Heatmap";

    private Mat? _heatmap;

    public Mat? Heatmap => _heatmap;

    public event Action<Mat?>? HeatmapUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        if (!inputs.TryGetValue("Image", out object? value) || value is not Mat inputImage)
        {
            ClearHeatmap();

            return new Dictionary<string, object?>
            {
                ["Heatmap"] = null
            };
        }

        if (inputImage.Empty())
        {
            ClearHeatmap();

            return new Dictionary<string, object?>
            {
                ["Heatmap"] = null
            };
        }

        Mat result = CreateHeatmap(inputImage);

        _heatmap?.Dispose();
        _heatmap = result;

        HeatmapUpdated?.Invoke(_heatmap);

        return new Dictionary<string, object?>
        {
            ["Heatmap"] = _heatmap
        };
    }

    private static Mat CreateHeatmap(Mat source)
    {
        using Mat gray = new();
        using Mat normalized = new();

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

        Cv2.Normalize(
            gray,
            normalized,
            0,
            255,
            NormTypes.MinMax,
            (int)MatType.CV_8UC1);

        Mat heatmap = new();

        Cv2.ApplyColorMap(
            normalized,
            heatmap,
            ColormapTypes.Jet);

        return heatmap;
    }

    private void ClearHeatmap()
    {
        _heatmap?.Dispose();
        _heatmap = null;

        HeatmapUpdated?.Invoke(null);
    }

    public void Dispose()
    {
        _heatmap?.Dispose();
        _heatmap = null;
    }
}