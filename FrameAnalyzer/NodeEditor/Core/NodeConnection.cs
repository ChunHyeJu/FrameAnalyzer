using System.Text.Json.Serialization;

namespace FrameAnalyzer.NodeEditor
{
    /// <summary>
    /// 두 포트 사이의 연결선 정보입니다.
    /// JSON 저장 시에는 Guid만 저장하고, 로드 후 From/To 포트를 다시 복원합니다.
    /// </summary>
    public class NodeConnection
    {
        public Guid FromNodeId { get; set; }
        public Guid FromPortId { get; set; }

        public Guid ToNodeId { get; set; }
        public Guid ToPortId { get; set; }

        [JsonIgnore]
        public NodePort? From { get; set; }

        [JsonIgnore]
        public NodePort? To { get; set; }
    }
}
