using System;
using System.IO;
using System.Text.Json;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Saves and loads last-session state to the user's AppData folder.
/// </summary>
public static class SessionService
{
    private static string GetSessionPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParadoxTranslator");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "session.json");
    }

    public static SessionState Load()
    {
        try
        {
            var path = GetSessionPath();
            if (!File.Exists(path))
                return new SessionState();

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var state = JsonSerializer.Deserialize<SessionState>(json, options);
            return state ?? new SessionState();
        }
        catch
        {
            return new SessionState();
        }
    }

    public static void Save(SessionState state)
    {
        try
        {
            state.SavedAt = DateTime.UtcNow;
            var path = GetSessionPath();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(state, options);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Swallow errors to avoid blocking app close
        }
    }
}
