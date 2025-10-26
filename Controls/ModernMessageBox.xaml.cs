using System.Windows;
using System.Windows.Media;

namespace ParadoxTranslator.Controls
{
    public partial class ModernMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private ModernMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            InitializeComponent();
            
            TitleText.Text = title;
            MessageText.Text = message;
            
            // Set icon and color based on type
            switch (icon)
            {
                case MessageBoxImage.Information:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // Blue
                    IconText.Text = "ⓘ";
                    break;
                case MessageBoxImage.Warning:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
                    IconText.Text = "⚠";
                    break;
                case MessageBoxImage.Error:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                    IconText.Text = "✖";
                    break;
                case MessageBoxImage.Question:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // Blue
                    IconText.Text = "?";
                    break;
                default:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                    IconText.Text = "✓";
                    break;
            }
            
            // Configure buttons based on MessageBoxButton type
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    OkButton.Visibility = Visibility.Visible;
                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    CancelButton.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.OKCancel:
                    OkButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNo:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    OkButton.Visibility = Visibility.Collapsed;
                    CancelButton.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNoCancel:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    OkButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = false;
        }

        // Static Show methods matching MessageBox.Show signatures
        public static MessageBoxResult Show(string messageBoxText)
        {
            return Show(messageBoxText, "Message", MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var dialog = new ModernMessageBox(messageBoxText, caption, button, icon);
            
            // Try to set owner to active window
            try
            {
                dialog.Owner = Application.Current.MainWindow?.IsActive == true 
                    ? Application.Current.MainWindow 
                    : Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            }
            catch
            {
                // If setting owner fails, continue without owner
            }
            
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
