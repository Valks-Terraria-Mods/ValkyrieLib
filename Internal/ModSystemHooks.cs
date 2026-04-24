using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.UI;

namespace ValkyrieLib;

internal class ModSystemHooks : ModSystem
{
    public override void UpdateUI(GameTime gameTime)
    {
        base.UpdateUI(gameTime);
        ValkyrieAPI.KeybindService.UpdateUI();
        ValkyrieAPI.UIService.UpdateUI(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        base.ModifyInterfaceLayers(layers);
        ValkyrieAPI.UIService.ModifyInterfaceLayers(layers);
    }
}
