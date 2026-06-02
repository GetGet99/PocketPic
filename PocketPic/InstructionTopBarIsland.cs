using DesktopFlyouts;

namespace PocketPic;

[QuickMarkup("""
    using DesktopFlyouts;
    <root>
        <DesktopFlyout IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=BottomRight !HideOnLostFocus PopupDirection=BottomToTop>
            <DesktopFlyoutIsland>
                <StackPanel Margin=24 Spacing=16 MaxWidth=400>
                    <TextBlock Text="You're all set!" FontSize=20 TextAlignment=Center />
                    <TextBlock Text="To open PocketPic, click the PocketPic icon in the bottom-right corner of your taskbar (system tray)." FontSize=13 TextWrapping=Wrap TextAlignment=Center />
                    <Border CornerRadius=8>
                        <Image Source=`new BitmapImage(new Uri($"{Package.Current.InstalledLocation.Path}/Assets/trayicon.png"))` Stretch=Uniform MaxWidth=300 />
                    </Border>
                    <Button Content="Got it!" CenterH @Click+=`OnGotIt()` />
                </StackPanel>
            </DesktopFlyoutIsland>
        </DesktopFlyout>
    </root>
    """)]
partial class InstructionTopBarIsland : IQuickMarkupComponent<DesktopFlyout>
{
    public event Action? Completed;

    void OnGotIt()
    {
        Completed?.Invoke();
    }
}
