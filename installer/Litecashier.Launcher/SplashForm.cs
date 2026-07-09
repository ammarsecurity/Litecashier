namespace Litecashier.Launcher;

internal sealed class SplashForm : Form
{
    private readonly Label _statusLabel;

    public SplashForm()
    {
        Text = "Litecashier";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 140);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;

        _statusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11F),
            Text = "جاري تشغيل النظام..."
        };

        Controls.Add(_statusLabel);
    }

    public void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(message));
            return;
        }

        _statusLabel.Text = message;
        Application.DoEvents();
    }

    public void ShowError(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => ShowError(message));
            return;
        }

        MessageBox.Show(message, "Litecashier", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Close();
    }
}
