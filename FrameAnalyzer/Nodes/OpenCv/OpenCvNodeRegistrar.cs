using FrameAnalyzer.Nodes.OpenCv.Basic;
using FrameAnalyzer.NodeEditor;

namespace FrameAnalyzer.Nodes.OpenCv;

public class OpenCvNodeRegistrar
{
    private readonly NodeCanvasControl _nodeCanvas;

    public OpenCvNodeRegistrar(NodeCanvasControl nodeCanvas)
    {
        _nodeCanvas = nodeCanvas;
        RegisterImageInputNode();
        RegisterExternalMatInputNode();
        RegisterGrayScaleNode();
        RegisterBlurNode();
        RegisterHeatmapNode();
        RegisterThresholdNode();
        RegisterEdgeDetectNode();
        RegisterHistogramNode();
        RegisterBrightnessContrastNode();
        RegisterMorphologyNode();
        RegisterBlobDetectNode();
        RegisterContourDetectNode();
        RegisterResizeNode();
        RegisterChannelSplitNode();
        RegisterROIColorSamplerNode();
        RegisterImageViewerNode();
    }

    private void RegisterImageInputNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Image Input", (canvas, point) =>
        {
            var processor = new ImageInputProcessor();

            var node = canvas.AddNode(
                "Image Input",
                Array.Empty<string>(),
                new[] { "Image" },
                point.X,
                point.Y,
                new ImageInputControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterExternalMatInputNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/External Mat Input", (canvas, point) =>
        {
            var processor = new ExternalMatInputProcessor();

            var node = canvas.AddNode(
                "External Mat Input",
                Array.Empty<string>(),
                new[] { "Image" },
                point.X,
                point.Y,
                new ExternalMatInputControl(processor));

            node.Processor = processor;

            return node;
        });
    }



    private void RegisterGrayScaleNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/GrayScale", (canvas, point) =>
        {
            var processor = new GrayScaleProcessor();

            var node = canvas.AddNode(
                "GrayScale",
                new[] { "Image" },
                new[] { "Image" },
                point.X,
                point.Y,
                new GrayScaleControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterBlurNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Blur", (canvas, point) =>
        {
            var processor = new BlurProcessor();

            var node = canvas.AddNode(
                "Blur",
                new[] { "Image" },
                new[] { "Image" },
                point.X,
                point.Y,
                new BlurControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterHeatmapNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Heatmap", (canvas, point) =>
        {
            var processor = new HeatmapProcessor();

            var node = canvas.AddNode(
                "Heatmap",
                new[] { "Image" },
                new[] { "Heatmap" },
                point.X,
                point.Y,
                new HeatmapControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterThresholdNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Threshold", (canvas, point) =>
        {
            var processor = new ThresholdProcessor();

            var node = canvas.AddNode(
                "Threshold",
                new[] { "Image" },
                new[] { "Binary" },
                point.X,
                point.Y,
                new ThresholdControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterEdgeDetectNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Edge Detect", (canvas, point) =>
        {
            var processor = new EdgeDetectProcessor();

            var node = canvas.AddNode(
                "Edge Detect",
                new[] { "Image" },
                new[] { "Edge" },
                point.X,
                point.Y,
                new EdgeDetectControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterHistogramNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Histogram", (canvas, point) =>
        {
            var processor = new HistogramProcessor();

            var node = canvas.AddNode(
                "Histogram",
                new[] { "Image" },
                new[] { "Image", "HistogramData" },
                point.X,
                point.Y,
                new HistogramControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterBrightnessContrastNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Brightness Contrast", (canvas, point) =>
        {
            var processor = new BrightnessContrastProcessor();

            var node = canvas.AddNode(
                "Brightness / Contrast",
                new[] { "Image" },
                new[] { "Image" },
                point.X,
                point.Y,
                new BrightnessContrastControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterMorphologyNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Morphology", (canvas, point) =>
        {
            var processor = new MorphologyProcessor();

            var node = canvas.AddNode(
                "Morphology",
                new[] { "Binary" },
                new[] { "Binary" },
                point.X,
                point.Y,
                new MorphologyControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterBlobDetectNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Blob Detect", (canvas, point) =>
        {
            var processor = new BlobDetectProcessor();

            var node = canvas.AddNode(
                "Blob Detect",
                new[] { "Binary" },
                new[] { "BlobList", "ResultImage" },
                point.X,
                point.Y,
                new BlobDetectControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterContourDetectNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Contour Detect", (canvas, point) =>
        {
            var processor = new ContourDetectProcessor();

            var node = canvas.AddNode(
                "Contour Detect",
                new[] { "Binary" },
                new[] { "Contours", "ResultImage" },
                point.X,
                point.Y,
                new ContourDetectControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterResizeNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Resize", (canvas, point) =>
        {
            var processor = new ResizeProcessor();

            var node = canvas.AddNode(
                "Resize",
                new[] { "Image" },
                new[] { "Image" },
                point.X,
                point.Y,
                new ResizeControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterChannelSplitNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Channel Split", (canvas, point) =>
        {
            var processor = new ChannelSplitProcessor();

            var node = canvas.AddNode(
                "Channel Split",
                new[] { "Image" },
                new[] { "B", "G", "R" },
                point.X,
                point.Y,
                new ChannelSplitControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterROIColorSamplerNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/ROI Color Sampler", (canvas, point) =>
        {
            var processor = new ROIColorSamplerProcessor();

            var node = canvas.AddNode(
                "ROI Color Sampler",
                new[] { "Image" },
                new[] { "Image" },
                point.X,
                point.Y,
                new ROIColorSamplerControl(processor));

            node.Processor = processor;
            return node;
        });
    }

    private void RegisterImageViewerNode()
    {
        _nodeCanvas.RegisterNodeFactory("OpenCV/Image Viewer", (canvas, point) =>
        {
            var processor = new ImageViewerProcessor();

            var node = canvas.AddNode(
                "Image Viewer",
                new[] { "Image" },
                Array.Empty<string>(),
                point.X,
                point.Y,
                new ImageViewerControl(processor));

            node.Processor = processor;
            return node;
        });
    }
}
