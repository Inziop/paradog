using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;
using MessageBox = ParadoxTranslator.Utils.ModernMessageBox;

namespace ParadoxTranslator
{
    public partial class ComparisonWindow : Window, INotifyPropertyChanged
    {
        private ComparisonResult _comparisonResult;
        private ObservableCollection<ComparisonEntry> _entries;
        private ICollectionView _entriesView;
        private string _projectId;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool ChangesApplied { get; private set; }
        public ComparisonResult? ComparisonResult => _comparisonResult;

        public ComparisonWindow(string projectId, ComparisonResult comparisonResult)
        {
            InitializeComponent();
            _projectId = projectId;
            _comparisonResult = comparisonResult;
            _entries = new ObservableCollection<ComparisonEntry>(comparisonResult.Entries);
            
            // Setup collection view for filtering
            _entriesView = CollectionViewSource.GetDefaultView(_entries);
            ComparisonDataGrid.ItemsSource = _entriesView;
            
            // Initialize UI
            UpdateVersionInfo();
            UpdateStatistics();
            
            // FORCE màu chữ cho buttons để TUYỆT ĐỐI không bị trắng trên trắng
            EnsureButtonTextColors();
            
            DataContext = this;
        }

        /// <summary>
        /// FORCE màu chữ cho tất cả filter buttons - BACKUP mechanism
        /// Đảm bảo TUYỆT ĐỐI không có nền trắng chữ trắng
        /// </summary>
        private void EnsureButtonTextColors()
        {
            // Set foreground explicitly cho tất cả buttons
            if (ShowAllButton != null)
            {
                ShowAllButton.Foreground = System.Windows.Media.Brushes.Black;
            }
            if (ShowNewButton != null)
            {
                ShowNewButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 87, 36)); // #155724
            }
            if (ShowModifiedButton != null)
            {
                ShowModifiedButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(133, 100, 4)); // #856404
            }
            if (ShowDeletedButton != null)
            {
                ShowDeletedButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(114, 28, 36)); // #721C24
            }
        }

        private void UpdateVersionInfo()
        {
            OldVersionText.Text = $"v{_comparisonResult.OldVersion.GameVersion} ({_comparisonResult.OldVersion.ImportDate:yyyy-MM-dd})";
            NewVersionText.Text = $"v{_comparisonResult.NewVersion.GameVersion} ({_comparisonResult.NewVersion.ImportDate:yyyy-MM-dd})";
            
            var oldFileName = System.IO.Path.GetFileName(_comparisonResult.OldVersion.FilePath);
            var newFileName = System.IO.Path.GetFileName(_comparisonResult.NewVersion.FilePath);
            OldFileNameText.Text = oldFileName;
            NewFileNameText.Text = newFileName;
        }

        private void UpdateStatistics()
        {
            NewCountText.Text = $"{_comparisonResult.NewCount} New";
            ModifiedCountText.Text = $"{_comparisonResult.ModifiedCount} Modified";
            DeletedCountText.Text = $"{_comparisonResult.DeletedCount} Deleted";
            UnchangedCountText.Text = $"{_comparisonResult.UnchangedCount} Unchanged";
        }

        private void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            if (_entriesView == null) return;

            // Determine which filters are active
            var showAll = ShowAllButton.IsChecked == true;
            var showNew = ShowNewButton.IsChecked == true;
            var showModified = ShowModifiedButton.IsChecked == true;
            var showDeleted = ShowDeletedButton.IsChecked == true;

            // If "All" is checked, show everything
            if (showAll)
            {
                _entriesView.Filter = null;
                return;
            }

            // Apply filter based on selected change types
            _entriesView.Filter = obj =>
            {
                if (obj is not ComparisonEntry entry) return false;

                return (showNew && entry.ChangeType == ChangeType.New) ||
                       (showModified && entry.ChangeType == ChangeType.Modified) ||
                       (showDeleted && entry.ChangeType == ChangeType.Deleted) ||
                       (!showNew && !showModified && !showDeleted); // Show all if none selected
            };
        }

        private void OnSelectAll(object sender, RoutedEventArgs e)
        {
            foreach (var entry in _entries.Where(e => e.ChangeType != ChangeType.Deleted))
            {
                entry.IsSelected = true;
            }
        }

        private void OnSelectNew(object sender, RoutedEventArgs e)
        {
            foreach (var entry in _entries)
            {
                entry.IsSelected = entry.ChangeType == ChangeType.New;
            }
        }

        private void OnDeselectAll(object sender, RoutedEventArgs e)
        {
            foreach (var entry in _entries)
            {
                entry.IsSelected = false;
            }
        }

        private async void OnAiTranslate(object sender, RoutedEventArgs e)
        {
            var selectedEntries = _entries.Where(e => e.IsSelected && e.NeedsReview).ToList();
            
            if (selectedEntries.Count == 0)
            {
                MessageBox.Show("Please select entries to translate.", "AI Translation", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Translate {selectedEntries.Count} selected entries with AI?\n\n" +
                "This may take several minutes and consume API quota.",
                "AI Translation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var config = SettingsService.LoadConfig();
                var translationService = new TranslationService(config);
                
                int completed = 0;
                int failed = 0;

                foreach (var entry in selectedEntries)
                {
                    try
                    {
                        entry.StatusMessage = "Translating...";
                        var sourceText = entry.NewSourceText;
                        
                        if (!string.IsNullOrWhiteSpace(sourceText))
                        {
                            var translateResult = await translationService.TranslateTextAsync(
                                sourceText, 
                                config.SourceLanguage, 
                                config.TargetLanguage);

                            if (translateResult.Success)
                            {
                                entry.UpdatedTranslation = translateResult.TranslatedText;
                                entry.StatusMessage = $"Translated with {translateResult.Engine}";
                                completed++;
                            }
                            else
                            {
                                entry.StatusMessage = $"Failed: {translateResult.ErrorMessage}";
                                failed++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        entry.StatusMessage = $"Error: {ex.Message}";
                        failed++;
                    }
                }

                MessageBox.Show(
                    $"AI Translation completed!\n\nSuccessful: {completed}\nFailed: {failed}",
                    "AI Translation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during AI translation:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportReport(object sender, RoutedEventArgs e)
        {
            try
            {
                var report = FileComparisonService.GenerateReport(_comparisonResult);
                
                var dialog = new SaveFileDialog
                {
                    Title = "Save Comparison Report",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"comparison_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, report);
                    MessageBox.Show("Report exported successfully!", "Export Report",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnApplyChanges(object sender, RoutedEventArgs e)
        {
            // Count entries with changes
            var hasNewTranslations = _entries.Any(e => 
                e.NeedsReview && 
                !string.IsNullOrWhiteSpace(e.UpdatedTranslation) &&
                e.UpdatedTranslation != e.ExistingTranslation);

            if (!hasNewTranslations)
            {
                var result = MessageBox.Show(
                    "No new translations detected. Apply changes anyway?",
                    "Apply Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            // Update the comparison result with user changes
            foreach (var entry in _entries)
            {
                var original = _comparisonResult.Entries.FirstOrDefault(e => e.Key == entry.Key);
                if (original != null)
                {
                    original.UpdatedTranslation = entry.UpdatedTranslation;
                    original.IsSelected = entry.IsSelected;
                    original.StatusMessage = entry.StatusMessage;
                }
            }

            ChangesApplied = true;
            DialogResult = true;
            Close();
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            if (ChangesApplied)
            {
                Close();
                return;
            }

            var hasUnsavedChanges = _entries.Any(e => 
                !string.IsNullOrWhiteSpace(e.UpdatedTranslation) &&
                e.UpdatedTranslation != e.ExistingTranslation);

            if (hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to close?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
            }

            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
