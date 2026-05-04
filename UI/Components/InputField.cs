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

    private const int Margin = 10;
    private const int RepeatDelay = 30;
    private const int RepeatRate = 3;
    private const int CaretHeight = 20;

    private static Color _transparentColor = new(100, 100, 100, 100);
    private static Color _caretColor = new(240, 240, 240);
    private string _value;
    private bool _hovered;
    private bool _focused;
    private int _cursorIndex;
    private Keys _heldKey = Keys.None;
    private int _heldFrames;
    private int _selectionStart;
    private int _selectionLength;
    private bool _pasteHandledThisFrame;
    private bool _isDraggingSelection;

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

        // Reset selection on click (no shift held)
        _cursorIndex = GetCursorIndexFromMouse();
        _selectionStart = _cursorIndex;
        _selectionLength = 0;
        _isDraggingSelection = true;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        _isDraggingSelection = false;
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

        UpdateDragSelection();
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

    public void ClearFocus()
    {
        _focused = false;
    }

    // ---------- Helper methods ----------

    private float GetPrefixWidth()
    {
        if (string.IsNullOrEmpty(_prefix))
            return 0f;
        return FontAssets.MouseText.Value.MeasureString(_prefix).X;
    }

    private float GetPrefixGap()
    {
        return string.IsNullOrEmpty(_prefix) ? 0 : 10; // 10px spacing after prefix
    }

    private float GetTextStartX(CalculatedStyle dimensions)
    {
        return dimensions.X + Margin + GetPrefixWidth() + GetPrefixGap();
    }

    private int GetCursorIndexFromMouse()
    {
        CalculatedStyle dimensions = GetDimensions();
        float textStartX = GetTextStartX(dimensions);
        float relativeX = Main.MouseScreen.X - textStartX;
        return GetCursorIndexFromRelativeX(relativeX);
    }

    private int GetCursorIndexFromRelativeX(float relativeX)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        int index = 0;
        float accumulatedWidth = 0f;
        while (index < _value.Length)
        {
            float charWidth = font.MeasureString(_value[index].ToString()).X;
            if (accumulatedWidth + (charWidth / 2f) > relativeX)
                break;
            accumulatedWidth += charWidth;
            index++;
        }
        return Math.Clamp(index, 0, _value.Length);
    }

    private void UpdateDragSelection()
    {
        // Stop dragging if focus is lost
        if (!_focused)
        {
            _isDraggingSelection = false;
            return;
        }

        if (_isDraggingSelection)
        {
            if (Main.mouseLeft)
            {
                // Still dragging: move the moving end of the selection
                _cursorIndex = GetCursorIndexFromMouse();
                _selectionLength = _cursorIndex - _selectionStart;
            }
            else
            {
                // Mouse button released
                _isDraggingSelection = false;
            }
        }
    }

    private void ClearSelection()
    {
        _selectionStart = _cursorIndex;
        _selectionLength = 0;
    }

    private (int start, int end) GetSelectionRange()
    {
        int selStart = _selectionStart;
        int selEnd = _selectionStart + _selectionLength;
        if (selStart > selEnd)
            (selStart, selEnd) = (selEnd, selStart);
        return (selStart, selEnd);
    }

    // ---------- Drawing helpers ----------

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

    private void DrawPrefixText(SpriteBatch spriteBatch, CalculatedStyle dimensions)
    {
        Vector2 titlePos = new(dimensions.X + Margin, dimensions.Y + Margin);
        Utils.DrawBorderString(spriteBatch, _prefix, titlePos, Color.White);
    }

    private void DrawValueText(SpriteBatch spriteBatch, CalculatedStyle dimensions)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        float textStartX = GetTextStartX(dimensions);
        Vector2 valuePos = new(textStartX, dimensions.Y + Margin);

        DrawSelectionHighlight(spriteBatch, valuePos, font);
        Utils.DrawBorderString(spriteBatch, _value, valuePos, Color.White);

        if (CaretCanBlink(_focused))
            DrawCaret(spriteBatch, valuePos, font);
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
            x: (int)highlightPos.X,
            y: (int)highlightPos.Y,
            width: (int)selectedWidth,
            height: CaretHeight
        );

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, Color.SkyBlue);
    }

    private void DrawCaret(SpriteBatch spriteBatch, Vector2 valuePos, DynamicSpriteFont font)
    {
        string textBeforeCursor = _value[.._cursorIndex];
        Vector2 textBeforeSize = font.MeasureString(textBeforeCursor);

        float caretX = valuePos.X + textBeforeSize.X - 1f;

        var caretRect = new Rectangle(
            x: (int)caretX,
            y: (int)valuePos.Y,
            width: 2,
            height: CaretHeight
        );

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, caretRect, _caretColor);
    }

    // ---------- Input handling ----------

    private void UpdateFocusedTextInput()
    {
        if (!_focused || _isDraggingSelection)
            return;

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();

        string oldValue = _value;

        bool shiftDown = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);
        bool ctrlDown = Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl);

        HandleCursorMovement(shiftDown, ctrlDown);
        HandleDeletion();

        var (selStart, selEnd) = GetSelectionRange();
        bool selectionActive = selEnd > selStart;

        ProcessTyping(selectionActive, selStart, selEnd);

        bool enterOrEscape = KeyPress(Keys.Enter) || KeyPress(Keys.Escape);

        _pasteHandledThisFrame = false;

        if (_value != oldValue)
            ValueChanged?.Invoke(_value);

        // ---- Close on Enter/Escape ----
        if (enterOrEscape)
        {
            _focused = false;
            _selectionStart = 0;
            _selectionLength = 0;
        }
    }

    private void HandleCursorMovement(bool shiftDown, bool ctrlDown)
    {
        HandleMoveLeft(shiftDown);
        HandleMoveRight(shiftDown);
        HandleWordBoundarySelection(ctrlDown, shiftDown);
        HandleHomeEnd();
    }

    private void HandleMoveLeft(bool shiftDown)
    {
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
                _cursorIndex = Math.Max(0, _cursorIndex - 1);
                ClearSelection();
            }
        }
    }

    private void HandleMoveRight(bool shiftDown)
    {
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
                _cursorIndex = Math.Min(_value.Length, _cursorIndex + 1);
                ClearSelection();
            }
        }
    }

    private void HandleWordBoundarySelection(bool ctrlDown, bool shiftDown)
    {
        if (!ctrlDown || !shiftDown) return;

        if (KeyPress(Keys.Left))
            SelectToWordBoundary(FindPrevWordBoundary(_cursorIndex));
        if (KeyPress(Keys.Right))
            SelectToWordBoundary(FindNextWordBoundary(_cursorIndex));
    }

    private void SelectToWordBoundary(int newCursorIndex)
    {
        if (_selectionLength == 0)
            _selectionStart = _cursorIndex;
        _cursorIndex = newCursorIndex;
        _selectionLength = _cursorIndex - _selectionStart;
    }

    private void HandleHomeEnd()
    {
        if (KeyPress(Keys.Home))
        {
            _cursorIndex = 0;
            ClearSelection();
        }
        if (KeyPress(Keys.End))
        {
            _cursorIndex = _value.Length;
            ClearSelection();
        }
    }

    private void HandleDeletion()
    {
        if (KeyPressRepeat(Keys.Delete))
            HandleDeleteKey();
        if (KeyPressRepeat(Keys.Back))
            HandleBackspaceKey();
    }

    private void HandleDeleteKey()
    {
        var (selStart, selEnd) = GetSelectionRange();
        bool selectionActive = selEnd > selStart;
        if (selectionActive)
        {
            DeleteTextRange(selStart, selEnd - selStart);
            _cursorIndex = selStart;
        }
        else if (_cursorIndex < _value.Length)
        {
            DeleteTextRange(_cursorIndex, 1);
        }
        ClearSelection();
    }

    private void HandleBackspaceKey()
    {
        var (selStart, selEnd) = GetSelectionRange();
        bool selectionActive = selEnd > selStart;
        if (selectionActive)
        {
            DeleteTextRange(selStart, selEnd - selStart);
            _cursorIndex = selStart;
        }
        else if (_cursorIndex > 0)
        {
            _cursorIndex--;
            DeleteTextRange(_cursorIndex, 1);
        }
        ClearSelection();
    }

    private void DeleteTextRange(int start, int count)
    {
        string newValue = _value.Remove(start, count);
        SetValue(newValue, notify: false);
    }

    // ---- Typing (replaces selection if active) ----
    private void ProcessTyping(bool selectionActive, int selStart, int selEnd)
    {
        string typed = Main.GetInputText(_value);
        if (typed != _value && !_pasteHandledThisFrame && typed.Length > _value.Length)
        {
            string added = typed[_value.Length..];
            if (selectionActive)
            {
                string newValue = _value.Remove(selStart, selEnd - selStart).Insert(selStart, added);
                if (newValue.Length > MaxLength)
                    newValue = newValue[..MaxLength];
                _cursorIndex = Math.Min(selStart + added.Length, newValue.Length);
                SetValue(newValue, notify: false);
                ClearSelection();
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

    // ---------- Word boundary navigation ----------

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

    // ---------- Keyboard state helpers ----------

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
            return _heldFrames >= RepeatDelay && (_heldFrames - RepeatDelay) % RepeatRate == 0;
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

    // ---------- Caret & focus utilities ----------

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
