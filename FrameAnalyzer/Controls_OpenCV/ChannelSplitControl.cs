using FrameAnalyzer.NodeEditor;
using OpenCvSharp;

namespace FrameAnalyzer.Controls_OpenCV;

public class ChannelSplitControl : OpenCvPreviewNodeControl
{
    private readonly ComboBox _channelComboBox = new();

    private readonly ChannelSplitProcessor _processor;

    private Mat? _bChannel;
    private Mat? _gChannel;
    private Mat? _rChannel;

    public ChannelSplitControl(ChannelSplitProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 240, settingsHeight: 28)
    {
        _processor = processor;

        _channelComboBox.Dock = DockStyle.Fill;
        _channelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _channelComboBox.Margin = Padding.Empty;
        _channelComboBox.Items.AddRange(new object[] { "B Channel", "G Channel", "R Channel" });
        _channelComboBox.SelectedIndex = 0;

        _channelComboBox.SelectedIndexChanged += (_, _) =>
        {
            UpdatePreview();
        };

        SettingsPanel.Controls.Add(_channelComboBox);

        _processor.ChannelsUpdated += OnChannelsUpdated;
    }

    private void OnChannelsUpdated(Mat? b, Mat? g, Mat? r)
    {
        _bChannel = b;
        _gChannel = g;
        _rChannel = r;

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        Mat? selected = GetSelectedChannel();
        string channelName = GetSelectedChannelName();

        SetPreviewImage(
            selected,
            selected == null || selected.Empty()
                ? "No Image"
                : $"{channelName} / {selected.Width} x {selected.Height} / {selected.Channels()}ch");
    }

    private Mat? GetSelectedChannel()
    {
        return _channelComboBox.SelectedIndex switch
        {
            0 => _bChannel,
            1 => _gChannel,
            2 => _rChannel,
            _ => _bChannel
        };
    }

    private string GetSelectedChannelName()
    {
        return _channelComboBox.SelectedIndex switch
        {
            0 => "B",
            1 => "G",
            2 => "R",
            _ => "B"
        };
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        _channelComboBox.BackColor = Color.White;
        _channelComboBox.ForeColor = Color.Black;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processor.ChannelsUpdated -= OnChannelsUpdated;
        }

        base.Dispose(disposing);
    }
}