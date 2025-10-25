using System;
using System.Collections.Generic;

namespace ParadoxTranslator.Models;

/// <summary>
/// Persisted session data to restore user's last working state.
/// </summary>
public class SessionState
{
    public List<string> OpenedFiles { get; set; } = new();
    public string? SelectedFilePath { get; set; }
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "vi";
    public bool AiEnabled { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    // FilePath -> (Key -> TranslatedText)
    public Dictionary<string, Dictionary<string, string>> Translations { get; set; } = new();

    // Optional window placement
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public string? WindowState { get; set; } // Normal, Maximized, Minimized
}
