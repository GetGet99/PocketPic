using DesktopFlyouts;
using System.Diagnostics;
using Windows.Storage;
using WinRT.Interop;

namespace PocketPic;

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
            topBarIsland.MarkupNode.Hide();
            LaunchOOBE();
        };
        systemTrayIcon = new(
            $"{Package.Current.InstalledLocation.Path}/Assets/"+ 
#if WINDOWS
            "icon.ico"
#else
            "Assets/Icons/icon_foreground.svg"
#endif
            ,
            "PocketPic",
            "com.getget99.pocketpic"
            // Guid.Parse("150b459a-c062-4db6-9750-6dce3ab19462")
        );
        systemTrayIcon.Show();
        systemTrayIcon.LeftClicked += OnSystemTrayLeftClicked;
        systemTrayIcon.RightClicked += OnSystemTrayIconRightClicked;
        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ImageDirectory"))
        {
            topBarIsland = new();
            activeFlyout = topBarIsland.MarkupNode;
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
            trayFlyout.MarkupNode.Show(e.Point);
    }

    public void LaunchOOBE()
    {
        var oobe = new OobeTopBarIsland();
        instruction = new();
        oobe.Completed += OnOobeCompleted;
        activeFlyout = oobe.MarkupNode;
        void r(object sender, RoutedEventArgs e)
        {
            oobe.MarkupNode.Loaded -= r;
            oobe.MarkupNode.Show();
        }
        oobe.MarkupNode.Loaded += r;
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

        EnableStartup();
        instruction.Completed += () =>
        {
            instruction.MarkupNode.Hide();
            if (topBarIsland is null)
                topBarIsland = new();
            else
                topBarIsland.ReloadImages();
            activeFlyout = topBarIsland.MarkupNode;
            topBarIsland.MarkupNode.Show();
        };
        activeFlyout = instruction.MarkupNode;
        instruction.MarkupNode.Show();
    }
    async void EnableStartup()
    {
#if WINDOWS
        var startupTask = await StartupTask.GetAsync("PocketPicStartupTaskId"); // Pass the task ID you specified in the appxmanifest file
        switch (startupTask.State)
        {
            case StartupTaskState.Disabled:
                StartupTaskState newState = await startupTask.RequestEnableAsync();
                Debug.WriteLine("Request to enable startup, result = {0}", newState);
                break;
            case StartupTaskState.DisabledByUser:
            case StartupTaskState.DisabledByPolicy:
            case StartupTaskState.Enabled:
                break;
        }
#endif
    }
}
