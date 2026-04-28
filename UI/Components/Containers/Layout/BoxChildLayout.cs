using Terraria.UI;

namespace ValkyrieLib;

internal readonly record struct BoxChildLayout(UIElement Element,
    float MinSize,
    float ExpandWeight,
    bool WasPreMeasured);
