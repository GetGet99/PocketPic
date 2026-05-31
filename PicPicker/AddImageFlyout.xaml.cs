using DesktopFlyouts;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PicPicker;

[QuickMarkup("""
    using DesktopFlyouts;
    string NewImageName;
    ImageSource PreviewImage;

    <root IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=TopCenter !HideOnLostFocus PopupDirection=TopToBottom>
        <DesktopFlyoutIsland>
            <Grid ColumnDefinitions=<>
                <ColumnDefinition Width=Auto />
                <ColumnDefinition Width=`new GridLength(1, GridUnitType.Star)` />
            </> Margin=16 ColumnSpacing=16>
                <Image Grid.Column=0 Source=`PreviewImage` Width=200 Height=200 Stretch=Uniform />
                <StackPanel Grid.Column=1 Spacing=8 VerticalAlignment=Center>
                    <TextBlock Text="Save image from clipboard:" />
                    nameInput = <TextBox PlaceholderText="Image name" Text<=>`NewImageName` MinWidth=200 />
                    <Button Content="Save" @Click+=`SaveImage()` HorizontalAlignment=Right />
                </StackPanel>
            </Grid>
        </DesktopFlyoutIsland>
    </root>
    """)]
partial class AddImageFlyout : DesktopFlyout
{
    readonly byte[] _imageData;
    public event Action? Completed;

    public AddImageFlyout(byte[] imageData)
    {
        _imageData = imageData;
        InitializeComponent();
        Init();
        Loaded += (s, e) => nameInput.Focus(FocusState.Programmatic);
        LoadPreview();
    }

    async void LoadPreview()
    {
        try
        {
            var bitmap = new BitmapImage();
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(_imageData.AsBuffer());
            stream.Seek(0);
            await bitmap.SetSourceAsync(stream);
            PreviewImage = bitmap;
        }
        catch { }
    }

    async void SaveImage()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewImageName))
                return;

            var fileName = NewImageName.Trim() + ".png";
            var imageDirectory = (string)ApplicationData.Current.LocalSettings.Values["ImageDirectory"];
            var targetPath = System.IO.Path.Combine(imageDirectory, fileName);
            var counter = 1;
            while (File.Exists(targetPath))
            {
                targetPath = System.IO.Path.Combine(imageDirectory, $"{NewImageName.Trim()}_{counter}.png");
                counter++;
            }

            await File.WriteAllBytesAsync(targetPath, _imageData);

            Hide();
            Completed?.Invoke();
        }
        catch { }
    }
}
