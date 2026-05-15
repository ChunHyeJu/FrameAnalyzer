using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Size = System.Drawing.Size;

namespace FrameAnalyzer;

/// <summary>
/// OpenCV 노드에서 공통으로 사용하는 미리보기 컨트롤입니다.
/// 설정 영역, 이미지 미리보기, 상태 표시를 공통 양식으로 제공합니다.
/// </summary>
public abstract class OpenCvPreviewNodeControl : UserControl, INodeThemeAware, INodeRuntimeAware
{
    #region Fields

    private string _lastStatusText = "No Image";
    private double _processingTimeMs;
    private bool _previewEnabled = true;
    private bool _showProcessingTime = true;

    #endregion

    #region Protected Controls

    /// <summary>
    /// 전체 레이아웃입니다.
    /// 0행: 설정 영역, 1행: 미리보기, 2행: 상태 표시
    /// </summary>
    protected readonly TableLayoutPanel RootLayout = new();

    /// <summary>
    /// 노드별 설정 컨트롤을 넣는 영역입니다.
    /// </summary>
    protected readonly Panel SettingsPanel = new();

    /// <summary>
    /// Mat 이미지를 Bitmap으로 변환해서 표시하는 영역입니다.
    /// </summary>
    protected readonly PictureBox PreviewBox = new();

    /// <summary>
    /// 이미지 정보, 처리 시간 등을 표시하는 상태 라벨입니다.
    /// </summary>
    protected readonly Label StatusLabel = new();

    #endregion

    #region Constructor

    protected OpenCvPreviewNodeControl(
        int defaultWidth = 240,
        int defaultHeight = 220,
        int settingsHeight = 0)
    {
        Size = new Size(defaultWidth, defaultHeight);
        MinimumSize = new Size(180, 140);

        Margin = Padding.Empty;
        Padding = Padding.Empty;

        InitializeRootLayout(settingsHeight);
        InitializeSettingsPanel(settingsHeight);
        InitializePreviewBox(settingsHeight);
        InitializeStatusLabel();

        RootLayout.Controls.Add(SettingsPanel, 0, 0);
        RootLayout.Controls.Add(PreviewBox, 0, 1);
        RootLayout.Controls.Add(StatusLabel, 0, 2);

        Controls.Add(RootLayout);
    }

    #endregion

    #region Runtime Options

    /// <summary>
    /// 미리보기 표시 여부입니다.
    /// 끄면 Bitmap 변환을 하지 않아서 처리 시간 측정에 유리합니다.
    /// </summary>
    public bool PreviewEnabled
    {
        get => _previewEnabled;
        set
        {
            _previewEnabled = value;

            if (!_previewEnabled)
            {
                ClearPreviewImage();
                SetStatusTextInternal(FormatStatusText(_lastStatusText));
            }
        }
    }

    /// <summary>
    /// 상태 라벨에 처리 시간을 표시할지 여부입니다.
    /// </summary>
    public bool ShowProcessingTime
    {
        get => _showProcessingTime;
        set
        {
            _showProcessingTime = value;
            SetStatusTextInternal(FormatStatusText(_lastStatusText));
        }
    }

    /// <summary>
    /// 노드 실행 시간을 갱신합니다.
    /// </summary>
    public void SetProcessingTime(double milliseconds)
    {
        _processingTimeMs = milliseconds;
        SetStatusTextInternal(FormatStatusText(_lastStatusText));
    }

    #endregion

    #region Initialize Layout

    private void InitializeRootLayout(int settingsHeight)
    {
        RootLayout.Dock = DockStyle.Fill;
        RootLayout.ColumnCount = 1;
        RootLayout.RowCount = 3;
        RootLayout.Padding = new Padding(8);

        RootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        RootLayout.RowStyles.Add(
            settingsHeight > 0
                ? new RowStyle(SizeType.Absolute, settingsHeight)
                : new RowStyle(SizeType.Absolute, 0));

        RootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        RootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
    }

    private void InitializeSettingsPanel(int settingsHeight)
    {
        SettingsPanel.Dock = DockStyle.Fill;
        SettingsPanel.Margin = Padding.Empty;
        SettingsPanel.Visible = settingsHeight > 0;
    }

    private void InitializePreviewBox(int settingsHeight)
    {
        PreviewBox.Dock = DockStyle.Fill;
        PreviewBox.Margin = new Padding(0, settingsHeight > 0 ? 6 : 0, 0, 6);
        PreviewBox.SizeMode = PictureBoxSizeMode.Zoom;
        PreviewBox.BorderStyle = BorderStyle.FixedSingle;
        PreviewBox.BackColor = Color.FromArgb(25, 25, 25);
    }

    private void InitializeStatusLabel()
    {
        StatusLabel.Dock = DockStyle.Fill;
        StatusLabel.Margin = Padding.Empty;
        StatusLabel.AutoEllipsis = true;
        StatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        StatusLabel.Text = "No Image";
    }

    #endregion

    #region Preview / Status

    /// <summary>
    /// Mat 이미지를 미리보기로 표시합니다.
    /// </summary>
    protected void SetPreviewImage(Mat? image, string? statusText = null)
    {
        if (InvokeRequired)
        {
            Mat? imageCopy = null;

            if (image != null && !image.Empty())
                imageCopy = image.Clone();

            BeginInvoke(new Action(() =>
            {
                try
                {
                    SetPreviewImage(imageCopy, statusText);
                }
                finally
                {
                    imageCopy?.Dispose();
                }
            }));

            return;
        }

        if (image == null || image.Empty())
        {
            _lastStatusText = "No Image";
            ClearPreviewImage();
            SetStatusTextInternal(FormatStatusText(_lastStatusText));
            return;
        }

        _lastStatusText = statusText ?? CreateDefaultStatusText(image);

        if (!PreviewEnabled)
        {
            ClearPreviewImage();
            SetStatusTextInternal(FormatStatusText(_lastStatusText));
            return;
        }

        ClearPreviewImage();

        using Mat displayMat = CreateDisplayMat(image);

        PreviewBox.Image = BitmapConverter.ToBitmap(displayMat);
        SetStatusTextInternal(FormatStatusText(_lastStatusText));
    }

    /// <summary>
    /// 상태 라벨만 갱신합니다.
    /// </summary>
    protected void SetStatusText(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatusText(text)));
            return;
        }

        _lastStatusText = text;
        SetStatusTextInternal(FormatStatusText(_lastStatusText));
    }

    /// <summary>
    /// 기본 상태 문자열을 생성합니다.
    /// </summary>
    protected static string CreateDefaultStatusText(Mat image)
    {
        return $"{image.Width} x {image.Height} / {image.Channels()}ch";
    }

    private void SetStatusTextInternal(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatusTextInternal(text)));
            return;
        }

        StatusLabel.Text = text;
    }

    private string FormatStatusText(string baseText)
    {
        if (!ShowProcessingTime)
            return baseText;

        if (baseText == "No Image")
            return baseText;

        return $"{baseText} / {_processingTimeMs:F2} ms";
    }

    private void ClearPreviewImage()
    {
        Image? oldImage = PreviewBox.Image;
        PreviewBox.Image = null;
        oldImage?.Dispose();
    }

    #endregion

    #region Mat Conversion

    /// <summary>
    /// PictureBox에 표시 가능한 8bit Mat으로 변환합니다.
    /// </summary>
    protected static Mat CreateDisplayMat(Mat source)
    {
        Mat display = new();

        if (source.Depth() == MatType.CV_8U)
        {
            source.CopyTo(display);
            return display;
        }

        Cv2.Normalize(
            source,
            display,
            0,
            255,
            NormTypes.MinMax,
            MatType.CV_8U);

        return display;
    }

    #endregion

    #region Theme

    /// <summary>
    /// 노드 테마를 적용합니다.
    /// </summary>
    public virtual void ApplyTheme(NodeCanvasTheme theme)
    {
        BackColor = theme.NodeBodyColor;
        ForeColor = theme.TextColor;

        RootLayout.BackColor = theme.NodeBodyColor;
        SettingsPanel.BackColor = theme.NodeBodyColor;

        StatusLabel.ForeColor = theme.TextColor;
        StatusLabel.BackColor = theme.NodeBodyColor;

        PreviewBox.BackColor = Color.FromArgb(25, 25, 25);
    }

    #endregion

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearPreviewImage();
        }

        base.Dispose(disposing);
    }

    #endregion
}