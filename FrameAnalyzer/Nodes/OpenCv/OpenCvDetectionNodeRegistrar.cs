using FrameAnalyzer.Nodes.OpenCv.Detection;
using FrameAnalyzer.NodeEditor;

namespace FrameAnalyzer.Nodes.OpenCv;

public class OpenCvDetectionNodeRegistrar
{
    private readonly NodeCanvasControl _nodeCanvas;

    public OpenCvDetectionNodeRegistrar(NodeCanvasControl nodeCanvas)
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
