using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static FrameAnalyzer.NodeEditor.NodeEnums;

namespace FrameAnalyzer.NodeEditor
{
    /// <summary>
    ///노드 하나에 붙는 입력/출력 포트 정보입니다.
    /// 실제 연결은 NodeConnection이 이 포트의 Id를 참조해서 구성합니다.
    /// </summary>
    public class NodePort
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public PortDirection Direction { get; set; }

        [JsonIgnore]
        public RectangleF Bounds { get; set; }

        [JsonIgnore]
        public NodeView? Owner { get; set; }
    }
}
