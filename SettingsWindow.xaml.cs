using System.Windows;
using ParadoxTranslator.ViewModels;

namespace ParadoxTranslator;

/// <summary>
/// Settings window for configuring translation options
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load current settings from configuration
        // This would typically load from a config file or settings service
    }

    private void SaveSettings()
    {
        // Save settings to configuration
        // This would typically save to a config file or settings service
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        // Reset to default settings
        EngineComboBox.SelectedIndex = 0;
        GoogleApiKeyTextBox.Text = "";
        DeepLApiKeyTextBox.Text = "";
        OverwriteExistingCheckBox.IsChecked = true;
        ValidatePlaceholdersCheckBox.IsChecked = true;
        CreateBackupCheckBox.IsChecked = true;
        MaxConcurrentTextBox.Text = "3";
        ThemeComboBox.SelectedIndex = 0;
        ShowProgressAnimationsCheckBox.IsChecked = true;
        AutoSaveCheckBox.IsChecked = false;
        QualityComboBox.SelectedIndex = 1;
        EnableLoggingCheckBox.IsChecked = false;
        ShowDebugInfoCheckBox.IsChecked = false;
    }
}
