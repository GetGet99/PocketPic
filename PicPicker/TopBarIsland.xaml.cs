using DesktopFlyouts;

namespace PicPicker;

[QuickMarkup("""
    using DesktopFlyouts;
    <root IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=TopCenter !HideOnLostFocus PopupDirection=TopToBottom>
        <DesktopFlyoutIsland>
            gallery = <GalleryPage Margin=8 MaxWidth=650 MaxHeight=480 />
        </DesktopFlyoutIsland>
    </root>
    """)]
partial class TopBarIsland : DesktopFlyout
{
    public TopBarIsland()
    {
        InitializeComponent();
        Init();
        gallery.HideParentRequested += () => Hide();
        gallery.ShowParentRequested += () => Show();
    }
}
