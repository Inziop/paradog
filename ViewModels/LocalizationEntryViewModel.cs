using System.ComponentModel;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;

namespace ParadoxTranslator.ViewModels;

/// <summary>
/// ViewModel for individual localization entry
/// </summary>
public class LocalizationEntryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly LocalizationEntry _entry;
    
    // Expose the underlying entry for batch operations
    public LocalizationEntry Entry => _entry;
    private string _target = string.Empty;
    private bool _hasPlaceholderIssues;
    private string _status = string.Empty;

    public LocalizationEntryViewModel(LocalizationEntry entry)
    {
        _entry = entry;
        _target = entry.TranslatedText;
        _hasPlaceholderIssues = entry.HasPlaceholderIssues;
        
        // Subscribe to model changes
        _entry.PropertyChanged += OnModelPropertyChanged;
    }

    public string Key => _entry.Key;
    public string SourceText => _entry.SourceText;

    public string TranslatedText
    {
        get => _target;
        set
        {
            if (_target != value)
            {
                _target = value;
                _entry.TranslatedText = value;
                OnPropertyChanged(nameof(TranslatedText));
                OnPropertyChanged(nameof(IsTranslated));
                ValidatePlaceholders();
            }
        }
    }

    public bool IsTranslated => !string.IsNullOrWhiteSpace(_target);

    public bool HasPlaceholderIssues
    {
        get => _hasPlaceholderIssues;
        set
        {
            if (_hasPlaceholderIssues != value)
            {
                _hasPlaceholderIssues = value;
                _entry.HasPlaceholderIssues = value;
                OnPropertyChanged(nameof(HasPlaceholderIssues));
            }
        }
    }

    public string StatusIcon => HasPlaceholderIssues ? "⚠️" : IsTranslated ? "✅" : "⏳";

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LocalizationEntry.TranslatedText):
                _target = _entry.TranslatedText;
                OnPropertyChanged(nameof(TranslatedText));
                OnPropertyChanged(nameof(IsTranslated));
                break;
            case nameof(LocalizationEntry.HasPlaceholderIssues):
                _hasPlaceholderIssues = _entry.HasPlaceholderIssues;
                OnPropertyChanged(nameof(HasPlaceholderIssues));
                OnPropertyChanged(nameof(StatusIcon));
                break;
        }
    }

    private void ValidatePlaceholders()
    {
        if (string.IsNullOrWhiteSpace(_target))
        {
            HasPlaceholderIssues = false;
            return;
        }

        var validation = PlaceholderValidator.ValidatePlaceholders(_entry.SourceText, _target);
        HasPlaceholderIssues = !validation.IsValid;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        // Unsubscribe from model changes to prevent memory leaks
        if (_entry != null)
        {
            _entry.PropertyChanged -= OnModelPropertyChanged;
        }
    }
}
