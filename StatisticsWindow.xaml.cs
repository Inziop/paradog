using System.Windows;
using System.Windows.Data;
using ParadoxTranslator.Utils;
using System.Windows.Threading;
using System;
using System.Linq;
using ParadoxTranslator.ViewModels;
using System.Collections.ObjectModel;

namespace ParadoxTranslator;

public class FileStatistic
{
    public string FileName { get; set; } = "";
    public int TotalEntries { get; set; }
    public int TranslatedEntries { get; set; }
    public double ProgressPercentage => TotalEntries > 0 ? (double)TranslatedEntries / TotalEntries * 100 : 0;
}

/// <summary>
/// Statistics window showing translation progress and metrics
/// </summary>
public partial class StatisticsWindow : Window
{
    private readonly DispatcherTimer _timer;
    private MainViewModel? _viewModel;
    private DateTime _startTime;

    public StatisticsWindow(MainViewModel? viewModel = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _startTime = DateTime.Now;
        _timer = new DispatcherTimer();
        
        LoadStatistics();
        InitializeTimer();
        
        // Subscribe to localization changes
        Services.LocalizationService.Instance.PropertyChanged += (s, e) => UpdateLocalization();
        UpdateLocalization();
        ApplyProgressAnimationBindings();
    }

    private void ApplyProgressAnimationBindings()
    {
        try
        {
            var cfg = Services.SettingsService.LoadConfig();
            if (cfg.ShowProgressAnimations)
            {
                var style = TryFindResource("AnimatedProgressBar") as Style;
                if (style != null)
                {
                    OverallProgressBar.Style = style;
                }

                OverallProgressBar.ApplyTemplate();
                var indicator = OverallProgressBar.Template.FindName("IndicatorScale", OverallProgressBar) as System.Windows.Media.ScaleTransform;
                if (indicator != null)
                {
                    var mb = new MultiBinding { Converter = new ProgressFractionConverter() };
                    mb.Bindings.Add(new Binding("Value") { Source = OverallProgressBar });
                    mb.Bindings.Add(new Binding("Maximum") { Source = OverallProgressBar });
                    BindingOperations.SetBinding(indicator, System.Windows.Media.ScaleTransform.ScaleXProperty, mb);
                }
            }
        }
        catch { }
    }

    private void UpdateLocalization()
    {
        var loc = Services.LocalizationService.Instance;
        
        // Window title and labels
        TitleText.Text = loc["Statistics"];
        SubtitleText.Text = loc["StatisticsSubtitle"];
        
        // Group box headers
        OverallProgressGroup.Header = loc["OverallProgress"];
        FileStatisticsGroup.Header = loc["FileStatistics"];
        TranslationQualityGroup.Header = loc["TranslationQualityStats"];
        TimeStatisticsGroup.Header = loc["TimeStatistics"];
        
        // Overall progress labels
        TotalFilesLabel.Text = loc["TotalFiles"];
        TotalEntriesLabel.Text = loc["TotalEntries"];
        TranslatedLabel.Text = loc["TranslatedEntries"];
        
        // Quality labels
        SuccessfulLabel.Text = loc["Successful"];
        FailedLabel.Text = loc["Failed"];
        
        // Time labels
        StartTimeLabel.Text = loc["SessionStart"];
        ElapsedTimeLabel.Text = loc["TimeElapsed"];
        AvgTimeLabel.Text = loc["AvgPerEntry"];
        
        // Buttons
        ExportButton.Content = loc["ExportReport"];
        CloseButton.Content = loc["Close"];
    }

    private void InitializeTimer()
    {
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateElapsedTime();
    }

    private void LoadStatistics()
    {
        if (_viewModel == null)
        {
            return;
        }

        // Overall statistics
        var totalFiles = _viewModel.Files.Count;
        var totalEntries = _viewModel.Files.Sum(f => f.Entries.Count);
        var translatedEntries = _viewModel.Files.Sum(f => f.Entries.Count(e => !string.IsNullOrWhiteSpace(e.Entry.TranslatedText)));

        TotalFilesText.Text = totalFiles.ToString();
        TotalEntriesText.Text = totalEntries.ToString();
        TranslatedText.Text = translatedEntries.ToString();

        var progress = totalEntries > 0 ? (double)translatedEntries / totalEntries * 100 : 0;
        OverallProgressBar.Value = progress;
        OverallProgressText.Text = $"{progress:F1}% Complete";

        // File statistics
        var fileStats = new ObservableCollection<FileStatistic>();
        foreach (var file in _viewModel.Files)
        {
            fileStats.Add(new FileStatistic
            {
                FileName = file.FileName,
                TotalEntries = file.Entries.Count,
                TranslatedEntries = file.Entries.Count(e => !string.IsNullOrWhiteSpace(e.Entry.TranslatedText))
            });
        }
        FileStatisticsGrid.ItemsSource = fileStats;

        // Quality statistics
        var successful = _viewModel.Files.Sum(f => f.Entries.Count(e => 
            !string.IsNullOrWhiteSpace(e.Entry.TranslatedText) && e.Status != "Error"));
        var failed = _viewModel.Files.Sum(f => f.Entries.Count(e => e.Status == "Error"));

        SuccessfulText.Text = successful.ToString();
        FailedText.Text = failed.ToString();

        // Time statistics
        StartTimeText.Text = _startTime.ToString("HH:mm:ss");
        UpdateElapsedTime();
    }

    private void UpdateElapsedTime()
    {
        var elapsed = DateTime.Now - _startTime;
        ElapsedTimeText.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

        if (_viewModel != null)
        {
            var totalEntries = _viewModel.Files.Sum(f => f.Entries.Count);
            var translatedEntries = _viewModel.Files.Sum(f => f.Entries.Count(e => !string.IsNullOrWhiteSpace(e.Entry.TranslatedText)));

            if (translatedEntries > 0)
            {
                var avgSeconds = elapsed.TotalSeconds / translatedEntries;
                AvgTimeText.Text = $"{avgSeconds:F1}s";
            }
        }
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
