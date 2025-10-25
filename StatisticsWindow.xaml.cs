using System.Windows;
using System.Windows.Threading;
using System;

namespace ParadoxTranslator;

/// <summary>
/// Statistics window showing translation progress and metrics
/// </summary>
public partial class StatisticsWindow : Window
{
    private DispatcherTimer _timer;

    public StatisticsWindow()
    {
        InitializeComponent();
        InitializeTimer();
    }

    private void InitializeTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        // Update elapsed time and other real-time statistics
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        // This would typically update from the main view model
        // For now, we'll just show placeholder data
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        base.OnClosed(e);
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximize(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeButton.Content = "□";
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeButton.Content = "❐";
        }
    }

    private void OnWindowClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
