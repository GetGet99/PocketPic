using System.Diagnostics;
#if HAS_UNO
using Uno.Extensions;
#endif
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
#if WINDOWS
            var streamRef = RandomAccessStreamReference.CreateFromFile(storageFile);
#endif

            var dataPackage = new DataPackage();
            var ext = System.IO.Path.GetExtension(path).ToLower();
#if WINDOWS
            dataPackage.SetBitmap(streamRef);
            dataPackage.SetStorageItems(new[] { storageFile });

            if (ext is ".png")
                dataPackage.SetData("PNG", streamRef);
            else if (ext is ".gif")
                dataPackage.SetData("GIF", streamRef);
#else
            // dataPackage.SetStorageItems(new[] { storageFile });
            var uri = new Uri(storageFile.Path).AbsoluteUri;
            var listBytes = System.Text.Encoding.UTF8.GetBytes(uri + "\r\n");
            dataPackage.SetData("text/uri-list", listBytes);
            
            var stream = await storageFile.OpenReadAsync();
            var bytes = await stream.ReadBytesAsync(CancellationToken.None);
            string? mimeType = ext switch
            {
                ".png"         => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif"         => "image/gif",
                ".bmp"         => "image/bmp",
                ".webp"        => "image/webp",
                ".svg"         => "image/svg+xml",
                ".tif" or ".tiff" => "image/tiff",
                ".ico"         => "image/x-icon",
                ".avif"        => "image/avif",
                ".heic"        => "image/heic",
                ".heif"        => "image/heif",
                _ => null
            };

            if (mimeType is not null)
            {
                dataPackage.SetData(mimeType, bytes);
            }
#endif
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
#if WINDOWS
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
#else
            LinuxClipboard.SetContent(dataPackage);
            LinuxClipboard.Flush();
#endif
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