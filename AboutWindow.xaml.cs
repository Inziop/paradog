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
