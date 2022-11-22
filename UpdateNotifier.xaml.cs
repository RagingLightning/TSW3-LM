#pragma warning disable IDE1006
using System.Diagnostics;
using System.Windows;

namespace TSW3LM
{
    /// <summary>
    /// Interaction logic for UpdateNotifier.xaml
    /// </summary>
    public partial class UpdateNotifier : Window
    {

        private readonly string link;

        public UpdateNotifier(string installed, string update, string link)
        {
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            lblInstalled.Content = installed;
            lblNew.Content = update;
            this.link = link;
        }

        private void close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void download(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", link);
            Close();
        }
    }
}
