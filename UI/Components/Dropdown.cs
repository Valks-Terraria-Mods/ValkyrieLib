using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ValkyrieLib;

public sealed class Dropdown<T> : UIElement where T : struct
{
    private const float ButtonWidth = 180f;
    private const float ButtonHeight = 30f;
    private const float OptionHeight = 30f;
    private const float ListPadding = 2f;
    private const float PopupMinHeight = 100f;
    private const float PopupMaxHeight = 420f;
    private const float PopupPadding = 4f;
    private const float PopupGap = 4f;
    private const float ScreenMargin = 8f;
    private const float ScrollbarWidth = 20f;
    private const float ScrollbarVerticalInset = 5f;
    private const float HiddenCoord = -10000f;
    private const float TextScale = 0.76f;

    private static readonly Color BackgroundColor = new(44, 57, 105, 255);
    private static readonly Color HoverColor = new(53, 64, 141, 255);
    private static readonly Color SelectedColor = new(100, 120, 200, 255);
    private static readonly Color PopupColor = new(33, 43, 79, 255);

    private readonly List<T> _options;
    private readonly Action<object> _onSelected;
    private readonly UITextPanel<string> _button;

    private T _currentValue;
    private UIPanel _dropdownPanel;
    private UIList _optionList;
    private UIScrollbar _scrollbar;
    private bool _popupVisible;
    private bool _wasLeftMouseDown;
    private bool _wasRightMouseDown;

    public Dropdown(T initialValue, Action<object> onSelected)
    {
        _currentValue = initialValue;
        _onSelected = onSelected;
        _options = BuildOptions();
        Width.Set(ButtonWidth, 0f);
        Height.Set(ButtonHeight, 0f);
        _button = BuildButton();
        Append(_button);
    }

    public void SetValue(T value, bool notify = true)
    {
        if (EqualityComparer<T>.Default.Equals(_currentValue, value))
            return;

        _currentValue = value;
        _button.SetText(FormatLabel(_currentValue));

        if (notify)
            _onSelected?.Invoke(value);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool leftDown = Main.mouseLeft;
        bool rightDown = Main.mouseRight;
        bool leftClicked = leftDown && !_wasLeftMouseDown;
        bool rightClicked = rightDown && !_wasRightMouseDown;
        _wasLeftMouseDown = leftDown;
        _wasRightMouseDown = rightDown;

        if (!_popupVisible || (!leftClicked && !rightClicked))
            return;

        bool overButton = _button.ContainsPoint(Main.MouseScreen);
        bool overPopup = _dropdownPanel?.ContainsPoint(Main.MouseScreen) ?? false;

        if (!overButton && !overPopup)
            ClosePopup();
    }

    private UITextPanel<string> BuildButton()
    {
        var button = new UITextPanel<string>(FormatLabel(_currentValue), TextScale)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            BackgroundColor = BackgroundColor,
        };

        button.OnLeftClick += (_, _) => TogglePopup();
        button.OnUpdate += _ =>
        {
            button.BackgroundColor = button.IsMouseHovering ? HoverColor : BackgroundColor;
        };

        return button;
    }

    private void TogglePopup()
    {
        if (_popupVisible)
            ClosePopup();
        else
            OpenPopup();
    }

    private void OpenPopup()
    {
        var root = GetRootElement();
        if (root is null)
            return;

        EnsurePopup(root);
        float optionContentHeight = _options.Count * OptionHeight + (Math.Max(0, _options.Count - 1) * ListPadding);
        PositionPopup(optionContentHeight);
        _dropdownPanel!.IgnoresMouseInteraction = false;
        _dropdownPanel.Recalculate();
        _popupVisible = true;
    }

    private void ClosePopup()
    {
        if (_dropdownPanel is null)
        {
            _popupVisible = false;
            return;
        }

        _dropdownPanel.Left.Set(HiddenCoord, 0f);
        _dropdownPanel.Top.Set(HiddenCoord, 0f);
        _dropdownPanel.Width.Set(0f, 0f);
        _dropdownPanel.Height.Set(0f, 0f);
        _dropdownPanel.IgnoresMouseInteraction = true;
        _popupVisible = false;
    }

    private void EnsurePopup(UIElement root)
    {
        if (_dropdownPanel is not null)
        {
            if (_dropdownPanel.Parent is null)
                root.Append(_dropdownPanel);
            return;
        }

        _dropdownPanel = new UIPanel
        {
            Left = StyleDimension.FromPixels(HiddenCoord),
            Top = StyleDimension.FromPixels(HiddenCoord),
            Width = StyleDimension.FromPixels(0f),
            Height = StyleDimension.FromPixels(0f),
            BackgroundColor = PopupColor,
            IgnoresMouseInteraction = true,
        };
        _dropdownPanel.SetPadding(PopupPadding);

        _optionList = new UIList
        {
            Width = StyleDimension.FromPixelsAndPercent(-ScrollbarWidth, 1f),
            Height = StyleDimension.Fill,
            ListPadding = ListPadding,
        };

        _scrollbar = new UIScrollbar
        {
            Width = StyleDimension.FromPixels(ScrollbarWidth),
            Top = StyleDimension.FromPixels(ScrollbarVerticalInset),
            Height = StyleDimension.FromPixelsAndPercent(-ScrollbarVerticalInset * 2f, 1f),
            HAlign = 1f,
        };

        _dropdownPanel.Append(_optionList);
        _dropdownPanel.Append(_scrollbar);
        _optionList.SetScrollbar(_scrollbar);
        PopulateOptions();
        root.Append(_dropdownPanel);
    }

    private void PopulateOptions()
    {
        foreach (var option in _options)
            _optionList!.Add(BuildOptionButton(option));
    }

    private UITextPanel<string> BuildOptionButton(T optionValue)
    {
        var optionBtn = new UITextPanel<string>(FormatLabel(optionValue), TextScale)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.FromPixels(OptionHeight),
            BackgroundColor = BackgroundColor,
        };

        optionBtn.OnLeftClick += (_, _) =>
        {
            SetValue(optionValue, notify: true);
            // Popup stays open by design; change to true to close on selection
        };

        optionBtn.OnUpdate += _ => ApplyOptionVisualState(optionBtn, optionValue);
        return optionBtn;
    }

    private void ApplyOptionVisualState(UITextPanel<string> optionBtn, T optionValue)
    {
        bool isSelected = EqualityComparer<T>.Default.Equals(optionValue, _currentValue);
        if (isSelected)
        {
            optionBtn.BackgroundColor = SelectedColor;
            return;
        }

        optionBtn.BackgroundColor = optionBtn.IsMouseHovering ? HoverColor : BackgroundColor;
    }

    private void PositionPopup(float optionContentHeight)
    {
        var buttonDims = GetDimensions();
        float popupWidth = ButtonWidth + ScrollbarWidth;
        float desiredHeight = optionContentHeight + PopupPadding * 2f;
        float boundedHeight = Math.Clamp(desiredHeight, PopupMinHeight, PopupMaxHeight);

        float buttonBottom = buttonDims.Y + buttonDims.Height;
        float spaceBelow = Main.screenHeight - ScreenMargin - buttonBottom - PopupGap;
        float spaceAbove = buttonDims.Y - PopupGap - ScreenMargin;
        bool placeAbove = spaceBelow < boundedHeight && spaceAbove >= spaceBelow;
        float available = placeAbove ? spaceAbove : spaceBelow;
        float finalHeight = Math.Min(boundedHeight, Math.Max(1f, available));

        float left = MathHelper.Clamp(buttonDims.X, ScreenMargin, Main.screenWidth - popupWidth - ScreenMargin);
        float top = placeAbove
            ? buttonDims.Y - PopupGap - finalHeight
            : buttonBottom + PopupGap;

        _dropdownPanel!.Left.Set(left, 0f);
        _dropdownPanel.Top.Set(top, 0f);
        _dropdownPanel.Width.Set(popupWidth, 0f);
        _dropdownPanel.Height.Set(finalHeight, 0f);
    }

    private static string FormatLabel(T value)
    {
        Type t = typeof(T);
        if (t.IsEnum)
            return value.ToString()!;

        if (Nullable.GetUnderlyingType(t) is Type underlying && underlying.IsEnum)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
                return "None";
            object boxed = value;
            return Enum.GetName(underlying, boxed) ?? boxed.ToString()!;
        }

        return value.ToString()!;
    }

    private UIElement GetRootElement()
    {
        UIElement element = this;
        while (element.Parent is not null)
            element = element.Parent;
        return element;
    }

    private static List<T> BuildOptions()
    {
        Type t = typeof(T);
        if (t.IsEnum)
        {
            var options = new List<T>();
            foreach (object val in Enum.GetValues(t))
                options.Add((T)val);
            return options;
        }

        if (Nullable.GetUnderlyingType(t) is Type underlying && underlying.IsEnum)
        {
            var options = new List<T> { default }; // null
            foreach (object val in Enum.GetValues(underlying))
            {
                object nullable = Activator.CreateInstance(
                    typeof(Nullable<>).MakeGenericType(underlying), val)!;
                options.Add((T)nullable);
            }
            return options;
        }

        return [default];
    }
}
