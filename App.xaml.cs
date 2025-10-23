using System.Windows;

namespace ParadoxTranslator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Set up global exception handling
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }
    
    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}", 
                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
