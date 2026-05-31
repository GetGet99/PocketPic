using DesktopFlyouts;
using Windows.Graphics;

namespace PicPicker;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        ReactiveInitializer.InitReactiveScheduler();
        systemTrayIcon = new(
            $"{Package.Current.InstalledLocation.Path}/Assets/icon.ico",
            "PicPicker",
            Guid.Parse("150b459a-c062-4db6-9750-6dce3ab19462")
        );
        systemTrayIcon.Show();
        topBarIsland = new();
        systemTrayIcon.LeftClicked += delegate
        {
            if (topBarIsland.IsOpen)
                topBarIsland.Hide();
            else
                topBarIsland.Show();
        };

    }
    SystemTrayIcon systemTrayIcon = null!;
    TopBarIsland topBarIsland = null!;
}
