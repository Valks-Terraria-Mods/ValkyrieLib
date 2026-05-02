using System;
using System.Collections.Generic;
using Terraria.UI;

namespace ValkyrieLib;

/// <summary>
/// Common layout container for stacking children along a primary axis.
/// Uses child percent sizes on the primary axis as expansion weights.
/// </summary>
public abstract class BoxContainer : UIElement
{
    private const float AutoEpsilon = 0.001f;
    private const float AutoSetEpsilon = 0.05f;
    private const float DefaultSpacing = 5f;

    private float _spacing = DefaultSpacing;
    private BoxContainerAutoSizingState _autoSizingState;
    private bool _isRecalculatingChildren;
    private bool _layoutPassQueued;

    protected abstract bool IsVertical { get; }

    /// <summary>
    /// Spacing between children in pixels along the primary axis.
    /// </summary>
    public float Spacing
    {
        get => _spacing;
        set
        {
            _spacing = MathF.Max(0f, value);
            Recalculate();
        }
    }

    protected BoxContainer()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, IsVertical ? 1f : 0f);

        BoxContainerAutoSizing.InitializeAutoFlags(
            this,
            IsVertical,
            AutoEpsilon,
            ref _autoSizingState);
    }

    /// <summary>
    /// Adds a child and recalculates layout.
    /// </summary>
    public new void Append(UIElement child)
    {
        if (child == null)
            return;

        base.Append(child);
        Recalculate();
    }

    /// <summary>
    /// Removes a child and recalculates layout.
    /// </summary>
    public bool Remove(UIElement child)
    {
        if (child?.Parent != this)
            return false;

        RemoveChild(child);
        Recalculate();
        return true;
    }

    /// <summary>
    /// Removes all children and recalculates layout.
    /// </summary>
    public void Clear()
    {
        RemoveAllChildren();
        Recalculate();
    }

    public override void RecalculateChildren()
    {
        if (_isRecalculatingChildren)
        {
            _layoutPassQueued = true;
            return;
        }

        do
        {
            _layoutPassQueued = false;
            _isRecalculatingChildren = true;

            try
            {
                RecalculateChildrenCore();
            }
            finally
            {
                _isRecalculatingChildren = false;
            }
        }
        while (_layoutPassQueued);
    }

    private bool NormalizeCrossWhenParentCrossAuto(bool parentIsVertical, float autoEpsilon)
    {
        if (parentIsVertical)
        {
            if (Width.Percent > 0f && MathF.Abs(Width.Pixels) <= autoEpsilon)
            {
                Width.Set(0f, 0f);
                return true;
            }

            return false;
        }

        if (Height.Percent > 0f && MathF.Abs(Height.Pixels) <= autoEpsilon)
        {
            Height.Set(0f, 0f);
            return true;
        }

        return false;
    }

    private void RecalculateChildrenCore()
    {
        base.RecalculateChildren();

        BoxContainerAutoSizing.SyncAutoFlags(
            this,
            IsVertical,
            AutoEpsilon,
            AutoSetEpsilon,
            ref _autoSizingState);

        if (Elements.Count == 0)
        {
            BoxContainerAutoSizing.ApplyAutoSize(
                this,
                IsVertical,
                0f,
                0f,
                AutoSetEpsilon,
                ref _autoSizingState);

            return;
        }

        float availablePrimary = BoxContainerAutoSizing.GetAvailablePrimary(this, IsVertical);

        List<BoxChildLayout> layouts = BoxContainerLayoutBuilder.BuildChildLayouts(
            new BoxChildLayoutBuildRequest(Elements,
                IsVertical,
                availablePrimary,
                AutoEpsilon,
                NormalizeChildCrossWhenParentCrossAuto),
            out float totalMinSize,
            out float totalExpandWeight);

        float totalSpacing = Math.Max(0, Elements.Count - 1) * _spacing;
        float remaining = MathF.Max(0f, availablePrimary - totalMinSize - totalSpacing);

        // Calculate the alignment offset for children along the primary axis.
        // For HBox: uses HAlign value (0 = start, 0.5 = center, 1 = end)
        // For VBox: uses VAlign value (0 = start, 0.5 = center, 1 = end)
        // This offset is purely visual and does not affect auto-size calculations.
        float alignmentValue = IsVertical ? VAlign : HAlign;
        float startOffset = remaining * alignmentValue;

        BoxLayoutAccumulationState accumulation = default;
        accumulation.Cursor = startOffset;

        for (int i = 0; i < layouts.Count; i++)
            ApplyChildLayout(layouts[i], availablePrimary, totalExpandWeight, remaining, ref accumulation);

        BoxContainerAutoSizing.ApplyAutoSize(this,
            IsVertical,
            accumulation.UsedPrimary + totalSpacing,
            accumulation.MaxCross,
            AutoSetEpsilon,
            ref _autoSizingState);
    }

    private void ApplyChildLayout(BoxChildLayout layout,
        float availablePrimary,
        float totalExpandWeight,
        float remaining,
        ref BoxLayoutAccumulationState accumulation)
    {
        UIElement child = layout.Element;
        float targetPrimarySize = BoxContainerLayoutBuilder.GetTargetPrimarySize(layout, totalExpandWeight, remaining);
        bool layoutChanged = ApplyChildPlacementAndSize(child, layout, accumulation.Cursor, targetPrimarySize, availablePrimary);

        if (layoutChanged || !layout.WasPreMeasured)
            child.Recalculate();

        float actualPrimary = BoxLayoutMetricsCalculator.GetActualPrimarySize(child, targetPrimarySize, availablePrimary, IsVertical);
        float actualCross = BoxLayoutMetricsCalculator.GetActualCrossSize(child, GetInnerDimensions(), IsVertical);

        accumulation.MaxCross = MathF.Max(accumulation.MaxCross, actualCross);
        accumulation.UsedPrimary += actualPrimary;
        accumulation.Cursor += actualPrimary + _spacing;
    }

    private bool ApplyChildPlacementAndSize(UIElement child,
        BoxChildLayout layout,
        float cursor,
        float targetPrimarySize,
        float availablePrimary)
    {
        bool placementChanged = BoxLayoutPlacementApplier.ApplyPrimaryPlacement(child, cursor, IsVertical, AutoEpsilon);

        bool primarySizeChanged = layout.ExpandWeight > 0f
            && BoxLayoutPlacementApplier.ApplyPrimarySize(child, targetPrimarySize, availablePrimary, IsVertical, AutoEpsilon);

        bool crossNormalizationChanged = !layout.WasPreMeasured
            && NormalizeChildCrossWhenParentCrossAuto(child);

        return placementChanged || primarySizeChanged || crossNormalizationChanged;
    }

    private bool NormalizeChildCrossWhenParentCrossAuto(UIElement child)
    {
        if (!_autoSizingState.AutoCross || child is not BoxContainer boxContainer)
            return false;

        return boxContainer.NormalizeCrossWhenParentCrossAuto(IsVertical, AutoEpsilon);
    }

    #region Helpers
    private static class BoxContainerLayoutBuilder
    {
        internal static List<BoxChildLayout> BuildChildLayouts(BoxChildLayoutBuildRequest request,
            out float totalMinSize,
            out float totalExpandWeight)
        {
            List<BoxChildLayout> layouts = [];

            totalMinSize = 0f;
            totalExpandWeight = 0f;

            foreach (UIElement child in request.Children)
            {
                float expandWeight = BoxLayoutMetricsCalculator.GetExpandWeight(child, request.IsVertical);
                bool needsPreMeasure = BoxLayoutMetricsCalculator.ShouldPreMeasureChild(child, expandWeight, request.IsVertical, request.AutoEpsilon);

                if (needsPreMeasure)
                {
                    request.NormalizeChildCrossWhenParentCrossAuto(child);
                    child.Recalculate();
                }

                float minSize = BoxLayoutMetricsCalculator.GetChildMinSize(
                    child,
                    request.AvailablePrimary,
                    expandWeight > 0f,
                    needsPreMeasure,
                    request.IsVertical);

                layouts.Add(new BoxChildLayout(child, minSize, expandWeight, needsPreMeasure));
                totalMinSize += minSize;

                if (expandWeight > 0f)
                    totalExpandWeight += expandWeight;
            }

            return layouts;
        }

        internal static float GetTargetPrimarySize(BoxChildLayout layout,
            float totalExpandWeight,
            float remaining)
        {
            float targetPrimarySize = layout.MinSize;

            if (layout.ExpandWeight > 0f && totalExpandWeight > 0f)
                targetPrimarySize += remaining * (layout.ExpandWeight / totalExpandWeight);

            return targetPrimarySize;
        }
    }

    private static class BoxLayoutPlacementApplier
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

    private static class BoxLayoutMetricsCalculator
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

        internal static float GetChildMinSize(UIElement child,
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

    private static class BoxContainerAutoSizing
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
    #endregion

    #region Structs
    private struct BoxLayoutAccumulationState
    {
        internal float Cursor;
        internal float UsedPrimary;
        internal float MaxCross;
    }

    private struct BoxContainerAutoSizingState
    {
        internal bool AutoPrimary;
        internal bool AutoCross;
        internal float LastAutoPrimaryPixels;
        internal float LastAutoCrossPixels;
    }

    private readonly record struct BoxChildLayoutBuildRequest(IEnumerable<UIElement> Children,
        bool IsVertical,
        float AvailablePrimary,
        float AutoEpsilon,
        Func<UIElement, bool> NormalizeChildCrossWhenParentCrossAuto);

    private readonly record struct BoxChildLayout(UIElement Element,
        float MinSize,
        float ExpandWeight,
        bool WasPreMeasured);
    #endregion
}
