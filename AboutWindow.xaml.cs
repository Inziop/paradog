using System.Diagnostics;
using System.Windows;

namespace ParadoxTranslator;

/// <summary>
/// About window showing application information
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        
        // Subscribe to localization changes
        Services.LocalizationService.Instance.PropertyChanged += (s, e) => UpdateLocalization();
        UpdateLocalization();
    }

    private void UpdateLocalization()
    {
        var loc = Services.LocalizationService.Instance;
        
        // Window title and headers
        TitleText.Text = loc["AboutTitle"];
        AppNameText.Text = "Paradox Translator"; // Keep app name
        VersionText.Text = loc["AboutVersion"];
        SubtitleText.Text = loc["AboutSubtitle"];
        DescriptionText.Text = loc["AboutDescription"];
        
        // Buttons
        GitHubButton.Content = loc["GitHubRepository"];
        CloseButton.Content = loc["Close"];
    }

    private void OnGitHubClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open GitHub repository in default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/yourusername/ParadoxTranslator",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open GitHub repository:\n{ex.Message}", "Error", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
