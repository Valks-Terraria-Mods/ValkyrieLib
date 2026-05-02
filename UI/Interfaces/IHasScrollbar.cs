using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ValkyrieLib;

public interface IHasScrollbar
{
    UIElement MainElement { get; }
    UIElement ScrollViewElement { get; }

    void SetScrollbar(UIScrollbar scrollbar);
}
