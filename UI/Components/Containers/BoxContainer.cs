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

        BoxLayoutAccumulationState accumulation = default;

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
}
