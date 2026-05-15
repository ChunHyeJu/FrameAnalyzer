using FrameAnalyzer.Controls_OpenCV_Detection;
using FrameAnalyzer.NodeEditor;

namespace FrameAnalyzer;

public class AddOpenCVDectectionNode
{
    private readonly NodeCanvasControl _nodeCanvas;

    public AddOpenCVDectectionNode(NodeCanvasControl nodeCanvas)
    {
        _nodeCanvas = nodeCanvas;
        RegisterBackgroundSubtractionNode();
    }

    private void RegisterBackgroundSubtractionNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV_Detection/Background Subtraction", (canvas, point) =>
        {
            var processor = new BackgroundSubtractionProcessor();

            var node = canvas.AddNode(
                "Background Subtraction",
                new[] { "Image" },
                new[] { "Mask", "Image" },
                point.X,
                point.Y,
                new BackgroundSubtractionControl(processor));

            node.Processor = processor;

            return node;
        });
    }
}
