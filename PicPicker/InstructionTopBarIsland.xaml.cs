using DesktopFlyouts;

namespace PicPicker;

[QuickMarkup("""
    using DesktopFlyouts;
    <root IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=BottomRight !HideOnLostFocus PopupDirection=BottomToTop>
        <DesktopFlyoutIsland>
            <StackPanel Margin=24 Spacing=16 MaxWidth=400>
                <TextBlock Text="You're all set!" FontSize=20 TextAlignment=Center />
                <TextBlock Text="To open PicPicker, click the PicPicker icon in the bottom-right corner of your taskbar (system tray)." FontSize=13 TextWrapping=Wrap TextAlignment=Center />
                <Border CornerRadius=8>
                    <Image Source=`new BitmapImage(new Uri($"{Package.Current.InstalledLocation.Path}/Assets/trayicon.png"))` Stretch=Uniform MaxWidth=300 />
                </Border>
                <Button Content="Got it!" CenterH @Click+=`OnGotIt()` />
            </StackPanel>
        </DesktopFlyoutIsland>
    </root>
    """)]
partial class InstructionTopBarIsland : DesktopFlyout
{
    public event Action? Completed;

    public InstructionTopBarIsland()
    {
        InitializeComponent();
        Init();
    }

    void OnGotIt()
    {
        Completed?.Invoke();
    }
}
