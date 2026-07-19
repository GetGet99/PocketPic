using DesktopFlyouts;

namespace PocketPic;

[QuickMarkup("""
    using DesktopFlyouts;
    <root>
        <DesktopMenuFlyout>
            <MenuFlyoutItem Text="Reset" Icon=<SymbolIcon Symbol=Refresh /> @Click+=`Reset?.Invoke()` />
            <MenuFlyoutItem Text="Exit" Icon=<SymbolIcon Symbol=Cancel /> @Click+=`Environment.Exit(0)` />
        </DesktopMenuFlyout>
    </root>
    """)]
partial class TrayFlyout : IQuickMarkupComponent<DesktopMenuFlyout>
{
    public event Action? Reset;
}
