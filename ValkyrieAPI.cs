using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

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
        public static class Colors
        {
            public static Color LightBackground { get; } = new(29, 37, 69, 224);
            public static Color DarkBackground { get; } = new(21, 30, 59, 230);
            public static Color Border { get; } = new(77, 96, 151);
        }

        public static class Assets
        {
            public static Asset<Texture2D> CategoryPanel => GetTexture(GameTexture.CharCreation_CategoryPanel);
            public static Asset<Texture2D> CategoryPanelBorder => GetTexture(GameTexture.CharCreation_CategoryPanelBorder);
            public static UIImageButton SearchCancelButton => new(GetTexture(GameTexture.SearchCancel));
            public static UIColoredImageButton Copy => BuildSmallButton(GameTexture.CharCreation_Copy);
            public static UIColoredImageButton Paste => BuildSmallButton(GameTexture.CharCreation_Paste);
            public static UIColoredImageButton Randomize => BuildSmallButton(GameTexture.CharCreation_Randomize);

            private static readonly Dictionary<GameTexture, Asset<Texture2D>> _textureCache = [];

            private static Asset<Texture2D> GetTexture(GameTexture texture)
            {
                const string BasePath = "Images/UI/";

                if (!_textureCache.TryGetValue(texture, out var asset))
                {
                    var normalizedTexture = texture.ToString().Replace("_", "/");
                    asset = Main.Assets.Request<Texture2D>(BasePath + normalizedTexture, AssetRequestMode.ImmediateLoad);
                    _textureCache[texture] = asset;
                }

                return asset;
            }

            private static UIColoredImageButton BuildSmallButton(GameTexture texture)
            {
                return new UIColoredImageButton(GetTexture(texture), isSmall: true);
            }

            private enum GameTexture
            {
                CharCreation_CategoryPanel,
                CharCreation_CategoryPanelBorder,
                CharCreation_Copy,
                CharCreation_Paste,
                CharCreation_Randomize,
                SearchCancel
            }
        }
    }
}
