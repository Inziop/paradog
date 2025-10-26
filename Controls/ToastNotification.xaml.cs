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
        private DoubleAnimation? _progressAnimation;

        public ToastNotification()
        {
            InitializeComponent();
            
            // Load storyboards immediately instead of waiting for Loaded event
            _showStoryboard = (Storyboard)Resources["ShowToast"];
            _hideStoryboard = (Storyboard)Resources["HideToast"];
            
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // Quick 3-second display
            };
            _autoCloseTimer.Tick += (s, e) => Hide();
        }

        public void Show(string title, string message, ToastType type = ToastType.Success)
        {
            TitleText.Text = title;
            MessageText.Text = message;
            
            // Set colors and icon based on type with modern dark theme
            switch (type)
            {
                case ToastType.Success:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                    IconText.Text = "✓";
                    break;
                case ToastType.Info:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // Blue
                    IconText.Text = "ⓘ";
                    break;
                case ToastType.Warning:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
                    IconText.Text = "⚠";
                    break;
                case ToastType.Error:
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                    IconText.Text = "✖";
                    break;
            }

            // SIMPLIFIED: Just make it visible immediately without animation
            Visibility = Visibility.Visible;
            Opacity = 1;
            
            _autoCloseTimer.Start();
            
            // Animate progress bar from full width to 0 (countdown effect)
            var progressColor = type switch
            {
                ToastType.Success => Color.FromRgb(40, 167, 69),
                ToastType.Info => Color.FromRgb(0, 122, 204),
                ToastType.Warning => Color.FromRgb(255, 193, 7),
                ToastType.Error => Color.FromRgb(220, 53, 69),
                _ => Color.FromRgb(40, 167, 69)
            };
            
            ProgressBarFill.Background = new SolidColorBrush(progressColor);
            
            // Animate width from 380 to 0
            _progressAnimation = new DoubleAnimation
            {
                From = 380,
                To = 0,
                Duration = TimeSpan.FromSeconds(3)
            };
            
            ProgressBarFill.BeginAnimation(Border.WidthProperty, _progressAnimation);
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
