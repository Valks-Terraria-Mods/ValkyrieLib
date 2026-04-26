using System.Collections.Generic;
using Terraria.UI;

namespace ValkyrieLib;

internal static class BoxContainerLayoutBuilder
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
