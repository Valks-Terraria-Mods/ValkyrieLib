using System;
using Microsoft.Xna.Framework;

namespace ValkyrieLib;

public sealed class HsvSlider : TriColorSlider
{
    private float _hue;
    private float _saturation;
    private float _value;

    public HsvSlider(Color initialColor)
    {
        var sliders = CreateChannelSliders(
            new ChannelSpec(p => HsvColorConverter.HsvToRgb(p, 1f, 1f), () => _hue, v => _hue = v),
            new ChannelSpec(p => HsvColorConverter.HsvToRgb(_hue, p, _value), () => _saturation, v => _saturation = v),
            new ChannelSpec(p => HsvColorConverter.HsvToRgb(_hue, _saturation, p), () => _value, v => _value = v)
        );

        InitializeSliderLayout(sliders, initialColor);
    }

    protected override Color GetCurrentColor() => HsvColorConverter.HsvToRgb(_hue, _saturation, _value);
    protected override void UnpackColorIntoFields(Color color)
    {
        HsvColorConverter.RgbToHsv(color, out _hue, out _saturation, out _value);
    }

    private static class HsvColorConverter
    {
        internal static Color HsvToRgb(float hue, float saturation, float value)
        {
            hue = ((hue % 1f) + 1f) % 1f;
            saturation = MathHelper.Clamp(saturation, 0f, 1f);
            value = MathHelper.Clamp(value, 0f, 1f);

            if (saturation <= 0f)
            {
                byte gray = (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
                return new Color(gray, gray, gray);
            }

            float scaledHue = hue * 6f;
            int sector = (int)MathF.Floor(scaledHue);
            float fraction = scaledHue - sector;

            float p = value * (1f - saturation);
            float q = value * (1f - (fraction * saturation));
            float t = value * (1f - ((1f - fraction) * saturation));

            sector %= 6;

            (float r, float g, float b) = sector switch
            {
                0 => (value, t, p),
                1 => (q, value, p),
                2 => (p, value, t),
                3 => (p, q, value),
                4 => (t, p, value),
                _ => (value, p, q),
            };

            byte red = (byte)Math.Clamp((int)MathF.Round(r * 255f), 0, 255);
            byte green = (byte)Math.Clamp((int)MathF.Round(g * 255f), 0, 255);
            byte blue = (byte)Math.Clamp((int)MathF.Round(b * 255f), 0, 255);

            return new Color(red, green, blue);
        }

        internal static void RgbToHsv(Color color, out float hue, out float saturation, out float value)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = MathF.Max(r, MathF.Max(g, b));
            float min = MathF.Min(r, MathF.Min(g, b));
            float delta = max - min;

            hue = 0f;

            if (delta > 0f)
            {
                if (max == r)
                    hue = (((g - b) / delta) + 6f) % 6f;
                else if (max == g)
                    hue = ((b - r) / delta) + 2f;
                else
                    hue = ((r - g) / delta) + 4f;

                hue /= 6f;

                if (hue < 0f)
                    hue++;
            }

            value = max;
            saturation = max <= 0f ? 0f : delta / max;
        }
    }
}
