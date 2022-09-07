using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace TSW3LM
{
    /// <summary>
    /// Interaktionslogik für LiveryInfoCollector.xaml
    /// </summary>
    public partial class LiveryInfoCollector : Window
    {
        internal string Name { get { return txtName.Text; } }
        internal string Model { get { return txtModel.Text; } }

        public LiveryInfoCollector()
        {
            InitializeComponent();
        }

        private void Window_SetOnTop(object sender, RoutedEventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
