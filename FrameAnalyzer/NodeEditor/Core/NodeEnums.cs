using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameAnalyzer.NodeEditor
{
    public static class NodeEnums
    {
        /// <summary>
        /// 노드 포트의 방향을 나타냅니다.
        /// Input  : 노드로 들어오는 연결점
        /// Output : 노드에서 나가는 연결점
        /// </summary>
        public enum PortDirection
        {
            Input,
            Output
        }

        /// <summary>
        /// 포트 배치 방식입니다.
        /// LeftRight : 입력은 왼쪽, 출력은 오른쪽
        /// TopBottom : 입력은 위쪽, 출력은 아래쪽
        /// </summary>
        public enum NodePortLayout
        {
            LeftRight,
            TopBottom
        }
    }
}
