using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace ValkyrieLib;

internal class ToggleableUI(string id, Func<UIState> stateFactory)
{
    /// <summary>
    /// Returns true if this ui is visible in-game.
    /// </summary>
    internal bool IsVisible { get; private set; }

    private GameTime _lastUpdateGameTime;
    private readonly UserInterface _userInterface = new();
    private readonly string _name = $"{nameof(ValkyrieLib)}: {id}";
    private UIState _state;
    private UIImageButton _closeBtn;
    private UIScrollbar _uiScrollbar;
    private bool _scrollBarVisible;

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

        _userInterface.SetState(_state);

        if (_state is IHasScrollbar hasScrollbar)
        {
            _uiScrollbar = CreateScrollBar(_state is IHasCloseButton);
            hasScrollbar.SetScrollbar(_uiScrollbar);
            hasScrollbar.MainElement.Append(_uiScrollbar);
            _scrollBarVisible = true;
        }
        
        if (_state is IHasCloseButton hasCloseBtn)
        {
            _closeBtn = CreateCloseButton();
            _closeBtn.OnLeftClick += OnClickCloseBtn;

            hasCloseBtn.MainElement.Append(_closeBtn);
        }
    }

    /// <summary>
    /// Makes this ui hidden.
    /// </summary>
    internal void Hide()
    {
        IsVisible = false;

        if (_state is IHasCloseButton)
            _closeBtn.OnLeftClick -= OnClickCloseBtn;

        _scrollBarVisible = false;
        _userInterface.SetState(null);
    }

    internal void Insert(List<GameInterfaceLayer> layers, int index)
    {
        layers.Insert(index, new LegacyGameInterfaceLayer(_name, Draw, InterfaceScaleType.UI));
    }

    internal void Update(GameTime gameTime)
    {
        // Only show the scrollbar if it can actually scroll (there is enough content overflow to allow scrolling)
        if (_state is IHasScrollbar hasScrollbar)
        {
            bool canScroll = _uiScrollbar.CanScroll;

            if (canScroll != _scrollBarVisible)
            {
                if (canScroll)
                    hasScrollbar.MainElement.Append(_uiScrollbar);
                else
                    _uiScrollbar.Remove();

                _scrollBarVisible = canScroll;
            }
        }

        _lastUpdateGameTime = gameTime;
        _userInterface.Update(gameTime);
    }

    private bool Draw()
    {
        _userInterface.Draw(Main.spriteBatch, _lastUpdateGameTime);

        if (_state is IBlocksInput @interface && @interface.MainElement.ContainsPoint(Main.MouseScreen))
        {
            // Consume mouse input if mouse is in the ui element (seems to only block sword swinging but not hovering over the hotbar)
            // https://github.com/tModLoader/tModLoader/wiki/Advanced-guide-to-custom-UI#preventing-mouse-clicks-from-using-selected-item
            Main.LocalPlayer.mouseInterface = true;

            // Prevent scroll changing hotbar (the passed in string can be anything)
            // https://github.com/tModLoader/tModLoader/wiki/Advanced-guide-to-custom-UI#preventing-scroll-wheel-from-shifting-selected-hotbar-item
            PlayerInput.LockVanillaMouseScroll($"{nameof(ValkyrieLib)}/ScrollList");
        }

        return true;
    }

    private void OnClickCloseBtn(UIMouseEvent evt, UIElement listeningElement) => Hide();

    private static UIScrollbar CreateScrollBar(bool hasCloseBtn)
    {
        float ScrollBarTopInset = hasCloseBtn ? 27f : 2f;
        const float ScrollBarBottomInset = 3;
        const float ScrollBarLeftInset = 5;

        return new UIScrollbar()
        {
            Height = StyleDimension.FromPixelsAndPercent(-ScrollBarTopInset - ScrollBarBottomInset, 1f),
            Top = StyleDimension.FromPixels(ScrollBarTopInset),
            Left = StyleDimension.FromPixels(ScrollBarLeftInset),
            HAlign = 1f
        };
    }

    /// <summary>
    /// Creates a close button docked to the top right.
    /// </summary>
    private static UIImageButton CreateCloseButton()
    {
        // Push the button slightly further to the top right corner
        const float Inset = 6.5f;

        var searchCancelBtn = GameAssets.SearchCancelButton;

        searchCancelBtn.HAlign = 1f;
        searchCancelBtn.Left = StyleDimension.FromPixels(Inset);
        searchCancelBtn.Top = StyleDimension.FromPixels(-Inset);

        return searchCancelBtn;
    }
}
