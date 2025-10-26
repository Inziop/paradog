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
using MessageBox = ParadoxTranslator.Utils.ModernMessageBox;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

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
        public RangeObservableCollection<FileViewModel> Files { get; } = new();
        private string _searchText = "";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                }
            }
        }

        // Autosave (debounced) state
        private readonly DispatcherTimer _autosaveTimer = new DispatcherTimer();
        private bool _pendingAutosave;
        private readonly HashSet<LocalizationEntryViewModel> _watchedEntries = new();

        // Toast notification callback
        public Action<string, string, int>? ShowToastCallback { get; set; }

        // Commands
        public RelayCommand OpenYamlFileCommand { get; }

        public RelayCommand ExportCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand ShowStatisticsCommand { get; }
        public RelayCommand ShowWelcomeCommand { get; }
        public RelayCommand ShowAboutCommand { get; }
        public RelayCommand<LocalizationEntryViewModel> CopySourceToTranslationCommand { get; }
        public RelayCommand<LocalizationEntryViewModel> BatchTranslateCommand { get; }
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand CompareWithNewVersionCommand { get; }

        public MainViewModel()
        {
            OpenYamlFileCommand = new RelayCommand(OpenYamlFile);
            ExportCommand = new RelayCommand(Export);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ShowStatisticsCommand = new RelayCommand(ShowStatistics);
            ShowWelcomeCommand = new RelayCommand(ShowWelcome);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            CopySourceToTranslationCommand = new RelayCommand<LocalizationEntryViewModel>(CopySourceToTranslation);
            BatchTranslateCommand = new RelayCommand<LocalizationEntryViewModel>(BatchTranslate);
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            CompareWithNewVersionCommand = new RelayCommand(CompareWithNewVersion);

            // Load settings
            var config = Services.SettingsService.LoadConfig();
            AiEnabled = config.EnableAi;
            SourceLanguage = config.SourceLanguage;
            TargetLanguage = config.TargetLanguage;

            // Init autosave timer (3s debounce)
            _autosaveTimer.Interval = TimeSpan.FromSeconds(3);
            _autosaveTimer.Tick += OnAutosaveTimerTick;
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
            // Unsubscribe from all watched entries to prevent memory leaks
            foreach (var entry in _watchedEntries)
            {
                entry.PropertyChanged -= OnEntryPropertyChanged;
            }
            _watchedEntries.Clear();

            // Clean up timer
            _autosaveTimer.Stop();
            GC.SuppressFinalize(this);
        }

        private async void OnAutosaveTimerTick(object? sender, EventArgs e)
        {
            _autosaveTimer.Stop();
            if (_pendingAutosave)
            {
                _pendingAutosave = false;
                await SaveAllAsync();
                // Don't show toast for autosave - it's too noisy
                // User can see "Last saved" in status bar instead
            }
        }

        private void ScheduleAutosave()
        {
            // Check if autosave is enabled in settings
            var config = Services.SettingsService.LoadConfig();
            if (!config.AutoSave) return;

            _pendingAutosave = true;
            _autosaveTimer.Stop();
            _autosaveTimer.Start();
        }

        private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationEntryViewModel.TranslatedText))
            {
                // Update counts on current file
                SelectedFile?.RecalculateCounts();
                // Debounced save
                ScheduleAutosave();
            }
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
                        // Hook change tracking for autosave
                        foreach (var vmEntry in entryViewModels)
                        {
                            if (_watchedEntries.Add(vmEntry))
                            {
                                vmEntry.PropertyChanged += OnEntryPropertyChanged;
                            }
                        }

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
                    var newFiles = new List<FileViewModel>();

                    foreach (var filePath in dialog.FileNames)
                    {
                        if (existing.Contains(filePath))
                        {
                            skipped++;
                            continue;
                        }

                        var entries = await ParadoxParser.ParseFileAsync(filePath);
                        var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();
                        // Hook change tracking for autosave
                        foreach (var vmEntry in entryViewModels)
                        {
                            if (_watchedEntries.Add(vmEntry))
                            {
                                vmEntry.PropertyChanged += OnEntryPropertyChanged;
                            }
                        }

                        var fileViewModel = new FileViewModel
                        {
                            FilePath = filePath,
                            LastModified = FileService.GetFileModifiedTime(filePath),
                            Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                        };

                        newFiles.Add(fileViewModel);
                        existing.Add(filePath);
                        added++;
                        lastAdded = fileViewModel;
                    }

                    // Add all new files at once with single UI update
                    if (newFiles.Count > 0)
                    {
                        Files.AddRange(newFiles);
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
                ShowToastCallback?.Invoke("Export Complete", $"Exported {exportedCount} file(s) successfully", 0); // 0 = Success
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
                var settingsWindow = new SettingsWindow
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ShowToastCallback = ShowToastCallback // Pass toast callback
                };
                settingsWindow.ShowDialog();
                
                // Refresh debug badge after settings window closes
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateDebugInfo();
                }
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
                var statisticsWindow = new StatisticsWindow(this)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
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
                var welcomeWindow = new WelcomeWindow
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
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
                var aboutWindow = new AboutWindow
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error showing about screen: {ex.Message}";
                MessageBox.Show($"Error showing about screen:\n{ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadFileAsync(string filePath)
        {
            try
            {
                // Check if already loaded
                if (Files.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var entries = await ParadoxParser.ParseFileAsync(filePath);
                var entryViewModels = entries.Select(e => new LocalizationEntryViewModel(e)).ToList();
                
                // Hook change tracking for autosave
                foreach (var vmEntry in entryViewModels)
                {
                    if (_watchedEntries.Add(vmEntry))
                    {
                        vmEntry.PropertyChanged += OnEntryPropertyChanged;
                    }
                }

                var fileViewModel = new FileViewModel
                {
                    FilePath = filePath,
                    LastModified = FileService.GetFileModifiedTime(filePath),
                    Entries = new ObservableCollection<LocalizationEntryViewModel>(entryViewModels)
                };

                Files.Add(fileViewModel);
                SelectedFile = fileViewModel;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load file: {ex.Message}";
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

        // Filter property for quick filtering
        private string _filterMode = "All";
        public string FilterMode
        {
            get => _filterMode;
            set
            {
                if (_filterMode != value)
                {
                    _filterMode = value;
                    OnPropertyChanged(nameof(FilterMode));
                    ApplyFilter();
                }
            }
        }

        private void ApplyFilter()
        {
            if (SelectedFile == null) return;
            
            // Store original entries if not already stored
            if (!_originalEntries.ContainsKey(SelectedFile.FilePath))
            {
                _originalEntries[SelectedFile.FilePath] = SelectedFile.Entries.ToList();
            }

            var original = _originalEntries[SelectedFile.FilePath];
            IEnumerable<LocalizationEntryViewModel> filtered = original;

            switch (_filterMode)
            {
                case "Translated":
                    filtered = original.Where(e => e.IsTranslated);
                    break;
                case "Untranslated":
                    filtered = original.Where(e => !e.IsTranslated);
                    break;
                case "HasIssues":
                    filtered = original.Where(e => e.HasPlaceholderIssues);
                    break;
                // "All" - no filter
            }

            SelectedFile.Entries = new ObservableCollection<LocalizationEntryViewModel>(filtered);
            SelectedFile.RecalculateCounts();
        }

        private readonly Dictionary<string, List<LocalizationEntryViewModel>> _originalEntries = new();

        private void CopySourceToTranslation(LocalizationEntryViewModel? entry)
        {
            if (entry == null) return;
            entry.Entry.TranslatedText = entry.Entry.SourceText;
            entry.Status = "Copied from source";
            StatusMessage = $"Copied source to translation for: {entry.Key}";
        }

        private void SelectAll(object? parameter)
        {
            if (SelectedFile?.Entries == null) return;
            foreach (var entry in SelectedFile.Entries)
            {
                entry.IsSelected = true;
            }
            StatusMessage = $"Selected {SelectedFile.Entries.Count} entries";
        }

        private void DeselectAll(object? parameter)
        {
            if (SelectedFile?.Entries == null) return;
            foreach (var entry in SelectedFile.Entries)
            {
                entry.IsSelected = false;
            }
            StatusMessage = "Deselected all entries";
        }

        private async void BatchTranslate(object? parameter)
        {
            if (SelectedFile?.Entries == null || !AiEnabled)
            {
                MessageBox.Show("Please ensure AI is enabled and a file is selected.", "AI Translate",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var config = Services.SettingsService.LoadConfig();
            var overwriteExisting = config.OverwriteExistingTranslations;
            
            // Smart selection: use ticked entries, or current entry passed as parameter
            var tickedEntries = SelectedFile.Entries.Where(e => e.IsSelected).ToList();
            List<LocalizationEntryViewModel> entriesToTranslate;
            
            if (tickedEntries.Count > 0)
            {
                // Batch mode: translate all ticked entries
                entriesToTranslate = overwriteExisting
                    ? tickedEntries // All ticked, including translated
                    : tickedEntries.Where(e => !e.IsTranslated).ToList(); // Only untranslated
            }
            else if (parameter is LocalizationEntryViewModel currentEntry)
            {
                // Single mode: translate current row if nothing is ticked
                if (!overwriteExisting && currentEntry.IsTranslated)
                {
                    var result = MessageBox.Show(
                        $"Entry '{currentEntry.Key}' is already translated.\n\nOverwrite existing translation?\n\nTip: Enable 'Overwrite Existing Translations' in Settings to skip this prompt.",
                        "Confirm Overwrite",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        StatusMessage = "Translation cancelled";
                        return;
                    }
                }
                entriesToTranslate = new List<LocalizationEntryViewModel> { currentEntry };
            }
            else
            {
                MessageBox.Show(
                    "No entries to translate.\n\n• Tick entries to translate multiple at once\n• Or click a row and press the Translate button",
                    "AI Translate", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            if (entriesToTranslate.Count == 0)
            {
                var message = overwriteExisting
                    ? "No entries to translate. Please tick entries first."
                    : "No untranslated entries found. To translate already-translated entries, enable 'Overwrite Existing Translations' in Settings.";
                MessageBox.Show(message, "AI Translate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Confirmation message
            string confirmMessage;
            if (entriesToTranslate.Count == 1)
            {
                confirmMessage = $"Translate entry '{entriesToTranslate[0].Key}' with AI?";
            }
            else
            {
                var alreadyTranslated = entriesToTranslate.Count(e => e.IsTranslated);
                confirmMessage = overwriteExisting && alreadyTranslated > 0
                    ? $"Translate {entriesToTranslate.Count} entries with AI?\n\n⚠️ {alreadyTranslated} entries are already translated and will be overwritten.\n\nThis may take several minutes and consume API quota."
                    : $"Translate {entriesToTranslate.Count} entries with AI?\n\nThis may take several minutes and consume API quota.";
            }
            
            var confirmResult = MessageBox.Show(confirmMessage, "AI Translate", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = $"Translating {entriesToTranslate.Count} {(entriesToTranslate.Count == 1 ? "entry" : "entries")}...";
                config.SourceLanguage = SourceLanguage;
                config.TargetLanguage = TargetLanguage;
                var translationService = new TranslationService(config);

                int completed = 0, failed = 0;
                foreach (var entry in entriesToTranslate)
                {
                    try
                    {
                        entry.Status = "Translating...";
                        var translateResult = await translationService.TranslateTextAsync(entry.Entry.SourceText, SourceLanguage, TargetLanguage);
                        if (translateResult.Success)
                        {
                            entry.Entry.TranslatedText = translateResult.TranslatedText;
                            entry.Status = $"Translated with {translateResult.Engine}";
                            completed++;
                        }
                        else
                        {
                            entry.Status = $"Failed: {translateResult.ErrorMessage}";
                            failed++;
                        }
                        StatusMessage = $"Progress: {completed + failed}/{entriesToTranslate.Count} ({completed} success, {failed} failed)";
                    }
                    catch
                    {
                        entry.Status = "Translation error";
                        failed++;
                    }
                }

                await SaveAllAsync();
                SelectedFile.RecalculateCounts();
                StatusMessage = $"Translation completed: {completed} success, {failed} failed";
                ShowToastCallback?.Invoke(
                    failed > 0 ? "Translation Complete (with errors)" : "Translation Complete",
                    $"{completed} translated, {failed} failed",
                    failed > 0 ? 2 : 0 // 2 = Warning if failures, 0 = Success
                );
            }
            catch (Exception ex)
            {
                StatusMessage = $"Translation error: {ex.Message}";
                MessageBox.Show($"Error during translation:\n{ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompareWithNewVersion(object? parameter)
        {
            if (SelectedFile == null)
            {
                MessageBox.Show("Please select a file to compare.", "Compare Versions",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Ask user to select new version file
                var dialog = new OpenFileDialog
                {
                    Title = "Select New Version File",
                    Filter = "YAML files (*.yml;*.yaml)|*.yml;*.yaml|All files (*.*)|*.*",
                    FilterIndex = 1
                };

                if (dialog.ShowDialog() != true) return;

                StatusMessage = "Comparing file versions...";

                // Parse new version file
                var newEntries = await ParadoxParser.ParseFileAsync(dialog.FileName);

                // Get current entries from selected file
                var currentEntries = SelectedFile.Entries.Select(e => e.Entry).ToList();

                // Get existing translations
                var existingTranslations = SelectedFile.Entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Entry.TranslatedText))
                    .ToDictionary(e => e.Entry.Key, e => e.Entry.TranslatedText);

                // Get project ID (or use a default)
                var projectId = "default"; // TODO: Get from current project

                // Create snapshots
                var oldVersion = new Models.FileVersion
                {
                    FilePath = SelectedFile.FilePath,
                    GameVersion = "Current",
                    ImportDate = SelectedFile.LastModified,
                    Entries = currentEntries,
                    FileHash = VersionControlService.CalculateEntriesHash(currentEntries)
                };

                var newVersion = new Models.FileVersion
                {
                    FilePath = dialog.FileName,
                    GameVersion = "New",
                    ImportDate = DateTime.Now,
                    Entries = newEntries.ToList(),
                    FileHash = VersionControlService.CalculateFileHash(dialog.FileName)
                };

                // Compare versions
                var comparison = FileComparisonService.CompareVersions(oldVersion, newVersion, existingTranslations);

                StatusMessage = $"Found {comparison.NewCount} new, {comparison.ModifiedCount} modified, {comparison.DeletedCount} deleted entries";

                // Show comparison window
                var comparisonWindow = new ComparisonWindow(projectId, comparison)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (comparisonWindow.ShowDialog() == true && comparisonWindow.ChangesApplied)
                {
                    // Apply changes from comparison
                    var updatedEntries = FileComparisonService.ApplyComparisonResults(comparison);

                    // Update current file with merged entries
                    SelectedFile.Entries.Clear();
                    var updatedViewModels = updatedEntries.Select(e => new LocalizationEntryViewModel(e)).ToList();
                    
                    // Hook change tracking
                    foreach (var vmEntry in updatedViewModels)
                    {
                        if (_watchedEntries.Add(vmEntry))
                        {
                            vmEntry.PropertyChanged += OnEntryPropertyChanged;
                        }
                    }

                    foreach (var vm in updatedViewModels)
                    {
                        SelectedFile.Entries.Add(vm);
                    }

                    SelectedFile.RecalculateCounts();

                    // Save snapshot of new version
                    VersionControlService.CreateSnapshot(projectId, dialog.FileName, "New", newEntries.ToList(), 
                        $"Compared with {Path.GetFileName(SelectedFile.FilePath)}");

                    await SaveAllAsync();

                    StatusMessage = $"Successfully merged changes from new version";
                    ShowToastCallback?.Invoke("Version Merged", 
                        $"Applied {comparison.NewCount} new and {comparison.ModifiedCount} modified entries", 0);
                }
                else
                {
                    StatusMessage = "Comparison cancelled";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error comparing versions: {ex.Message}";
                MessageBox.Show($"Error comparing versions:\n{ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}