using System;
using System.Windows;
using SunloginManager.Models;

namespace SunloginManager
{
    public partial class EditConnectionDialog : Window
    {
        private readonly RemoteConnection _originalConnection;
        
        public EditConnectionDialog(RemoteConnection connection)
        {
            InitializeComponent();
            _originalConnection = connection;
            
            // 填充表单数据
            NameTextBox.Text = connection.Name;
            IdentificationCodeTextBox.Text = connection.IdentificationCode;
            ConnectionCodeTextBox.Text = connection.ConnectionCode;
            NotesTextBox.Text = connection.Remarks ?? string.Empty;
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
                
            // 更新原始连接对象
            _originalConnection.Name = NameTextBox.Text.Trim();
            _originalConnection.IdentificationCode = IdentificationCodeTextBox.Text.Trim();
            _originalConnection.ConnectionCode = ConnectionCodeTextBox.Text.Trim();
            _originalConnection.Remarks = NotesTextBox.Text.Trim();
            
            return _originalConnection;
        }
    }
}