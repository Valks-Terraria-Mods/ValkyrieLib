using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ValkyrieLib;

public interface IHasMainPanel
{
    UIElement MainElement { get; set; }
}
