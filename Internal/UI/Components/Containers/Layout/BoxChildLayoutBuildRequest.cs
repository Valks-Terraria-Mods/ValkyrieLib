using System;
using System.Collections.Generic;
using Terraria.UI;

namespace ValkyrieLib;

internal readonly record struct BoxChildLayoutBuildRequest(IEnumerable<UIElement> Children,
    bool IsVertical,
    float AvailablePrimary,
    float AutoEpsilon,
    Func<UIElement, bool> NormalizeChildCrossWhenParentCrossAuto);
