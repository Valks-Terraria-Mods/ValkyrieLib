using System;
using Terraria.UI;

namespace ValkyrieLib;

internal static class BoxContainerAutoSizing
{
    internal static void InitializeAutoFlags(BoxContainer container,
        bool isVertical,
        float autoEpsilon,
        ref BoxContainerAutoSizingState state)
    {
        StyleDimension primaryDimension = GetPrimaryDimension(container, isVertical);
        StyleDimension crossDimension = GetCrossDimension(container, isVertical);

        state.AutoPrimary = IsDimensionAuto(primaryDimension, autoEpsilon);
        state.AutoCross = IsDimensionAuto(crossDimension, autoEpsilon);
        state.LastAutoPrimaryPixels = primaryDimension.Pixels;
        state.LastAutoCrossPixels = crossDimension.Pixels;
    }

    internal static void SyncAutoFlags(BoxContainer container,
        bool isVertical,
        float autoEpsilon,
        float autoSetEpsilon,
        ref BoxContainerAutoSizingState state)
    {
        SyncAutoFlag(ref state.AutoPrimary, ref state.LastAutoPrimaryPixels, GetPrimaryDimension(container, isVertical), autoEpsilon, autoSetEpsilon);
        SyncAutoFlag(ref state.AutoCross, ref state.LastAutoCrossPixels, GetCrossDimension(container, isVertical), autoEpsilon, autoSetEpsilon);
    }

    internal static float GetAvailablePrimary(UIElement container, bool isVertical)
    {
        CalculatedStyle innerDimensions = container.GetInnerDimensions();
        return isVertical ? innerDimensions.Height : innerDimensions.Width;
    }

    internal static void ApplyAutoSize(BoxContainer container,
        bool isVertical,
        float contentPrimary,
        float contentCross,
        float autoSetEpsilon,
        ref BoxContainerAutoSizingState state)
    {
        float clampedPrimary = MathF.Max(0f, contentPrimary);
        float clampedCross = MathF.Max(0f, contentCross);

        ApplyAutoAxisSize(container, isVertical, applyPrimaryAxis: true, clampedPrimary, autoSetEpsilon, ref state.AutoPrimary, ref state.LastAutoPrimaryPixels);
        ApplyAutoAxisSize(container, isVertical, applyPrimaryAxis: false, clampedCross, autoSetEpsilon, ref state.AutoCross, ref state.LastAutoCrossPixels);
    }

    private static bool IsDimensionAuto(StyleDimension dimension, float autoEpsilon)
    {
        return MathF.Abs(dimension.Pixels) <= autoEpsilon && MathF.Abs(dimension.Percent) <= autoEpsilon;
    }

    private static void SyncAutoFlag(ref bool isAuto,
        ref float lastAutoPixels,
        StyleDimension dimension,
        float autoEpsilon,
        float autoSetEpsilon)
    {
        bool dimensionIsAuto = IsDimensionAuto(dimension, autoEpsilon);

        if (dimensionIsAuto)
        {
            isAuto = true;
            lastAutoPixels = 0f;
            return;
        }

        if (!isAuto)
            return;

        bool percentWasOverridden = MathF.Abs(dimension.Percent) > autoEpsilon;
        bool pixelsWereOverridden = MathF.Abs(dimension.Pixels - lastAutoPixels) > autoSetEpsilon;

        if (percentWasOverridden || pixelsWereOverridden)
            isAuto = false;
    }

    private static void ApplyAutoAxisSize(BoxContainer container,
        bool isVertical,
        bool applyPrimaryAxis,
        float clampedSize,
        float autoSetEpsilon,
        ref bool autoFlag,
        ref float lastAutoPixels)
    {
        if (!autoFlag)
            return;

        if (MathF.Abs(clampedSize - lastAutoPixels) > autoSetEpsilon)
            SetAxisSize(container, isVertical, applyPrimaryAxis, clampedSize);

        lastAutoPixels = clampedSize;
    }

    private static void SetAxisSize(BoxContainer container, bool isVertical, bool applyPrimaryAxis, float size)
    {
        if (applyPrimaryAxis)
        {
            if (isVertical)
                container.Height.Set(size, 0f);
            else
                container.Width.Set(size, 0f);

            return;
        }

        if (isVertical)
            container.Width.Set(size, 0f);
        else
            container.Height.Set(size, 0f);
    }

    private static StyleDimension GetPrimaryDimension(BoxContainer container, bool isVertical)
    {
        return isVertical ? container.Height : container.Width;
    }

    private static StyleDimension GetCrossDimension(BoxContainer container, bool isVertical)
    {
        return isVertical ? container.Width : container.Height;
    }
}
