using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace ValkyrieLib;

internal static class GameAssets
{
    internal static Asset<Texture2D> CategoryPanel => GetTexture(GameTexture.CategoryPanel);
    internal static Asset<Texture2D> CategoryPanelBorder => GetTexture(GameTexture.CategoryPanelBorder);

    internal static class ColorActions
    {
        internal static UIColoredImageButton Copy => BuildSmallButton(GameTexture.Copy);
        internal static UIColoredImageButton Paste => BuildSmallButton(GameTexture.Paste);
        internal static UIColoredImageButton Randomize => BuildSmallButton(GameTexture.Randomize);
    }

    private static UIColoredImageButton BuildSmallButton(GameTexture texture)
    {
        return new UIColoredImageButton(GetTexture(texture), isSmall: true);
    }

    private static readonly Dictionary<GameTexture, Asset<Texture2D>> _textureCache = [];

    private static Asset<Texture2D> GetTexture(GameTexture texture)
    {
        const string BasePath = "Images/UI/CharCreation/";

        if (!_textureCache.TryGetValue(texture, out var asset))
        {
            asset = Main.Assets.Request<Texture2D>(BasePath + texture.ToString(), AssetRequestMode.ImmediateLoad);
            _textureCache[texture] = asset;
        }

        return asset;
    }

    private enum GameTexture
    {
        CategoryPanel,
        CategoryPanelBorder,
        Copy,
        Paste,
        Randomize
    }
}
