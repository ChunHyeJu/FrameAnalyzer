using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FrameAnalyzer.Controls_OpenCV_Detection;

public sealed class BackgroundSubtractionProcessor : INodeProcessor, IDisposable
{
    private BackgroundSubtractorMOG2? _mog2;

    private Mat? _mask;
    private Mat? _resultImage;

    private bool _isBackgroundFixed;
    private int _frameCount;

    // 배경 모델 생성에 사용되는 MOG2 기본 설정값
    private int _history = 500;
    private double _varThreshold = 16;
    private bool _detectShadows = false;

    // 처리 결과가 갱신되었을 때 Control에 알려주는 이벤트
    public event Action<Mat?, Mat?>? ResultUpdated;

    public string Name => "Background Subtraction";

    // 배경 모델이 참고할 이전 프레임 개수
    // 값이 바뀌면 기존 모델을 다시 만들어야 한다
    public int History
    {
        get => _history;
        set
        {
            int next = Math.Max(1, value);

            if (_history == next)
                return;

            _history = next;
            ResetModel();
        }
    }

    // 배경과 전경을 구분하는 민감도
    // 값이 낮을수록 변화에 민감하게 반응한다
    public double VarThreshold
    {
        get => _varThreshold;
        set
        {
            double next = Math.Max(0.1, value);

            if (Math.Abs(_varThreshold - next) < double.Epsilon)
                return;

            _varThreshold = next;
            ResetModel();
        }
    }

    // 그림자 검출 사용 여부
    // MOG2 생성 옵션이므로 변경 시 모델을 초기화한다
    public bool DetectShadows
    {
        get => _detectShadows;
        set
        {
            if (_detectShadows == value)
                return;

            _detectShadows = value;
            ResetModel();
        }
    }

    public bool IsBackgroundFixed => _isBackgroundFixed;

    public int FrameCount => _frameCount;

    public int LastObjectCount { get; private set; }

    // MOG2가 만든 마스크를 이진화할 기준값
    public int ThresholdValue { get; set; } = 200;

    // 최종 객체 영역 판단에 사용할 최소 면적 설정값
    public int MinArea { get; set; } = 80;

    // 마스크 정리 과정에서 유지할 contour 최소 면적
    public int FillMinArea { get; set; } = 80;

    // 작은 노이즈 제거용 Open 연산 설정
    public int OpenKernelSize { get; set; } = 3;
    public int OpenIterations { get; set; } = 1;

    // 끊어진 전경 영역을 연결하기 위한 Close 연산 설정
    public int CloseKernelSize { get; set; } = 21;
    public int CloseIterations { get; set; } = 2;

    // 초기 배경 모델 안정화를 위해 결과를 무시할 프레임 수
    public int WarmUpFrames { get; set; } = 0;

    // 노드 입력을 받아 배경 차분을 수행하고 Mask/Image 결과를 반환한다
    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = GetInputImage(inputs);

        if (source == null || source.Empty())
        {
            ClearResult();

            return new Dictionary<string, object?>
            {
                ["Mask"] = null,
                ["Image"] = null
            };
        }

        Process(source);

        // Control 프리뷰 갱신용 결과 전달
        ResultUpdated?.Invoke(_mask, _resultImage);

        return new Dictionary<string, object?>
        {
            ["Mask"] = _mask,
            ["Image"] = _resultImage
        };
    }

    // 현재 배경 모델을 고정하여 더 이상 학습하지 않게 한다
    public void FixBackground()
    {
        _isBackgroundFixed = true;
    }

    // 배경 모델 학습을 다시 진행하도록 한다
    public void ReleaseBackground()
    {
        _isBackgroundFixed = false;
    }

    // UI에서 배경 고정 상태를 직접 지정할 때 사용한다
    public void SetBackgroundFixed(bool fixedBackground)
    {
        _isBackgroundFixed = fixedBackground;
    }

    // 기존 MOG2 모델을 폐기하고 처음 상태로 되돌린다
    public void ResetModel()
    {
        _mog2?.Dispose();
        _mog2 = null;

        _isBackgroundFixed = false;
        _frameCount = 0;
        LastObjectCount = 0;
    }

    // 한 프레임에 대해 배경 차분, 마스크 정리, 결과 이미지 생성을 수행한다
    private void Process(Mat source)
    {
        using Mat gray = ToGray(source);
        using Mat rawMask = new();

        double learningRate = _isBackgroundFixed ? 0.0 : -1.0;

        // MOG2로 배경과 다른 영역을 추출한다
        GetMog2().Apply(gray, rawMask, learningRate);

        _frameCount++;

        Mat resultImage = CreateResultImage(source);
        Mat mask;

        // warm-up 구간에서는 배경 학습만 하고 검출 결과는 내보내지 않는다
        if (_frameCount <= WarmUpFrames)
        {
            mask = new Mat(source.Size(), MatType.CV_8UC1, Scalar.Black);
            LastObjectCount = 0;
        }
        else
        {
            mask = CreateCleanMask(rawMask);
            LastObjectCount = DrawComponents(resultImage, mask);
        }

        _mask?.Dispose();
        _resultImage?.Dispose();

        _mask = mask;
        _resultImage = resultImage;
    }

    // MOG2 객체를 필요할 때 생성하고 이후에는 재사용한다
    private BackgroundSubtractorMOG2 GetMog2()
    {
        if (_mog2 != null)
            return _mog2;

        _mog2 = BackgroundSubtractorMOG2.Create(
            history: History,
            varThreshold: VarThreshold,
            detectShadows: DetectShadows);

        return _mog2;
    }

    // MOG2 원본 마스크를 후처리해서 노이즈가 줄어든 최종 마스크를 만든다
    private Mat CreateCleanMask(Mat rawMask)
    {
        Mat mask = new();

        // 회색 단계의 raw mask를 흑백 마스크로 변환한다
        Cv2.Threshold(
            rawMask,
            mask,
            ThresholdValue,
            255,
            ThresholdTypes.Binary);

        // 작은 점 형태의 노이즈를 제거한다
        if (OpenKernelSize > 0 && OpenIterations > 0)
        {
            int size = NormalizeKernelSize(OpenKernelSize);

            using Mat openKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new Size(size, size));

            Cv2.MorphologyEx(
                mask,
                mask,
                MorphTypes.Open,
                openKernel,
                iterations: OpenIterations);
        }

        // 끊겨 있는 전경 영역을 이어준다
        if (CloseKernelSize > 0 && CloseIterations > 0)
        {
            int size = NormalizeKernelSize(CloseKernelSize);

            using Mat closeKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new Size(size, size));

            Cv2.MorphologyEx(
                mask,
                mask,
                MorphTypes.Close,
                closeKernel,
                iterations: CloseIterations);
        }

        // contour 단위로 작은 영역을 제거하고 내부를 채운다
        FillForegroundContours(mask, FillMinArea);

        return mask;
    }

    // 커널 크기가 짝수로 들어오면 홀수로 보정한다
    private static int NormalizeKernelSize(int value)
    {
        int size = Math.Max(1, value);

        if (size % 2 == 0)
            size++;

        return size;
    }

    // 마스크에서 contour를 찾고, 일정 면적 이상인 영역만 다시 채워 넣는다
    private static void FillForegroundContours(Mat mask, double minArea)
    {
        Cv2.FindContours(
            mask,
            out Point[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        mask.SetTo(Scalar.Black);

        foreach (Point[] contour in contours)
        {
            double area = Cv2.ContourArea(contour);

            if (area < minArea)
                continue;

            Cv2.DrawContours(
                mask,
                new[] { contour },
                -1,
                Scalar.White,
                thickness: -1);
        }
    }

    // 마스크의 연결 영역을 찾아 결과 이미지 위에 박스와 면적을 표시한다
    // 마스크의 연결 영역을 찾아 결과 이미지 위에 박스와 면적을 표시한다
    private int DrawComponents(Mat resultImage, Mat mask)
    {
        using Mat labels = new();
        using Mat stats = new();
        using Mat centroids = new();

        int count = Cv2.ConnectedComponentsWithStats(
            mask,
            labels,
            stats,
            centroids);

        int objectCount = 0;

        // 0번 label은 배경이므로 제외한다
        for (int i = 1; i < count; i++)
        {
            int x = stats.Get<int>(i, (int)ConnectedComponentsTypes.Left);
            int y = stats.Get<int>(i, (int)ConnectedComponentsTypes.Top);
            int w = stats.Get<int>(i, (int)ConnectedComponentsTypes.Width);
            int h = stats.Get<int>(i, (int)ConnectedComponentsTypes.Height);
            int area = stats.Get<int>(i, (int)ConnectedComponentsTypes.Area);

            // MinArea보다 작은 객체는 무시한다
            if (area < MinArea)
                continue;

            objectCount++;

            Cv2.Rectangle(
                resultImage,
                new Rect(x, y, w, h),
                new Scalar(0, 0, 255),
                2);

            Cv2.PutText(
                resultImage,
                area.ToString(),
                new Point(x, Math.Max(0, y - 4)),
                HersheyFonts.HersheySimplex,
                0.45,
                new Scalar(0, 255, 255),
                1);
        }

        return objectCount;
    }

    // 입력 이미지를 MOG2 처리용 grayscale 이미지로 변환한다
    private static Mat ToGray(Mat source)
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
            throw new InvalidOperationException($"지원하지 않는 채널 수입니다. Channels: {source.Channels()}");
        }

        return gray;
    }

    // 박스와 텍스트를 그릴 수 있도록 결과 이미지를 BGR 형태로 만든다
    private static Mat CreateResultImage(Mat source)
    {
        Mat result = new();

        if (source.Channels() == 1)
        {
            Cv2.CvtColor(source, result, ColorConversionCodes.GRAY2BGR);
        }
        else if (source.Channels() == 3)
        {
            source.CopyTo(result);
        }
        else if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, result, ColorConversionCodes.BGRA2BGR);
        }
        else
        {
            source.CopyTo(result);
        }

        return result;
    }

    // 입력 Dictionary에서 처리할 Mat 이미지를 찾는다
    private static Mat? GetInputImage(Dictionary<string, object?> inputs)
    {
        if (inputs.TryGetValue("Image", out object? imageObject) &&
            imageObject is Mat image &&
            !image.Empty())
        {
            return image;
        }

        return inputs.Values
            .OfType<Mat>()
            .FirstOrDefault(mat => !mat.Empty());
    }

    // 입력 이미지가 없을 때 기존 결과를 정리한다
    private void ClearResult()
    {
        _mask?.Dispose();
        _resultImage?.Dispose();

        _mask = null;
        _resultImage = null;

        LastObjectCount = 0;
    }

    // Processor가 가진 OpenCV 리소스를 정리한다
    public void Dispose()
    {
        _mog2?.Dispose();
        _mog2 = null;

        _mask?.Dispose();
        _resultImage?.Dispose();

        _mask = null;
        _resultImage = null;
    }
}