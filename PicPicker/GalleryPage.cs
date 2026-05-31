using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Path = System.IO.Path;

namespace PicPicker;

[QuickMarkup("""
    string SearchQuery;
    string NewImageName;

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
                    Flyout=AddImageFlyout=<Flyout>
                        <StackPanel Spacing=8 Padding=12>
                            <TextBlock Text="Name the image from clipboard:" />
                            <TextBox Text<=>`NewImageName` PlaceholderText="Image name" />
                            <Button Content="Save"
                                @Click+=`OnSaveClipboardImage()` />
                        </StackPanel>
                    </Flyout>
                />
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

    async void OnSaveClipboardImage()
    {
        try
        {
            var dataView = Clipboard.GetContent();
            if (!dataView.Contains(StandardDataFormats.Bitmap))
                return;
            if (string.IsNullOrWhiteSpace(NewImageName))
                return;

            var streamRef = await dataView.GetBitmapAsync();
            using var stream = await streamRef.OpenReadAsync();

            var fileName = NewImageName.Trim() + ".png";
            var targetPath = Path.Combine(ImageDirectory, fileName);
            var counter = 1;
            while (File.Exists(targetPath))
            {
                targetPath = Path.Combine(ImageDirectory, $"{NewImageName.Trim()}_{counter}.png");
                counter++;
            }

            using var fileStream = File.Create(targetPath);
            await stream.AsStream().CopyToAsync(fileStream);

            NewImageName = "";
            LoadImages();
            AddImageFlyout?.Hide();
        }
        catch { }
    }
}
