namespace ParadoxTranslator.Models;

/// <summary>
/// Result of a translation operation
/// </summary>
public class TranslationResult
{
    public bool Success { get; set; }
    public string TranslatedText { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Engine { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for translation services
/// </summary>
public class TranslationConfig
{
    public string GoogleApiKey { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
    public string DeepLApiKey { get; set; } = string.Empty;
    public string SelectedEngine { get; set; } = "Google";
    public int MaxConcurrentRequests { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 30;
    public bool OverwriteExistingTranslations { get; set; } = false;
    public bool TreatPlaceholderMismatchAsError { get; set; } = true;

    // UI and advanced settings persisted for convenience
    public bool CreateBackupBeforeSave { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public bool ShowProgressAnimations { get; set; } = true;
    public bool AutoSave { get; set; } = false;
    public string Quality { get; set; } = "Balanced";
    public bool EnableLogging { get; set; } = false;
    public bool ShowDebugInfo { get; set; } = false;
    public bool EnableAi { get; set; } = false;
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "vi";
}

/// <summary>
/// Placeholder validation result
/// </summary>
public class PlaceholderValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, int> SourcePlaceholders { get; set; } = new();
    public Dictionary<string, int> TargetPlaceholders { get; set; } = new();
}
