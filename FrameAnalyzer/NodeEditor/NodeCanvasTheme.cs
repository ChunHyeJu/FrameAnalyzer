
namespace FrameAnalyzer.NodeEditor
{
    /// <summary>
    /// 노드 에디터의 색상 테마 정보입니다.
    /// 다크/라이트 모드 전환 시 이 클래스의 색상값을 사용합니다.
    /// </summary>
    public class NodeCanvasTheme
    {
        public Color BackgroundColor { get; set; }
        public Color GridColor { get; set; }

        public Color NodeBodyColor { get; set; }
        public Color NodeTitleColor { get; set; }
        public Color NodeBorderColor { get; set; }
        public Color SelectedBorderColor { get; set; }

        public Color TextColor { get; set; }
        public Color PortTextColor { get; set; }

        public Color InputPortColor { get; set; }
        public Color OutputPortColor { get; set; }

        public Color ConnectionColor { get; set; }
        public Color SelectedConnectionColor { get; set; }
        public Color TempConnectionColor { get; set; }

        public static NodeCanvasTheme Dark => new()
        {
            BackgroundColor = Color.FromArgb(35, 35, 35),
            GridColor = Color.FromArgb(45, 45, 45),

            NodeBodyColor = Color.FromArgb(55, 55, 55),
            NodeTitleColor = Color.FromArgb(75, 75, 75),
            NodeBorderColor = Color.FromArgb(120, 120, 120),
            SelectedBorderColor = Color.DeepSkyBlue,

            TextColor = Color.White,
            PortTextColor = Color.Gainsboro,

            InputPortColor = Color.DeepSkyBlue,
            OutputPortColor = Color.Orange,

            ConnectionColor = Color.LightGreen,
            SelectedConnectionColor = Color.Yellow,
            TempConnectionColor = Color.Gray
        };

        public static NodeCanvasTheme Light => new()
        {
            BackgroundColor = Color.FromArgb(245, 245, 245),
            GridColor = Color.FromArgb(220, 220, 220),

            NodeBodyColor = Color.White,
            NodeTitleColor = Color.FromArgb(235, 235, 235),
            NodeBorderColor = Color.FromArgb(160, 160, 160),
            SelectedBorderColor = Color.DodgerBlue,

            TextColor = Color.Black,
            PortTextColor = Color.FromArgb(40, 40, 40),

            InputPortColor = Color.DodgerBlue,
            OutputPortColor = Color.DarkOrange,

            ConnectionColor = Color.ForestGreen,
            SelectedConnectionColor = Color.Red,
            TempConnectionColor = Color.Gray
        };
    }
}
