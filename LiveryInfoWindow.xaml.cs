using System.Threading;
using System.Windows;

namespace TSW3LM
{
    /// <summary>
    /// Interaktionslogik für LiveryInfoCollector.xaml
    /// </summary>
    public partial class LiveryInfoWindow : Window
    {
#pragma warning disable CS8618
        internal static LiveryInfoWindow INSTANCE;
#pragma warning restore CS8618

        internal ThreadStart? Callback = null;

        internal string LiveryName {
            get { return txtName.Text; }
            set { txtName.Text = value; txtName.InvalidateVisual(); }
        }
        internal string LiveryModel {
            get { return txtModel.Text; }
            set { txtModel.Text = value; txtName.InvalidateVisual(); }
        }
        internal string LiveryId {
            get { return txtId.Text; }
            set { txtId.Text = value; txtName.InvalidateVisual(); }
        }

        public LiveryInfoWindow()
        {
            InitializeComponent();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;

            if (Callback != null)
            {
                Callback.Invoke();
                Callback = null;
            }
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;

            if (LiveryId != "<empty>")
            {
                GameLiveryInfo.SetInfo(LiveryId, LiveryName, LiveryModel, true);
                MainWindow.INSTANCE.UpdateGameGui();
            }

            if (Callback != null)
            {
                Callback.Invoke();
                Callback = null;
            }
        }
    }
}
