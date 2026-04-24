using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ValkyrieLib;

internal class UserInterfaceService(KeybindService keybindService)
{
    private const string VanillaMouseLayerId = "Vanilla: Mouse Text";
    private readonly List<ToggleableUi> _uis = [];

    internal void Insert(Mod mod, string name, string keybind, Func<UIState> stateFactory)
    {
        ToggleableUi ui = new(name, stateFactory);
        keybindService.Register(mod, name, keybind, ui.Toggle);
        _uis.Add(ui);
    }

    internal void UpdateUI(GameTime gameTime)
    {
        foreach (ToggleableUi ui in _uis)
        {
            if (ui.IsVisible)
                ui.Update(gameTime);
        }
    }

    internal void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int vanillaMouseLayer = layers.FindIndex(layer => layer.Name.Equals(VanillaMouseLayerId, StringComparison.Ordinal));

        if (vanillaMouseLayer == -1)
            return;

        foreach (ToggleableUi ui in _uis)
        {
            if (ui.IsVisible)
            {
                ui.Insert(layers, vanillaMouseLayer);
                vanillaMouseLayer++;
            }
        }
    }
}
