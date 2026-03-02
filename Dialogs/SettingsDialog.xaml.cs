using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;

namespace SunloginManager
{
    public partial class SettingsDialog : Window
    {
        private string _sunloginPath;
        private bool _enableLogging;
        
        public string SunloginPath => _sunloginPath;
        public bool EnableLogging => _enableLogging;
        
        public SettingsDialog()
        {
            InitializeComponent();
            
            // 加载当前设置
            LoadCurrentSettings();
        }
        
        private void LoadCurrentSettings()
        {
            // 从配置文件加载设置
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string configPath = Path.Combine(appDataPath, "SunloginManager", "config.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    string configContent = File.ReadAllText(configPath);
                    // 简单的配置解析，实际项目中可以使用JSON库
                    var lines = configContent.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("SunloginPath="))
                            _sunloginPath = line.Substring("SunloginPath=".Length).Trim();
                        else if (line.StartsWith("EnableLogging="))
                            _enableLogging = bool.Parse(line.Substring("EnableLogging=".Length).Trim());
                    }
                }
                catch
                {
                    // 如果解析失败，使用默认值
                    _sunloginPath = "";
                    _enableLogging = true;
                }
            }
            else
            {
                // 使用默认值
                _sunloginPath = "";
                _enableLogging = true;
            }
            
            // 更新UI
            SunloginPathTextBox.Text = _sunloginPath;
            EnableLoggingCheckBox.IsChecked = _enableLogging;
        }
        
        private void SaveSettings()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string configDir = Path.Combine(appDataPath, "SunloginManager");
            string configPath = Path.Combine(configDir, "config.json");
            
            // 确保目录存在
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            
            // 保存设置
            string configContent = $"SunloginPath={_sunloginPath}\nEnableLogging={_enableLogging}\n";
            File.WriteAllText(configPath, configContent);
        }
        
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择向日葵主程序",
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                FilterIndex = 1
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                SunloginPathTextBox.Text = openFileDialog.FileName;
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            _sunloginPath = SunloginPathTextBox.Text.Trim();
            _enableLogging = EnableLoggingCheckBox.IsChecked ?? true;
            
            // 验证向日葵路径
            if (!string.IsNullOrEmpty(_sunloginPath) && !File.Exists(_sunloginPath))
            {
                MessageBox.Show("指定的向日葵程序文件不存在，请检查路径是否正确。", "路径错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 保存设置
            SaveSettings();
            
            DialogResult = true;
            Close();
        }
    }
}