using Windows.ApplicationModel.DataTransfer;

namespace PocketPic;

static class ClipboardHelpers
{
    extension(StandardDataFormats)
    {
        public static string Png => 
#if WINDOWS
            "PNG"
#else
            "image/png"
#endif
            ;
    }
    extension(DataPackageView dataView)
    {
        public Task<byte[]?> GetPngAsync()
            => dataView.GetStreamAsync(StandardDataFormats.Png);
        private async Task<byte[]?> GetStreamAsync(string format)
        {
            var item = await dataView.GetDataAsync(format);
#if WINDOWS
            if (item is IRandomAccessStream randStream)
            {
                using var memStream = new MemoryStream();
                await randStream.AsStream().CopyToAsync(memStream);
                return memStream.ToArray();
            }
#else
            if (item is byte[] bytes)
            {
                return bytes;
            }
#endif
            return null;
        }
    }
}