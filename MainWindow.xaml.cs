using System.Windows;
using System.Windows.Controls;
using ParadoxTranslator.ViewModels;

namespace ParadoxTranslator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
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
}
