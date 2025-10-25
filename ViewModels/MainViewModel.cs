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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        /// <summary>
        /// Explicitly save current session snapshot to disk.
        /// Safe to call frequently; errors are swallowed by the service.
        /// </summary>
        public Task SaveAllAsync()
        {
            try
            {
                var snapshot = BuildSessionSnapshot();
                Services.SessionService.Save(snapshot);
            }
            catch
            {
                // ignore
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Restore previously opened files and basic preferences from a session state.
        /// Missing files are skipped gracefully.
        /// </summary>
        public async Task RestoreSessionAsync(SessionState session)
        {
            if (session == null) return;

            try
            {
                // Restore preferences first
                AiEnabled = session.AiEnabled;
                SourceLanguage = string.IsNullOrWhiteSpace(session.SourceLanguage) ? SourceLanguage : session.SourceLanguage;
                TargetLanguage = string.IsNullOrWhiteSpace(session.TargetLanguage) ? TargetLanguage : session.TargetLanguage;

                if (session.OpenedFiles == null || session.OpenedFiles.Count == 0)
                {
                    return;
                }

                StatusMessage = "Restoring last session...";

                Files.Clear();
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                FileViewModel? toSelect = null;

                foreach (var filePath in session.OpenedFiles)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || existing.Contains(filePath))
                            continue;

                        var entries = await ParadoxParser.ParseFileAsync(filePath);

                        // Apply saved translations (by key) if available for this file
                        Dictionary<string, string>? savedMap = null;
                        if (session.Translations != null && session.Translations.TryGetValue(filePath, out var map) && map != null)
                        {
                            savedMap = map;
                            foreach (var entry in entries)
                            {
                                if (!string.IsNullOrWhiteSpace(entry.Key) && savedMap.TryGetValue(entry.Key, out var t) && !string.IsNullOrWhiteSpace(t))
                                {
                                    entry.TranslatedText = t;
                                }
                            }
                        }

                        var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();

                        var fileViewModel = new FileViewModel
                        {
                            FilePath = filePath,
                            LastModified = FileService.GetFileModifiedTime(filePath),
                            Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                        };

                        Files.Add(fileViewModel);
                        existing.Add(filePath);

                        // Ensure counts reflect applied translations
                        fileViewModel.RecalculateCounts();

                        if (!string.IsNullOrWhiteSpace(session.SelectedFilePath) &&
                            filePath.Equals(session.SelectedFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            toSelect = fileViewModel;
                        }
                    }
                    catch
                    {
                        // Skip problematic files
                    }
                }

                if (toSelect == null && Files.Count > 0)
                {
                    toSelect = Files[0];
                }

                SelectedFile = toSelect;
                StatusMessage = Files.Count > 0 ? $"Restored {Files.Count} file(s) from last session" : "No files restored";
            }
            catch
            {
                // Non-fatal
            }
        }

        /// <summary>
        /// Build a serializable snapshot of the current session.
        /// </summary>
        public SessionState BuildSessionSnapshot()
        {
            var state = new SessionState
            {
                OpenedFiles = Files.Select(f => f.FilePath).Where(p => !string.IsNullOrWhiteSpace(p)).ToList(),
                SelectedFilePath = SelectedFile?.FilePath,
                SourceLanguage = SourceLanguage,
                TargetLanguage = TargetLanguage,
                AiEnabled = AiEnabled
            };

            // Capture translations per file (only non-empty translations)
            foreach (var file in Files)
            {
                if (string.IsNullOrWhiteSpace(file.FilePath)) continue;
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var vm in file.Entries)
                {
                    var key = vm.Entry.Key;
                    var val = vm.Entry.TranslatedText;
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                    {
                        map[key] = val;
                    }
                }
                if (map.Count > 0)
                {
                    state.Translations[file.FilePath] = map;
                }
            }
            return state;
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
                FilterIndex = 1,
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = "Loading file(s)...";

                    // Keep existing files and append newly selected ones; avoid duplicates
                    var existing = new HashSet<string>(Files.Select(f => f.FilePath), StringComparer.OrdinalIgnoreCase);
                    int added = 0, skipped = 0;
                    FileViewModel? lastAdded = null;

                    foreach (var filePath in dialog.FileNames)
                    {
                        if (existing.Contains(filePath))
                        {
                            skipped++;
                            continue;
                        }

                        var entries = await ParadoxParser.ParseFileAsync(filePath);
                        var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();

                        var fileViewModel = new FileViewModel
                        {
                            FilePath = filePath,
                            LastModified = FileService.GetFileModifiedTime(filePath),
                            Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                        };

                        Files.Add(fileViewModel);
                        existing.Add(filePath);
                        added++;
                        lastAdded = fileViewModel;
                    }

                    if (lastAdded != null)
                        SelectedFile = lastAdded;

                    if (added == 0)
                        StatusMessage = skipped > 0 ? $"No new files added. Skipped {skipped} already-open file(s)." : "No files were selected.";
                    else
                        StatusMessage = skipped > 0 ? $"Added {added} file(s), skipped {skipped} duplicate(s)." : $"Added {added} file(s).";
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



        private async void Export(object? parameter)
        {
            if (Files.Count == 0)
            {
                MessageBox.Show("No files to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Ask for export options (format & scope)
                var optionsWin = new ExportOptionsWindow { Owner = Application.Current.MainWindow };
                var confirm = optionsWin.ShowDialog();
                if (confirm != true)
                {
                    StatusMessage = "Export canceled";
                    return;
                }

                // Ask user to pick an export folder
                var folderDialog = new CommonOpenFileDialog
                {
                    Title = "Select export folder",
                    IsFolderPicker = true,
                };

                if (folderDialog.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    StatusMessage = "Export canceled";
                    return;
                }

                var exportDir = folderDialog.FileName;

                // Decide target files
                var targets = optionsWin.ExportAllFiles ? Files.ToList() : (SelectedFile != null ? new List<FileViewModel> { SelectedFile } : new List<FileViewModel>());
                if (targets.Count == 0)
                {
                    StatusMessage = "No target files to export.";
                    return;
                }

                int exportedCount = 0;
                foreach (var file in targets)
                {
                    StatusMessage = $"Exporting {Path.GetFileName(file.FilePath)}...";

                    switch (optionsWin.SelectedFormat)
                    {
                        case ExportOptionsWindow.ExportFormat.Csv:
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("Key,Source,Translation");
                            foreach (var entryVm in file.Entries)
                            {
                                var key = Csv(entryVm.Entry.Key);
                                var src = Csv(entryVm.Entry.SourceText);
                                var trg = Csv(entryVm.Entry.TranslatedText);
                                sb.AppendLine($"{key},{src},{trg}");
                            }
                            var outPath = Path.Combine(exportDir, Path.GetFileNameWithoutExtension(file.FilePath) + ".csv");
                            await FileService.SaveFileAsync(outPath, sb.ToString(), includeBom: true);
                            break;
                        }
                        case ExportOptionsWindow.ExportFormat.Json:
                        {
                            var payload = file.Entries.Select(e => new { key = e.Entry.Key, source = e.Entry.SourceText, translation = e.Entry.TranslatedText });
                            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                            var outPath = Path.Combine(exportDir, Path.GetFileNameWithoutExtension(file.FilePath) + ".json");
                            await FileService.SaveFileAsync(outPath, json, includeBom: true);
                            break;
                        }
                        case ExportOptionsWindow.ExportFormat.Yaml:
                        {
                            // Use Paradox format writer
                            var entries = file.Entries.Select(vm => vm.Entry).ToList();
                            var header = $"l_{TargetLanguage}:"; // use selected target language
                            var outPath = Path.Combine(exportDir, Path.GetFileNameWithoutExtension(file.FilePath) + ".yml");
                            await ParadoxParser.SaveLocalizationAsync(entries, outPath, header);
                            break;
                        }
                    }

                    exportedCount++;
                }

                StatusMessage = $"Exported {exportedCount} file(s) to {exportDir}";
                MessageBox.Show(StatusMessage, "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during export: {ex.Message}";
                MessageBox.Show($"Error during export:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Csv(string? value)
        {
            value ??= string.Empty;
            var needsQuotes = value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r');
            var escaped = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
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
                    // Persist progress
                    await SaveAllAsync();
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