using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace SunloginManager
{
    public partial class LogViewerDialog : Window
    {
        public LogViewerDialog()
        {
            InitializeComponent();
        }

        private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取日志目录路径
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDirectory = Path.Combine(appDataPath, "SunloginManager", "Logs");

                // 如果目录不存在，创建它
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                    MessageBox.Show("日志目录已创建，但还没有日志文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 打开日志目录
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDirectory,
                    UseShellExecute = true,
                    Verb = "open"
                });

                // 关闭对话框
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开日志目录: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}