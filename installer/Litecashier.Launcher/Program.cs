using Litecashier.Launcher;

ApplicationConfiguration.Initialize();

using var mutex = new Mutex(true, @"Global\Litecashier.Launcher", out var ownsMutex);
if (!ownsMutex)
{
    ServiceManager.OpenBrowser();
    return;
}

using var splash = new SplashForm();
splash.Show();
Application.DoEvents();

Exception? startupError = null;

var startupTask = Task.Run(async () =>
{
    try
    {
        await ServiceManager.EnsureRunningAsync(splash.SetStatus).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        startupError = ex;
    }
});

while (!startupTask.IsCompleted)
{
    Application.DoEvents();
    Thread.Sleep(50);
}

await startupTask.ConfigureAwait(true);

if (startupError != null)
{
    splash.ShowError(ServiceManager.BuildErrorMessage(startupError));
    return;
}

splash.SetStatus("جاري فتح النظام...");
ServiceManager.OpenBrowser();
splash.Close();
