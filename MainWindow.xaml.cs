using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TextBox = System.Windows.Controls.TextBox;
using ParadoxTranslator.ViewModels;
using ParadoxTranslator.Services;
using ParadoxTranslator.Controls;
using ParadoxTranslator.Models;
using System.Threading.Tasks;

namespace ParadoxTranslator
{
    public partial class MainWindow : Window
    {
        private Project _currentProject;
        
        public MainWindow(Project project)
        {
            InitializeComponent();
            _currentProject = project;
            
            var viewModel = new MainViewModel();
            DataContext = viewModel;

            // Set project languages
            viewModel.SourceLanguage = project.SourceLanguage;
            viewModel.TargetLanguage = project.TargetLanguage;
            
            // Wire up Toast callback
            viewModel.ShowToastCallback = (title, message, type) => ShowToast(title, message, (ToastType)type);

            // Update project name in header
            ProjectNameTextBlock.Text = project.Name;

            Loaded += OnLoadedAsync;
            StateChanged += OnStateChanged;
            Closing += OnClosing;
            
            // Register keyboard shortcuts
            RegisterKeyboardShortcuts();
            
            // Update debug info visibility
            UpdateDebugInfo();
            
            // Subscribe to localization changes
            Services.LocalizationService.Instance.PropertyChanged += (s, e) => UpdateLocalization();
            UpdateLocalization();
        }

        private void UpdateLocalization()
        {
            var loc = Services.LocalizationService.Instance;
            
            // Toolbar buttons
            OpenFileButton.Content = $"📂 {loc["OpenFile"]}";
            CompareButton.Content = $"🔄 {loc["Compare"]}";
            SettingsButton.Content = $"⚙️ {loc["Settings"]}";
            StatisticsButton.Content = $"📊 {loc["Statistics"]}";
            ExportButton.Content = $"💾 {loc["MenuExport"]}";
            HelpButton.Content = $"❓ {loc["MenuHelp"]}";
            AboutButton.Content = $"ℹ️ {loc["MenuAbout"]}";
            
            // Language labels
            FromLabel.Text = loc["FromLanguage"];
            ToLabel.Text = loc["ToLanguage"];
            
            // AI checkbox
            EnableAICheckBox.Content = loc["EnableAICheckbox"];
            
            // Batch action buttons
            SelectAllButton.Content = loc["SelectAll"];
            DeselectAllButton.Content = loc["DeselectAll"];
            TranslateButton.Content = $"🤖 {loc["TranslateSelected"]}";
            
            // Switch project button
            SwitchProjectButton.Content = $"⇄ {loc["SwitchProject"]}";
        }

        public void UpdateDebugInfo()
        {
            var config = Services.SettingsService.LoadConfig();
            if (config.ShowDebugInfo)
            {
                DebugInfoBorder.Visibility = Visibility.Visible;
                var vm = DataContext as MainViewModel;
                var fileCount = vm?.Files.Count ?? 0;
                var entryCount = vm?.Files.Sum(f => f.Entries.Count) ?? 0;
                DebugInfoText.Text = $"F:{fileCount} E:{entryCount}";
            }
            else
            {
                DebugInfoBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void OnStateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Adjust window to fit within working area (excluding taskbar)
                var workArea = SystemParameters.WorkArea;
                MaxHeight = workArea.Height;
                MaxWidth = workArea.Width;
                
                // Remove all margins to make window reach screen edges completely
                BorderThickness = new Thickness(0);
                Margin = new Thickness(0);
            }
            else
            {
                MaxHeight = double.PositiveInfinity;
                MaxWidth = double.PositiveInfinity;
                BorderThickness = new Thickness(0);
                Margin = new Thickness(0);
            }
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            
            // Update maximize button icon
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void ShowToast(string title, string message, ToastType type = ToastType.Success)
        {
            // Must be on UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowToast(title, message, type));
                return;
            }
            
            var toast = new ToastNotification();
            ToastContainer?.Children.Add(toast);
            toast.Show(title, message, type);
            
            // Auto-remove from container after animation completes (3s display + 0.2s fade-out)
            var removeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.3) };
            removeTimer.Tick += (s, e) =>
            {
                removeTimer.Stop();
                ToastContainer?.Children.Remove(toast);
            };
            removeTimer.Start();
        }

        private void RegisterKeyboardShortcuts()
        {
            // Ctrl+O: Open files
            var openCmd = new RoutedCommand();
            openCmd.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(openCmd, (s, e) => {
                if (DataContext is MainViewModel vm) vm.OpenYamlFileCommand.Execute(null);
            }));

            // Ctrl+E: Export
            var exportCmd = new RoutedCommand();
            exportCmd.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(exportCmd, (s, e) => {
                if (DataContext is MainViewModel vm) vm.ExportCommand.Execute(null);
            }));

            // Ctrl+, (Comma): Settings
            var settingsCmd = new RoutedCommand();
            settingsCmd.InputGestures.Add(new KeyGesture(Key.OemComma, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(settingsCmd, (s, e) => {
                if (DataContext is MainViewModel vm) vm.OpenSettingsCommand.Execute(null);
            }));

            // F5: Refresh (show statistics for now)
            var refreshCmd = new RoutedCommand();
            refreshCmd.InputGestures.Add(new KeyGesture(Key.F5));
            CommandBindings.Add(new CommandBinding(refreshCmd, (s, e) => {
                if (DataContext is MainViewModel vm) vm.ShowStatisticsCommand.Execute(null);
            }));
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel) return;
            
            var textBox = sender as TextBox;
            var searchText = textBox?.Text?.Trim() ?? string.Empty;
            
            // Show/hide clear button
            if (FindName("ClearSearchButton") is Button clearButton)
            {
                clearButton.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Collapsed : Visibility.Visible;
            }
            
            // Get the ListBox
            var listBox = FindName("FileListBox") as ListBox;
            if (listBox == null) return;
            
            // Apply filter using CollectionViewSource
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(viewModel.Files);
            if (view == null) return;
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                view.Filter = null; // Show all
            }
            else
            {
                view.Filter = obj =>
                {
                    if (obj is FileViewModel file)
                    {
                        return file.FileName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                               file.RelativePath.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                };
            }
        }

        private void OnClearSearchClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SearchText = string.Empty;
            }
        }

        private async void OnSwitchProject(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmDialog(
                "Switch Project",
                "Do you want to switch to another project?\nAny unsaved changes will be saved automatically.");
            dialog.Owner = this;
            
            if (dialog.ShowDialog() == true)
            {
                // Save current work
                if (DataContext is MainViewModel vm)
                {
                    var snapshot = vm.BuildSessionSnapshot();
                    SessionService.Save(snapshot);
                    
                    // Update current project with file list
                    _currentProject.FilePaths = vm.Files.Select(f => f.FilePath).ToList();
                    ProjectService.SaveProject(_currentProject);
                }

                // Show project selection window
                var projectSelection = new ProjectSelectionWindow();
                projectSelection.Owner = this;
                projectSelection.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (projectSelection.ShowDialog() == true && projectSelection.SelectedProject != null)
                {
                    // Reload current window with new project
                    await LoadProject(projectSelection.SelectedProject);
                    
                    // Make sure main window stays focused
                    this.Activate();
                    this.Focus();
                }
            }
        }

        private void OnScanModFolder(object sender, RoutedEventArgs e)
        {
            // Open folder picker using WindowsAPICodePack
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                Title = "Select mod folder to scan",
                IsFolderPicker = true,
                EnsurePathExists = true
            };

            if (dialog.ShowDialog() != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                return;

            var modPath = dialog.FileName;
            
            // Get game configuration
            var gameConfig = GameConfig.GetAllConfigs()[_currentProject.GameType];
            
            // Prepare target languages (exclude source language)
            var targetLanguages = new[] { "english", "french", "german", "spanish", 
                                         "portuguese", "russian", "polish", "simp_chinese", 
                                         "japanese", "korean" }
                .Where(lang => lang != _currentProject.SourceLanguage)
                .ToList();

            try
            {
                // Perform scan (no progress dialog, do it synchronously)
                var scanner = new FolderScannerService();
                var summary = scanner.PerformComprehensiveScan(
                    modPath,
                    gameConfig.LocalizationFolder,
                    _currentProject.SourceLanguage,
                    targetLanguages
                );

                // Show results window
                var resultsWindow = new FolderScanResultsWindow(
                    summary,
                    _currentProject.SourceLanguage,
                    gameConfig
                );
                resultsWindow.Owner = this;
                resultsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.Log("ERROR", "Error scanning folder", ex);
                Utils.ModernMessageBox.Show(
                    $"Error scanning folder: {ex.Message}",
                    "Scan Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async Task LoadProject(Project project)
        {
            _currentProject = project;
            
            // Update UI
            ProjectNameTextBlock.Text = project.Name;
            
            // Create new ViewModel
            var viewModel = new MainViewModel();
            viewModel.SourceLanguage = project.SourceLanguage;
            viewModel.TargetLanguage = project.TargetLanguage;
            viewModel.ShowToastCallback = (title, message, type) => ShowToast(title, message, (ToastType)type);
            
            DataContext = viewModel;
            
            // Load project files
            if (project.FilePaths != null && project.FilePaths.Count > 0)
            {
                foreach (var filePath in project.FilePaths)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        await viewModel.LoadFileAsync(filePath);
                    }
                }
                
                // Restore translations from session
                ParadoxTranslator.Models.SessionState session = SessionService.Load();
                if (session.Translations != null)
                {
                    foreach (var file in viewModel.Files)
                    {
                        if (session.Translations.TryGetValue(file.FilePath, out var translations))
                        {
                            foreach (var entry in file.Entries)
                            {
                                if (translations.TryGetValue(entry.Entry.Key, out var translatedText))
                                {
                                    entry.Entry.TranslatedText = translatedText;
                                }
                            }
                        }
                    }
                }
            }
            
            ShowToast("Project Loaded", $"Switched to project: {project.Name}", ToastType.Success);
        }

        // Context menu handlers
        private void OnCopyKey(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is LocalizationEntryViewModel entry)
            {
                System.Windows.Clipboard.SetText(entry.Key);
                if (DataContext is MainViewModel vm)
                    vm.StatusMessage = $"Copied key: {entry.Key}";
            }
        }

        private void OnCopySource(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is LocalizationEntryViewModel entry)
            {
                System.Windows.Clipboard.SetText(entry.Entry.SourceText);
                if (DataContext is MainViewModel vm)
                    vm.StatusMessage = "Copied source text";
            }
        }

        private void OnCopyTranslation(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is LocalizationEntryViewModel entry)
            {
                System.Windows.Clipboard.SetText(entry.Entry.TranslatedText ?? string.Empty);
                if (DataContext is MainViewModel vm)
                    vm.StatusMessage = "Copied translation";
            }
        }

        private void OnClearTranslation(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is LocalizationEntryViewModel entry)
            {
                entry.Entry.TranslatedText = string.Empty;
                entry.Status = "Cleared";
                if (DataContext is MainViewModel vm)
                    vm.StatusMessage = $"Cleared translation for: {entry.Key}";
            }
        }

        private async void OnLoadedAsync(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Wire up toast callback
                vm.ShowToastCallback = (title, message, type) => ShowToast(title, message, (ToastType)type);

                // Load files from current project
                if (_currentProject.FilePaths != null && _currentProject.FilePaths.Count > 0)
                {
                    foreach (var filePath in _currentProject.FilePaths)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            await vm.LoadFileAsync(filePath);
                        }
                    }
                    
                    // Try to restore translations from session for this project
                    ParadoxTranslator.Models.SessionState session = SessionService.Load();
                    // Only restore translations, not file list
                    if (session.Translations != null)
                    {
                        foreach (var file in vm.Files)
                        {
                            if (session.Translations.TryGetValue(file.FilePath, out var translations))
                            {
                                foreach (var entry in file.Entries)
                                {
                                    if (translations.TryGetValue(entry.Entry.Key, out var translatedText))
                                    {
                                        entry.Entry.TranslatedText = translatedText;
                                    }
                                }
                            }
                        }
                    }
                }

                // Restore window placement
                var session2 = SessionService.Load();
                if (session2.Width.HasValue && session2.Height.HasValue)
                {
                    this.Width = session2.Width.Value;
                    this.Height = session2.Height.Value;
                }
                if (session2.Left.HasValue && session2.Top.HasValue)
                {
                    this.Left = session2.Left.Value;
                    this.Top = session2.Top.Value;
                }
                if (!string.IsNullOrWhiteSpace(session2.WindowState))
                {
                    if (System.Enum.TryParse<WindowState>(session2.WindowState, out var state))
                    {
                        this.WindowState = state;
                    }
                }
                
                // Bring window to front
                this.Activate();
                this.Focus();
            }
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                ParadoxTranslator.Models.SessionState snapshot = vm.BuildSessionSnapshot();
                // Capture window placement
                snapshot.WindowState = this.WindowState.ToString();
                snapshot.Width = this.Width;
                snapshot.Height = this.Height;
                snapshot.Left = this.Left;
                snapshot.Top = this.Top;
                SessionService.Save(snapshot);
                
                // Update project with current file list
                _currentProject.FilePaths = vm.Files.Select(f => f.FilePath).ToList();
                ProjectService.SaveProject(_currentProject);
            }
        }

        private void OnTranslateClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && EntriesDataGrid.SelectedItem is LocalizationEntryViewModel selectedEntry)
            {
                // Pass selected entry to command
                vm.BatchTranslateCommand.Execute(selectedEntry);
            }
            else if (DataContext is MainViewModel vm2)
            {
                // No selection, pass null (will use ticked entries)
                vm2.BatchTranslateCommand.Execute(null);
            }
        }
    }
}