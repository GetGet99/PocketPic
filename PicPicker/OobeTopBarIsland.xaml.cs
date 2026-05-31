using DesktopFlyouts;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PicPicker;

[QuickMarkup("""
    using DesktopFlyouts;
    string SelectedFolder;
    bool HasFolder;

    <root IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=TopCenter !HideOnLostFocus PopupDirection=TopToBottom>
        <DesktopFlyoutIsland>
            <StackPanel Margin=24 Spacing=12 MaxWidth=400>
                <TextBlock Text="Welcome to PicPicker" FontSize=20 TextAlignment=Center />
                <TextBlock Text="Select a folder to store your images." FontSize=13 TextWrapping=Wrap TextAlignment=Center />

                <Button Content="Choose Folder" CenterH
                    @Click+=`SelectFolderAsync()` />

                <TextBlock Text<=>`SelectedFolder` FontSize=12 TextWrapping=Wrap CenterH TextAlignment=Center />

                <Button Content="Get Started" IsEnabled<=>`HasFolder` CenterH
                    @Click+=`OnGetStarted()` />
            </StackPanel>
        </DesktopFlyoutIsland>
    </root>
    """)]
partial class OobeTopBarIsland : DesktopFlyout
{
    public event Action<string>? Completed;

    public OobeTopBarIsland()
    {
        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ImageDirectory"))
        {
            SelectedFolder = (string)ApplicationData.Current.LocalSettings.Values["ImageDirectory"];
        }
        InitializeComponent();
        Init();
    }

    async void SelectFolderAsync()
    {
        Hide();

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, App.MainWindowHandle);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            SelectedFolder = folder.Path;
            HasFolder = true;
        }

        Show();
    }

    void OnGetStarted()
    {
        Completed?.Invoke(SelectedFolder);
    }
}
