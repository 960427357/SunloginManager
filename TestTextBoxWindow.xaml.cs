using System.Windows;

namespace SunloginManager
{
    public partial class TestTextBoxWindow : Window
    {
        public TestTextBoxWindow()
        {
            InitializeComponent();
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}