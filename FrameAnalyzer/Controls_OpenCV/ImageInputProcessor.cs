using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ImageInputProcessor : INodeProcessor, IDisposable
{
    public string Name => "Image Input";

    private Mat? _image;

    public Mat? Image => _image;

    public void LoadImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
            throw new FileNotFoundException("이미지 파일을 찾을 수 없습니다.", path);

        Mat loaded = Cv2.ImRead(path, ImreadModes.Color);

        if (loaded.Empty())
        {
            loaded.Dispose();
            throw new InvalidOperationException("이미지를 읽을 수 없습니다.");
        }

        _image?.Dispose();
        _image = loaded;
    }

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        return new Dictionary<string, object?>
        {
            ["Image"] = _image
        };
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}