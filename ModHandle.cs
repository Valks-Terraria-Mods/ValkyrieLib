using System;
using Terraria.ModLoader;
using Terraria.UI;

namespace ValkyrieLib;

/// <summary>
/// Contains API specifically tied to your mod.
/// </summary>
public class ModHandle
{
    private readonly Mod _mod;
    private readonly KeybindService _keybindService;
    private readonly UserInterfaceService _uiService;

    internal ModHandle(Mod mod, KeybindService kb, UserInterfaceService ui)
    {
        _mod = mod;
        _keybindService = kb;
        _uiService = ui;
    }

    /// <summary>
    /// Registers a UI panel that can be toggled on and off by a keybind.
    /// </summary>
    /// <param name="name">The display name used for the keybind in the in-game settings and internally for the panel.</param>
    /// <param name="keybind">The default key binding (e.g. <c>"H"</c>) that toggles the panel.</param>
    /// <param name="factory">A factory that creates the <see cref="UIState"/> to display when the panel is shown.</param>
    public void RegisterUI(string name, string keybind, Func<UIState> factory) => _uiService.Insert(_mod, name, keybind, factory);

    /// <summary>
    /// Registers a new keybind that invokes an action.
    /// </summary>
    /// <param name="name">The name of this keybind.</param>
    /// <param name="defaultKey">The key to use for this keybind.</param>
    /// <param name="action">The action this keybind invokes.</param>
    public void RegisterKeybind(string name, string defaultKey, Action action) => _keybindService.Register(_mod, name, defaultKey, action);
}
