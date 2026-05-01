using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ValkyrieLib;

public class ValkyreLibConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [DefaultValue(true)]
    public bool ClearLogOnStartup { get; set; } = true;

    [DefaultValue("Logs.txt")]
    public string LogFile { get; set; } = "Logs.txt";
}
