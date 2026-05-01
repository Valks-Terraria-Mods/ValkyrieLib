using Microsoft.Xna.Framework;
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
            const float Inset = 6.5f;

            var searchCancelBtn = GameAssets.SearchCancelButton;

            searchCancelBtn.HAlign = 1f;
            searchCancelBtn.Left = StyleDimension.FromPixels(Inset);
            searchCancelBtn.Top = StyleDimension.FromPixels(-Inset);

            return searchCancelBtn;
        }

        public static class Colors
        {
            public static Color LightBackground { get; } = new(29, 37, 69, 224);
            public static Color DarkBackground { get; } = new(21, 30, 59, 230);
            public static Color Border { get; } = new(77, 96, 151);
        }
    }
}
