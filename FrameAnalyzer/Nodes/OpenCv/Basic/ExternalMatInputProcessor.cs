using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using System.CodeDom.Compiler;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class ExternalMatInputProcessor : INodeProcessor, IDisposable
{
    public string Name => "External Mat Input";

    private readonly object _lock = new();
    private Mat? _currentImage;

    public Mat? CurrentImage
    {
        get
        {
            lock (_lock)
            {
                return _currentImage;
            }
        }
    }

    public event Action<Mat?>? ImageUpdated;

    /// <summary>
    /// 외부에서 Mat 이미지를 입력합니다.
    /// 기본적으로 Clone해서 내부에 보관합니다.
    /// </summary>
    public void SetImage(Mat? image)
    {
        lock (_lock)
        {
            _currentImage?.Dispose();
            _currentImage = null;

            if (image != null && !image.Empty())
                _currentImage = image.Clone();
        }

        ImageUpdated?.Invoke(CurrentImage);
    }

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        lock (_lock)
        {
            return new Dictionary<string, object?>
            {
                ["Image"] = _currentImage
            };
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _currentImage?.Dispose();
            _currentImage = null;
        }
    }
}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//외부 Mat 넣고 결과 받는 함수 예시
//public Mat? ProcessExternalMat(Mat input)
//{
//    if (_matInputProcessor == null)
//        return null;

//    _matInputProcessor.SetImage(input);

//    NodeGraphExecutor executor = new(
//        _nodeCanvas.Nodes,
//        _nodeCanvas.Connections);

//    Dictionary<NodeView, Dictionary<string, object?>> results =
//        executor.ExecuteAllSinks();

//    foreach (var pair in results)
//    {
//        if (pair.Key.Title != "Image Viewer")
//            continue;

//        if (pair.Value.TryGetValue("Image", out object? value) &&
//            value is Mat resultImage &&
//            !resultImage.Empty())
//        {
//            return resultImage.Clone();
//        }
//    }

//    return null;
//}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//특정 Viewer 노드만 알고 있으면 더 간단함

//Viewer 노드 변수를 가지고 있다면 ExecuteAllSinks() 말고 직접 실행해도 됩니다.

//    NodeGraphExecutor executor = new(
//    _nodeCanvas.Nodes,
//    _nodeCanvas.Connections);

//    Dictionary<string, object?> result = executor.Execute(imageViewerNode);

//if (result.TryGetValue("Image", out object? value) &&
//    value is Mat image &&
//    !image.Empty())
//{
//    Mat copy = image.Clone();

//    // 최종 결과 사용
//}