using System.ComponentModel;

namespace ParadoxTranslator.Models;

/// <summary>
/// Represents a single localization entry with source and target text
/// </summary>
public class LocalizationEntry : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private string _sourceText = string.Empty;
    private string _translatedText = string.Empty;
    private string _rawLineBefore = string.Empty;
    private string _rawLineAfter = string.Empty;
    private bool _isTranslated;
    private bool _hasPlaceholderIssues;

    public string Key
    {
        get => _key;
        set
        {
            _key = value;
            OnPropertyChanged(nameof(Key));
        }
    }

    public string SourceText
    {
        get => _sourceText;
        set
        {
            _sourceText = value;
            OnPropertyChanged(nameof(SourceText));
        }
    }

    public string TranslatedText
    {
        get => _translatedText;
        set
        {
            _translatedText = value;
            OnPropertyChanged(nameof(TranslatedText));
            IsTranslated = !string.IsNullOrWhiteSpace(value);
        }
    }

    public string RawLineBefore
    {
        get => _rawLineBefore;
        set
        {
            _rawLineBefore = value;
            OnPropertyChanged(nameof(RawLineBefore));
        }
    }

    public string RawLineAfter
    {
        get => _rawLineAfter;
        set
        {
            _rawLineAfter = value;
            OnPropertyChanged(nameof(RawLineAfter));
        }
    }

    public bool IsTranslated
    {
        get => _isTranslated;
        set
        {
            _isTranslated = value;
            OnPropertyChanged(nameof(IsTranslated));
        }
    }

    public bool HasPlaceholderIssues
    {
        get => _hasPlaceholderIssues;
        set
        {
            _hasPlaceholderIssues = value;
            OnPropertyChanged(nameof(HasPlaceholderIssues));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
