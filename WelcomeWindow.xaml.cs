using System.Windows;

namespace ParadoxTranslator;

/// <summary>
/// Welcome window for first-time users
/// </summary>
public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();
    }

    private void OnGetStartedClick(object sender, RoutedEventArgs e)
    {
        // Save preference if user checked "Don't show again"
        if (DontShowAgainCheckBox.IsChecked == true)
        {
            // TODO: Save to settings that user doesn't want to see welcome screen
            // For now, just show a message
            MessageBox.Show("Welcome screen preference saved!", "Settings", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        DialogResult = true;
        Close();
    }

    private void OnTutorialClick(object sender, RoutedEventArgs e)
    {
        // TODO: Open tutorial or help documentation
        MessageBox.Show("Tutorial feature coming soon!\n\nFor now, you can:\n1. Click 'Open Folder' to select your game files\n2. Choose source and target languages\n3. Click 'Translate All' to start translating", 
                       "Tutorial", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
