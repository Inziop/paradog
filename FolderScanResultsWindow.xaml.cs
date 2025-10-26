using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;
using MessageBox = ParadoxTranslator.Utils.ModernMessageBox;

namespace ParadoxTranslator
{
    public partial class FolderScanResultsWindow : Window
    {
        private readonly FolderScanSummary _summary;
        private readonly string _sourceLanguage;
        private readonly GameConfig _gameConfig;

        public FolderScanResultsWindow(FolderScanSummary summary, string sourceLanguage, GameConfig gameConfig)
        {
            InitializeComponent();
            
            _summary = summary;
            _sourceLanguage = sourceLanguage;
            _gameConfig = gameConfig;
            
            LoadResults();
        }

        private void LoadResults()
        {
            // Display summary
            FolderPathText.Text = $"📂 Scanned: {_summary.ModFolderPath}";
            
            var totalMissing = _summary.MissingFilesByLanguage.Sum(kvp => kvp.Value);
            var totalExisting = _summary.ExistingFilesByLanguage.Sum(kvp => kvp.Value);
            
            SummaryText.Text = $"Total files scanned: {_summary.TotalFilesScanned} | " +
                              $"Source files: {_summary.SourceLanguageFiles} | " +
                              $"Existing: {totalExisting} | " +
                              $"Missing: {totalMissing}";
            
            // Display results (only missing files by default)
            var missingFiles = _summary.AllResults.Where(r => !r.Exists).ToList();
            ResultsDataGrid.ItemsSource = missingFiles;
            
            // Disable generate button if no missing files
            GenerateAllButton.IsEnabled = missingFiles.Any();
        }

        private void OnGenerateSingle(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not ScanResult result)
                return;

            var scanner = new FolderScannerService();
            var created = scanner.GenerateMissingFiles(
                new List<string> { result.FilePath },
                _sourceLanguage,
                result.Language,
                _gameConfig
            );

            if (created.Any())
            {
                result.Exists = true;
                ResultsDataGrid.Items.Refresh();
                MessageBox.Show($"Generated: {result.FileName}", "Success", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Failed to generate: {result.FileName}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnGenerateAll(object sender, RoutedEventArgs e)
        {
            var missingFiles = _summary.AllResults.Where(r => !r.Exists).ToList();
            if (!missingFiles.Any())
            {
                MessageBox.Show("No missing files to generate.", "Information", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Generate {missingFiles.Count} missing file(s)?",
                "Confirm Generation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            var scanner = new FolderScannerService();
            var totalCreated = 0;

            foreach (var lang in _summary.MissingFilesByLanguage.Keys)
            {
                var langMissing = missingFiles.Where(f => f.Language == lang)
                    .Select(f => f.FilePath).ToList();
                
                if (!langMissing.Any())
                    continue;

                var created = scanner.GenerateMissingFiles(
                    langMissing,
                    _sourceLanguage,
                    lang,
                    _gameConfig
                );

                totalCreated += created.Count;
                
                // Update results
                foreach (var file in created)
                {
                    var scanResult = missingFiles.FirstOrDefault(r => r.FilePath == file);
                    if (scanResult != null)
                        scanResult.Exists = true;
                }
            }

            ResultsDataGrid.Items.Refresh();
            MessageBox.Show($"Successfully generated {totalCreated} file(s).", "Success", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Reload to update summary
            LoadResults();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
