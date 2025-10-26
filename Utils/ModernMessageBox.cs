using System.Windows;

namespace ParadoxTranslator.Utils
{
    /// <summary>
    /// Helper class to show modern styled message boxes throughout the application.
    /// Drop-in replacement for System.Windows.MessageBox.
    /// </summary>
    public static class ModernMessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
        {
            return Controls.ModernMessageBox.Show(messageBoxText);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Controls.ModernMessageBox.Show(messageBoxText, caption);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return Controls.ModernMessageBox.Show(messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return Controls.ModernMessageBox.Show(messageBoxText, caption, button, icon);
        }
    }
}
