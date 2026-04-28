using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ValkyrieLib;

internal class KeybindService
{
    private readonly List<Keybind> _keybinds = [];

    internal void Register(Mod mod, string name, string defaultKey, Action action)
    {
        _keybinds.Add(new Keybind(mod, name, defaultKey, action));
    }

    internal void UpdateUI()
    {
        foreach (Keybind keybind in _keybinds)
        {
            if (keybind.JustPressed)
                keybind.Execute();
        }
    }
}
