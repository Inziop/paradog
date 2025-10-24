using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ParadoxTranslator.ViewModels;

namespace ParadoxTranslator;

/// <summary>
/// Settings window for configuring translation options
/// </summary>
public partial class SettingsWindow : Window, IDisposable
{
    private readonly HttpClient _httpClient = new();
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void OnTestGoogleClick(object sender, RoutedEventArgs e)
    {
        var key = GoogleApiKeyTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show("Google API key is empty. Please enter a key to test.", "Test API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Basic feedback: inform user key is present. Full validation requires network/API check.
        MessageBox.Show("Google API key is set. To fully validate the key, save settings and try translating an entry.", "Test API Key", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnTestDeepLClick(object sender, RoutedEventArgs e)
    {
        var key = DeepLApiKeyTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show("DeepL API key is empty. Please enter a key to test.", "Test API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show("DeepL API key is set. To fully validate the key, save settings and try translating an entry.", "Test API Key", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadSettings()
    {
        // Load current settings from configuration
        var cfg = Services.SettingsService.LoadConfig();

        // Engine
        // Reset and load all fields, especially new Gemini ones
        for (int i = 0; i < EngineComboBox.Items.Count; i++)
        {
            if (EngineComboBox.Items[i] is ComboBoxItem item && (item.Tag?.ToString() ?? "") == cfg.SelectedEngine)
            {
                EngineComboBox.SelectedIndex = i;
                break;
            }
        }

        GoogleApiKeyTextBox.Text = cfg.GoogleApiKey;
        DeepLApiKeyTextBox.Text = cfg.DeepLApiKey;
        GeminiApiKeyTextBox.Text = cfg.GeminiApiKey;aaa
        GeminiEndpointTextBox.Text = cfg.GeminiEndpoint;
        EnableAICheckBox.IsChecked = cfg.EnableAi;
        OverwriteExistingCheckBox.IsChecked = cfg.OverwriteExistingTranslations;
        ValidatePlaceholdersCheckBox.IsChecked = cfg.TreatPlaceholderMismatchAsError;
        CreateBackupCheckBox.IsChecked = cfg.CreateBackupBeforeSave;
        MaxConcurrentTextBox.Text = cfg.MaxConcurrentRequests.ToString();

        // UI settings
        ThemeComboBox.SelectedIndex = cfg.Theme == "Light" ? 1 : 0;
        ShowProgressAnimationsCheckBox.IsChecked = cfg.ShowProgressAnimations;
        AutoSaveCheckBox.IsChecked = cfg.AutoSave;

        // Advanced
        QualityComboBox.SelectedIndex = cfg.Quality == "Fast" ? 0 : cfg.Quality == "High" ? 2 : 1;
        EnableLoggingCheckBox.IsChecked = cfg.EnableLogging;
        ShowDebugInfoCheckBox.IsChecked = cfg.ShowDebugInfo;
    }

    private void SaveSettings()
    {
        // Read UI and save settings to configuration
        var cfg = Services.SettingsService.LoadConfig();

        // AI enable
        cfg.EnableAi = EnableAICheckBox.IsChecked == true;

        if (EngineComboBox.SelectedItem is ComboBoxItem engineItem)
            cfg.SelectedEngine = engineItem.Tag?.ToString() ?? cfg.SelectedEngine;

        cfg.GoogleApiKey = GoogleApiKeyTextBox.Text.Trim();
        cfg.DeepLApiKey = DeepLApiKeyTextBox.Text.Trim();
        cfg.GeminiApiKey = GeminiApiKeyTextBox.Text.Trim();
        cfg.GeminiEndpoint = GeminiEndpointTextBox.Text.Trim();
        cfg.OverwriteExistingTranslations = OverwriteExistingCheckBox.IsChecked == true;
        cfg.TreatPlaceholderMismatchAsError = ValidatePlaceholdersCheckBox.IsChecked == true;
        cfg.CreateBackupBeforeSave = CreateBackupCheckBox.IsChecked == true;

        if (int.TryParse(MaxConcurrentTextBox.Text, out var maxConc) && maxConc > 0)
            cfg.MaxConcurrentRequests = Math.Clamp(maxConc, 1, 50);

        cfg.Theme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? cfg.Theme;
        cfg.ShowProgressAnimations = ShowProgressAnimationsCheckBox.IsChecked == true;
        cfg.AutoSave = AutoSaveCheckBox.IsChecked == true;

        cfg.Quality = (QualityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? cfg.Quality;
        cfg.EnableLogging = EnableLoggingCheckBox.IsChecked == true;
        cfg.ShowDebugInfo = ShowDebugInfoCheckBox.IsChecked == true;

        // Validate API key when AI is enabled
        if (cfg.EnableAi)
        {
            var engine = (cfg.SelectedEngine ?? string.Empty).ToLowerInvariant();
            if ((engine == "google" && string.IsNullOrWhiteSpace(cfg.GoogleApiKey)) ||
                (engine == "deepl" && string.IsNullOrWhiteSpace(cfg.DeepLApiKey)) ||
                ((engine == "gemini" || engine == "openai" || engine == "openai-gemini") && string.IsNullOrWhiteSpace(cfg.GeminiApiKey)))
            {
                MessageBox.Show("AI is enabled but the required API key for the selected engine is missing. Please provide the API key or disable AI.", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // do not save
            }
        }

        Services.SettingsService.SaveConfig(cfg);
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

    private async void OnTestGeminiClick(object sender, RoutedEventArgs e)
    {
        var key = GeminiApiKeyTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show("Gemini API key is empty. Please enter a key to test.", "Test API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Try a minimal request to validate the key
            var endpoint = GeminiEndpointTextBox.Text?.Trim() ?? string.Empty;
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = "hello" } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsync(endpoint, content, cts.Token);
            response.EnsureSuccessStatusCode();

            MessageBox.Show("Successfully connected to Google AI Studio (Gemini)! The API key and endpoint are valid.", 
                          "API Test Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to validate Gemini API key: {ex.Message}\n\nPlease verify your key and endpoint URL.", 
                          "API Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        // Reset to default settings
        EngineComboBox.SelectedIndex = 0;
        GoogleApiKeyTextBox.Text = "";
        DeepLApiKeyTextBox.Text = "";
        GeminiApiKeyTextBox.Text = "";
        GeminiEndpointTextBox.Text = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
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

    public void Dispose()
    {
        try { _httpClient?.Dispose(); } catch { }
    }
}
