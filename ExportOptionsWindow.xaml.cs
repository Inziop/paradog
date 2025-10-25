using System.Windows;

namespace ParadoxTranslator;

public partial class ExportOptionsWindow : Window
{
    public enum ExportFormat { Csv, Json, Yaml }

    public ExportFormat SelectedFormat { get; private set; } = ExportFormat.Csv;
    public bool ExportAllFiles { get; private set; } = true;

    public ExportOptionsWindow()
    {
        InitializeComponent();
    }

    private void OnContinue(object sender, RoutedEventArgs e)
    {
        if (CsvRadio.IsChecked == true) SelectedFormat = ExportFormat.Csv;
        else if (JsonRadio.IsChecked == true) SelectedFormat = ExportFormat.Json;
        else SelectedFormat = ExportFormat.Yaml;

        ExportAllFiles = AllFilesRadio.IsChecked == true;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnWindowClose(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
