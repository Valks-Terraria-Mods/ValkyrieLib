using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace ValkyrieLib;

internal class Logger
{
    private readonly string _logPath;

    public Logger(Mod mod)
    {
        var config = ModContent.GetInstance<ValkyreLibConfig>();

        _logPath = Path.Combine(Main.SavePath, "ModSources", mod.Name, config.LogFile);

        // Clear log file on mod startup
        if (config.ClearLogOnStartup)
            File.WriteAllText(_logPath, "");
    }

    internal void Log(object message)
    {
        // Silently fail if the directory does not exist so players using this mod do not get an error
        if (!Directory.Exists(Path.GetDirectoryName(_logPath)))
            return;

        File.AppendAllText(_logPath, message?.ToString() + Environment.NewLine);
    }
}
