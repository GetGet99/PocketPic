using DesktopFlyouts;

namespace PicPicker;

[QuickMarkup("""
    <root>
        <MenuFlyoutItem Text="Reset" Icon=<SymbolIcon Symbol=Refresh /> @Click+=`Reset?.Invoke()` />
        <MenuFlyoutItem Text="Exit" Icon=<SymbolIcon Symbol=Cancel /> @Click+=`Environment.Exit(0)` />
    </root>
    """)]
partial class TrayFlyout : DesktopMenuFlyout
{
    public event Action? Reset;
}
