using System;
using System.IO;
using System.Windows;

namespace GUI
{
    public partial class AboutWindow : Window
    {
        public AboutWindow(string aboutText)
        {
            InitializeComponent();
            AboutTextBlock.Text = aboutText;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}