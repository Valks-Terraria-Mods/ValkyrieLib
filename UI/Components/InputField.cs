using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
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
    private static Color _caretColor = new (240, 240, 240);
    private const int Margin = 10;
    private string _value;
    private bool _hovered;
    private bool _focused;
    private int _cursorIndex;
    private Keys _heldKey = Keys.None;
    private int _heldFrames;
    private const int RepeatDelay = 30;
    private const int RepeatRate = 3;

    private readonly string _prefix;

    public InputField(string initialValue = "", string prefix = "")
    {
        _prefix = prefix;
        Width.Set(0f, 1f);
        Height.Set(40, 0f);
        SetValue(initialValue, notify: false);
        _cursorIndex = _value.Length;
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        _focused = true;

        CalculatedStyle dimensions = GetDimensions();
        float prefixWidth = GetPrefixWidth();
        float gap = string.IsNullOrEmpty(_prefix) ? 0 : 10;
        float textStartX = dimensions.X + Margin + prefixWidth + gap;

        float mouseX = Main.MouseScreen.X;
        float relativeX = mouseX - textStartX;

        // Find the character index that matches the click position
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        int index = 0;
        float accumulatedWidth = 0f;

        while (index < _value.Length)
        {
            float charWidth = font.MeasureString(_value[index].ToString()).X;
            if (accumulatedWidth + charWidth / 2f > relativeX)
                break;
            accumulatedWidth += charWidth;
            index++;
        }
        _cursorIndex = Math.Clamp(index, 0, _value.Length);
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
        _cursorIndex = Math.Min(_cursorIndex, _value.Length);

        if (notify)
            ValueChanged?.Invoke(_value);
    }

    private float GetPrefixWidth()
    {
        if (string.IsNullOrEmpty(_prefix))
            return 0f;
        return FontAssets.MouseText.Value.MeasureString(_prefix).X;
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

        DrawSplicedPanel(spriteBatch, ValkyrieAPI.UI.Assets.CategoryPanel, x, y, width, height, _transparentColor);

        if (_hovered || _focused)
            DrawSplicedPanel(spriteBatch, ValkyrieAPI.UI.Assets.CategoryPanelBorder, x, y, width, height, Color.White);
    }

    private void DrawValueText(SpriteBatch spriteBatch, CalculatedStyle dimensions)
    {
        float prefixWidth = GetPrefixWidth();
        float gap = string.IsNullOrEmpty(_prefix) ? 0 : 10;  // 10px spacing after prefix
        float valueX = dimensions.X + Margin + prefixWidth + gap;
        Vector2 valuePos = new(valueX, dimensions.Y + Margin);

        Utils.DrawBorderString(spriteBatch, _value, valuePos, Color.White);

        if (CaretCanBlink(_focused))
        {
            string textBeforeCursor = _value[.._cursorIndex];
            Vector2 textBeforeSize = FontAssets.MouseText.Value.MeasureString(textBeforeCursor);

            float caretX = valuePos.X + textBeforeSize.X - 1f;
            const float CaretHeight = 20;

            var caretRect = new Rectangle(
                x: (int)caretX,
                y: (int)valuePos.Y,
                width: 2,
                height: (int)CaretHeight
            );

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, caretRect, _caretColor);
        }
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

        string oldValue = _value;

        KeyboardState keyState = Main.keyState;
        KeyboardState oldKeyState = Main.oldKeyState;

        // Cursor movement
        if (KeyPressRepeat(Keys.Left))
            _cursorIndex = Math.Max(0, _cursorIndex - 1);
        if (KeyPressRepeat(Keys.Right))
            _cursorIndex = Math.Min(_value.Length, _cursorIndex + 1);
        if (KeyPress(Keys.Home))
            _cursorIndex = 0;
        if (KeyPress(Keys.End))
            _cursorIndex = _value.Length;

        if (KeyPressRepeat(Keys.Delete) && _cursorIndex < _value.Length)
        {
            string newValue = _value.Remove(_cursorIndex, 1);
            SetValue(newValue, notify: false);
        }
        if (KeyPressRepeat(Keys.Back) && _cursorIndex > 0)
        {
            string newValue = _value.Remove(_cursorIndex - 1, 1);
            _cursorIndex--;
            SetValue(newValue, notify: false);
        }

        // Main.GetInputText always appends new characters to the end of the string.
        // To support typing at any cursor position, we take only the added portion
        // and insert it at the current cursor index, then advance the caret.
        string typed = Main.GetInputText(_value);
        if (typed != _value)
        {
            string old = _value;
            if (typed.Length > old.Length)
            {
                string added = typed[old.Length..];
                string newValue = old.Insert(_cursorIndex, added);
                if (newValue.Length > MaxLength)
                    newValue = newValue[..MaxLength];
                _cursorIndex = Math.Min(_cursorIndex + added.Length, newValue.Length);
                SetValue(newValue, notify: false);
            }
        }

        if (_value != oldValue)
            ValueChanged?.Invoke(_value);

        if (KeyPress(Keys.Enter) || KeyPress(Keys.Escape))
            _focused = false;
    }

    private bool KeyPressRepeat(Keys key)
    {
        bool down = Main.keyState.IsKeyDown(key);
        bool wasDown = Main.oldKeyState.IsKeyDown(key);

        // Initial press
        if (down && !wasDown)
        {
            _heldKey = key;
            _heldFrames = 0;
            return true;
        }

        // Held down
        if (down && wasDown && _heldKey == key)
        {
            _heldFrames++;
            if (_heldFrames >= RepeatDelay && (_heldFrames - RepeatDelay) % RepeatRate == 0)
                return true;
            return false;
        }

        // Key released or different key took over
        if (!down && wasDown && _heldKey == key)
        {
            _heldKey = Keys.None;
            _heldFrames = 0;
        }

        return false;
    }

    private static bool KeyPress(Keys key)
    {
        return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
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

    private static void DrawSplicedPanel(SpriteBatch spriteBatch, Asset<Texture2D> texture, int x, int y, int width, int height, Color color)
    {
        Utils.DrawSplicedPanel(spriteBatch, texture.Value, x, y, width, height, leftEnd: Margin, rightEnd: Margin, topEnd: Margin, bottomEnd: Margin, color);
    }
}
