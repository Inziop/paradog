using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ParadoxTranslator.Controls
{
    public partial class ToastNotification : UserControl
    {
        private readonly DispatcherTimer _autoCloseTimer;
        private Storyboard? _showStoryboard;
        private Storyboard? _hideStoryboard;

        public ToastNotification()
        {
            InitializeComponent();
            
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _autoCloseTimer.Tick += (s, e) => Hide();
            
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _showStoryboard = (Storyboard)Resources["ShowToast"];
            _hideStoryboard = (Storyboard)Resources["HideToast"];
        }

        public void Show(string title, string message, ToastType type = ToastType.Success)
        {
            TitleText.Text = title;
            MessageText.Text = message;
            
            // Set colors and icon based on type
            var border = (Border)Content;
            switch (type)
            {
                case ToastType.Success:
                    border.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 126, 52));
                    IconText.Text = "✓";
                    break;
                case ToastType.Info:
                    border.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 92, 153));
                    IconText.Text = "ℹ";
                    break;
                case ToastType.Warning:
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 154, 5));
                    IconText.Text = "⚠";
                    TitleText.Foreground = new SolidColorBrush(Colors.Black);
                    MessageText.Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                    CloseButton.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ToastType.Error:
                    border.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(176, 42, 55));
                    IconText.Text = "✖";
                    break;
            }

            Visibility = Visibility.Visible;
            _showStoryboard?.Begin(this);
            _autoCloseTimer.Start();
        }

        public void Hide()
        {
            _autoCloseTimer.Stop();
            _hideStoryboard?.Begin(this);
            
            // Remove after animation completes
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.3) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }

    public enum ToastType
    {
        Success,
        Info,
        Warning,
        Error
    }
}
