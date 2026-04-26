using System;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using SunloginManager.Services;

namespace SunloginManager
{
    public enum PasswordDialogMode
    {
        Login,
        Change
    }

    public partial class PasswordDialog : Window
    {
        private readonly PasswordDialogMode _mode;
        private int _loginAttempts;
        private const int MaxLoginAttempts = 3;

        // Password visibility toggles
        private bool _currentPasswordVisible;
        private bool _newPasswordVisible;
        private bool _confirmPasswordVisible;

        public PasswordDialog(PasswordDialogMode mode)
        {
            _mode = mode;
            InitializeComponent();
            ConfigureForMode();
        }

        private void ConfigureForMode()
        {
            switch (_mode)
            {
                case PasswordDialogMode.Login:
                    Title = "输入主密码";
                    TitleText.Text = "输入主密码";
                    SubtitleText.Text = "请输入主密码解锁应用";
                    NewPasswordRow.Visibility = Visibility.Collapsed;
                    ConfirmNewPasswordRow.Visibility = Visibility.Collapsed;
                    break;

                case PasswordDialogMode.Change:
                    Title = "修改主密码";
                    TitleText.Text = "修改主密码";
                    SubtitleText.Text = "请输入当前密码和新密码";
                    CurrentPasswordLabel.Text = "当前密码";
                    NewPasswordLabel.Text = "新密码";
                    NewPasswordRow.Visibility = Visibility.Visible;
                    ConfirmNewPasswordRow.Visibility = Visibility.Visible;
                    Height = 420;
                    break;
            }
        }

        // Eye button click handlers for password show/hide
        private void CurrentEyeButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPasswordVisible = !_currentPasswordVisible;
            TogglePasswordVisibility(CurrentPasswordBox, CurrentPasswordTextBox, CurrentEyeButton);
        }

        private void NewEyeButton_Click(object sender, RoutedEventArgs e)
        {
            _newPasswordVisible = !_newPasswordVisible;
            TogglePasswordVisibility(NewPasswordBox, NewPasswordTextBox, NewEyeButton);
        }

        private void ConfirmEyeButton_Click(object sender, RoutedEventArgs e)
        {
            _confirmPasswordVisible = !_confirmPasswordVisible;
            TogglePasswordVisibility(ConfirmNewPasswordBox, ConfirmNewPasswordTextBox, ConfirmEyeButton);
        }

        private void TogglePasswordVisibility(System.Windows.Controls.PasswordBox passwordBox, System.Windows.Controls.TextBox textBox, System.Windows.Controls.Button eyeButton)
        {
            if (passwordBox.Visibility == Visibility.Visible)
            {
                textBox.Text = passwordBox.Password;
                passwordBox.Visibility = Visibility.Collapsed;
                textBox.Visibility = Visibility.Visible;
                eyeButton.Content = "\U0001F441"; // eye closed
            }
            else
            {
                passwordBox.Password = textBox.Text;
                passwordBox.Visibility = Visibility.Visible;
                textBox.Visibility = Visibility.Collapsed;
                eyeButton.Content = "\U0001F441"; // eye open
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_mode)
            {
                case PasswordDialogMode.Login:
                    HandleLogin();
                    break;

                case PasswordDialogMode.Change:
                    HandleChange();
                    break;
            }
        }

        private void HandleLogin()
        {
            string password = GetCurrentPassword();
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ds = new DataService();
            if (ds.VerifyMasterPassword(password))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                _loginAttempts++;
                int remaining = MaxLoginAttempts - _loginAttempts;
                if (remaining <= 0)
                {
                    MessageBox.Show("密码错误次数过多，程序将退出", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                }
                else
                {
                    MessageBox.Show($"密码错误，剩余 {remaining} 次机会", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurrentPasswordBox.Password = string.Empty;
                    CurrentPasswordBox.Focus();
                }
            }
        }

        private void HandleChange()
        {
            string currentPassword = GetCurrentPassword();
            string newPassword = GetNewPassword();
            string confirmPassword = GetConfirmNewPassword();

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("所有字段必须填写", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("两次输入的新密码不一致", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ds = new DataService();
            if (ds.ChangeMasterPassword(currentPassword, newPassword))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("当前密码错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                CurrentPasswordBox.Password = string.Empty;
                CurrentPasswordBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public string GetCurrentPassword()
        {
            return CurrentPasswordBox.Visibility == Visibility.Visible
                ? CurrentPasswordBox.Password
                : CurrentPasswordTextBox.Text;
        }

        public string GetNewPassword()
        {
            return NewPasswordBox.Visibility == Visibility.Visible
                ? NewPasswordBox.Password
                : NewPasswordTextBox.Text;
        }

        public string GetConfirmNewPassword()
        {
            return ConfirmNewPasswordBox.Visibility == Visibility.Visible
                ? ConfirmNewPasswordBox.Password
                : ConfirmNewPasswordTextBox.Text;
        }
    }
}
