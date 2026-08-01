namespace LiteRestaurant.Launcher;

internal sealed class SplashForm : Form
{
    private readonly System.Windows.Forms.Timer _pulseTimer;
    private string _statusText = "جاري تشغيل نظام المطاعم...";
    private int _pulsePhase;
    private Image? _logoImage;
    private bool _isError;

    private static readonly Color BgTop = Color.FromArgb(0, 18, 28);
    private static readonly Color BgBottom = Color.FromArgb(0, 37, 54);
    private static readonly Color Accent = Color.FromArgb(61, 180, 208);
    private static readonly Color AccentSoft = Color.FromArgb(55, 61, 180, 208);
    private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
    private static readonly Color TextMuted = Color.FromArgb(148, 190, 200);

    public SplashForm()
    {
        Text = "LiteRestaurant";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(580, 380);
        BackColor = BgTop;
        DoubleBuffered = true;
        ShowInTaskbar = true;
        TopMost = true;
        // Do not enable RightToLeftLayout — it mirrors DrawImage (logo reads backwards).
        RightToLeft = RightToLeft.No;
        RightToLeftLayout = false;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        _logoImage = LoadEmbeddedLogo();
        ApplyRoundedRegion(20);

        _pulseTimer = new System.Windows.Forms.Timer { Interval = 24 };
        _pulseTimer.Tick += (_, _) =>
        {
            _pulsePhase = (_pulsePhase + 5) % 1000;
            Invalidate(new Rectangle(40, ClientSize.Height - 78, ClientSize.Width - 80, 18));
        };
        _pulseTimer.Start();

        Resize += (_, _) => ApplyRoundedRegion(20);
    }

    public void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(message));
            return;
        }

        _statusText = message;
        Invalidate();
        Application.DoEvents();
    }

    public void ShowError(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => ShowError(message));
            return;
        }

        _isError = true;
        _pulseTimer.Stop();
        _statusText = "تعذّر التشغيل";
        Invalidate();
        MessageBox.Show(message, "LiteRestaurant", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                   ClientRectangle, BgTop, BgBottom, 90F))
        {
            g.FillRectangle(brush, ClientRectangle);
        }

        // Soft teal glow behind logo
        using (var glow = new System.Drawing.Drawing2D.GraphicsPath())
        {
            glow.AddEllipse(ClientSize.Width / 2 - 160, 20, 320, 180);
            using var glowBrush = new System.Drawing.Drawing2D.PathGradientBrush(glow)
            {
                CenterColor = Color.FromArgb(55, Accent),
                SurroundColors = [Color.FromArgb(0, Accent)]
            };
            g.FillPath(glowBrush, glow);
        }

        if (_logoImage != null)
        {
            const int maxLogoW = 480;
            const int maxLogoH = 175;
            var scale = Math.Min(
                maxLogoW / (float)_logoImage.Width,
                maxLogoH / (float)_logoImage.Height);
            var lw = (int)(_logoImage.Width * scale);
            var lh = (int)(_logoImage.Height * scale);
            var lx = (ClientSize.Width - lw) / 2;
            var ly = 36;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(_logoImage, new Rectangle(lx, ly, lw, lh));
        }

        using var productFont = CreateFont(12.5F, FontStyle.Bold);
        using var statusFont = CreateFont(13.5F, FontStyle.Regular);
        using var footerFont = CreateFont(8.75F, FontStyle.Regular);

        var productRect = new Rectangle(24, 220, ClientSize.Width - 48, 30);
        TextRenderer.DrawText(
            g,
            "LiteRestaurant  ·  نظام المطاعم",
            productFont,
            productRect,
            Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var statusColor = _isError ? Color.FromArgb(254, 202, 202) : TextPrimary;
        var statusRect = new Rectangle(24, 252, ClientSize.Width - 48, 34);
        TextRenderer.DrawText(
            g,
            _statusText,
            statusFont,
            statusRect,
            statusColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // Progress track
        var track = new Rectangle(56, ClientSize.Height - 70, ClientSize.Width - 112, 7);
        using (var trackBrush = new SolidBrush(Color.FromArgb(50, Accent)))
        using (var trackPath = RoundedRect(track, 4))
        {
            g.FillPath(trackBrush, trackPath);
        }

        var fillW = 92;
        var travel = Math.Max(track.Width - fillW, 1);
        float t = (_pulsePhase % 200) / 200f;
        // ease back-and-forth
        float ping = t < 0.5f ? t * 2f : (1f - t) * 2f;
        var fillX = track.X + (int)(ping * travel);
        var fillRect = new Rectangle(fillX, track.Y, fillW, track.Height);
        using (var fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                   fillRect,
                   Color.FromArgb(120, Accent),
                   Accent,
                   0F))
        using (var fillPath = RoundedRect(fillRect, 4))
        {
            g.FillPath(fillBrush, fillPath);
        }

        var footerRect = new Rectangle(24, ClientSize.Height - 48, ClientSize.Width - 48, 24);
        TextRenderer.DrawText(
            g,
            "LIGHT CASHIER  ·  إعداد الخدمات المحلية",
            footerFont,
            footerRect,
            TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        using var borderPen = new Pen(AccentSoft, 1.6F);
        using var borderPath = RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), 18);
        g.DrawPath(borderPen, borderPath);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _pulseTimer.Stop();
        _pulseTimer.Dispose();
        _logoImage?.Dispose();
        base.OnFormClosed(e);
    }

    private void ApplyRoundedRegion(int radius)
    {
        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), radius);
        Region = new Region(path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(radius * 2, 1);
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Font CreateFont(float size, FontStyle style)
    {
        string[] candidates =
        [
            "Segoe UI Variable Display",
            "Segoe UI Semibold",
            "Segoe UI",
            "Tahoma"
        ];

        foreach (var name in candidates)
        {
            try
            {
                var font = new Font(name, size, style, GraphicsUnit.Point);
                if (!font.FontFamily.IsStyleAvailable(style))
                {
                    font.Dispose();
                    font = new Font(name, size, FontStyle.Regular, GraphicsUnit.Point);
                }

                return font;
            }
            catch
            {
                // try next family
            }
        }

        return new Font(SystemFonts.MessageBoxFont!.FontFamily, size, style, GraphicsUnit.Point);
    }

    private static Image? LoadEmbeddedLogo()
    {
        var asm = typeof(SplashForm).Assembly;
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("splash-logo.png", StringComparison.OrdinalIgnoreCase));
        if (resource == null)
        {
            return null;
        }

        using var stream = asm.GetManifestResourceStream(resource);
        if (stream == null)
        {
            return null;
        }

        using var temp = Image.FromStream(stream);
        return new Bitmap(temp);
    }
}
