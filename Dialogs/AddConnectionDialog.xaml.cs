using System;
using System.Windows;
using System.Windows.Input;
using SunloginManager.Models;
using SunloginManager.Services;

namespace SunloginManager
{
    public partial class AddConnectionDialog : Window
    {
        private readonly DataService _dataService;

        public AddConnectionDialog()
        {
            InitializeComponent();
            _dataService = new DataService();
            LoadGroups();
        }

        private void LoadGroups()
        {
            var groups = _dataService.GetAllGroups();
            GroupComboBox.ItemsSource = groups;
            if (groups.Count > 0)
            {
                GroupComboBox.SelectedIndex = 0;
            }
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
            
            if (string.IsNullOrWhiteSpace(ConnectionCodePasswordBox.Password))
            {
                System.Windows.MessageBox.Show("请输入连接码", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                ConnectionCodePasswordBox.Focus();
                return;
            }
            
            DialogResult = true;
            Close();
        }
        
        public RemoteConnection GetConnection()
        {
            if (DialogResult != true)
                return null;

            int groupId = 1; // 默认分组
            if (GroupComboBox.SelectedValue != null)
            {
                groupId = (int)GroupComboBox.SelectedValue;
            }
                
            return new RemoteConnection
            {
                Name = NameTextBox.Text.Trim(),
                IdentificationCode = IdentificationCodeTextBox.Text.Trim(),
                ConnectionCode = ConnectionCodePasswordBox.Password.Trim(),
                GroupId = groupId,
                Remarks = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
                LastConnectedAt = DateTime.MinValue,
                CreatedAt = DateTime.Now
            };
        }

        // 眼睛按钮按下 - 显示明文
        private void ShowPasswordButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 同步密码到TextBox
            ConnectionCodeTextBox.Text = ConnectionCodePasswordBox.Password;
            
            // 切换显示
            ConnectionCodePasswordBox.Visibility = Visibility.Collapsed;
            ConnectionCodeTextBox.Visibility = Visibility.Visible;
            ConnectionCodeTextBox.Focus();
            ConnectionCodeTextBox.SelectionStart = ConnectionCodeTextBox.Text.Length;
        }

        // 眼睛按钮松开 - 隐藏明文
        private void ShowPasswordButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 同步TextBox到密码
            ConnectionCodePasswordBox.Password = ConnectionCodeTextBox.Text;
            
            // 切换显示
            ConnectionCodeTextBox.Visibility = Visibility.Collapsed;
            ConnectionCodePasswordBox.Visibility = Visibility.Visible;
        }

        // 鼠标离开按钮 - 隐藏明文
        private void ShowPasswordButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // 同步TextBox到密码
            ConnectionCodePasswordBox.Password = ConnectionCodeTextBox.Text;
            
            // 切换显示
            ConnectionCodeTextBox.Visibility = Visibility.Collapsed;
            ConnectionCodePasswordBox.Visibility = Visibility.Visible;
        }
    }
}