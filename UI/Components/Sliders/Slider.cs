using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ValkyrieLib;

public class Slider : UIElement
{
    /// <summary>
    /// Fired when the slider value changes due to user input.
    /// </summary>
    public event Action<float> ValueChanged;

    /// <summary>
    /// Current value represented by this slider.
    /// </summary>
    public float Value { get; private set; }

    public Func<float, Color> GradientColorAt { get; set; }

    private const float KnobWidthBase = 12f;
    private const float KnobHeightBase = 14f;

    private Color _trackBackgroundColor = new(18, 22, 30);
    private Color _trackBorderColor = ValkyrieAPI.UI.Colors.Border;
    private Color _fillColor = new(255, 255, 255);
    private Color _knobFaceColor = new(24, 32, 44);
    private Color _knobBorderColor = new(146, 190, 255);
    private Color _knobGripColor = new(186, 216, 255);

    private readonly UIElement _track;
    private readonly float _minValue;
    private readonly float _maxValue;
    private readonly float _range;

    private bool _dragging;

    public Slider(float initialValue, float minValue, float maxValue)
    {
        const float SliderHeight = 24f;
        const float TrackLeft = 5f;
        const float TrackRightInset = 10f;
        const float TrackHeight = 8f;
        const float TrackTop = (SliderHeight - TrackHeight) * 0.5f;

        if (maxValue < minValue)
            (minValue, maxValue) = (maxValue, minValue);

        _minValue = minValue;
        _maxValue = maxValue;
        _range = _maxValue - _minValue;

        Width.Set(0, 1f);
        Height.Set(SliderHeight, 0f);

        _track = new UIElement();
        _track.Left.Set(TrackLeft, 0f);
        _track.Width.Set(-TrackRightInset, 1f);
        _track.Top.Set(TrackTop, 0f);
        _track.Height.Set(TrackHeight, 0f);
        _track.OnLeftMouseDown += OnTrackMouseDown;
        _track.OnLeftMouseUp += OnTrackMouseUp;
        Append(_track);

        SetValue(initialValue, notify: false);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!_dragging)
            return;

        if (!Main.mouseLeft)
        {
            StopDragging();
            return;
        }

        UpdateValueFromMouse();
    }

    public void SetValue(float value, bool notify)
    {
        const float ValueEpsilon = 0.0001f;
        float clamped = MathHelper.Clamp(value, _minValue, _maxValue);

        if (Math.Abs(Value - clamped) <= ValueEpsilon)
            return;

        Value = clamped;

        if (notify)
            ValueChanged?.Invoke(Value);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        DrawSlider(spriteBatch);

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    private void OnTrackMouseDown(UIMouseEvent evt, UIElement listeningElement)
    {
        _dragging = true;
        UpdateValueFromMouse();
    }

    private void OnTrackMouseUp(UIMouseEvent evt, UIElement listeningElement)
    {
        StopDragging();
    }

    private void StopDragging()
    {
        _dragging = false;
    }

    /// <summary>
    /// Converts cursor X position to a clamped value on the slider range.
    /// </summary>
    private void UpdateValueFromMouse()
    {
        CalculatedStyle dimensions = _track.GetDimensions();

        if (dimensions.Width <= 0f)
            return;

        const float knobWidth = KnobWidthBase;

        float sliderMinX = dimensions.X + 1f;
        float sliderWidth = Math.Max(1f, dimensions.Width - 2f);
        float travelWidth = Math.Max(1f, sliderWidth - knobWidth);

        float knobLeft = Main.MouseScreen.X - (knobWidth * 0.5f);
        float t = (knobLeft - sliderMinX) / travelWidth;
        t = MathHelper.Clamp(t, 0f, 1f);

        float value = _range <= 0f ? _minValue : _minValue + (t * _range);
        SetValue(value, notify: true);
    }

    private void DrawSlider(SpriteBatch spriteBatch)
    {
        SliderGeometryBuildRequest buildRequest = new(_track, Value, _minValue, _range, KnobWidthBase, KnobHeightBase);

        if (!SliderRenderer.TryCreateGeometry(buildRequest, out SliderGeometry geometry))
            return;

        SliderRenderer.Draw(spriteBatch, geometry, new SliderRenderStyle(
            _trackBorderColor,
            _trackBackgroundColor,
            _fillColor,
            _knobBorderColor,
            _knobFaceColor,
            _knobGripColor,
            GradientColorAt));
    }

    private static class SliderRenderer
    {
        internal static bool TryCreateGeometry(SliderGeometryBuildRequest request, out SliderGeometry geometry)
        {
            CalculatedStyle dimensions = request.Track.GetDimensions();
            geometry = default;

            if (dimensions.Width <= 2f || dimensions.Height <= 2f)
                return false;

            Rectangle outerRect = new((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height);
            Rectangle innerRect = new(outerRect.X + 1, outerRect.Y + 1, Math.Max(1, outerRect.Width - 2), Math.Max(1, outerRect.Height - 2));

            float normalizedValue = request.Range <= 0f ? 0f : (request.Value - request.MinValue) / request.Range;
            normalizedValue = MathHelper.Clamp(normalizedValue, 0f, 1f);

            int knobPixelWidth = Math.Max(1, (int)MathF.Round(request.KnobWidthBase));
            int knobPixelHeight = Math.Max(1, (int)MathF.Round(request.KnobHeightBase));
            int travelWidth = Math.Max(0, innerRect.Width - knobPixelWidth);

            int knobLeft = innerRect.X + (int)MathF.Round(travelWidth * normalizedValue);
            int knobTop = innerRect.Y + ((innerRect.Height - knobPixelHeight) / 2);
            int fillWidth = Math.Clamp(knobLeft + knobPixelWidth - innerRect.X, 0, innerRect.Width);

            Rectangle fillRect = new(innerRect.X, innerRect.Y, fillWidth, innerRect.Height);
            Rectangle knobRect = new(knobLeft, knobTop, knobPixelWidth, knobPixelHeight);
            Rectangle knobInnerRect = new(knobRect.X + 1, knobRect.Y + 1, Math.Max(1, knobRect.Width - 2), Math.Max(1, knobRect.Height - 2));

            geometry = new SliderGeometry(outerRect, innerRect, fillRect, knobRect, knobInnerRect);
            return true;
        }

        internal static void Draw(
            SpriteBatch spriteBatch,
            SliderGeometry geometry,
            SliderRenderStyle style)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            DrawTrack(spriteBatch, pixel, geometry, style.TrackBorderColor);

            if (style.GradientColorAt is null)
                DrawSolidTrackFill(spriteBatch, pixel, geometry, style);
            else
                DrawGradientFill(spriteBatch, pixel, geometry.InnerRect, style.GradientColorAt);

            DrawKnob(spriteBatch, pixel, geometry, style);
        }

        private static void DrawTrack(SpriteBatch spriteBatch, Texture2D pixel, SliderGeometry geometry, Color trackBorderColor)
        {
            spriteBatch.Draw(pixel, geometry.OuterRect, trackBorderColor);
        }

        private static void DrawSolidTrackFill(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            SliderGeometry geometry,
            SliderRenderStyle style)
        {
            spriteBatch.Draw(pixel, geometry.InnerRect, style.TrackBackgroundColor);
            spriteBatch.Draw(pixel, geometry.FillRect, style.FillColor);
        }

        private static void DrawGradientFill(SpriteBatch spriteBatch, Texture2D pixel, Rectangle innerRect, Func<float, Color> gradientColorAt)
        {
            for (int x = 0; x < innerRect.Width; x++)
            {
                float point = innerRect.Width <= 1 ? 0f : x / (float)(innerRect.Width - 1);
                Color sampleColor = gradientColorAt(point);
                spriteBatch.Draw(pixel, new Rectangle(innerRect.X + x, innerRect.Y, 1, innerRect.Height), sampleColor);
            }
        }

        private static void DrawKnob(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            SliderGeometry geometry,
            SliderRenderStyle style)
        {
            spriteBatch.Draw(pixel, geometry.KnobRect, style.KnobBorderColor);
            spriteBatch.Draw(pixel, geometry.KnobInnerRect, style.KnobFaceColor);

            int gripHeight = Math.Max(Math.Max(2, (int)MathF.Round(4f)), geometry.KnobInnerRect.Height - Math.Max(2, (int)MathF.Round(6f)));
            int gripTop = geometry.KnobInnerRect.Y + ((geometry.KnobInnerRect.Height - gripHeight) / 2);
            int gripInset = Math.Max(1, (int)MathF.Round(3f));
            int gripSpacing = Math.Max(1, (int)MathF.Round(2f));

            for (int i = 0; i < 3; i++)
            {
                int gripX = geometry.KnobInnerRect.X + gripInset + (i * gripSpacing);
                spriteBatch.Draw(pixel, new Rectangle(gripX, gripTop, 1, gripHeight), style.KnobGripColor);
            }
        }
    }
}

internal readonly record struct SliderRenderStyle(
    Color TrackBorderColor,
    Color TrackBackgroundColor,
    Color FillColor,
    Color KnobBorderColor,
    Color KnobFaceColor,
    Color KnobGripColor,
    Func<float, Color> GradientColorAt);

internal readonly record struct SliderGeometry(
    Rectangle OuterRect,
    Rectangle InnerRect,
    Rectangle FillRect,
    Rectangle KnobRect,
    Rectangle KnobInnerRect);

internal readonly record struct SliderGeometryBuildRequest(
    UIElement Track,
    float Value,
    float MinValue,
    float Range,
    float KnobWidthBase,
    float KnobHeightBase);
