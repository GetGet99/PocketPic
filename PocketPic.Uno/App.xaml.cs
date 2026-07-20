using DesktopFlyouts;
using System.Diagnostics;
using Windows.Storage;
using WinRT.Interop;
using Path = System.IO.Path;

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
            "icon_foreground.svg.icon"
#endif
            ,
            "PocketPic",
#if WINDOWS
            Guid.Parse("150b459a-c062-4db6-9750-6dce3ab19462")
#else
            "com.getget99.pocketpic"
#endif
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
#else
        try
        {
            // 1. Locate the XDG autostart directory (~/.config/autostart)
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string autostartDir = Path.Combine(homeDir, ".config", "autostart");
            
            if (!Directory.Exists(autostartDir))
            {
                Directory.CreateDirectory(autostartDir);
            }

            string desktopFilePath = Path.Combine(autostartDir, "PocketPic.desktop");

            // 2. Resolve paths dynamically based on runtime context
            string execPath;
            
            // Check if running as a self-contained single file binary or via dotnet muxer
            string? currentProcess = Environment.ProcessPath; 
            
            if (Path.GetFileName(currentProcess) == "dotnet")
            {
                // Fallback for execution via 'dotnet App.dll' during debug/development
                string dllPath = AppContext.BaseDirectory;
                string dllName = "PocketPic.Uno.dll";
                execPath = $"dotnet {Path.Combine(dllPath, dllName)} --startup";
            }
            else
            {
                // Production execution path (Self-contained binary, Flatpak, or Snap run)
                execPath = $"\"{currentProcess}\" --startup";
            }

            // Dynamically locate your icon assets folder path relative to the binary
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon_foreground.png");

            // 3. Draft the sanitized XDG standard config payload
            string desktopContent = $"""
            [Desktop Entry]
            Type=Application
            Name=PocketPic
            GenericName=Top of screen picture picker
            Comment=Launches PocketPic on system boot
            Exec={execPath}
            Icon={iconPath}
            Terminal=false
            StartupNotify=true
            X-GNOME-Autostart-enabled=true
            """;

            // 4. Safely commit payload changes asynchronously to local disk storage
            await File.WriteAllTextAsync(desktopFilePath, desktopContent);
            Debug.WriteLine("Linux autostart shortcut registered successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write Linux startup entry: {ex.Message}");
        }
#endif
    }
}
