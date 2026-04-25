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
    public partial class BulkManageDialog : Window
    {
        private List<RemoteConnection> _connections;
        private List<RemoteConnection> _allConnections;
        private DataService _dataService;
        private ConnectionGroup _defaultGroup;
        private List<ConnectionGroup> _groups;

        public int ImportedCount { get; private set; } = 0;

        public BulkManageDialog(List<RemoteConnection> connections)
        {
            try
            {
                InitializeComponent();
                _dataService = new DataService();
                _groups = _dataService.GetAllGroups();
                _defaultGroup = _groups.FirstOrDefault(g => g.IsDefault) ?? new ConnectionGroup { Id = 1, Name = "默认分组" };

                // 从数据库加载所有连接
                _allConnections = _dataService.GetAllConnections();

                // 如果传入了已解析的连接数据（从设置对话框导入），将其作为待导入的新连接
                if (connections != null && connections.Count > 0)
                {
                    _connections = new List<RemoteConnection>(connections);
                }
                else
                {
                    _connections = new List<RemoteConnection>(_allConnections);
                }

                // 初始化分组过滤器
                InitializeGroupFilter();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"对话框初始化失败：{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeGroupFilter()
        {
            // 添加"所有分组"选项
            var allGroups = new List<ConnectionGroup>
            {
                new ConnectionGroup { Id = 0, Name = "所有分组" }
            };
            allGroups.AddRange(_groups);

            GroupFilterComboBox.ItemsSource = allGroups;
            GroupFilterComboBox.SelectedIndex = 0;
        }

        private void RefreshDataGrid()
        {
            DataGrid.SelectedItem = null;
            DataGrid.ItemsSource = _connections;
            StatusText.Text = $"共 {_connections.Count} 个连接";
        }

        private void DataGrid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateDataGridWithGroupNames();
            RefreshDataGrid();

            // 没有数据时，显示提示
            if (_connections.Count == 0)
            {
                StatusText.Text = "暂无数据，请先导入连接";
            }
        }

        private void UpdateDataGridWithGroupNames()
        {
            // 为每个连接添加分组名称属性
            foreach (var conn in _connections)
            {
                var group = _groups.FirstOrDefault(g => g.Id == conn.GroupId);
                conn.GroupName = group?.Name ?? "未分组";
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择导入文件",
                Filter = "Excel 文件|*.xlsx;*.xls|CSV 文件|*.csv|文本文件|*.txt|JSON 文件|*.json|所有文件|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = FilePathTextBox.Text.Trim();
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("请选择文件或输入内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                List<RemoteConnection> newConnections;

                if (File.Exists(filePath))
                {
                    newConnections = ParseImportFile(filePath);
                }
                else
                {
                    // 尝试解析为 CSV 内容
                    newConnections = ParseCSVContent(filePath);
                }

                if (newConnections.Count > 0)
                {
                    // 按识别码去重：识别码已存在的连接跳过
                    var existingCodes = _allConnections.Select(c => c.IdentificationCode).ToHashSet();
                    var uniqueConnections = new List<RemoteConnection>();
                    int duplicateCount = 0;

                    foreach (var conn in newConnections)
                    {
                        if (existingCodes.Contains(conn.IdentificationCode))
                        {
                            duplicateCount++;
                        }
                        else
                        {
                            existingCodes.Add(conn.IdentificationCode);
                            uniqueConnections.Add(conn);
                        }
                    }

                    if (uniqueConnections.Count > 0)
                    {
                        _connections.AddRange(uniqueConnections);
                        _allConnections.AddRange(uniqueConnections);
                        UpdateDataGridWithGroupNames();
                        RefreshDataGrid();
                    }

                    if (duplicateCount > 0)
                    {
                        MessageBox.Show($"成功导入 {uniqueConnections.Count} 个连接\n跳过 {duplicateCount} 个重复识别码的连接", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"成功导入 {uniqueConnections.Count} 个连接", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("未找到有效的连接数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GroupFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (GroupFilterComboBox.SelectedValue is int selectedGroupId)
            {
                if (selectedGroupId == 0)
                {
                    _connections = new List<RemoteConnection>(_allConnections);
                }
                else
                {
                    _connections = _allConnections.Where(c => c.GroupId == selectedGroupId).ToList();
                }
                UpdateDataGridWithGroupNames();
                RefreshDataGrid();
            }
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            GroupFilterComboBox.SelectedIndex = 0;
            ApplyFilter();
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
            int startIndex = lines.Length > 0 && (lines[0].Contains("名称") || lines[0].Contains("name")) ? 1 : 0;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 支持 CSV 格式：名称，识别码，连接码，备注
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    var conn = new RemoteConnection
                    {
                        Name = parts[0].Trim(),
                        IdentificationCode = parts[1].Trim(),
                        ConnectionCode = parts[2].Trim(),
                        GroupId = _defaultGroup.Id
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
                            GroupId = _defaultGroup.Id,
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
                if (connections != null)
                {
                    // 设置默认分组
                    foreach (var conn in connections)
                    {
                        conn.GroupId = _defaultGroup.Id;
                    }
                }

                return connections ?? new List<RemoteConnection>();
            }
            catch
            {
                // 如果 JSON 解析失败，尝试按 CSV 解析
                return ParseCSVContent(File.ReadAllText(filePath, Encoding.UTF8));
            }
        }

        private List<RemoteConnection> ParseCSVContent(string content)
        {
            var connections = new List<RemoteConnection>();
            var lines = content.Split('\n');

            // 跳过标题行
            int startIndex = lines.Length > 0 && (lines[0].Contains("名称") || lines[0].Contains("name")) ? 1 : 0;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    var conn = new RemoteConnection
                    {
                        Name = parts[0].Trim(),
                        IdentificationCode = parts[1].Trim(),
                        ConnectionCode = parts[2].Trim(),
                        GroupId = _defaultGroup.Id
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

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要清空所有数据吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _connections.Clear();
                _allConnections.Clear();
                RefreshDataGrid();
                EditSelectedButton.IsEnabled = false;
            }
        }

        private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            EditSelectedButton.IsEnabled = DataGrid.SelectedItem != null;
        }

        private void EditSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid.SelectedItem is RemoteConnection selectedConnection)
            {
                var dialog = new EditConnectionDialog(selectedConnection);
                if (dialog.ShowDialog() == true)
                {
                    // 刷新 DataGrid 显示
                    DataGrid.Items.Refresh();
                    StatusText.Text = $"共 {_connections.Count} 个连接";
                }
            }
            else
            {
                MessageBox.Show("请先选择要编辑的连接", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_connections.Count == 0)
                {
                    MessageBox.Show("没有要导入的连接", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 保存到数据库
                foreach (var conn in _connections)
                {
                    _dataService.SaveConnection(conn);
                }

                ImportedCount = _connections.Count;

                // 刷新主窗口连接列表
                RefreshMainWindow();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshMainWindow()
        {
            try
            {
                // 查找主窗口并刷新连接列表
                foreach (var window in System.Windows.Application.Current.Windows)
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.LoadConnections();
                        break;
                    }
                }
            }
            catch
            {
                // 忽略刷新失败
            }
        }
    }
}
