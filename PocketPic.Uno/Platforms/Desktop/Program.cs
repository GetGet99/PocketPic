using Uno.UI.Hosting;

namespace PocketPic.Uno;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if launched from startup to apply specific initialization adjustments
        bool launchedFromStartup = args.Contains("--startup");

        if (launchedFromStartup)
        {
            Thread.Sleep(5000);
        }
        // App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
