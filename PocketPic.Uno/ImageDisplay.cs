using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PocketPic;

[QuickMarkup("""
    using Windows.UI;
    using Microsoft.UI;
    string ImagePath = "";
    private Color ComputedTintColor => `(ThemeBrushes.Global.LayerFill as SolidColorBrush)?.Color ?? Colors.Red`;
    private ImageSource? ImageSource => `
        ImagePath is "" ? null :
        new BitmapImage(new Uri(ImagePath, UriKind.Absolute)) { DecodePixelWidth = 200 }`;
    private string FileName => `ImagePath is "" ? "" : System.IO.Path.GetFileName(ImagePath)`;
    <setup>
        var theme = ThemeBrushes.Global;
    </setup>
    <root>
        <Button
            Margin=4
            BorderBrush=`theme.CardStroke`
            BorderThickness=1
            Width=200 MinHeight=150
            @Click+=`CopyImageToClipboard(ImagePath)`
            Padding=0
            ContextFlyout=<Flyout Placement=RightEdgeAlignedTop>
                <ScrollViewer>
                    <VStack Spacing=8>
                        <Image Source=`ImageSource` Width=200 />
                        <TextBlock Text=`FileName` MaxWidth=200 TextWrapping=WrapWholeWords />
                        <VStack XYFocusKeyboardNavigation=Enabled>
                            <ImageDisplayMenuItem
                                Icon=Copy Text="Copy"
                                @Click+=`CopyImageToClipboard(ImagePath)`
                            />
                            <ImageDisplayMenuItem
                                Icon=Delete Text="Delete"
                                CustomForeground=`theme.SystemCritical`
                                @Click+=`DeleteRequest?.Invoke()`
                            />
                        </VStack>
                    </VStack>
                </ScrollViewer>
            </Flyout>
            AutomationProperties.Name="ImagePath"
            AutomationProperties.AccessibilityView=Raw
            VerticalContentAlignment=Stretch
        >
            <Grid>
                <Image Source=`ImageSource`
                    Stretch=UniformToFill
                />
                <Border Bottom Padding=4
                    Background=<AcrylicBrush
                        TintOpacity=0.5
                        TintLuminosityOpacity=0.5
                        TintColor=`ComputedTintColor`
                        FallbackColor=`ComputedTintColor`
                    />
                >
                    <TextBlock Text=`FileName` Right
                        Foreground=`theme.PrimaryText`
                        TextTrimming=WordEllipsis
                    />
                </Border>
            </Grid>
        </Button>
    </root>
    """)]
partial class ImageDisplay : IQuickMarkupComponent
{
    public event Action? Completed;
    public event Action? DeleteRequest;
    async void CopyImageToClipboard(string path)
    {
        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(path);
            var streamRef = RandomAccessStreamReference.CreateFromFile(storageFile);

            var dataPackage = new DataPackage();
            dataPackage.SetBitmap(streamRef);
            dataPackage.SetStorageItems(new[] { storageFile });

            var ext = System.IO.Path.GetExtension(path).ToLower();
            if (ext is ".png")
                dataPackage.SetData("PNG", streamRef);
            else if (ext is ".gif")
                dataPackage.SetData("GIF", streamRef);

            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
            Completed?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
[QuickMarkup("""
    using Microsoft.UI;
    Symbol Icon;
    string Text = "";
    Brush? CustomForeground = null;
    <setup>
        var theme = ThemeBrushes.Global;
        var transparent = new SolidColorBrush(Colors.Transparent);
    </setup>
    <root>
        btn = <Button StretchH HorizontalContentAlignment=Stretch
            Background=`transparent`
            BorderThickness=0
        >
            <HStack Spacing=8>
                <SymbolIcon Symbol=`Icon` CenterV />
                <TextBlock Text=`Text` CenterV />
            </HStack>
        </Button>
    </root>
    """)]
partial class ImageDisplayMenuItem : IQuickMarkupComponent<Button>
{
    public ImageDisplayMenuItem()
    {
        Init();
        CustomForegroundProp!.Watch((b) =>
        {
            btn.Foreground = b;
            btn.Resources["ButtonForegroundPointerOver"] = b;
            btn.Resources["ButtonForegroundPressed"] = b;
        });
    }
}