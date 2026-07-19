using System.Diagnostics;
using CommunityToolkit.WinUI;

namespace Get.Layout;

public partial class OrientedStack : Panel
{
    public static readonly DependencyProperty LengthTypeProperty =
        DependencyProperty.RegisterAttached(
            "LengthType",
            typeof(GridUnitType),
            typeof(OrientedStack),
            new PropertyMetadata(default(GridUnitType), OnLengthTypeChanged));

    public static readonly DependencyProperty LengthValueProperty =
        DependencyProperty.RegisterAttached(
            "LengthValue",
            typeof(double),
            typeof(OrientedStack),
            new PropertyMetadata(default(double), OnLengthValueChanged));

    public static readonly DependencyProperty LengthProperty =
        DependencyProperty.RegisterAttached(
            "Length",
            typeof(GridLength),
            typeof(OrientedStack),
            new PropertyMetadata(default(GridLength), OnLengthChanged));

    public static readonly DependencyProperty VisibilityTrackingProperty =
        DependencyProperty.RegisterAttached(
            "VisibilityTracking",
            typeof(bool),
            typeof(OrientedStack),
            new PropertyMetadata(false));

    public static GridUnitType GetLengthType(DependencyObject obj) => (GridUnitType)obj.GetValue(LengthTypeProperty);
    public static void SetLengthType(DependencyObject obj, GridUnitType value) => obj.SetValue(LengthTypeProperty, value);
    public static double GetLengthValue(DependencyObject obj) => (double)obj.GetValue(LengthValueProperty);
    public static void SetLengthValue(DependencyObject obj, double value) => obj.SetValue(LengthValueProperty, value);
    public static GridLength GetLength(DependencyObject obj) => (GridLength)obj.GetValue(LengthProperty);
    public static void SetLength(DependencyObject obj, GridLength value) => obj.SetValue(LengthProperty, value);
    public static bool GetVisibilityTracking(DependencyObject obj) => (bool)obj.GetValue(VisibilityTrackingProperty);
    public static void SetVisibilityTracking(DependencyObject obj, bool value) => obj.SetValue(VisibilityTrackingProperty, value);

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public static DependencyProperty OrientationProperty {get;} = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(OrientedStack),
        new(default(Orientation), (d, e) => ((OrientedStack)d).OnOrientationChanged((Orientation)e.NewValue))
    );
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public static DependencyProperty SpacingProperty {get;}= DependencyProperty.Register(
        nameof(Spacing),
        typeof(double),
        typeof(OrientedStack),
        new(0d)
    );
    void OnOrientationChanged(Orientation newValue)
    {
        InvalidateArrange();
        InvalidateMeasure();
    }
    public OrientedStack()
    {

    }
    public OrientedStack(Orientation orientation = default, double spacing = 0) : this()
    {
        Orientation = orientation;
        Spacing = spacing;
    }
    static void OnLengthTypeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        var newValue = (GridUnitType)args.NewValue;
        var length = GetLength(obj);
        length = new(length.Value, newValue);
        if (GetLength(obj) != length)
            SetLength(obj, length);
    }
    static void OnLengthValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        var newValue = (double)args.NewValue;
        var length = GetLength(obj);
        length = new(newValue, length.GridUnitType);
        if (GetLength(obj) != length)
            SetLength(obj, length);
    }
    static void OnLengthChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        var newValue = (GridLength)args.NewValue;
        if (GetLengthValue(obj) != newValue.Value)
            SetLengthValue(obj, newValue.Value);
        if (GetLengthType(obj) != newValue.GridUnitType)
            SetLengthType(obj, newValue.GridUnitType);
        if (newValue.IsStar && newValue.Value <= 0)
        {
            throw new System.InvalidOperationException();
        }
        (VisualTreeHelper.GetParent(obj) as OrientedStack)?.InvalidateArrange();
    }
    void OnChildVisibilityChanged(DependencyObject sender, DependencyProperty property)
    {
        InvalidateArrange();
        InvalidateMeasure();
    }
    readonly Dictionary<UIElement, long> CachedChildrenVisibility = [];
    readonly HashSet<UIElement> CachedChildren = [];
    protected override Size MeasureOverride(Size availableSize)
    {
#if DEBUG
        if (Tag is "Debug")
            Debugger.Break();
#endif
        var orientation = Orientation;
        (double Along, double Opposite) SizeToOF(Size size) =>
            orientation is Orientation.Horizontal ?
            (size.Width, size.Height) : (size.Height, size.Width);
        Size OFToSize((double Along, double Opposite) of) =>
            orientation is Orientation.Horizontal ?
            new(of.Along, of.Opposite) : new(of.Opposite, of.Along);
        var panelSize = SizeToOF(availableSize);
        var panelRemainingSize = panelSize;
        double totalUsed = 0;
        var count = Children.Count;
        List<(double pixel, UIElement ele)> pixelList = new(count);
        List<UIElement> autoList = new(count);
        List<(double star, UIElement ele)> starList = new(count);

        double totalAbsolutePixel = 0, totalStar = 0, maxOpposite = 0;
        int visibleChildren = 0;
        foreach (var child in Children)
        {
            if (GetVisibilityTracking(child))
            {
                if (!CachedChildren.Remove(child))
                {
                    CachedChildrenVisibility[child] = child.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, OnChildVisibilityChanged);
                }
            }
            if (child.Visibility is not Visibility.Visible) continue;
            visibleChildren++;
            var length = GetLength(child);
            if (length.IsAuto)
            {
                autoList.Add(child);
            }
            else if (length.IsStar)
            {
                totalStar += length.Value;
                starList.Add((length.Value, child));
            }
            else
            {
                totalAbsolutePixel += length.Value;
            }
        }
        // these children are no longer here
        foreach (var child in CachedChildren)
        {
            if (CachedChildrenVisibility.Remove(child, out var token))
                child.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token);
        }
        CachedChildren.Clear();
        foreach (var child in CachedChildrenVisibility.Keys)
        {
            CachedChildren.Add(child);
        }
        // ...
        // add spacing as part of absolute pixel
        if (visibleChildren > 1)
        {
            totalAbsolutePixel += (visibleChildren - 1) * Spacing;
        }
        foreach (var (pixel, child) in pixelList)
        {
            child.Measure(OFToSize((pixel, panelSize.Opposite)));
            var (_, Opposite) = SizeToOF(child.DesiredSize);
            if (Opposite > maxOpposite)
                maxOpposite = Opposite;
        }
        panelRemainingSize.Along -= totalAbsolutePixel;
        totalUsed += totalAbsolutePixel;
        panelRemainingSize.Along = Math.Max(panelRemainingSize.Along, 0);
        foreach (var child in autoList)
        {
            child.Measure(OFToSize(panelRemainingSize));
            var (Along, Opposite) = SizeToOF(child.DesiredSize);
            if (Opposite > maxOpposite)
                maxOpposite = Opposite;
            totalUsed += Along;
            panelRemainingSize.Along -= Along;
            panelRemainingSize.Along = Math.Max(panelRemainingSize.Along, 0);
        }
        double maxStarSize = 0;
        foreach (var (star, child) in starList)
        {
            child.Measure(OFToSize(panelRemainingSize));
            var (Along, Opposite) = SizeToOF(child.DesiredSize);
            if (Opposite > maxOpposite)
                maxOpposite = Opposite;
            var starSize = Along / star;
            if (starSize > maxStarSize)
                maxStarSize = starSize;
        }
        var computed = maxStarSize * totalStar;
        panelRemainingSize.Along -= computed;
        totalUsed += computed;
        panelRemainingSize.Along = Math.Max(panelRemainingSize.Along, 0);
        var toReturn = OFToSize((totalUsed, Math.Min(maxOpposite, panelSize.Opposite)));
#if DEBUG
        if (Tag is "Debug")
            Debugger.Break();
#endif
        return toReturn;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
#if DEBUG
        if (Tag is "Debug")
            Debugger.Break();
#endif
        var orientation = Orientation;
        (double Along, double Opposite) SizeToOF(Size size) =>
            orientation is Orientation.Horizontal ?
            (size.Width, size.Height) : (size.Height, size.Width);
        Size OFToSize((double Along, double Opposite) of) =>
            orientation is Orientation.Horizontal ?
            new(of.Along, of.Opposite) : new(of.Opposite, of.Along);
        Point OFToPoint((double Along, double Opposite) of) =>
            orientation is Orientation.Horizontal ?
            new(of.Along, of.Opposite) : new(of.Opposite, of.Along);
        var panelSize = SizeToOF(finalSize);
        var panelRemainingSize = panelSize;
        double totalAbsolutePixel = 0, totalStar = 0;
        int visibleChildren = 0;
        foreach (var child in Children)
        {
            if (child.Visibility is not Visibility.Visible) continue;
            visibleChildren++;
            var length = GetLength(child);
            if (length.IsAuto)
            {
                var desiredSize = SizeToOF(child.DesiredSize);
                totalAbsolutePixel += desiredSize.Along;
            }
            else if (length.IsStar)
            {
                totalStar += length.Value;
            }
            else
            {
                totalAbsolutePixel += length.Value;
            }
        }
        // add spacing as part of absolute pixel
        if (visibleChildren > 1)
        {
            totalAbsolutePixel += (visibleChildren - 1) * Spacing;
        }
        panelRemainingSize.Along -= totalAbsolutePixel;
        panelRemainingSize.Along = Math.Max(panelRemainingSize.Along, 0);
        double alongOffset = 0;

        // To avoid divide by 0
        if (totalStar is 0) totalStar = 1;
        var starLength = panelRemainingSize.Along / totalStar;

        foreach (var child in Children)
        {
            if (child.Visibility is not Visibility.Visible) continue;
            var length = GetLength(child);
            if (length.IsAuto)
            {
                var desiredSize = SizeToOF(child.DesiredSize);
                child.Arrange(new(
                    OFToPoint((alongOffset, 0)),
                    OFToSize((desiredSize.Along, panelRemainingSize.Opposite))
                ));
                alongOffset += desiredSize.Along + Spacing;
                panelRemainingSize.Along -= desiredSize.Along + Spacing;
            }
            else if (length.IsStar)
            {
                var computedLength = starLength * length.Value;
                child.Arrange(new(
                    OFToPoint((alongOffset, 0)),
                    OFToSize((computedLength, panelRemainingSize.Opposite))
                ));
                alongOffset += computedLength + Spacing;
                panelRemainingSize.Along -= computedLength + Spacing;
            }
            else
            {
                child.Arrange(new(
                    OFToPoint((alongOffset, 0)),
                    OFToSize((length.Value, panelRemainingSize.Opposite))
                ));
                alongOffset += length.Value + Spacing;
                panelRemainingSize.Along -= length.Value + Spacing;
            }
        }
        panelRemainingSize.Along = Math.Max(panelRemainingSize.Along, 0);

#if DEBUG
        if (Tag is "Debug")
            Debugger.Break();
#endif
        return OFToSize((panelSize.Along - panelRemainingSize.Along, Math.Min(panelRemainingSize.Opposite, panelSize.Opposite)));
    }
}
