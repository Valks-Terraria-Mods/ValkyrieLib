using System;
using Terraria.UI;

namespace ValkyrieLib;

internal static class BoxLayoutMetricsCalculator
{
    internal static float GetExpandWeight(UIElement child, bool isVertical)
    {
        float percent = isVertical ? child.Height.Percent : child.Width.Percent;
        return percent > 0f ? percent : 0f;
    }

    internal static bool ShouldPreMeasureChild(UIElement child, 
        float expandWeight, 
        bool isVertical, 
        float autoEpsilon)
    {
        if (expandWeight > 0f)
            return false;

        if (isVertical)
            return ShouldPreMeasureChildOnAxis(child.Height, child.MinHeight, autoEpsilon);

        return ShouldPreMeasureChildOnAxis(child.Width, child.MinWidth, autoEpsilon);
    }

    internal static float GetActualPrimarySize(UIElement child, 
        float fallback, 
        float parentPrimary, 
        bool isVertical)
    {
        float measured = GetMeasuredSize(child, useHeight: isVertical);
        StyleDimension size = isVertical ? child.Height : child.Width;
        float styleSize = GetStyleSize(size, parentPrimary);

        return ResolveActualSize(fallback, measured, styleSize);
    }

    internal static float GetActualCrossSize(UIElement child, 
        CalculatedStyle innerDimensions, 
        bool isVertical)
    {
        float measured = GetMeasuredSize(child, useHeight: !isVertical);
        StyleDimension size = isVertical ? child.Width : child.Height;
        float parentSize = isVertical ? innerDimensions.Width : innerDimensions.Height;
        float styleSize = GetStyleSize(size, parentSize);

        return ResolveActualSize(0f, measured, styleSize);
    }

    private static bool ShouldPreMeasureChildOnAxis(StyleDimension size, 
        StyleDimension minSize, 
        float autoEpsilon)
    {
        if (HasStyleOverride(size, autoEpsilon))
            return false;

        return !HasStyleOverride(minSize, autoEpsilon);
    }

    private static bool HasStyleOverride(StyleDimension dimension, float autoEpsilon)
    {
        return dimension.Percent > autoEpsilon || dimension.Pixels > autoEpsilon;
    }

    public static float GetChildMinSize(UIElement child, 
        float parentSize, 
        bool isExpandable, 
        bool includeMeasuredSize, 
        bool isVertical)
    {
        float min = GetMinConstraintSize(child, parentSize, isVertical);

        if (!isExpandable)
        {
            min = IncludeMeasuredSize(min, child, includeMeasuredSize, isVertical);
            min = IncludeStyleSize(min, child, parentSize, isVertical);
        }

        return MathF.Max(0f, min);
    }

    private static float GetMinConstraintSize(UIElement child, 
        float parentSize, 
        bool isVertical)
    {
        return isVertical
            ? MathF.Max(0f, child.MinHeight.Pixels + (child.MinHeight.Percent * parentSize))
            : MathF.Max(0f, child.MinWidth.Pixels + (child.MinWidth.Percent * parentSize));
    }

    private static float IncludeMeasuredSize(float min, 
        UIElement child, 
        bool includeMeasuredSize, 
        bool isVertical)
    {
        if (!includeMeasuredSize)
            return min;

        float measured = GetMeasuredSize(child, useHeight: isVertical);

        if (measured <= 0f)
            return min;

        return MathF.Max(min, measured);
    }

    private static float IncludeStyleSize(float min, 
        UIElement child, 
        float parentSize, 
        bool isVertical)
    {
        StyleDimension size = isVertical ? child.Height : child.Width;
        float styleSize = GetStyleSize(size, parentSize);

        if (styleSize <= 0f)
            return min;

        return MathF.Max(min, styleSize);
    }

    private static float GetMeasuredSize(UIElement child, bool useHeight)
    {
        return useHeight ? child.GetOuterDimensions().Height : child.GetOuterDimensions().Width;
    }

    private static float GetStyleSize(StyleDimension size, float parentSize)
    {
        return size.Pixels + (size.Percent * parentSize);
    }

    private static float ResolveActualSize(float fallback, float measured, float styleSize)
    {
        float actual = fallback;

        if (measured > actual)
            actual = measured;

        if (styleSize > actual)
            actual = styleSize;

        return MathF.Max(0f, actual);
    }
}
