using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Path = System.IO.Path;

namespace PicPicker;

[QuickMarkup("""
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
                <TextBox PlaceholderText="Search images..." MinWidth=300
                    Text<=>`SearchQuery`
                    @TextChanged+=`ApplyFilter()`
                />
                <Button Grid.Column=1 Content="+"
                    @Click+=`OnAddImageClick()` />
            </Grid>
            
            <ScrollViewer Grid.Row=1>
                <VariableSizedWrapGrid Orientation=Horizontal>
                    foreach (var imagePath in `FilteredImages`; `imagePath`) {
                        <ImageDisplay ImagePath=`imagePath` @Completed+=`Completed?.Invoke()` @DeleteRequest+=`Delete(imagePath)` />
                    }
                </VariableSizedWrapGrid>
            </ScrollViewer>
            
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
            if (!dataView.Contains(StandardDataFormats.Bitmap))
                return;

            var streamRef = await dataView.GetBitmapAsync();
            using var stream = await streamRef.OpenReadAsync();

            using var memStream = new MemoryStream();
            await stream.AsStream().CopyToAsync(memStream);
            var imageData = memStream.ToArray();

            HideParentRequested?.Invoke();

            var flyout = new AddImageFlyout(imageData);
            flyout.Completed += () =>
            {
                ShowParentRequested?.Invoke();
                LoadImages();
            };
            void r(object sender, RoutedEventArgs e)
            {
                flyout.Loaded -= r;
                flyout.Show();
            }
            flyout.Loaded += r;
        }
        catch { }
    }
}
