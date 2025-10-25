using System.Windows;
using Application = System.Windows.Application;

namespace ParadoxTranslator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize localization service
        Services.LocalizationService.Instance.LoadLanguage();
        
        // Set up global exception handling
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Don't auto-shutdown when dialogs close
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Show project selection window first
        var projectSelection = new ProjectSelectionWindow();
        var result = projectSelection.ShowDialog();
        
        System.Diagnostics.Debug.WriteLine($"ProjectSelectionWindow result: {result}");
        
        if (result == true && projectSelection.SelectedProject != null)
        {
            // Open main window with selected project
            var mainWindow = new MainWindow(projectSelection.SelectedProject);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = mainWindow;
            mainWindow.Show();
            
            // Ensure window is activated and focused
            mainWindow.Activate();
            mainWindow.Focus();
        }
        else
        {
            // User cancelled - exit application
            System.Diagnostics.Debug.WriteLine("User cancelled project selection, shutting down");
            Shutdown();
        }
    }
    
    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}", 
                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
