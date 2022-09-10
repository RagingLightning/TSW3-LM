using System.Windows;

namespace TSW3LM
{
    /// <summary>
    /// Interaktionslogik für LiveryInfoCollector.xaml
    /// </summary>
    public partial class LiveryInfoCollector : Window
    {
        internal string LiveryName { get { return txtName.Text; } }
        internal string LiveryModel { get { return txtModel.Text; } }

        public LiveryInfoCollector()
        {
            InitializeComponent();
        }

        internal void Prepare(string cause)
        {
            ShowActivated = true;
            lblCause.Content = cause;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_SetOnTopD(object sender, System.EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }

        private void Window_SetOnTopL(object sender, RoutedEventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }
    }
}
