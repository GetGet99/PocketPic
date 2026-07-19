namespace Get.Layout;

public static class LayoutExtensions
{
    public static T CenterV<T>(this T element)
        where T : FrameworkElement
    {
        element.VerticalAlignment = VerticalAlignment.Center;
        return element;
    }
    public static T CenterH<T>(this T element)
        where T : FrameworkElement
    {
        element.HorizontalAlignment = HorizontalAlignment.Center;
        return element;
    }
    public static T StretchV<T>(this T element)
        where T : FrameworkElement
    {
        element.VerticalAlignment = VerticalAlignment.Stretch;
        return element;
    }
    public static T StretchH<T>(this T element)
        where T : FrameworkElement
    {
        element.HorizontalAlignment = HorizontalAlignment.Stretch;
        return element;
    }
    public static T Top<T>(this T element) where T : FrameworkElement
    {
        element.VerticalAlignment = VerticalAlignment.Top;
        return element;
    }
    public static T Bottom<T>(this T element) where T : FrameworkElement
    {
        element.VerticalAlignment = VerticalAlignment.Bottom;
        return element;
    }
    public static T Left<T>(this T element) where T : FrameworkElement
    {
        element.HorizontalAlignment = HorizontalAlignment.Left;
        return element;
    }
    public static T Right<T>(this T element) where T : FrameworkElement
    {
        element.HorizontalAlignment = HorizontalAlignment.Right;
        return element;
    }
}