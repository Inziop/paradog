using System;
using System.IO;
using System.Text.Json;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

public static class SettingsService
{
    private static string GetSettingsPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParadoxTranslator");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static TranslationConfig LoadConfig()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
                return new TranslationConfig();

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cfg = JsonSerializer.Deserialize<TranslationConfig>(json, options);
            return cfg ?? new TranslationConfig();
        }
        catch
        {
            return new TranslationConfig();
        }
    }

    public static void SaveConfig(TranslationConfig config)
    {
        var path = GetSettingsPath();
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(path, json);
    }
}
