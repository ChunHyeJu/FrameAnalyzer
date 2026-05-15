using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ChannelSplitProcessor : INodeProcessor, IDisposable
{
    public string Name => "Channel Split";

    private Mat? _bChannel;
    private Mat? _gChannel;
    private Mat? _rChannel;

    public Mat? BChannel => _bChannel;
    public Mat? GChannel => _gChannel;
    public Mat? RChannel => _rChannel;

    public event Action<Mat?, Mat?, Mat?>? ChannelsUpdated;

    public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
    {
        Mat? source = FindMatInput(inputs);

        if (source == null || source.Empty())
        {
            ClearChannels();

            return new Dictionary<string, object?>
            {
                ["B"] = null,
                ["G"] = null,
                ["R"] = null
            };
        }

        SplitChannels(source);

        ChannelsUpdated?.Invoke(_bChannel, _gChannel, _rChannel);

        return new Dictionary<string, object?>
        {
            ["B"] = _bChannel,
            ["G"] = _gChannel,
            ["R"] = _rChannel
        };
    }

    private void SplitChannels(Mat source)
    {
        DisposeChannels();

        if (source.Channels() == 1)
        {
            // 흑백 이미지는 B/G/R에 같은 이미지를 복사해서 출력
            _bChannel = source.Clone();
            _gChannel = source.Clone();
            _rChannel = source.Clone();
            return;
        }

        if (source.Channels() != 3 && source.Channels() != 4)
        {
            throw new InvalidOperationException(
                $"지원하지 않는 이미지 채널 수입니다. Channels: {source.Channels()}");
        }

        Mat[] channels = Cv2.Split(source);

        // OpenCV 기본 컬러 순서는 BGR
        _bChannel = channels[0];
        _gChannel = channels[1];
        _rChannel = channels[2];

        // BGRA 이미지일 경우 Alpha 채널은 사용하지 않으므로 해제
        for (int i = 3; i < channels.Length; i++)
        {
            channels[i].Dispose();
        }
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

    private void ClearChannels()
    {
        DisposeChannels();
        ChannelsUpdated?.Invoke(null, null, null);
    }

    private void DisposeChannels()
    {
        _bChannel?.Dispose();
        _gChannel?.Dispose();
        _rChannel?.Dispose();

        _bChannel = null;
        _gChannel = null;
        _rChannel = null;
    }

    public void Dispose()
    {
        DisposeChannels();
    }
}