using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.UI;

namespace ValkyrieLib;

internal class UserInterfaceService(KeybindService keybindService)
{
    private const string VanillaMouseLayerId = "Vanilla: Mouse Text";
    private readonly List<(Mod Mod, string Name, ToggleableUI UI)> _uis = [];

    internal void Insert(Mod mod, string name, string keybind, Func<UIState> stateFactory)
    {
        ToggleableUI ui = new(name, stateFactory);
        keybindService.Register(mod, name, keybind, ui.Toggle);
        _uis.Add((mod, name, ui));
    }

    internal void Toggle(Mod mod, string name)
    {
        foreach (var registeredUI in _uis)
        {
            if (ReferenceEquals(registeredUI.Mod, mod) && string.Equals(registeredUI.Name, name, StringComparison.Ordinal))
            {
                registeredUI.UI.Toggle();
                return;
            }
        }
    }

    internal void UpdateUI(GameTime gameTime)
    {
        foreach (var registeredUI in _uis)
        {
            ToggleableUI ui = registeredUI.UI;

            if (ui.IsVisible)
                ui.Update(gameTime);
        }
    }

    internal void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int vanillaMouseLayer = layers.FindIndex(layer => layer.Name.Equals(VanillaMouseLayerId, StringComparison.Ordinal));

        if (vanillaMouseLayer == -1)
            return;

        foreach (var registeredUI in _uis)
        {
            ToggleableUI ui = registeredUI.UI;

            if (ui.IsVisible)
            {
                ui.Insert(layers, vanillaMouseLayer);
                vanillaMouseLayer++;
            }
        }
    }
}
