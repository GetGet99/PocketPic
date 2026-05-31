using DesktopFlyouts;

namespace PicPicker;

[QuickMarkup("""
    using DesktopFlyouts;
    private bool IsOpenPrivate;
    <root IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=TopCenter
        !HideOnLostFocus PopupDirection=TopToBottom
        IsOpen=>`IsOpenPrivate`
    >
        <DesktopFlyoutIsland>
            gallery = <GalleryPage Margin=8 MaxWidth=650 MinWidth=600 MaxHeight=480 MinHeight=200 />
        </DesktopFlyoutIsland>
    </root>
    """)]
partial class TopBarIsland : DesktopFlyout
{
    public void ReloadImages() => gallery.ReloadImages();
    public TopBarIsland()
    {
        InitializeComponent();
        Init();
        gallery.Completed += () => Hide();
        gallery.HideParentRequested += () => Hide();
        gallery.ShowParentRequested += () => Show();
        IsOpenPrivateProp!.Watch(isOpen =>
        {
            if (isOpen)
                gallery.FocusTextBox();
        });
    }
}
