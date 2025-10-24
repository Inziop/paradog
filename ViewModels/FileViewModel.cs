using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;

namespace ParadoxTranslator.ViewModels;

/// <summary>
/// ViewModel for file representation
/// </summary>
public class FileViewModel : INotifyPropertyChanged, IDisposable
{
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private int _totalEntries;
    private int _translatedEntries;
    private bool _isSelected;
    private DateTime _lastModified;
    private ObservableCollection<LocalizationEntryViewModel> _entries = new();

    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            FileName = Path.GetFileName(value);
            OnPropertyChanged(nameof(FilePath));
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged(nameof(FileName));
        }
    }

    public int TotalEntries
    {
        get => _totalEntries;
        set
        {
            _totalEntries = value;
            OnPropertyChanged(nameof(TotalEntries));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    public int TranslatedEntries
    {
        get => _translatedEntries;
        set
        {
            _translatedEntries = value;
            OnPropertyChanged(nameof(TranslatedEntries));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public DateTime LastModified
    {
        get => _lastModified;
        set
        {
            _lastModified = value;
            OnPropertyChanged(nameof(LastModified));
        }
    }

    public ObservableCollection<LocalizationEntryViewModel> Entries
    {
        get => _entries;
        set
        {
            _entries = value;
            OnPropertyChanged(nameof(Entries));
            UpdateCounts();
        }
    }

    public string ProgressText => $"{TranslatedEntries}/{TotalEntries} translated";

    public double ProgressPercentage => TotalEntries > 0 ? (double)TranslatedEntries / TotalEntries * 100 : 0;

    private void UpdateCounts()
    {
        if (_entries != null)
        {
            TotalEntries = _entries.Count;
            TranslatedEntries = _entries.Count(e => e.IsTranslated);
        }
        else
        {
            TotalEntries = 0;
            TranslatedEntries = 0;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        // Dispose all entry view models to prevent memory leaks
        if (_entries != null)
        {
            foreach (var entry in _entries)
            {
                entry?.Dispose();
            }
            _entries.Clear();
        }
    }

    /// <summary>
    /// Recalculate counts for total/translated entries. Call after updating individual entries.
    /// </summary>
    public void RecalculateCounts()
    {
        UpdateCounts();
        // Notify properties that depend on counts
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressPercentage));
    }
}
