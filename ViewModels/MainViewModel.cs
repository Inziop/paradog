using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;
using ParadoxTranslator.Utils;

namespace ParadoxTranslator.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TranslationService _translationService;
    private readonly TranslationConfig _config;
    private FileViewModel? _selectedFile;
    private string _sourceLanguage = "en";
    private string _targetLanguage = "vi";
    private string _statusMessage = "Ready";
    private string _progressText = "0/0";
    private bool _isTranslating;

    public MainViewModel()
    {
        _config = new TranslationConfig
        {
            SelectedEngine = "Google",
            MaxConcurrentRequests = 3,
            OverwriteExistingTranslations = false
        };
        
        _translationService = new TranslationService(_config);
        Files = new ObservableCollection<FileViewModel>();
        
        // Initialize commands
        OpenFolderCommand = new RelayCommand(OpenFolder);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        TranslateAllCommand = new AsyncRelayCommand(TranslateAllAsync, (p) => !IsTranslating && SelectedFile != null);
        SaveCommand = new AsyncRelayCommand(SaveAsync, (p) => SelectedFile != null);
        ShowStatisticsCommand = new RelayCommand(ShowStatistics);
        ExportCommand = new AsyncRelayCommand(ExportAsync, (p) => Files.Count > 0);
        RefreshCommand = new RelayCommand(Refresh);
        ShowAboutCommand = new RelayCommand(ShowAbout);
        ShowWelcomeCommand = new RelayCommand(ShowWelcome);
    }

    public ObservableCollection<FileViewModel> Files { get; }
    
    public FileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged(nameof(SelectedFile));
            OnPropertyChanged(nameof(ProgressText));
        }
    }
    
    public string SourceLanguage
    {
        get => _sourceLanguage;
        set
        {
            _sourceLanguage = value;
            OnPropertyChanged(nameof(SourceLanguage));
        }
    }
    
    public string TargetLanguage
    {
        get => _targetLanguage;
        set
        {
            _targetLanguage = value;
            OnPropertyChanged(nameof(TargetLanguage));
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }
    
    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value;
            OnPropertyChanged(nameof(ProgressText));
        }
    }
    
    public bool IsTranslating
    {
        get => _isTranslating;
        set
        {
            _isTranslating = value;
            OnPropertyChanged(nameof(IsTranslating));
        }
    }

    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand ShowStatisticsCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ShowAboutCommand { get; }
    public RelayCommand ShowWelcomeCommand { get; }
    public AsyncRelayCommand TranslateAllCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }

    private async void OpenFolder(object? parameter)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Localization Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadLocalizationFilesAsync(dialog.FolderName);
        }
    }

    private void OpenSettings(object? parameter)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = Application.Current.MainWindow;
        settingsWindow.ShowDialog();
    }

    private void ShowStatistics(object? parameter)
    {
        var statisticsWindow = new StatisticsWindow();
        statisticsWindow.Owner = Application.Current.MainWindow;
        statisticsWindow.ShowDialog();
    }

    private async Task ExportAsync(object? parameter)
    {
        try
        {
            StatusMessage = "Exporting translations...";
            
            var saveDialog = new SaveFileDialog
            {
                Title = "Export Translations",
                Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx",
                DefaultExt = "json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                // TODO: Implement export functionality
                StatusMessage = "Export completed successfully";
                MessageBox.Show("Export functionality will be implemented soon!", "Info", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
            MessageBox.Show($"Export error:\n{ex.Message}", "Error", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Refresh(object? parameter)
    {
        if (SelectedFile != null)
        {
            // TODO: Implement refresh functionality
            StatusMessage = "Refreshed current file";
        }
    }

    private void ShowAbout(object? parameter)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.Owner = Application.Current.MainWindow;
        aboutWindow.ShowDialog();
    }

    private void ShowWelcome(object? parameter)
    {
        var welcomeWindow = new WelcomeWindow();
        welcomeWindow.Owner = Application.Current.MainWindow;
        welcomeWindow.ShowDialog();
    }

    private async Task LoadLocalizationFilesAsync(string folderPath)
    {
        try
        {
            StatusMessage = "Loading files...";
            Files.Clear();

            var filePaths = await FileService.GetLocalizationFilesAsync(folderPath);
            
            foreach (var filePath in filePaths)
            {
                var entries = await ParadoxParser.ParseFileAsync(filePath);
                var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();
                
                var fileViewModel = new FileViewModel
                {
                    FilePath = filePath,
                    LastModified = FileService.GetFileModifiedTime(filePath),
                    Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                };
                
                Files.Add(fileViewModel);
            }

            StatusMessage = $"Loaded {Files.Count} files";
            UpdateProgressText();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading files: {ex.Message}";
            MessageBox.Show($"Error loading files:\n{ex.Message}", "Error", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TranslateAllAsync(object? parameter)
    {
        if (SelectedFile == null) return;

        try
        {
            IsTranslating = true;
            StatusMessage = "Translating...";
            
            // Get entries from selected file
            var entries = GetEntriesFromSelectedFile();
            if (entries == null || !entries.Any()) return;

            var progress = new Progress<int>(percent =>
            {
                StatusMessage = $"Translating... {percent}%";
            });

            var results = await _translationService.TranslateBatchAsync(
                entries.Select(e => e.Entry), // Access the underlying LocalizationEntry model
                SourceLanguage, 
                TargetLanguage, 
                progress);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            StatusMessage = $"Translation complete: {successCount} successful, {failureCount} failed";
            UpdateProgressText();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Translation error: {ex.Message}";
            MessageBox.Show($"Translation error:\n{ex.Message}", "Error", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsTranslating = false;
        }
    }

    private async Task SaveAsync(object? parameter)
    {
        if (SelectedFile == null) return;

        try
        {
            StatusMessage = "Saving...";
            
            // Create backup
            await FileService.CreateBackupAsync(SelectedFile.FilePath);
            
            // Get entries and save
            var entries = GetEntriesFromSelectedFile();
            if (entries != null)
            {
                await ParadoxParser.SaveLocalizationAsync(
                    entries.Select(e => e.Entry), 
                    SelectedFile.FilePath);
            }

            StatusMessage = "Saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
            MessageBox.Show($"Save error:\n{ex.Message}", "Error", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private List<LocalizationEntryViewModel>? GetEntriesFromSelectedFile()
    {
        if (SelectedFile == null) return null;
        return SelectedFile.Entries.ToList();
    }

    private void UpdateProgressText()
    {
        if (SelectedFile != null)
        {
            ProgressText = $"{SelectedFile.TranslatedEntries}/{SelectedFile.TotalEntries}";
        }
        else
        {
            ProgressText = "0/0";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        // Dispose translation service
        _translationService?.Dispose();
        
        // Dispose all file view models
        if (Files != null)
        {
            foreach (var file in Files)
            {
                file?.Dispose();
            }
            Files.Clear();
        }
    }
}
