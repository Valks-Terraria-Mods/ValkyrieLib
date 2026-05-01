using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.OS;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace ValkyrieLib;

public abstract class TriColorSlider : UIElement
{
    public event Action<Color> ColorChanged;
    public Color SelectedColor { get; private set; }

    private const float SliderHeightPixels = 132f;
    private const float LayoutSpacingPixels = 6f;
    private const float ControlGapHeightPixels = 4f;
    private const float BottomRowHeightPixels = 32f;
    private const float BottomRowSpacingPixels = 4f;
    private const float ButtonHostWidthPixels = 120f;
    private const float ColorSwatchWidthPixels = 58f;
    private const float HexPanelWidthPixels = 120f;
    private const float SwatchBorderLightenFactor = 0.35f;

    private UIColoredImageButton _copyButton;
    private UIColoredImageButton _pasteButton;
    private UIColoredImageButton _randomizeButton;
    private UIPanel _colorSwatch;
    private UIText _hexText;

    private bool _suppressNotifications;

    private readonly List<ColorChannel> _channels = [];

    protected TriColorSlider()
    {
        Width.Set(0f, 1f);
        Height.Set(SliderHeightPixels, 0f);
    }

    public void SetColor(Color color, bool notify = true)
    {
        UnpackColorIntoFields(color);
        RunWithoutNotifications(SyncSlidersFromFields);
        RefreshColorDisplay(notify);
    }

    protected abstract Color GetCurrentColor();
    protected abstract void UnpackColorIntoFields(Color color);

    protected void RegisterColorChannel(Slider slider, Func<float> getValue, Action<float> setValue)
    {
        _channels.Add(new ColorChannel(slider, getValue, setValue));
    }

    protected void RandomizeChannels()
    {
        RandomizeChannelFields();
        RunWithoutNotifications(SyncSlidersFromFields);
        RefreshColorDisplay(true);
    }

    protected void SyncSlidersFromFields()
    {
        foreach (var ch in _channels)
            ch.Slider.SetValue(ch.GetValue(), notify: false);
    }

    protected void RandomizeChannelFields()
    {
        foreach (var ch in _channels)
            ch.SetValue(Main.rand.NextFloat());
    }

    protected void RefreshColorDisplay(bool notify)
    {
        Color color = GetCurrentColor();

        _colorSwatch.BackgroundColor = color;
        _colorSwatch.BorderColor = Color.Lerp(color, Color.White, SwatchBorderLightenFactor);

        _hexText.SetText($"#{color.R:X2}{color.G:X2}{color.B:X2}");
        PublishSelectedColor(color, notify);
    }

    protected void PublishSelectedColor(Color color, bool notify)
    {
        SelectedColor = color;
        if (notify)
            ColorChanged?.Invoke(color);
    }

    protected static bool TryParseHexColor(string hexText, out Color color)
    {
        if (string.IsNullOrWhiteSpace(hexText))
        {
            color = Color.White;
            return false;
        }

        string normalized = hexText.Trim().TrimStart('#');
        if (normalized.Length != 6)
        {
            color = Color.White;
            return false;
        }

        if (!uint.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
        {
            color = Color.White;
            return false;
        }

        int red = (int)((packed >> 16) & 0xFF);
        int green = (int)((packed >> 8) & 0xFF);
        int blue = (int)(packed & 0xFF);
        color = new Color(red, green, blue);
        return true;
    }

    protected static UIColoredImageButton SetupActionButton(UIColoredImageButton button, float leftPixels, Action action)
    {
        button.Left = StyleDimension.FromPixels(leftPixels);
        button.SetVisibility(1f, 0.55f);
        button.OnLeftMouseDown += (_, _) => action();
        return button;
    }

    protected void RunWithoutNotifications(Action action)
    {
        _suppressNotifications = true;

        try
        {
            action();
        }
        finally
        {
            _suppressNotifications = false;
        }
    }

    protected Slider CreateChannelSlider(float initialValue,
        Action<float> onChanged,
        Func<float, Color> gradientAt,
        Func<float> getValue,
        Action<float> setValue)
    {
        Slider slider = new(initialValue, 0f, 1f);
        slider.Width.Set(0f, 1f);
        slider.GradientColorAt = gradientAt;
        slider.ValueChanged += value =>
        {
            if (_suppressNotifications)
                return;
            onChanged(value);
        };

        _channels.Add(new ColorChannel(slider, getValue, setValue));

        return slider;
    }

    protected void BuildCommonFooter(VBoxContainer layout)
    {
        UIElement controlGap = new()
        {
            Height = StyleDimension.FromPixels(ControlGapHeightPixels),
        };
        layout.Append(controlGap);

        HBoxContainer bottomRow = new()
        {
            Spacing = BottomRowSpacingPixels,
            Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
            Height = StyleDimension.FromPixels(BottomRowHeightPixels),
        };
        layout.Append(bottomRow);

        UIElement buttonHost = new()
        {
            Width = StyleDimension.FromPixels(ButtonHostWidthPixels),
            Height = StyleDimension.FromPixels(BottomRowHeightPixels),
        };
        bottomRow.Append(buttonHost);

        _copyButton = SetupActionButton(GameAssets.ColorActions.Copy, 0f, CopyHexToClipboard);
        _pasteButton = SetupActionButton(GameAssets.ColorActions.Paste, 40f, PasteHexFromClipboard);
        _randomizeButton = SetupActionButton(GameAssets.ColorActions.Randomize, 80f, RandomizeChannels);

        buttonHost.Append(_copyButton);
        buttonHost.Append(_pasteButton);
        buttonHost.Append(_randomizeButton);

        _colorSwatch = new UIPanel
        {
            Width = StyleDimension.FromPixels(ColorSwatchWidthPixels),
            Height = StyleDimension.FromPixels(BottomRowHeightPixels),
        };
        _colorSwatch.SetPadding(0f);
        bottomRow.Append(_colorSwatch);

        UIElement spacer = new()
        {
            Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
            Height = StyleDimension.FromPixels(BottomRowHeightPixels),
        };
        bottomRow.Append(spacer);

        UIPanel hexPanel = new()
        {
            Width = StyleDimension.FromPixels(HexPanelWidthPixels),
            Height = StyleDimension.FromPixels(BottomRowHeightPixels),
        };
        hexPanel.SetPadding(0f);
        bottomRow.Append(hexPanel);

        _hexText = new UIText("#000000")
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
        };
        hexPanel.Append(_hexText);
    }

    protected void InitializeSliderLayout(IEnumerable<Slider> channelSliders, Color initialColor)
    {
        VBoxContainer layout = new()
        {
            Spacing = LayoutSpacingPixels,
            Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
            Height = StyleDimension.FromPixelsAndPercent(0f, 1f),
        };
        Append(layout);

        foreach (Slider slider in channelSliders)
            layout.Append(slider);

        BuildCommonFooter(layout);
        SetColor(initialColor, notify: false);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (ContainsPoint(Main.MouseScreen) && Main.LocalPlayer is not null)
            Main.LocalPlayer.mouseInterface = true;

        HsvSliderTooltipRenderer.DrawIfHovering(
            spriteBatch,
            _copyButton.IsMouseHovering,
            _pasteButton.IsMouseHovering,
            _randomizeButton.IsMouseHovering);
    }

    protected Slider[] CreateChannelSliders(params ChannelSpec[] specs)
    {
        var sliders = new Slider[specs.Length];
        for (int i = 0; i < specs.Length; i++)
        {
            var specLocal = specs[i];
            Slider slider = new(0f, 0f, 1f);
            slider.Width.Set(0f, 1f);
            slider.GradientColorAt = specLocal.GradientAt;
            slider.ValueChanged += v =>
            {
                if (_suppressNotifications) return;
                float clamped = MathHelper.Clamp(v, 0f, 1f);
                specLocal.SetValue(clamped);
                RefreshColorDisplay(true);
            };

            _channels.Add(new ColorChannel(slider, specLocal.GetValue, specLocal.SetValue));
            sliders[i] = slider;
        }
        return sliders;
    }

    protected struct ColorChannel(Slider slider, Func<float> getValue, Action<float> setValue)
    {
        public Slider Slider = slider;
        public Func<float> GetValue = getValue;
        public Action<float> SetValue = setValue;
    }

    protected readonly struct ChannelSpec(Func<float, Color> gradientAt, Func<float> getValue, Action<float> setValue)
    {
        public readonly Func<float, Color> GradientAt = gradientAt;
        public readonly Func<float> GetValue = getValue;
        public readonly Action<float> SetValue = setValue;
    }

    private void CopyHexToClipboard() => Platform.Get<IClipboard>().Value = _hexText.Text;

    private void PasteHexFromClipboard()
    {
        string clipboard = Platform.Get<IClipboard>().Value;
        if (TryParseHexColor(clipboard, out Color color))
            SetColor(color, notify: true);
    }

    private static class HsvSliderTooltipRenderer
    {
        internal static void DrawIfHovering(SpriteBatch spriteBatch, bool copyHovered, bool pasteHovered, bool randomizeHovered)
        {
            if (!TryGetTooltip(copyHovered, pasteHovered, randomizeHovered, out string tooltip))
                return;

            Vector2 drawPos = CalculateDrawPosition(tooltip);
            byte textShade = Main.mouseTextColor;
            Color textColor = new(textShade, textShade, textShade);

            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tooltip, drawPos.X, drawPos.Y, textColor, Color.Black, Vector2.Zero);
        }

        private static bool TryGetTooltip(bool copyHovered, bool pasteHovered, bool randomizeHovered, out string tooltip)
        {
            tooltip = null;

            if (copyHovered)
                tooltip = Language.GetTextValue("UI.CopyColorToClipboard");
            else if (pasteHovered)
                tooltip = Language.GetTextValue("UI.PasteColorFromClipboard");
            else if (randomizeHovered)
                tooltip = Language.GetTextValue("UI.RandomizeColor");

            return !string.IsNullOrEmpty(tooltip);
        }

        private static Vector2 CalculateDrawPosition(string tooltip)
        {
            float textWidth = FontAssets.MouseText.Value.MeasureString(tooltip).X;
            Vector2 drawPos = new(Main.mouseX + 16f, Main.mouseY + 16f);

            if (drawPos.Y > Main.screenHeight - 30)
                drawPos.Y = Main.screenHeight - 30;

            if (drawPos.X > Main.screenWidth - textWidth)
                drawPos.X = Main.screenWidth - textWidth - 16f;

            return drawPos;
        }
    }
}
