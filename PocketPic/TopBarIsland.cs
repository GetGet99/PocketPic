using DesktopFlyouts;

namespace PocketPic;

[QuickMarkup("""
    using DesktopFlyouts;
    private bool IsOpenPrivate;
    <root>
        <DesktopFlyout
            IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=TopCenter
            !HideOnLostFocus PopupDirection=TopToBottom
            IsOpen=>`IsOpenPrivate`>
            <DesktopFlyoutIsland>
                gallery = <GalleryPage Margin=8 MaxWidth=650 MinWidth=600 MaxHeight=480 MinHeight=200 />
            </DesktopFlyoutIsland>
        </DesktopFlyout>
    </root>
    """)]
partial class TopBarIsland : IQuickMarkupComponent<DesktopFlyout>
{
    public void ReloadImages() => gallery.ReloadImages();
    public TopBarIsland()
    {
        //InitializeComponent();
        Init();
        gallery.Completed += () => MarkupNode.Hide();
        gallery.HideParentRequested += () => MarkupNode.Hide();
        gallery.ShowParentRequested += () => MarkupNode.Show();
        IsOpenPrivateProp!.Watch(isOpen =>
        {
            if (isOpen)
                gallery.FocusTextBox();
        });
    }
}
