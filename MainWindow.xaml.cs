using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using ParadoxTranslator.ViewModels;
using ParadoxTranslator.Services;
using System.Threading.Tasks;

namespace ParadoxTranslator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            Loaded += OnLoadedAsync;
            Closing += OnClosing;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && DataContext is MainViewModel viewModel)
            {
                // TODO: Implement search functionality
                // This would filter the Files collection based on the search text
            }
        }

        private async void OnLoadedAsync(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Restore session
                ParadoxTranslator.Models.SessionState session = SessionService.Load();
                await vm.RestoreSessionAsync(session);

                // Restore window placement if available
                if (session.Width.HasValue && session.Height.HasValue)
                {
                    this.Width = session.Width.Value;
                    this.Height = session.Height.Value;
                }
                if (session.Left.HasValue && session.Top.HasValue)
                {
                    this.Left = session.Left.Value;
                    this.Top = session.Top.Value;
                }
                if (!string.IsNullOrWhiteSpace(session.WindowState))
                {
                    if (System.Enum.TryParse<WindowState>(session.WindowState, out var state))
                    {
                        this.WindowState = state;
                    }
                }
            }
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                ParadoxTranslator.Models.SessionState snapshot = vm.BuildSessionSnapshot();
                // Capture window placement
                snapshot.WindowState = this.WindowState.ToString();
                snapshot.Width = this.Width;
                snapshot.Height = this.Height;
                snapshot.Left = this.Left;
                snapshot.Top = this.Top;
                SessionService.Save(snapshot);
            }
        }
    }
}