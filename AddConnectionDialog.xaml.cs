using System;
using System.Windows;
using SunloginManager.Models;

namespace SunloginManager
{
    public partial class AddConnectionDialog : Window
    {
        public AddConnectionDialog()
        {
            InitializeComponent();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                System.Windows.MessageBox.Show("请输入连接名称", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(IdentificationCodeTextBox.Text))
            {
                System.Windows.MessageBox.Show("请输入识别码", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                IdentificationCodeTextBox.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ConnectionCodeTextBox.Text))
            {
                System.Windows.MessageBox.Show("请输入连接码", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                ConnectionCodeTextBox.Focus();
                return;
            }
            
            DialogResult = true;
            Close();
        }
        
        public RemoteConnection GetConnection()
        {
            if (DialogResult != true)
                return null;
                
            return new RemoteConnection
            {
                Name = NameTextBox.Text.Trim(),
                IdentificationCode = IdentificationCodeTextBox.Text.Trim(),
                ConnectionCode = ConnectionCodeTextBox.Text.Trim(),
                Remarks = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
                LastConnectedAt = DateTime.MinValue,
                CreatedAt = DateTime.Now
            };
        }
    }
}