namespace FrameAnalyzer.NodeEditor;

public interface INodeRuntimeAware
{
    bool PreviewEnabled { get; set; }

    bool ShowProcessingTime { get; set; }

    void SetProcessingTime(double milliseconds);
}