using Terraria.UI;

namespace ValkyrieLib;

/// <summary>
/// This ui element consumes keyboard / mouse input.
/// </summary>
public interface IInputConsumer
{
    /// <summary>
    /// The main ui element.
    /// </summary>
    UIElement MainElement { get; }
}
