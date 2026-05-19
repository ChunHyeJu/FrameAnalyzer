using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameAnalyzer.NodeEditor
{
    /// <summary>
    /// 내부 노드 컨트롤이 테마 변경을 받을 수 있도록 하는 인터페이스입니다.
    /// </summary>
    public interface INodeThemeAware
    {
        void ApplyTheme(NodeCanvasTheme theme);
    }
}
