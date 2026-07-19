using Microsoft.UI.Text;

namespace Get.Layout;

public static class TextExtensions
{
    public static TextBlock Bold(this TextBlock element)
    {
        element.FontWeight = FontWeights.Bold;
        return element;
    }
    public static TextBlock Italic(this TextBlock element)
    {
        element.FontStyle = FontStyle.Italic;
        return element;
    }
    public static void Tight(this TextBlock element)
    {
#if HAS_UNO
#else
        element.TextLineBounds = TextLineBounds.Tight;
#endif
    }
}