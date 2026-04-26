using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace ValkyrieLib;

internal static class GameAssets
{
    internal static Asset<Texture2D> CategoryPanel => GetTexture(GameTexture.CategoryPanel);
    internal static Asset<Texture2D> CategoryPanelBorder => GetTexture(GameTexture.CategoryPanelBorder);
    
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
        CategoryPanelBorder
    }
}
