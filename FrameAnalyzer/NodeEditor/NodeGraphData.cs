using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameAnalyzer.NodeEditor
{
    /// <summary>
    // 그래프 저장/불러오기용 DTO입니다.
    // 노드 목록과 연결 목록을 JSON으로 직렬화합니다.
    /// </summary>
    public class NodeGraphData
    {
        public List<NodeView> Nodes { get; set; } = new();
        public List<NodeConnection> Connections { get; set; } = new();
    }
}
