using System;
using Terraria.ModLoader;

namespace ValkyrieLib;

/// <summary>
/// Registers a new keybind that invokes an action when pressed.
/// </summary>
/// <param name="mod">The Terraria mod instance.</param>
/// <param name="name">The name of this keybind.</param>
/// <param name="binding">The key to use for this keybind.</param>
/// <param name="action">The action this keybind invokes.</param>
internal class Keybind(Mod mod, string name, string binding, Action action)
{
    private readonly ModKeybind _keybind = KeybindLoader.RegisterKeybind(mod, name, binding.ToUpper());

    /// <summary>
    /// Returns true if this keybind was pressed.
    /// </summary>
    internal bool JustPressed => _keybind.JustPressed;

    /// <summary>
    /// Invoke the action tied to this keybind.
    /// </summary>
    internal void Execute() => action();
}
