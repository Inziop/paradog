using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
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
        
        // Windows API for proper maximize handling
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        
        private const int WM_GETMINMAXINFO = 0x0024;
        
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
            
            // Hook Windows message for proper maximize
            SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
            };
            
            // Set optimal window size based on screen resolution
            SetOptimalWindowSize();
            
            // Register keyboard shortcuts
            RegisterKeyboardShortcuts();
            
            // Update debug info visibility
            UpdateDebugInfo();
            
            // Subscribe to localization changes
            Services.LocalizationService.Instance.PropertyChanged += (s, e) => UpdateLocalization();
            UpdateLocalization();
        }

        private void SetOptimalWindowSize()
        {
            var workArea = SystemParameters.WorkArea;
            
            // Normal state: 80% of work area (balanced, not too big, not too small)
            Width = workArea.Width * 0.8;
            Height = workArea.Height * 0.8;
            
            // Ensure minimum size
            if (Width < 1200) Width = 1200;
            if (Height < 700) Height = 700;
            
            // Center on screen PROPERLY
            Left = workArea.Left + (workArea.Width - Width) / 2;
            Top = workArea.Top + (workArea.Height - Height) / 2;
        }

        // Handle Windows messages for proper maximize
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // Get monitor info
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (GetMonitorInfo(monitor, ref monitorInfo))
                    {
                        var workArea = monitorInfo.rcWork;
                        var maxInfo = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;
                        
                        // Set maximize size to work area (exclude taskbar)
                        maxInfo.ptMaxSize.X = workArea.Right - workArea.Left;
                        maxInfo.ptMaxSize.Y = workArea.Bottom - workArea.Top;
                        maxInfo.ptMaxPosition.X = workArea.Left - monitorInfo.rcMonitor.Left;
                        maxInfo.ptMaxPosition.Y = workArea.Top - monitorInfo.rcMonitor.Top;
                        
                        Marshal.StructureToPtr(maxInfo, lParam, true);
                        handled = true;
                    }
                }
            }
            return IntPtr.Zero;
        }

        private void UpdateLocalization()
        {
            var loc = Services.LocalizationService.Instance;
            
            // Toolbar buttons
            OpenFileButton.Content = $"📂 {loc["OpenFile"]}";
            CompareButton.Content = $"🔄 {loc["Compare"]}";
            SettingsButton.Content = $"⚙️ {loc["Settings"]}";
            ExportButton.Content = $"� {loc["MenuExport"]}";
            MoreButton.Content = $"⋮ {loc["More"] ?? "More"}";
            // Statistics, Help, About now in More menu
            
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
                // When maximized: remove rounded corners, fit to work area (exclude taskbar)
                var workArea = SystemParameters.WorkArea;
                MaxHeight = workArea.Height;
                MaxWidth = workArea.Width;
                
                // Remove border for edge-to-edge maximized window
                var mainBorder = (Border)((Grid)Content).Children[0];
                mainBorder.CornerRadius = new CornerRadius(0);
                mainBorder.BorderThickness = new Thickness(0);
                
                // Update button: Show "restore down" icon and tooltip
                UpdateMaximizeButton(isMaximized: true);
            }
            else if (WindowState == WindowState.Normal)
            {
                // When restored: add rounded corners back AND set optimal size
                MaxHeight = double.PositiveInfinity;
                MaxWidth = double.PositiveInfinity;
                
                var mainBorder = (Border)((Grid)Content).Children[0];
                mainBorder.CornerRadius = new CornerRadius(8);
                mainBorder.BorderThickness = new Thickness(1);
                
                // Set optimal size and center position for restored window
                SetOptimalWindowSize();
                
                // Update button: Show "maximize" icon and tooltip
                UpdateMaximizeButton(isMaximized: false);
            }
        }

        private void UpdateMaximizeButton(bool isMaximized)
        {
            // Find the MaximizeButton in visual tree
            var button = this.FindName("MaximizeButton") as Button;
            if (button != null)
            {
                if (isMaximized)
                {
                    // Maximized state: show "Restore Down" (❐ or 🗗)
                    button.Content = "🗗";
                    button.ToolTip = "Restore Down";
                }
                else
                {
                    // Normal state: show "Maximize" (□ or 🗖)
                    button.Content = "🗖";
                    button.ToolTip = "Maximize";
                }
            }
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
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
            
            // Safety check: ensure ToastContainer exists
            if (ToastContainer == null)
            {
                System.Diagnostics.Debug.WriteLine($"ToastContainer is null! Cannot show toast: {title} - {message}");
                return;
            }
            
            var toast = new ToastNotification();
            ToastContainer.Children.Add(toast);
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
            
            // Debug info
            LoggingService.Log("INFO", $"Project GameType: {_currentProject.GameType}");
            LoggingService.Log("INFO", $"Localization folder: {gameConfig.LocalizationFolder}");
            LoggingService.Log("INFO", $"Source language: {_currentProject.SourceLanguage}");
            
            // Prepare target languages
            List<string> targetLanguages;
            
            if (gameConfig.UseOverrideMode)
            {
                // In override mode, only use the project's target language
                targetLanguages = new List<string> { _currentProject.TargetLanguage };
                LoggingService.Log("INFO", $"Override mode: Using only project target language: {_currentProject.TargetLanguage}");
            }
            else
            {
                // Normal mode: Use all languages from gameConfig (excluding source)
                var allLanguages = gameConfig.LanguageKeys.Keys.ToList();
                targetLanguages = allLanguages
                    .Where(lang => gameConfig.LanguageKeys[lang] != gameConfig.LanguageKeys.GetValueOrDefault(_currentProject.SourceLanguage, _currentProject.SourceLanguage))
                    .Select(lang => gameConfig.LanguageKeys[lang])
                    .ToList();
            }
            
            LoggingService.Log("INFO", $"Target languages: {string.Join(", ", targetLanguages)}");

            try
            {
                // Perform scan
                var scanner = new FolderScannerService();
                var summary = scanner.PerformComprehensiveScan(
                    modPath,
                    gameConfig.LocalizationFolder,
                    gameConfig.LanguageKeys.GetValueOrDefault(_currentProject.SourceLanguage, _currentProject.SourceLanguage),
                    targetLanguages,
                    gameConfig
                );

                // Show results window
                var resultsWindow = new FolderScanResultsWindow(
                    summary,
                    gameConfig.LanguageKeys.GetValueOrDefault(_currentProject.SourceLanguage, _currentProject.SourceLanguage),
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
            
            // Get game config to display game name
            var gameConfig = GameConfig.GetAllConfigs()[project.GameType];
            
            // Update UI with project name and game type
            ProjectNameTextBlock.Text = $"{project.Name} ({gameConfig.DisplayName})";
            
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
                if (session?.Translations != null)
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
                try
                {
                    System.Windows.Clipboard.SetText(entry.Key);
                    if (DataContext is MainViewModel vm)
                        vm.StatusMessage = $"Copied key: {entry.Key}";
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    if (DataContext is MainViewModel vm)
                        vm.StatusMessage = "Clipboard is busy, please try again";
                }
            }
        }

        private void OnCopySource(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is LocalizationEntryViewModel entry)
            {
                try
                {
                    System.Windows.Clipboard.SetText(entry.Entry.SourceText);
                    if (DataContext is MainViewModel vm)
                        vm.StatusMessage = "Copied source text";
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    if (DataContext is MainViewModel vm)
                        vm.StatusMessage = "Clipboard is busy, please try again";
                }
            }
        }

        private void OnCopyTranslation(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is LocalizationEntryViewModel entry)
            {
                try
                {
                    System.Windows.Clipboard.SetText(entry.Entry.TranslatedText ?? string.Empty);
                    if (DataContext is MainViewModel vm)
                        vm.StatusMessage = "Copied translation";
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    if (DataContext is MainViewModel vm)
                        vm.StatusMessage = "Clipboard is busy, please try again";
                }
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
                    if (session?.Translations != null)
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

        private void OnFilterMenuClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && DataContext is MainViewModel vm)
            {
                var filterMode = menuItem.Tag?.ToString() ?? "All";
                vm.FilterMode = filterMode;
            }
        }

        private void OnMoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}