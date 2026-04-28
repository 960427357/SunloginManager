using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using SunloginManager.Models;
using SunloginManager.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace SunloginManager
{
    public partial class SettingsDialog : Window
    {
        private string _sunloginPath = string.Empty;
        private bool _enableLogging;
        private int _autoLockMinutes;
        private bool _hasMasterPassword;

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
                    // 简单的配置解析，实际项目中可以使用 JSON 库
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

            // 更新 UI
            SunloginPathTextBox.Text = _sunloginPath;
            EnableLoggingCheckBox.IsChecked = _enableLogging;

            // 加载安全设置
            var ds = new DataService();
            _autoLockMinutes = ds.GetAutoLockMinutes();
            _hasMasterPassword = ds.HasMasterPassword();
            ChangePasswordButton.Content = _hasMasterPassword ? "修改密码" : "设置密码";
            RemovePasswordButton.IsEnabled = _hasMasterPassword;
            foreach (System.Windows.Controls.ComboBoxItem item in AutoLockComboBox.Items)
            {
                if (item.Tag.ToString() == _autoLockMinutes.ToString())
                {
                    AutoLockComboBox.SelectedItem = item;
                    break;
                }
            }
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

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "导入连接",
                Filter = "Excel 文件|*.xlsx;*.xls|CSV 文件|*.csv|文本文件|*.txt|JSON 文件|*.json|所有文件|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var connections = ParseImportFile(openFileDialog.FileName);
                    if (connections.Count > 0)
                    {
                        var bulkDialog = new BulkManageDialog(connections);
                        bulkDialog.Owner = this;
                        if (bulkDialog.ShowDialog() == true)
                        {
                            MessageBox.Show($"成功导入 {bulkDialog.ImportedCount} 个连接", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出连接",
                Filter = "CSV 文件|*.csv|JSON 文件|*.json|所有文件|*.*",
                FilterIndex = 1
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var dataService = new DataService();
                    var connections = dataService.GetAllConnections();

                    if (saveFileDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToCSV(connections, saveFileDialog.FileName);
                    }
                    else if (saveFileDialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToJSON(connections, saveFileDialog.FileName);
                    }

                    MessageBox.Show($"成功导出 {connections.Count} 个连接", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BulkManageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataService = new DataService();
                var connections = dataService.GetAllConnections();

                var bulkDialog = new BulkManageDialog(connections);
                bulkDialog.Owner = this;
                bulkDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开批量管理失败：{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShortcutSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ShortcutSettingsDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var mode = _hasMasterPassword ? PasswordDialogMode.Change : PasswordDialogMode.SetPassword;
            var dialog = new PasswordDialog(mode);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show(_hasMasterPassword ? "密码修改成功" : "密码设置成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _hasMasterPassword = true;
                ChangePasswordButton.Content = "修改密码";
                RemovePasswordButton.IsEnabled = true;
            }
        }

        private void RemovePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要移除主密码吗？\n移除后启动应用将不再需要密码验证。", "移除主密码",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                var ds = new DataService();
                ds.RemoveMasterPassword();
                MessageBox.Show("主密码已移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                _hasMasterPassword = false;
                ChangePasswordButton.Content = "设置密码";
                RemovePasswordButton.IsEnabled = false;
            }
        }

        private List<RemoteConnection> ParseImportFile(string filePath)
        {
            var connections = new List<RemoteConnection>();

            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return ParseJSONFile(filePath);
            }

            if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return ParseExcelFile(filePath);
            }

            // CSV/TXT 格式 - 尝试多种编码
            string content = ReadFileWithAutoDetect(filePath);
            var lines = content.Split('\n');

            // 跳过标题行
            int startIndex = lines.Length > 0 && lines[0].Contains("名称") ? 1 : 0;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 支持 CSV 格式：名称，识别码，连接码，分组
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    var conn = new RemoteConnection
                    {
                        Name = parts[0].Trim(),
                        IdentificationCode = parts[1].Trim(),
                        ConnectionCode = parts[2].Trim()
                    };

                    if (parts.Length >= 4)
                    {
                        conn.Remarks = parts[3].Trim();
                    }

                    connections.Add(conn);
                }
            }

            return connections;
        }

        private List<RemoteConnection> ParseExcelFile(string filePath)
        {
            var connections = new List<RemoteConnection>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IWorkbook workbook;
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else
                {
                    workbook = new HSSFWorkbook(stream);
                }

                var sheet = workbook.GetSheetAt(0);
                int startIndex = 0;

                // 在前5行中查找包含"名称"的标题行
                for (int r = 0; r < Math.Min(5, sheet.LastRowNum + 1); r++)
                {
                    var row = sheet.GetRow(r);
                    if (row != null)
                    {
                        var firstCell = row.GetCell(0);
                        if (firstCell != null && GetCellStringValue(firstCell).Contains("名称"))
                        {
                            startIndex = r + 1;
                            break;
                        }
                    }
                }

                for (int i = startIndex; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null || row.RowNum < startIndex) continue;

                    // 获取单元格值
                    string name = GetCellStringValue(row.GetCell(0));
                    string identificationCode = GetCellStringValue(row.GetCell(1));
                    string connectionCode = GetCellStringValue(row.GetCell(2));
                    string remarks = GetCellStringValue(row.GetCell(3));

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(identificationCode) && !string.IsNullOrEmpty(connectionCode))
                    {
                        var conn = new RemoteConnection
                        {
                            Name = name.Trim(),
                            IdentificationCode = identificationCode.Trim(),
                            ConnectionCode = connectionCode.Trim(),
                            GroupId = 1,
                            Remarks = string.IsNullOrEmpty(remarks) ? null : remarks.Trim()
                        };

                        connections.Add(conn);
                    }
                }
            }

            return connections;
        }

        private string GetCellStringValue(ICell cell)
        {
            if (cell == null) return string.Empty;

            try
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell) && cell.DateCellValue != null)
                        {
                            return cell.DateCellValue.Value.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            return cell.NumericCellValue.ToString();
                        }
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    case CellType.Formula:
                        try
                        {
                            return cell.StringCellValue;
                        }
                        catch
                        {
                            return cell.NumericCellValue.ToString();
                        }
                    default:
                        return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private List<RemoteConnection> ParseJSONFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var connections = System.Text.Json.JsonSerializer.Deserialize<List<RemoteConnection>>(json, options);
                return connections ?? new List<RemoteConnection>();
            }
            catch
            {
                return new List<RemoteConnection>();
            }
        }

        private string ReadFileWithAutoDetect(string filePath)
        {
            // 尝试多种编码：UTF-8, GBK, GB2312, 系统默认编码
            Encoding[] encodings = {
                Encoding.UTF8,
                Encoding.GetEncoding("GBK"),
                Encoding.GetEncoding("GB2312"),
                Encoding.Default
            };

            string content = null;
            foreach (var encoding in encodings)
            {
                try
                {
                    content = File.ReadAllText(filePath, encoding);
                    LogService.LogInfo($"文件编码检测成功：{encoding.EncodingName}");
                    break;
                }
                catch
                {
                    continue;
                }
            }

            if (content == null)
            {
                throw new Exception("无法读取文件，请确保文件为 UTF-8 或 GBK 编码");
            }

            return content;
        }

        private void ExportToCSV(List<RemoteConnection> connections, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("名称，识别码，连接码，备注");

            foreach (var conn in connections)
            {
                sb.AppendLine($"{conn.Name},{conn.IdentificationCode},{conn.ConnectionCode},{conn.Remarks}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private void ExportToJSON(List<RemoteConnection> connections, string filePath)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            string json = System.Text.Json.JsonSerializer.Serialize(connections, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
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

            // 保存自动锁定设置
            var ds = new DataService();
            if (AutoLockComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Tag.ToString(), out int minutes))
            {
                ds.SetAutoLockMinutes(minutes);
            }

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
