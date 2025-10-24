using System.Windows;

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
