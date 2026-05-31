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
    InstructionTopBarIsland instruction = null!;
    DesktopFlyout? activeFlyout;
    TrayFlyout trayFlyout = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        ReactiveInitializer.InitReactiveScheduler();

        _window = new Window();
        MainWindowHandle = WindowNative.GetWindowHandle(_window);
        trayFlyout = new();
        trayFlyout.Reset += delegate
        {
            topBarIsland.Hide();
            LaunchOOBE();
        };
        systemTrayIcon = new(
            $"{Package.Current.InstalledLocation.Path}/Assets/icon.ico",
            "PicPicker",
            Guid.Parse("150b459a-c062-4db6-9750-6dce3ab19462")
        );
        systemTrayIcon.Show();
        systemTrayIcon.LeftClicked += OnSystemTrayLeftClicked;
        systemTrayIcon.RightClicked += OnSystemTrayIconRightClicked;
        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ImageDirectory"))
        {
            topBarIsland = new();
            activeFlyout = topBarIsland;
        }
        else
        {
            LaunchOOBE();
        }
    }

    private void OnSystemTrayIconRightClicked(object? sender, MouseEventReceivedEventArgs e)
    {
        // only allow right click this after completion
        if (topBarIsland is not null)
            trayFlyout.Show(e.Point);
    }

    public void LaunchOOBE()
    {
        var oobe = new OobeTopBarIsland();
        instruction = new();
        oobe.Completed += OnOobeCompleted;
        activeFlyout = oobe;
        void r(object sender, RoutedEventArgs e)
        {
            oobe.Loaded -= r;
            oobe.Show();
        }
        oobe.Loaded += r;
    }

    void OnSystemTrayLeftClicked(object? sender, DesktopFlyouts.MouseEventReceivedEventArgs e)
    {
        if (activeFlyout is null) return;
        if (activeFlyout.IsOpen) activeFlyout.Hide();
        else activeFlyout.Show();
    }

    public void OnAppActivated()
    {
        activeFlyout?.DispatcherQueue.TryEnqueue(() => activeFlyout?.Show());
    }

    void OnOobeCompleted(string path)
    {
        ApplicationData.Current.LocalSettings.Values["ImageDirectory"] = path;
        activeFlyout?.Hide();

        instruction.Completed += () =>
        {
            instruction.Hide();
            if (topBarIsland is null)
                topBarIsland = new();
            else
                topBarIsland.ReloadImages();
            activeFlyout = topBarIsland;
            topBarIsland.Show();
        };
        activeFlyout = instruction;
        instruction.Show();
    }
}
