using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Path = System.IO.Path;

namespace PocketPic;

[QuickMarkup("""
    bool IsTeachingTipOpen = false;

    string SearchQuery;

    <setup>
        var theme = ThemeBrushes.Global;
        LoadImages();
    </setup>
    <root>
        <Grid RowDefinitions=<>
            <RowDefinition Height=`GridLength.Auto` />
            <RowDefinition />
        </> Padding=12>
            <Grid Grid.Row=0 ColumnSpacing=8 ColumnDefinitions=<>
                <ColumnDefinition />
                <ColumnDefinition Width=Auto />
            </>>
                searchTb = <TextBox PlaceholderText="Search images..." MinWidth=300
                    Text<=>`SearchQuery`
                    @TextChanged+=`ApplyFilter()`
                />
                addButton = <Button Grid.Column=1 Content=<SymbolIcon(Add) />
                    Padding=5 @Click+=`OnAddImageClick()` />
            </Grid>
            
            <ScrollViewer Grid.Row=1>
                <VariableSizedWrapGrid Orientation=Horizontal XYFocusKeyboardNavigation=Enabled>
                    foreach (var imagePath in `FilteredImages`; `imagePath`) {
                        <ImageDisplay ImagePath=`imagePath` @Completed+=`Completed?.Invoke()` @DeleteRequest+=`Delete(imagePath)` />
                    }
                </VariableSizedWrapGrid>
            </ScrollViewer>

            teachingTip = <TeachingTip Target=`addButton`
                Title="Tip"
                Subtitle="To add images, copy them to your clipboard first, then click +"
                PreferredPlacement=Bottom
                IsOpen=`IsTeachingTipOpen`
                @CloseButtonClick+=`IsTeachingTipOpen = false`
                />
            
        </Grid>
    </root>
    """)]
public partial class GalleryPage : Grid
{
    public event Action? Completed;
    public event Action? HideParentRequested;
    public event Action? ShowParentRequested;
    ObservableCollection<string> ImageFiles = new();
    ObservableCollection<string> FilteredImages = new();
    string ImageDirectory => (string)ApplicationData.Current.LocalSettings.Values["ImageDirectory"];

    public void ReloadImages() => LoadImages();

    void LoadImages()
    {
        ImageFiles.Clear();
        if (!Directory.Exists(ImageDirectory))
            Directory.CreateDirectory(ImageDirectory);
        foreach (var file in Directory.GetFiles(ImageDirectory))
        {
            var ext = Path.GetExtension(file).ToLower();
            if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp")
                ImageFiles.Add(file);
        }
        ApplyFilter();
    }
    void Delete(string path)
    {
        FilteredImages.Remove(path);
        File.Delete(path);
    }

    void ApplyFilter()
    {
        FilteredImages.Clear();
        var query = SearchQuery ?? "";
        foreach (var img in ImageFiles)
        {
            if (Path.GetFileName(img).Contains(query, StringComparison.OrdinalIgnoreCase))
                FilteredImages.Add(img);
        }
    }

    async void OnAddImageClick()
    {
        try
        {
            var dataView = Clipboard.GetContent();
            byte[]? imageData = null;
            string? fileExt = null;

            if (dataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await dataView.GetStorageItemsAsync();
                var file = items.OfType<StorageFile>().FirstOrDefault();
                if (file != null)
                {
                    var ext = Path.GetExtension(file.Path).ToLower();
                    if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp")
                    {
                        using var fileStream = await file.OpenAsync(FileAccessMode.Read);
                        using var memStream = new MemoryStream();
                        await fileStream.AsStream().CopyToAsync(memStream);
                        imageData = memStream.ToArray();
                        fileExt = ext;
                    }
                }
            }

            if (imageData == null && dataView.Contains("PNG"))
            {
                var item = await dataView.GetDataAsync("PNG");
                if (item is IRandomAccessStream pngStream)
                {
                    using var memStream = new MemoryStream();
                    await pngStream.AsStream().CopyToAsync(memStream);
                    imageData = memStream.ToArray();
                    fileExt = ".png";
                }
            }

            if (imageData == null && dataView.Contains(StandardDataFormats.Bitmap))
            {
                var streamRef = await dataView.GetBitmapAsync();
                using var stream = await streamRef.OpenReadAsync();
                using var memStream = new MemoryStream();
                await stream.AsStream().CopyToAsync(memStream);
                imageData = memStream.ToArray();
            }

            if (imageData == null)
            {
                IsTeachingTipOpen = true;
                return;
            }

            HideParentRequested?.Invoke();

            var flyout = new AddImageFlyout(imageData, fileExt ?? ".png");
            flyout.Completed += () =>
            {
                ShowParentRequested?.Invoke();
                LoadImages();
            };
            void r(object sender, RoutedEventArgs e)
            {
                flyout.MarkupNode.Loaded -= r;
                flyout.MarkupNode.Show();
            }
            flyout.MarkupNode.Loaded += r;
        }
        catch { }
    }
    public void FocusTextBox()
    {
        searchTb.Focus(FocusState.Programmatic);
    }
}
