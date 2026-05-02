using Terraria.ModLoader.UI;

namespace ValkyrieLib;

public class Button : UIButton<string>
{
    private const float VerticalPadding = 5;
    private const float HorizontalPadding = 20;

    public Button(string text) : base(text)
    {
        // Auto size the buttons panel
        ScalePanel = true;
        // Scale panel is a little too generous with size so lets restrict it
        PaddingTop = VerticalPadding;
        PaddingBottom = VerticalPadding;
        PaddingLeft = HorizontalPadding;
        PaddingRight = HorizontalPadding;
    }
}
