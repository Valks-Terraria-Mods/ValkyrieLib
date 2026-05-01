using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ValkyrieLib;

internal static class GameAssets
{
    internal static Asset<Texture2D> CategoryPanel => GetTexture(GameTexture.CharCreation_CategoryPanel);
    internal static Asset<Texture2D> CategoryPanelBorder => GetTexture(GameTexture.CharCreation_CategoryPanelBorder);
    internal static UIImageButton SearchCancelButton => new(GetTexture(GameTexture.SearchCancel));

    internal static class ColorActions
    {
        internal static UIColoredImageButton Copy => BuildSmallButton(GameTexture.CharCreation_Copy);
        internal static UIColoredImageButton Paste => BuildSmallButton(GameTexture.CharCreation_Paste);
        internal static UIColoredImageButton Randomize => BuildSmallButton(GameTexture.CharCreation_Randomize);
    }

    private static UIColoredImageButton BuildSmallButton(GameTexture texture)
    {
        return new UIColoredImageButton(GetTexture(texture), isSmall: true);
    }

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
