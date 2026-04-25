using System;
using Terraria.UI;

namespace ValkyrieLib;

internal static class BoxLayoutPlacementApplier
{
    internal static bool ApplyPrimaryPlacement(UIElement child, 
        float primaryOffset, 
        bool isVertical, 
        float epsilon)
    {
        if (isVertical)
            return ApplyVerticalPlacement(child, primaryOffset, epsilon);

        return ApplyHorizontalPlacement(child, primaryOffset, epsilon);
    }

    internal static bool ApplyPrimarySize(UIElement child, 
        float size, 
        float parentSize, 
        bool isVertical, 
        float epsilon)
    {
        if (isVertical)
            return ApplyVerticalPrimarySize(child, size, parentSize, epsilon);

        return ApplyHorizontalPrimarySize(child, size, parentSize, epsilon);
    }

    private static bool ApplyVerticalPlacement(UIElement child, 
        float primaryOffset, 
        float epsilon)
    {
        bool changed = false;
        changed |= TrySetAlignment(child.VAlign, epsilon, value => child.VAlign = value);
        changed |= TrySetPosition(child.Top.Pixels, child.Top.Percent, primaryOffset, epsilon, (targetPixels, targetPercent) => child.Top.Set(targetPixels, targetPercent));
        changed |= TrySetPosition(child.Left.Pixels, child.Left.Percent, 0f, epsilon, (targetPixels, targetPercent) => child.Left.Set(targetPixels, targetPercent));
        return changed;
    }

    private static bool ApplyHorizontalPlacement(UIElement child, 
        float primaryOffset, 
        float epsilon)
    {
        bool changed = false;
        changed |= TrySetAlignment(child.HAlign, epsilon, value => child.HAlign = value);
        changed |= TrySetPosition(child.Left.Pixels, child.Left.Percent, primaryOffset, epsilon, (targetPixels, targetPercent) => child.Left.Set(targetPixels, targetPercent));
        changed |= TrySetPosition(child.Top.Pixels, child.Top.Percent, 0f, epsilon, (targetPixels, targetPercent) => child.Top.Set(targetPixels, targetPercent));
        return changed;
    }

    private static bool TrySetAlignment(float currentValue, 
        float epsilon, 
        Action<float> applyAlignment)
    {
        if (MathF.Abs(currentValue) <= epsilon)
            return false;

        applyAlignment(0f);
        return true;
    }

    private static bool TrySetPosition(
        float currentPixels,
        float currentPercent,
        float targetPixels,
        float epsilon,
        Action<float, float> applyPosition)
    {
        if (MathF.Abs(currentPixels - targetPixels) <= epsilon && MathF.Abs(currentPercent) <= epsilon)
            return false;

        applyPosition(targetPixels, 0f);
        return true;
    }

    private static bool ApplyVerticalPrimarySize(UIElement child, 
        float size, 
        float parentSize, 
        float epsilon)
    {
        return ApplyPrimarySizeForDimension(
            child.Height,
            size,
            parentSize,
            epsilon,
            (targetPixels, targetPercent) => child.Height.Set(targetPixels, targetPercent));
    }

    private static bool ApplyHorizontalPrimarySize(UIElement child, 
        float size, 
        float parentSize, 
        float epsilon)
    {
        return ApplyPrimarySizeForDimension(
            child.Width,
            size,
            parentSize,
            epsilon,
            (targetPixels, targetPercent) => child.Width.Set(targetPixels, targetPercent));
    }

    private static bool ApplyPrimarySizeForDimension(StyleDimension dimension,
        float size,
        float parentSize,
        float epsilon,
        Action<float, float> applySize)
    {
        return TryApplyPrimarySize(
            dimension.Pixels,
            dimension.Percent,
            size,
            parentSize,
            epsilon,
            applySize);
    }

    private static bool TryApplyPrimarySize(float currentPixels,
        float currentPercent,
        float size,
        float parentSize,
        float epsilon,
        Action<float, float> applySize)
    {
        float targetPercent = currentPercent > 0f ? currentPercent : 0f;
        float targetPixels = currentPercent > 0f ? size - (parentSize * currentPercent) : size;

        if (MathF.Abs(currentPixels - targetPixels) <= epsilon
            && MathF.Abs(currentPercent - targetPercent) <= epsilon)
        {
            return false;
        }

        applySize(targetPixels, targetPercent);
        return true;
    }
}
