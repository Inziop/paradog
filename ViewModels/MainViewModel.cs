using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;
using ParadoxTranslator.Utils;
using System.IO;
using MessageBox = System.Windows.MessageBox;

namespace ParadoxTranslator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _statusMessage = "";
        private FileViewModel? _selectedFile;
        private string _aiStatusText = "AI: OFF";
        private bool _aiEnabled;
        private string _sourceLanguage = "en";
        private string _targetLanguage = "vi";
        private System.Windows.Media.Brush _aiStatusBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)); // Red color for OFF
        public ObservableCollection<FileViewModel> Files { get; } = new();

        // Commands
        public RelayCommand OpenYamlFileCommand { get; }

        public RelayCommand ExportCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand ShowStatisticsCommand { get; }
        public RelayCommand ShowWelcomeCommand { get; }
        public RelayCommand ShowAboutCommand { get; }
        public RelayCommand<LocalizationEntryViewModel> TranslateWithAiCommand { get; }

        public MainViewModel()
        {
            OpenYamlFileCommand = new RelayCommand(OpenYamlFile);
            ExportCommand = new RelayCommand(Export);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ShowStatisticsCommand = new RelayCommand(ShowStatistics);
            ShowWelcomeCommand = new RelayCommand(ShowWelcome);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            TranslateWithAiCommand = new RelayCommand<LocalizationEntryViewModel>(TranslateWithAi);

            // Load settings
            var config = Services.SettingsService.LoadConfig();
            AiEnabled = config.EnableAi;
            SourceLanguage = config.SourceLanguage;
            TargetLanguage = config.TargetLanguage;
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

        public FileViewModel? SelectedFile
        {
            get => _selectedFile;
            set
            {
                _selectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
            }
        }



        public bool AiEnabled
        {
            get => _aiEnabled;
            set
            {
                _aiEnabled = value;
                OnPropertyChanged(nameof(AiEnabled));
                UpdateAiStatus();
            }
        }

        public string AiStatusText
        {
            get => _aiStatusText;
            set
            {
                _aiStatusText = value;
                OnPropertyChanged(nameof(AiStatusText));
            }
        }

        public System.Windows.Media.Brush AiStatusBrush
        {
            get => _aiStatusBrush;
            set
            {
                _aiStatusBrush = value;
                OnPropertyChanged(nameof(AiStatusBrush));
            }
        }

        private void UpdateAiStatus()
        {
            if (_aiEnabled)
            {
                AiStatusText = "AI: ON";
                AiStatusBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)); // Green
            }
            else
            {
                AiStatusText = "AI: OFF";
                AiStatusBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)); // Red
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            // Clean up resources if needed
            GC.SuppressFinalize(this);
        }

        private void UpdateProgressText()
        {
            // Will implement later if needed
        }

        private async void OpenYamlFile(object? parameter)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select YAML File",
                Filter = "YAML files (*.yml;*.yaml)|*.yml;*.yaml|All files (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = "Loading file...";
                    Files.Clear();

                    var filePath = dialog.FileName;
                    var entries = await ParadoxParser.ParseFileAsync(filePath);
                    var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();
                    
                    var fileViewModel = new FileViewModel
                    {
                        FilePath = filePath,
                        LastModified = FileService.GetFileModifiedTime(filePath),
                        Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                    };
                    
                    Files.Add(fileViewModel);
                    SelectedFile = fileViewModel;

                    StatusMessage = "File loaded successfully";
                    UpdateProgressText();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error loading file: {ex.Message}";
                    MessageBox.Show($"Error loading file:\n{ex.Message}", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void Export(object? parameter)
        {
            if (Files.Count == 0)
            {
                MessageBox.Show("No files to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var file in Files)
                {
                    // TODO: Implement export logic
                    StatusMessage = $"Exporting {Path.GetFileName(file.FilePath)}...";
                }
                StatusMessage = "Export completed successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during export: {ex.Message}";
                MessageBox.Show($"Error during export:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSettings(object? parameter)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening settings: {ex.Message}";
                MessageBox.Show($"Error opening settings:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowStatistics(object? parameter)
        {
            try
            {
                var statisticsWindow = new StatisticsWindow();
                statisticsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error showing statistics: {ex.Message}";
                MessageBox.Show($"Error showing statistics:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowWelcome(object? parameter)
        {
            try
            {
                var welcomeWindow = new WelcomeWindow();
                welcomeWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error showing welcome screen: {ex.Message}";
                MessageBox.Show($"Error showing welcome screen:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAbout(object? parameter)
        {
            try
            {
                var aboutWindow = new AboutWindow();
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error showing about screen: {ex.Message}";
                MessageBox.Show($"Error showing about screen:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string SourceLanguage
        {
            get => _sourceLanguage;
            set
            {
                if (_sourceLanguage != value)
                {
                    _sourceLanguage = value;
                    OnPropertyChanged(nameof(SourceLanguage));

                    var config = Services.SettingsService.LoadConfig();
                    config.SourceLanguage = value;
                    Services.SettingsService.SaveConfig(config);
                }
            }
        }

        public string TargetLanguage
        {
            get => _targetLanguage;
            set
            {
                if (_targetLanguage != value)
                {
                    _targetLanguage = value;
                    OnPropertyChanged(nameof(TargetLanguage));

                    var config = Services.SettingsService.LoadConfig();
                    config.TargetLanguage = value;
                    Services.SettingsService.SaveConfig(config);
                }
            }
        }

        private async void TranslateWithAi(LocalizationEntryViewModel? entry)
        {
            if (entry == null || !AiEnabled) return;

            try
            {
                StatusMessage = "Translating with AI...";
                entry.Status = "Translating...";

                var config = Services.SettingsService.LoadConfig();
                config.SourceLanguage = SourceLanguage;
                config.TargetLanguage = TargetLanguage;

                var translationService = new TranslationService(config);
                var result = await translationService.TranslateTextAsync(entry.Entry.SourceText, SourceLanguage, TargetLanguage);
                if (result.Success)
                {
                    entry.Entry.TranslatedText = result.TranslatedText;
                    entry.Status = $"Translated with {result.Engine}";
                }
                else
                {
                    entry.Status = $"Translation failed: {result.ErrorMessage}";
                }

                StatusMessage = "Translation completed";
            }
            catch (Exception ex)
            {
                entry.Status = "Translation failed";
                StatusMessage = $"Translation error: {ex.Message}";
                MessageBox.Show($"Error during translation:\n{ex.Message}", "Translation Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}