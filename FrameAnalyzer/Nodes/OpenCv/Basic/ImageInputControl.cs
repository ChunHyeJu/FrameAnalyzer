using FrameAnalyzer.NodeEditor;
using OpenCvSharp;
using FrameAnalyzer.Nodes.OpenCv;

namespace FrameAnalyzer.Nodes.OpenCv.Basic;

public class ImageInputControl : OpenCvPreviewNodeControl
{
    private readonly TextBox _textBox = new();

    private readonly ImageInputProcessor _processor;

    public ImageInputControl(ImageInputProcessor processor)
        : base(defaultWidth: 240, defaultHeight: 220, settingsHeight: 28)
    {
        _processor = processor;

        _textBox.Dock = DockStyle.Fill;
        _textBox.ReadOnly = true;
        _textBox.Cursor = Cursors.Hand;
        _textBox.PlaceholderText = "이미지 선택...";
        _textBox.Margin = Padding.Empty;
        _textBox.Click += (_, _) => SelectImage();

        SettingsPanel.Controls.Add(_textBox);
    }

    private void SelectImage()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "이미지 파일 선택",
            Filter =
                "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|" +
                "BMP Files|*.bmp|" +
                "JPEG Files|*.jpg;*.jpeg|" +
                "PNG Files|*.png|" +
                "TIFF Files|*.tif;*.tiff|" +
                "All Files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            _processor.LoadImage(dialog.FileName);

            _textBox.Text = Path.GetFileName(dialog.FileName);

            Mat? image = _processor.Image;

            SetPreviewImage(
                image,
                image == null || image.Empty()
                    ? "No Image"
                    : $"{image.Width} x {image.Height} / {image.Channels()}ch");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "이미지 로드 실패",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public override void ApplyTheme(NodeCanvasTheme theme)
    {
        base.ApplyTheme(theme);

        _textBox.BackColor = Color.White;
        _textBox.ForeColor = Color.Black;
    }
}