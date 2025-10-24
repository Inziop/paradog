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
        private bool _mockMode = true;
        private string _aiStatusText = "AI: OFF";
        private bool _aiEnabled;
        private System.Windows.Media.Brush _aiStatusBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)); // Red color for OFF
        public ObservableCollection<FileViewModel> Files { get; } = new();

        // Commands
        public RelayCommand OpenYamlFileCommand { get; }
        public RelayCommand OpenFolderCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand ShowStatisticsCommand { get; }
        public RelayCommand ShowWelcomeCommand { get; }
        public RelayCommand ShowAboutCommand { get; }
        public RelayCommand<LocalizationEntryViewModel> TranslateWithAiCommand { get; }

        public MainViewModel()
        {
            OpenYamlFileCommand = new RelayCommand(OpenYamlFile);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            ExportCommand = new RelayCommand(Export);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ShowStatisticsCommand = new RelayCommand(ShowStatistics);
            ShowWelcomeCommand = new RelayCommand(ShowWelcome);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            TranslateWithAiCommand = new RelayCommand<LocalizationEntryViewModel>(TranslateWithAi);

            // Initialize AI status
            MockMode = true; // Start with Mock mode ON by default
            AiEnabled = false; // Start with AI OFF by default
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

        public bool MockMode
        {
            get => _mockMode;
            set
            {
                _mockMode = value;
                OnPropertyChanged(nameof(MockMode));
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

        private void OpenFolder(object? parameter)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                Title = "Select Folder with YAML Files",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                try
                {
                    StatusMessage = "Loading files...";
                    Files.Clear();

                    var yamlFiles = Directory.GetFiles(dialog.FileName, "*.yml", SearchOption.AllDirectories)
                                           .Concat(Directory.GetFiles(dialog.FileName, "*.yaml", SearchOption.AllDirectories));

                    foreach (var filePath in yamlFiles)
                    {
                        var entries = ParadoxParser.ParseFileAsync(filePath).Result;
                        var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();
                        
                        var fileViewModel = new FileViewModel
                        {
                            FilePath = filePath,
                            LastModified = FileService.GetFileModifiedTime(filePath),
                            Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                        };
                        
                        Files.Add(fileViewModel);
                    }

                    if (Files.Count > 0)
                    {
                        SelectedFile = Files[0];
                        StatusMessage = "Files loaded successfully";
                    }
                    else
                    {
                        StatusMessage = "No YAML files found in selected folder";
                    }
                    UpdateProgressText();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error loading files: {ex.Message}";
                    MessageBox.Show($"Error loading files:\n{ex.Message}", "Error", 
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

        private async void TranslateWithAi(LocalizationEntryViewModel? entry)
        {
            if (entry == null || !AiEnabled) return;

            try
            {
                StatusMessage = "Translating with AI...";
                entry.Status = "Translating...";

                if (MockMode)
                {
                    // Simulate translation delay
                    await Task.Delay(1000);
                    entry.Entry.TranslatedText = $"[AI Mock] {entry.Entry.SourceText}";
                    entry.Status = "Mock translated";
                }
                else
                {
                    // TODO: Implement real AI translation here
                    await Task.Delay(2000); // Temporary delay
                    entry.Entry.TranslatedText = $"[AI] {entry.Entry.SourceText}";
                    entry.Status = "AI translated";
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