using System.Windows;

namespace ParadoxTranslator
{
    public partial class ConfirmDialog : Window
    {
        public bool Result { get; private set; }

        public ConfirmDialog(string title, string message)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }
    }
}
