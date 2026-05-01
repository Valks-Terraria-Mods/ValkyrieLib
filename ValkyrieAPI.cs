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
        public static UIButton<string> CreateCloseButton()
        {
            const int Size = 32;

            return new UIButton<string>("X")
            {
                Width = StyleDimension.FromPixels(Size),
                Height = StyleDimension.FromPixels(Size),
                HAlign = 1f,
            };
        }
    }
}
