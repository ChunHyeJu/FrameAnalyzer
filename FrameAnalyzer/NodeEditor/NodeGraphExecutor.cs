using System.Diagnostics;

namespace FrameAnalyzer.NodeEditor;

public class NodeGraphExecutor
{
    private readonly List<NodeView> _nodes;
    private readonly List<NodeConnection> _connections;

    private readonly Dictionary<Guid, Dictionary<string, object?>> _cache = new();
    private readonly HashSet<Guid> _executing = new();

    public NodeGraphExecutor(
        IEnumerable<NodeView> nodes,
        IEnumerable<NodeConnection> connections)
    {
        _nodes = nodes.ToList();
        _connections = connections.ToList();
    }

    public Dictionary<string, object?> Execute(NodeView targetNode)
    {
        if (_cache.TryGetValue(targetNode.Id, out var cached))
            return cached;

        if (!_executing.Add(targetNode.Id))
            throw new InvalidOperationException($"순환 연결이 감지되었습니다: {targetNode.Title}");

        try
        {
            if (targetNode.Processor == null)
                throw new InvalidOperationException($"{targetNode.Title} 처리기가 없습니다.");

            Dictionary<string, object?> inputs = new();

            foreach (var inputPort in targetNode.Inputs)
            {
                // 입력 포트는 하나의 연결만 사용
                var connection = _connections.FirstOrDefault(c =>
                    c.To == inputPort);

                if (connection == null)
                    continue;

                var fromNode = connection.From?.Owner;
                var fromPort = connection.From;

                if (fromNode == null || fromPort == null)
                    continue;

                var upstreamOutputs = Execute(fromNode);

                if (!upstreamOutputs.ContainsKey(fromPort.Name))
                {
                    throw new InvalidOperationException(
                        $"{fromNode.Title} 노드에 '{fromPort.Name}' 출력값이 없습니다.");
                }

                inputs[inputPort.Name] = upstreamOutputs[fromPort.Name];
            }

            Stopwatch sw = Stopwatch.StartNew();

            var result = targetNode.Processor.Execute(inputs);

            sw.Stop();

            if (targetNode.HostedControl is INodeRuntimeAware runtimeAware)
            {
                runtimeAware.SetProcessingTime(sw.Elapsed.TotalMilliseconds);
            }

            _cache[targetNode.Id] = result;

            return result;
        }
        finally
        {
            _executing.Remove(targetNode.Id);
        }
    }

    public Dictionary<NodeView, Dictionary<string, object?>> ExecuteAllSinks()
    {
        Dictionary<NodeView, Dictionary<string, object?>> results = new();

        List<NodeView> sinkNodes = FindSinkNodes();

        foreach (var node in sinkNodes)
        {
            results[node] = Execute(node);
        }

        return results;
    }

    public Dictionary<NodeView, Dictionary<string, object?>> ExecuteAll()
    {
        Dictionary<NodeView, Dictionary<string, object?>> results = new();

        foreach (var node in _nodes)
        {
            if (node.Processor == null)
                continue;

            results[node] = Execute(node);
        }

        return results;
    }

    private List<NodeView> FindSinkNodes()
    {
        return _nodes
            .Where(node => node.Processor != null)
            .Where(node => !_connections.Any(c =>
                c.From != null &&
                c.From.Owner == node))
            .ToList();
    }
}