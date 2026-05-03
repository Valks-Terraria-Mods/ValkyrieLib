using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using ReLogic.OS;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace ValkyrieLib;

public class InputField : UIElement
{
    public event Action<string> ValueChanged;

    public int MaxLength { get; set; } = 20;

    private const int Margin = 10;
    private const int RepeatDelay = 30;
    private const int RepeatRate = 3;
    private const int CaretHeight = 20;

    private static Color _transparentColor = new(100, 100, 100, 100);
    private static Color _caretColor = new (240, 240, 240);
    private string _value;
    private bool _hovered;
    private bool _focused;
    private int _cursorIndex;
    private Keys _heldKey = Keys.None;
    private int _heldFrames;
    private int _selectionStart;
    private int _selectionLength;
    private bool _shiftHeld;

    private readonly string _prefix;

    public InputField(string initialValue = "", string prefix = "")
    {
        _prefix = prefix;
        Width.Set(0f, 1f);
        Height.Set(40, 0f);
        SetValue(initialValue, notify: false);
        _cursorIndex = _value.Length;
        _selectionStart = _cursorIndex;
        _selectionLength = 0;
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

        // Reset selection on click (no shift held)
        _selectionStart = _cursorIndex;
        _selectionLength = 0;
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

        DrawSelectionHighlight(spriteBatch, valuePos, FontAssets.MouseText.Value);
        Utils.DrawBorderString(spriteBatch, _value, valuePos, Color.White);

        if (CaretCanBlink(_focused))
        {
            string textBeforeCursor = _value[.._cursorIndex];
            Vector2 textBeforeSize = FontAssets.MouseText.Value.MeasureString(textBeforeCursor);

            float caretX = valuePos.X + textBeforeSize.X - 1f;

            var caretRect = new Rectangle(
                x: (int)caretX,
                y: (int)valuePos.Y,
                width: 2,
                height: CaretHeight
            );
            
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, caretRect, _caretColor);
        }
    }

    private void DrawSelectionHighlight(SpriteBatch spriteBatch, Vector2 valuePos, DynamicSpriteFont font)
    {
        var (start, end) = GetSelectionRange();
        if (start == end) return;

        string before = _value[..start];
        float beforeWidth = font.MeasureString(before).X;

        string selected = _value[start..end];
        float selectedWidth = font.MeasureString(selected).X;

        var highlightPos = new Vector2(valuePos.X + beforeWidth, valuePos.Y);

        var rect = new Rectangle(
            (int)highlightPos.X,
            (int)highlightPos.Y,
            (int)selectedWidth,
            CaretHeight
        );

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, Color.SkyBlue);
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

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();

        string oldValue = _value;

        bool clearSelection = false;
        bool shiftDown = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);
        bool ctrlDown = Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl);

        // ---- Cursor movement with shift selection ----
        if (KeyPressRepeat(Keys.Left))
        {
            if (shiftDown)
            {
                if (_selectionLength == 0)
                    _selectionStart = _cursorIndex;
                _cursorIndex = Math.Max(0, _cursorIndex - 1);
                _selectionLength = _selectionStart - _cursorIndex;
            }
            else
            {
                clearSelection = true;
                _cursorIndex = Math.Max(0, _cursorIndex - 1);
            }
        }

        if (KeyPressRepeat(Keys.Right))
        {
            if (shiftDown)
            {
                if (_selectionLength == 0)
                    _selectionStart = _cursorIndex;
                _cursorIndex = Math.Min(_value.Length, _cursorIndex + 1);
                _selectionLength = _cursorIndex - _selectionStart;
            }
            else
            {
                clearSelection = true;
                _cursorIndex = Math.Min(_value.Length, _cursorIndex + 1);
            }
        }

        // Ctrl+Shift+Left/Right: select whole words
        if (KeyPress(Keys.Left) && ctrlDown && shiftDown)
        {
            if (_selectionLength == 0)
                _selectionStart = _cursorIndex;
            _cursorIndex = FindPrevWordBoundary(_cursorIndex);
            _selectionLength = _cursorIndex - _selectionStart;
        }
        if (KeyPress(Keys.Right) && ctrlDown && shiftDown)
        {
            if (_selectionLength == 0)
                _selectionStart = _cursorIndex;
            _cursorIndex = FindNextWordBoundary(_cursorIndex);
            _selectionLength = _cursorIndex - _selectionStart;
        }

        if (KeyPress(Keys.Home))
        {
            clearSelection = true;
            _cursorIndex = 0;
        }
        if (KeyPress(Keys.End))
        {
            clearSelection = true;
            _cursorIndex = _value.Length;
        }

        if (clearSelection)
        {
            _selectionStart = _cursorIndex;
            _selectionLength = 0;
        }

        // ---- Deletion / Backspace with selection ----
        var (selStart, selEnd) = GetSelectionRange();
        bool selectionActive = selEnd - selStart > 0;

        if (KeyPressRepeat(Keys.Delete))
        {
            if (selectionActive)
            {
                string newValue = _value.Remove(selStart, selEnd - selStart);
                _cursorIndex = selStart;
                SetValue(newValue, notify: false);
                clearSelection = true;
            }
            else if (_cursorIndex < _value.Length)
            {
                string newValue = _value.Remove(_cursorIndex, 1);
                SetValue(newValue, notify: false);
            }
        }

        if (KeyPressRepeat(Keys.Back))
        {
            if (selectionActive)
            {
                string newValue = _value.Remove(selStart, selEnd - selStart);
                _cursorIndex = selStart;
                SetValue(newValue, notify: false);
                clearSelection = true;
            }
            else if (_cursorIndex > 0)
            {
                string newValue = _value.Remove(_cursorIndex - 1, 1);
                _cursorIndex--;
                SetValue(newValue, notify: false);
            }
        }

        // ---- Typing (replaces selection if active) ----
        string typed = Main.GetInputText(_value);
        if (typed != _value)
        {
            if (typed.Length > _value.Length)
            {
                string added = typed[_value.Length..];
                if (selectionActive)
                {
                    string newValue = _value.Remove(selStart, selEnd - selStart).Insert(selStart, added);
                    if (newValue.Length > MaxLength)
                        newValue = newValue[..MaxLength];
                    _cursorIndex = Math.Min(selStart + added.Length, newValue.Length);
                    SetValue(newValue, notify: false);
                    clearSelection = true;
                }
                else
                {
                    string newValue = _value.Insert(_cursorIndex, added);
                    if (newValue.Length > MaxLength)
                        newValue = newValue[..MaxLength];
                    _cursorIndex = Math.Min(_cursorIndex + added.Length, newValue.Length);
                    SetValue(newValue, notify: false);
                }
            }
        }

        // ---- Copy / Paste / Cut ----
        if (KeyPress(Keys.C) && ctrlDown && selectionActive)
            CopySelection();

        if (KeyPress(Keys.X) && ctrlDown)
        {
            if (selectionActive)
            {
                CopySelection();
                string newValue = _value.Remove(selStart, selEnd - selStart);
                _cursorIndex = selStart;
                SetValue(newValue, notify: false);
                clearSelection = true;
                _selectionStart = _cursorIndex;
                _selectionLength = 0;
            }
        }

        if (clearSelection)
        {
            _selectionStart = _cursorIndex;
            _selectionLength = 0;
        }

        // ---- Notify if value changed ----
        if (_value != oldValue)
            ValueChanged?.Invoke(_value);

        // ---- Close on Enter/Escape ----
        if (KeyPress(Keys.Enter) || KeyPress(Keys.Escape))
        {
            _focused = false;
            _selectionStart = 0;
            _selectionLength = 0;
        }
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

    private int FindNextWordBoundary(int pos)
    {
        if (pos >= _value.Length) return _value.Length;

        if (char.IsLetterOrDigit(_value[pos]))
        {
            while (pos < _value.Length && char.IsLetterOrDigit(_value[pos]))
                pos++;
        }
        else
        {
            while (pos < _value.Length && !char.IsLetterOrDigit(_value[pos]))
                pos++;
        }
        return pos;
    }

    private int FindPrevWordBoundary(int pos)
    {
        if (pos <= 0) return 0;
        pos--;

        if (char.IsLetterOrDigit(_value[pos]))
        {
            while (pos > 0 && char.IsLetterOrDigit(_value[pos - 1]))
                pos--;
        }
        else
        {
            while (pos > 0 && !char.IsLetterOrDigit(_value[pos - 1]))
                pos--;
        }
        return pos;
    }

    private void CopySelection()
    {
        var (start, end) = GetSelectionRange();
        string selected = _value[start..end];
        if (!string.IsNullOrEmpty(selected))
            Platform.Get<IClipboard>().Value = selected;
    }

    private void PasteClipboard(bool selectionActive, int selStart, int selEnd)
    {
        string clipText = Platform.Get<IClipboard>().Value;
        clipText = clipText.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        if (clipText.Length == 0) return;

        if (selectionActive)
        {
            string newValue = _value.Remove(selStart, selEnd - selStart).Insert(selStart, clipText);
            if (newValue.Length > MaxLength) newValue = newValue[..MaxLength];
            _cursorIndex = Math.Min(selStart + clipText.Length, newValue.Length);
            SetValue(newValue, notify: false);
        }
        else
        {
            string newValue = _value.Insert(_cursorIndex, clipText);
            if (newValue.Length > MaxLength) newValue = newValue[..MaxLength];
            _cursorIndex = Math.Min(_cursorIndex + clipText.Length, newValue.Length);
            SetValue(newValue, notify: false);
        }
    }

    private (int start, int end) GetSelectionRange()
    {
        int selStart = _selectionStart;
        int selEnd = _selectionStart + _selectionLength;
        if (selStart > selEnd)
            (selStart, selEnd) = (selEnd, selStart);
        return (selStart, selEnd);
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
