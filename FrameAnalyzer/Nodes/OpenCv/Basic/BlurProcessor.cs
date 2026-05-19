using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public enum BlurType
{
    Normal,
    Gaussian,
    Median,
    Bilateral
}

public class BlurProcessor : INodeProcessor, IDisposable
{
    public string Name => "Blur";

    private Mat? _blurImage;

    public Mat? BlurImage => _blurImage;

    public BlurType BlurType { get; set; } = BlurType.Gaussian;

    public int KernelSize { get; set; } = 5;

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

        Mat result = ApplyBlur(source);

        _blurImage?.Dispose();
        _blurImage = result;

        ImageUpdated?.Invoke(_blurImage);

        return new Dictionary<string, object?>
        {
            ["Image"] = _blurImage
        };
    }

    private Mat ApplyBlur(Mat source)
    {
        // OpenCV Blur 계열은 보통 홀수 커널을 사용하므로 보정
        int kernel = NormalizeKernelSize(KernelSize);

        Mat result = new();

        switch (BlurType)
        {
            case BlurType.Normal:
                // 일반 평균 블러: 주변 픽셀 평균으로 부드럽게 처리
                Cv2.Blur(
                    source,
                    result,
                    new OpenCvSharp.Size(kernel, kernel));
                break;

            case BlurType.Gaussian:
                // 가우시안 블러: 자연스러운 노이즈 제거에 많이 사용
                Cv2.GaussianBlur(
                    source,
                    result,
                    new OpenCvSharp.Size(kernel, kernel),
                    0);
                break;

            case BlurType.Median:
                // 미디언 블러: 점처럼 튀는 노이즈 제거에 효과적
                Cv2.MedianBlur(
                    source,
                    result,
                    kernel);
                break;

            case BlurType.Bilateral:
                // 양방향 필터: 경계는 최대한 유지하면서 노이즈 제거
                Cv2.BilateralFilter(
                    source,
                    result,
                    kernel,
                    kernel * 2,
                    kernel / 2.0);
                break;

            default:
                // 알 수 없는 타입이면 원본 복사
                source.CopyTo(result);
                break;
        }

        return result;
    }

    private static int NormalizeKernelSize(int value)
    {
        if (value < 1)
            value = 1;

        // OpenCV의 GaussianBlur, MedianBlur는 홀수 커널이 안전함
        if (value % 2 == 0)
            value += 1;

        return value;
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
        _blurImage?.Dispose();
        _blurImage = null;

        ImageUpdated?.Invoke(null);
    }

    public void Dispose()
    {
        _blurImage?.Dispose();
        _blurImage = null;
    }
}