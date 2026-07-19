using CommunityToolkit.WinUI;

namespace Get.Layout;

public partial class Card : Control
{
    public UIElement? Child { get => GetValue(ChildProperty) as UIElement; set => SetValue(ChildProperty, value); }
    public static DependencyProperty ChildProperty = DependencyProperty.Register(
        nameof(Child),
        typeof(UIElement),
        typeof(Card),
        new(null)
    );
    public Card()
    {
        InitializeComponent();
    }
}
