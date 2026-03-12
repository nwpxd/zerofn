using System;
using System.IO;
using System.Text.Json;

namespace ZeroFN;

public class MacroConfig
{
    public string KeyName { get; set; } = "E";
    public bool Enabled { get; set; } = false;
}

public class AppConfig
{
    public MacroConfig Loot { get; set; } = new() { KeyName = "E" };
    public MacroConfig Edit { get; set; } = new() { KeyName = "F" };

    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroFN");

    private static readonly string ConfigPath =
        Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
        }
        catch
        {
            // Fall through to default
        }

        var config = new AppConfig();
        Save(config);
        return config;
    }

    public static void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Silently fail — non-critical
        }
    }
}
