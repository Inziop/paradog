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
    public string GeminiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
    public string DeepLApiKey { get; set; } = string.Empty;
    public string SelectedEngine { get; set; } = "Google";
    public int MaxConcurrentRequests { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 30;
    public bool OverwriteExistingTranslations { get; set; } = false;
    public bool TreatPlaceholderMismatchAsError { get; set; } = true;
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
