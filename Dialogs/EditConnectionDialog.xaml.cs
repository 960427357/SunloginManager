using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SunloginManager.Models;
using SunloginManager.Services;

namespace SunloginManager
{
    public partial class EditConnectionDialog : Window
    {
        private readonly RemoteConnection _originalConnection;
        private readonly DataService _dataService;
        
        public EditConnectionDialog(RemoteConnection connection)
        {
            InitializeComponent();
            _originalConnection = connection;
            _dataService = new DataService();
            
            LoadGroups();
            
            // 填充表单数据
            NameTextBox.Text = connection.Name;
            IdentificationCodeTextBox.Text = connection.IdentificationCode;
            ConnectionCodePasswordBox.Password = connection.ConnectionCode;
            ConnectionCodeTextBox.Text = connection.ConnectionCode;
            NotesTextBox.Text = connection.Remarks ?? string.Empty;
        }

        private void LoadGroups()
        {
            var groups = _dataService.GetAllGroups();
            GroupComboBox.ItemsSource = groups;
            
            // 选择当前分组
            var currentGroup = groups.FirstOrDefault(g => g.Id == _originalConnection.GroupId);
            if (currentGroup != null)
            {
                GroupComboBox.SelectedItem = currentGroup;
            }
            else if (groups.Count > 0)
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

            // 立即保存到数据库
            try
            {
                _originalConnection.Name = NameTextBox.Text.Trim();
                _originalConnection.IdentificationCode = IdentificationCodeTextBox.Text.Trim();
                _originalConnection.ConnectionCode = ConnectionCodePasswordBox.Password.Trim();

                int groupId = 1;
                if (GroupComboBox.SelectedValue != null)
                {
                    groupId = (int)GroupComboBox.SelectedValue;
                }
                _originalConnection.GroupId = groupId;

                _originalConnection.Remarks = NotesTextBox.Text.Trim();

                // 保存到数据库
                _dataService.SaveConnection(_originalConnection);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                
            // 更新原始连接对象
            _originalConnection.Name = NameTextBox.Text.Trim();
            _originalConnection.IdentificationCode = IdentificationCodeTextBox.Text.Trim();
            _originalConnection.ConnectionCode = ConnectionCodePasswordBox.Password.Trim();
            _originalConnection.GroupId = groupId;
            _originalConnection.Remarks = NotesTextBox.Text.Trim();
            
            return _originalConnection;
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