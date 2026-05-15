using System.Text.Json.Serialization;

namespace FrameAnalyzer.NodeEditor;

/// <summary>
/// Runtime and serializable state for a single graph node.
/// UI controls and processors are runtime-only; NodeType is saved so a graph can
/// rebuild the correct control and processor from the registered factory.
/// </summary>
public class NodeView
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? NodeType { get; set; }

    public string Title { get; set; } = "";

    public RectangleF Bounds { get; set; }

    public List<NodePort> Inputs { get; set; } = new();

    public List<NodePort> Outputs { get; set; } = new();

    [JsonIgnore]
    public Control? HostedControl { get; set; }

    [JsonIgnore]
    public NodeCanvasControl.NodePortLayout PortLayout { get; set; } =
        NodeCanvasControl.NodePortLayout.LeftRight;

    [JsonIgnore]
    public INodeProcessor? Processor { get; set; }
}
