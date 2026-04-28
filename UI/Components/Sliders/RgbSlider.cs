using System;
using Microsoft.Xna.Framework;

namespace ValkyrieLib;

public sealed class RgbSlider : TriColorSlider
{
    private float _red;
    private float _green;
    private float _blue;

    public RgbSlider(Color initialColor)
    {
        var sliders = CreateChannelSliders(
            new ChannelSpec(p => ComposeColor(p, 0f, 0f), () => _red, v => _red = v),
            new ChannelSpec(p => ComposeColor(0f, p, 0f), () => _green, v => _green = v),
            new ChannelSpec(p => ComposeColor(0f, 0f, p), () => _blue, v => _blue = v)
        );

        InitializeSliderLayout(sliders, initialColor);
    }

    protected override Color GetCurrentColor() => ComposeColor(_red, _green, _blue);

    protected override void UnpackColorIntoFields(Color color)
    {
        _red = color.R / 255f;
        _green = color.G / 255f;
        _blue = color.B / 255f;
    }

    private static Color ComposeColor(float red, float green, float blue)
    {
        return new(ChannelToByte(red), ChannelToByte(green), ChannelToByte(blue), 255);
    }

    private static byte ChannelToByte(float channel)
    {
        int scaled = (int)MathF.Round(MathHelper.Clamp(channel, 0f, 1f) * 255f);
        return (byte)Math.Clamp(scaled, 0, 255);
    }
}
