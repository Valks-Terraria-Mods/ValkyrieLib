using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace ValkyrieLib;

/// <summary>
/// The main entry point to the ValkyrieAPI.
/// </summary>
public static class ValkyrieAPI
{
    internal static KeybindService KeybindService { get; private set; }
    internal static UserInterfaceService UIService { get; private set; }

    internal static void Initialize(KeybindService kb, UserInterfaceService ui)
    {
        KeybindService = kb;
        UIService = ui;
    }

    /// <summary>
    /// Returns a <see cref="ModHandle"/> that can be used to do things tied to your mod.
    /// </summary>
    /// <param name="mod">The mod instance.</param>
    public static ModHandle GetHandle(Mod mod) => new(mod, KeybindService, UIService);

    /// <summary>
    /// Contains various UI related helpers.
    /// </summary>
    public static class UI
    {
        /// <summary>
        /// Creates a close button docked to the top right.
        /// </summary>
        public static UIImageButton CreateCloseButton()
        {
            // Push the button slightly further to the top right corner
            const int Inset = 5;

            var searchCancelBtn = GameAssets.SearchCancelButton;

            searchCancelBtn.HAlign = 1f;
            searchCancelBtn.Left = StyleDimension.FromPixels(Inset);
            searchCancelBtn.Top = StyleDimension.FromPixels(-Inset);

            return searchCancelBtn;
        }
    }
}
