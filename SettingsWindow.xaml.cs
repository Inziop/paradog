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
        UpdateLocalization();
        
        // Subscribe to language changes
        Services.LocalizationService.Instance.PropertyChanged += (s, e) => UpdateLocalization();
    }

    private void UpdateLocalization()
    {
        var loc = Services.LocalizationService.Instance;
        
        // Update UI texts
        SettingsTitleText.Text = loc["Settings"];
        SettingsDescriptionText.Text = loc["SettingsTitle"];
        ResetButton.Content = loc["ResetDefaults"];
        CancelButton.Content = loc["Cancel"];
        SaveButton.Content = loc["SaveSettings"];
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

    private void OnGetGoogleKeyClick(object sender, RoutedEventArgs e)
    {
        var message = @"To get a Google Cloud Translation API key:

1. Go to Google Cloud Console (https://console.cloud.google.com)
2. Create a new project or select an existing one
3. Enable the Cloud Translation API:
   - Go to APIs & Services > Library
   - Search for 'Cloud Translation API'
   - Click Enable
4. Create credentials:
   - Go to APIs & Services > Credentials
   - Click Create Credentials > API Key
   - Copy the generated API key

Would you like to open the Google Cloud Console?";

        var result = MessageBox.Show(message, "Get Google API Key", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result == MessageBoxResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://console.cloud.google.com",
                UseShellExecute = true
            });
        }
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
        GeminiApiKeyTextBox.Text = cfg.GeminiApiKey;
        GeminiEndpointTextBox.Text = cfg.GeminiEndpoint;
        EnableAICheckBox.IsChecked = cfg.EnableAi;
        OverwriteExistingCheckBox.IsChecked = cfg.OverwriteExistingTranslations;
        ValidatePlaceholdersCheckBox.IsChecked = cfg.TreatPlaceholderMismatchAsError;
        CreateBackupCheckBox.IsChecked = cfg.CreateBackupBeforeSave;
        MaxConcurrentTextBox.Text = cfg.MaxConcurrentRequests.ToString();

        // UI settings
        for (int i = 0; i < LanguageComboBox.Items.Count; i++)
        {
            if (LanguageComboBox.Items[i] is ComboBoxItem item && (item.Tag?.ToString() ?? "") == cfg.AppLanguage)
            {
                LanguageComboBox.SelectedIndex = i;
                break;
            }
        }
        ThemeComboBox.SelectedIndex = cfg.Theme == "Light" ? 1 : 0;
        ShowProgressAnimationsCheckBox.IsChecked = cfg.ShowProgressAnimations;
        AutoSaveCheckBox.IsChecked = cfg.AutoSave;

        // Advanced
        QualityComboBox.SelectedIndex = cfg.Quality == "Fast" ? 0 : cfg.Quality == "High" ? 2 : 1;
        EnableLoggingCheckBox.IsChecked = cfg.EnableLogging;
        ShowDebugInfoCheckBox.IsChecked = cfg.ShowDebugInfo;
        MemoryLimitSlider.Value = Math.Clamp(cfg.TranslationMemoryLimit, 20000, 1500000);
        UpdateMemoryLimitText(MemoryLimitSlider.Value);
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

        // Language setting
        cfg.AppLanguage = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? cfg.AppLanguage;

        cfg.Quality = (QualityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? cfg.Quality;
        cfg.EnableLogging = EnableLoggingCheckBox.IsChecked == true;
        cfg.ShowDebugInfo = ShowDebugInfoCheckBox.IsChecked == true;
        cfg.TranslationMemoryLimit = (int)MemoryLimitSlider.Value;

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
        
        // Apply language change immediately
        var config = Services.SettingsService.LoadConfig();
        Services.LocalizationService.Instance.SaveLanguage(config.AppLanguage);
        
        // Reload translation memory config and enforce new limit if needed
        Services.TranslationMemoryService.Instance.ReloadConfig();
        
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
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", key);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.PostAsync(endpoint, content, cts.Token);

            var body = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                // Throw a more informative exception
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
            }

            // Validate the response shape to avoid false positives (e.g., hitting a web page)
            try
            {
                var jsonEl = JsonSerializer.Deserialize<JsonElement>(body);
                if (jsonEl.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
                {
                    MessageBox.Show("Successfully connected to Google AI Studio (Gemini)! The API key and endpoint are valid.",
                                  "API Test Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception($"Unexpected response from endpoint. Body: {Truncate(body, 500)}");
                }
            }
            catch (JsonException)
            {
                throw new Exception($"Endpoint did not return JSON as expected. Body: {Truncate(body, 500)}");
            }
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
    GeminiEndpointTextBox.Text = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
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
        MemoryLimitSlider.Value = 100000;
        UpdateMemoryLimitText(100000);
    }

    public void Dispose()
    {
        try { _httpClient?.Dispose(); } catch { }
    }

    // Helper to keep error messages readable
    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value.Substring(0, max) + "...";
    }

    private void OnMemoryLimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateMemoryLimitText(e.NewValue);
    }

    private void UpdateMemoryLimitText(double value)
    {
        if (MemoryLimitValueText != null)
        {
            MemoryLimitValueText.Text = $"{value:N0}";
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximize(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeButton.Content = "□";
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeButton.Content = "❐";
        }
    }

    private void OnWindowClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
