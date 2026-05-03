using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ValkyrieLib;

public class UITitle : UIText
{
    private const float HeaderPadding = 25f;

    public UITitle(string name) : base(name, textScale: 0.5f, large: true)
    {
        Height = StyleDimension.FromPixels(Height.Pixels + HeaderPadding);
        HAlign = 0.5f;
    }
}
