using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace ValkyrieLib;

public class InputField : UIElement
{
    public event Action<string> ValueChanged;

    public int MaxLength { get; set; } = 20;

    private static Color _transparentColor = new(100, 100, 100, 100);
    private const int Margin = 10;
    private string _value;
    private bool _hovered;
    private bool _focused;

    private readonly string _prefix;

    public InputField(string prefix, string initialValue = "")
    {
        _prefix = prefix;
        Width.Set(0f, 1f);
        Height.Set(40, 0f);
        SetValue(initialValue, notify: false);
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        _focused = true;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        _hovered = true;
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        _hovered = false;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (HasClickedOutside(this, _focused))
            _focused = false;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        UpdateFocusedTextInput();

        CalculatedStyle dimensions = GetDimensions();
        DrawBackground(spriteBatch, dimensions);
        DrawPrefixText(spriteBatch, dimensions);
        DrawValueText(spriteBatch, dimensions);

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    public void SetValue(string value, bool notify = true)
    {
        string normalizedValue = value.Replace(" ", "");

        if (normalizedValue.Length > MaxLength)
            normalizedValue = normalizedValue[..MaxLength];

        _value = normalizedValue;

        if (notify)
            ValueChanged?.Invoke(_value);
    }

    public void ClearFocus()
    {
        _focused = false;
    }

    private void DrawBackground(SpriteBatch spriteBatch, CalculatedStyle dimensions)
    {
        int x = (int)dimensions.X;
        int y = (int)dimensions.Y;
        int width = (int)dimensions.Width;
        int height = (int)dimensions.Height;

        DrawSplicedPanel(spriteBatch, GameAssets.CategoryPanel, x, y, width, height, _transparentColor);

        if (_hovered || _focused)
            DrawSplicedPanel(spriteBatch, GameAssets.CategoryPanelBorder, x, y, width, height, Color.White);
    }

    private void DrawValueText(SpriteBatch spriteBatch, CalculatedStyle dimensions)
    {
        const int ValueLeftPadding = 70;

        Vector2 valuePos = new(dimensions.X + ValueLeftPadding, dimensions.Y + Margin);

        Utils.DrawBorderString(spriteBatch, _value, valuePos, Color.White);

        if (!CaretCanBlink(_focused))
            return;

        DrawCaret(spriteBatch, valuePos, _value);
    }

    private void DrawPrefixText(SpriteBatch spriteBatch, CalculatedStyle dimensions)
    {
        Vector2 titlePos = new(dimensions.X + Margin, dimensions.Y + Margin);
        Utils.DrawBorderString(spriteBatch, _prefix, titlePos, Color.White);
    }

    private void UpdateFocusedTextInput()
    {
        if (!_focused)
            return;

        // Prevent the player moving around when typing
        PlayerInput.WritingText = true;

        // Handles special multilanguage characters
        Main.instance.HandleIME();

        SetValue(Main.GetInputText(_value));

        KeyboardState keys = Main.keyState;

        if (keys.IsKeyDown(Keys.Enter) || keys.IsKeyDown(Keys.Escape))
            _focused = false;
    }

    private static bool CaretCanBlink(bool isFocused)
    {
        const int CaretVisibleTicks = 20;
        const int CaretBlinkPeriodTicks = 40;

        return isFocused && Main.GameUpdateCount % CaretBlinkPeriodTicks < CaretVisibleTicks;
    }

    private static bool HasClickedOutside(UIElement element, bool isFocused)
    {
        return isFocused && !element.ContainsPoint(Main.MouseScreen) && (Main.mouseLeft || Main.mouseRight);
    }

    private static void DrawCaret(SpriteBatch spriteBatch, Vector2 valuePos, string valueText)
    {
        const int CaretSpacingPixels = 2;

        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(valueText);
        Vector2 caretPos = new(valuePos.X + textSize.X + CaretSpacingPixels, valuePos.Y);

        Utils.DrawBorderString(spriteBatch, "|", caretPos, Color.White);
    }

    private static void DrawSplicedPanel(SpriteBatch spriteBatch, Asset<Texture2D> texture, int x, int y, int width, int height, Color color)
    {
        Utils.DrawSplicedPanel(spriteBatch, texture.Value, x, y, width, height, leftEnd: Margin, rightEnd: Margin, topEnd: Margin, bottomEnd: Margin, color);
    }
}
