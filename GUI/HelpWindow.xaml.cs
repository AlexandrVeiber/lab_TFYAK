using System.Windows;

namespace GUI
{
    public partial class HelpWindow : Window
    {
        public HelpWindow(string title, string text)
        {
            InitializeComponent();
            Title = title;
            HelpTextBlock.Text = text;
        }
    }
}