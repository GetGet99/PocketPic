using DesktopFlyouts;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace PicPicker;

public partial class App : Application
{
    internal static IntPtr MainWindowHandle { get; private set; }

    private Window? _window;
    SystemTrayIcon systemTrayIcon = null!;
    TopBarIsland topBarIsland = null!;
    DesktopFlyout? activeFlyout;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        ReactiveInitializer.InitReactiveScheduler();

        _window = new Window();
        MainWindowHandle = WindowNative.GetWindowHandle(_window);

        systemTrayIcon = new(
            $"{Package.Current.InstalledLocation.Path}/Assets/icon.ico",
            "PicPicker",
            Guid.Parse("150b459a-c062-4db6-9750-6dce3ab19462")
        );
        systemTrayIcon.Show();
        systemTrayIcon.LeftClicked += OnSystemTrayLeftClicked;

        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ImageDirectory"))
        {
            topBarIsland = new();
            activeFlyout = topBarIsland;
        }
        else
        {
            var oobe = new OobeTopBarIsland();
            oobe.Completed += OnOobeCompleted;
            activeFlyout = oobe;
            void r(object sender, RoutedEventArgs e)
            {
                oobe.Loaded -= r;
                oobe.Show();
            }
            oobe.Loaded += r;
        }
    }

    void OnSystemTrayLeftClicked(object? sender, DesktopFlyouts.MouseEventReceivedEventArgs e)
    {
        if (activeFlyout is null) return;
        if (activeFlyout.IsOpen) activeFlyout.Hide();
        else activeFlyout.Show();
    }

    void OnOobeCompleted(string path)
    {
        ApplicationData.Current.LocalSettings.Values["ImageDirectory"] = path;
        activeFlyout?.Hide();
        topBarIsland = new();
        activeFlyout = topBarIsland;
        topBarIsland.Show();
    }
}
