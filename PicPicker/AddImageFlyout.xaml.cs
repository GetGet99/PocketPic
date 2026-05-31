using DesktopFlyouts;
using Microsoft.UI.Xaml.Automation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PicPicker;

[QuickMarkup("""
    using DesktopFlyouts;
    string NewImageName;
    string NewImageExtension;
    bool CanSave => `!string.IsNullOrWhiteSpace(NewImageName) && NewImageExtension is { Length: > 1 } && NewImageExtension.StartsWith('.')`;
    ImageSource PreviewImage;

    <root IsBackdropEnabled BackdropKind=DesktopAcrylic Placement=TopCenter !HideOnLostFocus PopupDirection=TopToBottom
        ActivationMode=NoActivateOnOpen // we will do this manually
    >
        <DesktopFlyoutIsland>
            <Grid ColumnDefinitions=<>
                <ColumnDefinition Width=Auto />
                <ColumnDefinition Width=`new GridLength(1, GridUnitType.Star)` />
            </> Margin=16 ColumnSpacing=16>
                <Image Grid.Column=0 Source=`PreviewImage` Width=200 Height=200 Stretch=Uniform />
                <StackPanel Grid.Column=1 Spacing=8 VerticalAlignment=Center>
                    <TextBlock Text="Save image from clipboard:" />
                    <StackPanel Orientation=Horizontal Spacing=4>
                        nameInput = <TextBox PlaceholderText="Image name" Text<=>`NewImageName` MinWidth=170 />
                        <TextBox PlaceholderText=".ext" Text<=>`NewImageExtension` Width=60 />
                    </StackPanel>
                    <StackPanel Orientation=Horizontal HorizontalAlignment=Right Spacing=8>
                        <Button Content="Cancel" @Click+=`Cancel()` />
                        <Button Content="Save" @Click+=`SaveImage()` IsEnabled=`CanSave` />
                    </StackPanel>
                </StackPanel>
            </Grid>
        </DesktopFlyoutIsland>
    </root>
    """)]
partial class AddImageFlyout : DesktopFlyout
{
    readonly byte[] _imageData;
    public event Action? Completed;

    public AddImageFlyout(byte[] imageData, string fileExtension = ".png")
    {
        _imageData = imageData;
        NewImageExtension = fileExtension;
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

    void Cancel()
    {
        Hide();
        Completed?.Invoke();
    }

    async void SaveImage()
    {
        try
        {
            var ext = NewImageExtension;
            if (string.IsNullOrWhiteSpace(NewImageName) || string.IsNullOrWhiteSpace(ext) || !ext.StartsWith('.'))
                return;

            var fileName = NewImageName.Trim() + ext;
            var imageDirectory = (string)ApplicationData.Current.LocalSettings.Values["ImageDirectory"];
            var targetPath = System.IO.Path.Combine(imageDirectory, fileName);
            var counter = 1;
            while (File.Exists(targetPath))
            {
                targetPath = System.IO.Path.Combine(imageDirectory, $"{NewImageName.Trim()}_{counter}{ext}");
                counter++;
            }

            await File.WriteAllBytesAsync(targetPath, _imageData);

            Hide();
            Completed?.Invoke();
        }
        catch { }
    }
}
