using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ValkyrieLib;

public interface IHasCloseButton
{
    UIElement MainElement { get; }
    void SetCloseButton(UIImageButton button);
}
