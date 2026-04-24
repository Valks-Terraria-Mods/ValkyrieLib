using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace ValkyrieLib;

internal class ToggleableUi(string id, Func<UIState> stateFactory)
{
    /// <summary>
    /// Returns true if this ui is visible in-game.
    /// </summary>
    internal bool IsVisible { get; private set; }

    private GameTime _lastUpdateGameTime;
    private readonly UserInterface _userInterface = new();
    private readonly string _name = $"{nameof(ValkyrieLib)}: {id}";
    private UIState _state;

    /// <summary>
    /// Toggles the visibility of this ui.
    /// </summary>
    internal void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// Makes this ui visible.
    /// </summary>
    internal void Show()
    {
        IsVisible = true;

        _state = stateFactory();

        if (_state is IClosable closable)
            closable.CloseRequested += OnCloseRequested;

        _userInterface.SetState(_state);
    }

    /// <summary>
    /// Makes this ui hidden.
    /// </summary>
    internal void Hide()
    {
        IsVisible = false;

        if (_state is IClosable closable)
            closable.CloseRequested -= OnCloseRequested;

        _userInterface.SetState(null);
    }

    internal void Insert(List<GameInterfaceLayer> layers, int index)
    {
        layers.Insert(index, new LegacyGameInterfaceLayer(_name, Draw, InterfaceScaleType.UI));
    }

    internal void Update(GameTime gameTime)
    {
        _lastUpdateGameTime = gameTime;
        _userInterface.Update(gameTime);
    }

    private bool Draw()
    {
        _userInterface.Draw(Main.spriteBatch, _lastUpdateGameTime);
        
        // Consume mouse input if mouse is in the ui element (seems to only block sword swinging but not hovering over the hotbar)
        if (_state is IHasMainElement withElement && withElement.MainElement.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
        
        return true;
    }

    private void OnCloseRequested() => Hide();
}
