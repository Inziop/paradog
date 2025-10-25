using System.Windows;
using System.Windows.Data;
using ParadoxTranslator.Utils;

namespace ParadoxTranslator;

/// <summary>
/// Progress dialog for translation operations
/// </summary>
public partial class ProgressDialog : Window
{
    public bool IsCancelled { get; private set; }

    public ProgressDialog()
    {
        InitializeComponent();
        IsCancelled = false;
        ApplyProgressAnimationBindings();
    }

    private void ApplyProgressAnimationBindings()
    {
        try
        {
            var cfg = Services.SettingsService.LoadConfig();
            if (cfg.ShowProgressAnimations)
            {
                // show spinner
                HeaderSpinner.Visibility = Visibility.Visible;

                // set Animated style if available
                var style = TryFindResource("AnimatedProgressBar") as Style;
                if (style != null)
                {
                    OverallProgressBar.Style = style;
                }

                // bind ScaleX of indicator to Value/Maximum
                OverallProgressBar.ApplyTemplate();
                var indicatorBorder = OverallProgressBar.Template.FindName("PART_Indicator", OverallProgressBar) as System.Windows.Controls.Border;
                var scale = indicatorBorder?.RenderTransform as System.Windows.Media.ScaleTransform;
                if (scale != null)
                {
                    var mb = new MultiBinding { Converter = new ProgressFractionConverter() };
                    mb.Bindings.Add(new Binding("Value") { Source = OverallProgressBar });
                    mb.Bindings.Add(new Binding("Maximum") { Source = OverallProgressBar });
                    BindingOperations.SetBinding(scale, System.Windows.Media.ScaleTransform.ScaleXProperty, mb);
                }
            }
            else
            {
                HeaderSpinner.Visibility = Visibility.Collapsed;
            }
        }
        catch { /* non-fatal */ }
    }

    public void UpdateProgress(int completed, int total, string currentKey, string currentSource, string currentTarget)
    {
        // Update overall progress
        OverallProgressBar.Maximum = total;
        OverallProgressBar.Value = completed;
        OverallProgressText.Text = $"{completed} / {total} entries";

        // Update current entry
        CurrentKeyText.Text = $"Key: {currentKey}";
        CurrentSourceText.Text = $"Source: {currentSource}";
        CurrentTargetText.Text = $"Target: {currentTarget}";

        // Update statistics
        CompletedText.Text = completed.ToString();
        RemainingText.Text = (total - completed).ToString();
    }

    public void UpdateStatus(string status)
    {
        StatusText.Text = status;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        IsCancelled = true;
        CancelButton.Content = "Cancelling...";
        CancelButton.IsEnabled = false;
    }
}
