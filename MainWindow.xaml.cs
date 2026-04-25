using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SunloginManager.Models;
using SunloginManager.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using WpfColor = System.Windows.Media.Color;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace SunloginManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DataService _dataService;
        private readonly SunloginService _sunloginService;
        private RemoteConnection? _selectedConnection;
        private TextBlock? _searchPlaceholderControl;
        private DispatcherTimer _statusTimer;
        private const uint WM_SHOWWINDOW_CUSTOM = 0x0400 + 1;
        
        public ObservableCollection<RemoteConnection> Connections { get; set; }
        
        public RemoteConnection? SelectedConnection
        {
            get => _selectedConnection;
            set
            {
                _selectedConnection = value;
                OnPropertyChanged(nameof(SelectedConnection));
                UpdateConnectionDetails();
            }
        }
        
        public TextBlock? SearchPlaceholderControl
        {
            get => _searchPlaceholderControl;
            set
            {
                _searchPlaceholderControl = value;
                OnPropertyChanged(nameof(SearchPlaceholderControl));
            }
        }

      

        public MainWindow()
        {
            InitializeComponent();
            _dataService = new DataService();
            _sunloginService = new SunloginService(_dataService);
            Connections = new ObservableCollection<RemoteConnection>();
            
            DataContext = this;
            
            // 初始化搜索占位符
            SearchPlaceholderControl = FindName("SearchPlaceholder") as TextBlock;
            
            // 初始化状态计时器
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusTimer.Tick += (sender, e) =>
            {
                StatusTextBlock.Text = $"共 {Connections.Count} 个连接";
                _statusTimer.Stop();
            };
            
            LoadGroups();
            LoadConnections();
            
            // 初始化搜索文本框
            SearchTextBox.Text = "";
            if (SearchPlaceholderControl != null)
            {
                SearchPlaceholderControl.Visibility = Visibility.Visible;
            }
            
            // 设置事件处理程序
            ConnectionsListView.SelectionChanged += ConnectionsListView_SelectionChanged;
            SearchTextBox.GotFocus += SearchTextBox_GotFocus;
            SearchTextBox.LostFocus += SearchTextBox_LostFocus;
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            
            // 添加窗口消息处理
            this.SourceInitialized += MainWindow_SourceInitialized;
        }
        
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // 获取窗口句柄并添加消息钩子
            HwndSource? source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
            
            LogService.LogInfo($"主窗口已初始化，句柄: {source?.Handle}");
        }
        
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 处理自定义窗口消息
            if (msg == WM_SHOWWINDOW_CUSTOM)
            {
                LogService.LogInfo("收到显示窗口消息");
                // 使用 Dispatcher 确保在 UI 线程上执行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ShowAndActivateWindow();
                }));
                handled = true;
            }
            return IntPtr.Zero;
        }
        
        private void ShowAndActivateWindow()
        {
            try
            {
                LogService.LogInfo("开始激活窗口");
                
                // 确保窗口显示在任务栏和屏幕上
                this.ShowInTaskbar = true;
                
                // 如果窗口已经隐藏，则显示它
                if (this.Visibility != Visibility.Visible)
                {
                    this.Show();
                    LogService.LogInfo("窗口已显示");
                }
                
                // 确保窗口状态正常并激活
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                    LogService.LogInfo("窗口已从最小化状态恢复");
                }
                
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
                this.Focus();
                
                LogService.LogInfo("窗口已激活");
            }
            catch (Exception ex)
            {
                LogService.LogError($"激活窗口失败: {ex.Message}", ex);
            }
        }
        
        // 刷新按钮点击事件
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshConnectionList();
            UpdateStatusText("连接列表已刷新");
        }
        
        // 设置按钮点击事件
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog();
            if (dialog.ShowDialog() == true)
            {
                // 更新SunloginService的路径
                _dataService.SaveSunloginPath(dialog.SunloginPath);
                LogService.LogInfo("设置已保存");
            }
        }
        
        // 关于按钮点击事件
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new Views.AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // 分组管理按钮点击事件
        private void ManageGroupsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ManageGroupsDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadGroups();
                LoadConnections();
                UpdateStatusText("分组已更新");
            }
        }

        // 分组过滤器选择变化事件
        private void GroupFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            FilterConnections();
        }
        
        private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!System.IO.Directory.Exists(logPath))
                {
                    System.IO.Directory.CreateDirectory(logPath);
                }
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
                
                UpdateStatusText("已打开日志文件夹");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"无法打开日志文件夹: {ex.Message}");
            }
        }
        
        // 添加连接按钮点击事件
        private void AddConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddConnectionDialog();
            if (dialog.ShowDialog() == true)
            {
                var newConnection = dialog.GetConnection();
                if (newConnection != null)
                {
                    _dataService.SaveConnection(newConnection);
                    LoadConnections();
                    LogService.LogInfo($"已添加连接: {newConnection.Name}");
                    UpdateStatusText($"连接 '{newConnection.Name}' 已添加");
                }
            }
        }
        
        // 编辑按钮点击事件
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedConnection = ConnectionsListView.SelectedItem as RemoteConnection;
            if (selectedConnection != null)
            {
                // 打开编辑连接对话框
                var dialog = new EditConnectionDialog(selectedConnection);
                if (dialog.ShowDialog() == true)
                {
                    var updatedConnection = dialog.GetConnection();
                    if (updatedConnection != null)
                    {
                        _dataService.UpdateConnection(updatedConnection);
                        RefreshConnectionList();
                        UpdateStatusText($"连接 '{updatedConnection.Name}' 已更新");
                    }
                }
            }
            else
            {
                UpdateStatusText("请先选择要编辑的连接");
            }
        }
        
        // 删除按钮点击事件
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedConnection = ConnectionsListView.SelectedItem as RemoteConnection;
            if (selectedConnection != null)
            {
                // 记录删除操作到日志
                LogService.LogInfo($"删除连接: {selectedConnection.Name} (ID: {selectedConnection.Id})");
                
                _dataService.DeleteConnection(selectedConnection.Id.ToString());
                RefreshConnectionList();
                LogService.LogInfo($"已删除连接: {selectedConnection.Name}");
                UpdateStatusText($"已删除连接: {selectedConnection.Name}");
            }
            else
            {
                LogService.LogInfo("删除操作失败: 未选择连接");
                UpdateStatusText("请先选择要删除的连接");
            }
        }
        
        // 连接按钮点击事件
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedConnection = ConnectionsListView.SelectedItem as RemoteConnection;
            if (selectedConnection == null)
            {
                UpdateStatusText("请先选择一个连接");
                return;
            }
            
            try
            {
                LogService.LogInfo($"正在连接到: {selectedConnection.Name}");
                UpdateStatusText($"正在连接到: {selectedConnection.Name}");
                
                bool success = await _sunloginService.ConnectToRemoteAsync(selectedConnection);
                
                if (success)
                {
                    // 更新最后连接时间
                    selectedConnection.LastConnectedAt = DateTime.Now;
                    _dataService.UpdateConnection(selectedConnection);
                    LoadConnections();
                    
                    LogService.LogInfo($"已连接到: {selectedConnection.Name}");
                    UpdateStatusText($"成功连接到 {selectedConnection.Name}");
                }
                else
                {
                    LogService.LogInfo("连接失败");
                    UpdateStatusText("连接失败，请检查网络或连接码是否正确");
                }
            }
            catch (Exception ex)
            {
                LogService.LogInfo($"连接出错: {ex.Message}");
                UpdateStatusText($"连接出错: {ex.Message}");
            }
        }
        
        // 从详情面板连接按钮点击事件
        private async void ConnectFromDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection != null)
            {
                try
                {
                    LogService.LogInfo($"开始连接: {SelectedConnection.Name} (ID: {SelectedConnection.Id})");
                    UpdateStatusText($"正在连接 {SelectedConnection.Name}...");
                    
                    // 启动向日葵客户端并连接
                    bool success = await _sunloginService.ConnectToRemoteAsync(SelectedConnection);
                    
                    if (success)
                    {
                        // 更新最后连接时间
                        SelectedConnection.LastConnectedAt = DateTime.Now;
                        _dataService.UpdateConnection(SelectedConnection);
                        
                        UpdateStatusText($"已连接到 {SelectedConnection.Name}");
                        LogService.LogInfo($"成功连接到: {SelectedConnection.Name}");
                    }
                    else
                    {
                        UpdateStatusText($"连接失败");
                        LogService.LogError($"连接失败: {SelectedConnection.Name}");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatusText($"连接失败: {ex.Message}");
                    LogService.LogError($"连接失败: {ex.Message}", ex);
                }
            }
            else
            {
                UpdateStatusText("请先选择一个连接");
            }
        }
        
        // 从详情面板编辑按钮点击事件
        private void EditFromDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection != null)
            {
                // 打开编辑连接对话框
                var dialog = new EditConnectionDialog(SelectedConnection);
                if (dialog.ShowDialog() == true)
                {
                    var updatedConnection = dialog.GetConnection();
                    if (updatedConnection != null)
                    {
                        _dataService.UpdateConnection(updatedConnection);
                        RefreshConnectionList();
                        UpdateStatusText($"连接 '{updatedConnection.Name}' 已更新");
                    }
                }
            }
            else
            {
                UpdateStatusText("请先选择要编辑的连接");
            }
        }

        // 从详情面板删除按钮点击事件
        private void DeleteFromDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection != null)
            {
                // 记录删除操作到日志
                LogService.LogInfo($"删除连接: {SelectedConnection.Name} (ID: {SelectedConnection.Id})");
                
                _dataService.DeleteConnection(SelectedConnection.Id.ToString());
                RefreshConnectionList();
                LogService.LogInfo($"已删除连接: {SelectedConnection.Name}");
                UpdateStatusText($"已删除连接: {SelectedConnection.Name}");
            }
            else
            {
                LogService.LogInfo("删除操作失败: 未选择连接");
                UpdateStatusText("请先选择要删除的连接");
            }
        }
        
        // 表格中的连接按钮点击事件
        private async void ConnectFromTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is RemoteConnection connection)
            {
                try
                {
                    LogService.LogInfo($"开始连接: {connection.Name} (ID: {connection.Id})");
                    UpdateStatusText($"正在连接 {connection.Name}...");
                    
                    // 启动向日葵客户端并连接
                    bool success = await _sunloginService.ConnectToRemoteAsync(connection);
                    
                    if (success)
                    {
                        // 更新最后连接时间
                        connection.LastConnectedAt = DateTime.Now;
                        _dataService.UpdateConnection(connection);
                        LoadConnections();
                        
                        UpdateStatusText($"已连接到 {connection.Name}");
                        LogService.LogInfo($"成功连接到: {connection.Name}");
                    }
                    else
                    {
                        UpdateStatusText($"连接失败");
                        LogService.LogError($"连接失败: {connection.Name}");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatusText($"连接失败: {ex.Message}");
                    LogService.LogError($"连接失败: {ex.Message}", ex);
                }
            }
        }
        
        // 表格中的编辑按钮点击事件
        private void EditFromTableButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取按钮所在的行数据
            var button = sender as System.Windows.Controls.Button;
            if (button?.DataContext is RemoteConnection connection)
            {
                // 选中所编辑的连接
                ConnectionsListView.SelectedItem = connection;
                
                // 打开编辑连接对话框
                var dialog = new EditConnectionDialog(connection);
                if (dialog.ShowDialog() == true)
                {
                    var updatedConnection = dialog.GetConnection();
                    if (updatedConnection != null)
                    {
                        _dataService.UpdateConnection(updatedConnection);
                        RefreshConnectionList();
                        UpdateStatusText($"连接 '{updatedConnection.Name}' 已更新");
                    }
                }
            }
        }
        
        // 表格中的删除按钮点击事件
        private void DeleteFromTableButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取按钮所在的行数据
            var button = sender as System.Windows.Controls.Button;
            if (button?.DataContext is RemoteConnection connection)
            {
                // 记录删除操作到日志
                LogService.LogInfo($"删除连接: {connection.Name} (ID: {connection.Id})");
                
                _dataService.DeleteConnection(connection.Id.ToString());
                RefreshConnectionList();
                LogService.LogInfo($"已删除连接: {connection.Name}");
                UpdateStatusText($"已删除连接: {connection.Name}");
            }
        }

        // 分享按钮点击事件（详情面板）
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedConnection != null)
            {
                ShareConnection(SelectedConnection);
            }
            else
            {
                UpdateStatusText("请先选择一个连接");
            }
        }

        // 表格中的分享按钮点击事件
        private void ShareFromTableButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.DataContext is RemoteConnection connection)
            {
                ShareConnection(connection);
            }
        }

        // 分享连接信息
        private void ShareConnection(RemoteConnection connection)
        {
            try
            {
                // 构建分享文本
                var shareText = new StringBuilder();
                shareText.AppendLine($"【向日葵远程连接】");
                shareText.AppendLine($"名称：{connection.Name}");
                shareText.AppendLine($"识别码：{connection.IdentificationCode}");
                shareText.AppendLine($"连接码：{connection.ConnectionCode}");
                if (!string.IsNullOrEmpty(connection.Remarks))
                {
                    shareText.AppendLine($"备注：{connection.Remarks}");
                }

                // 复制到剪贴板（忽略剪贴板被占用的异常）
                try
                {
                    System.Windows.Clipboard.SetText(shareText.ToString());
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // 剪贴板被其他进程占用，重试一次
                    System.Threading.Thread.Sleep(100);
                    System.Windows.Clipboard.SetText(shareText.ToString());
                }

                UpdateStatusText("连接信息已复制到剪贴板");
                LogService.LogInfo($"已分享连接：{connection.Name}");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"分享失败：{ex.Message}");
                LogService.LogError($"分享连接失败：{ex.Message}", ex);
            }
        }
        
        // 更新状态文本
        private void UpdateStatusText(string message)
        {
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = message;
                _statusTimer.Stop();
                _statusTimer.Start();
            }
        }
        
        private void LoadGroups()
        {
            var groups = _dataService.GetAllGroups();
            
            // 添加"所有分组"选项
            var allGroups = new System.Collections.Generic.List<ConnectionGroup>
            {
                new ConnectionGroup { Id = 0, Name = "所有分组" }
            };
            allGroups.AddRange(groups);
            
            GroupFilterComboBox.ItemsSource = allGroups;
            GroupFilterComboBox.SelectedIndex = 0;
        }

        public void LoadConnections()
        {
            Connections.Clear();
            var connections = _dataService.GetAllConnections();
            foreach (var connection in connections)
            {
                Connections.Add(connection);
            }
            FilterConnections();
        }

        private void FilterConnections()
        {
            if (ConnectionsListView.Items == null)
                return;

            int selectedGroupId = 0;
            if (GroupFilterComboBox.SelectedValue != null)
            {
                selectedGroupId = (int)GroupFilterComboBox.SelectedValue;
            }

            string searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

            ConnectionsListView.Items.Filter = item =>
            {
                if (item is RemoteConnection connection)
                {
                    // 分组过滤
                    bool groupMatch = selectedGroupId == 0 || connection.GroupId == selectedGroupId;
                    
                    // 搜索过滤
                    bool searchMatch = string.IsNullOrWhiteSpace(searchText) ||
                                       connection.Name.ToLower().Contains(searchText) ||
                                       connection.IdentificationCode.ToLower().Contains(searchText) ||
                                       connection.ConnectionCode.ToLower().Contains(searchText);
                    
                    return groupMatch && searchMatch;
                }
                return false;
            };
        }
        
        private void RefreshConnectionList()
        {
            LoadConnections();
        }
        
        private void ConnectionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConnectionsListView.SelectedItem is RemoteConnection connection)
            {
                SelectedConnection = connection;
            }
        }
        
        private void UpdateConnectionDetails()
        {
            if (SelectedConnection == null)
            {
                ConnectionDetailsPanel.Children.Clear();
                ConnectionDetailsPanel.Children.Add(
                    new TextBlock 
                    { 
                        Text = "请从左侧列表中选择一个连接", 
                        FontSize = 14, 
                        Foreground = new SolidColorBrush(WpfColor.FromRgb(142, 142, 147)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    });
                return;
            }
            
            ConnectionDetailsPanel.Children.Clear();
            
            // 创建连接详情UI
            var detailsGrid = new Grid();
            detailsGrid.Margin = new Thickness(0, 0, 0, 20);
            
            // 定义行和列
            for (int i = 0; i < 8; i++)
            {
                detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // 连接名称
            var nameLabel = new TextBlock { Text = "连接名称:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var nameText = new TextBlock { Text = SelectedConnection.Name, Margin = new Thickness(10, 0, 0, 15), TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);
            Grid.SetRow(nameText, 0);
            Grid.SetColumn(nameText, 1);
            
            // 识别码
            var idLabel = new TextBlock { Text = "识别码:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var idText = new TextBlock { Text = SelectedConnection.IdentificationCode, Margin = new Thickness(10, 0, 0, 15) };
            Grid.SetRow(idLabel, 1);
            Grid.SetColumn(idLabel, 0);
            Grid.SetRow(idText, 1);
            Grid.SetColumn(idText, 1);
            
            // 连接码
            var codeLabel = new TextBlock { Text = "连接码:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var codeText = new TextBlock { Text = SelectedConnection.ConnectionCode, Margin = new Thickness(10, 0, 0, 15) };
            Grid.SetRow(codeLabel, 2);
            Grid.SetColumn(codeLabel, 0);
            Grid.SetRow(codeText, 2);
            Grid.SetColumn(codeText, 1);
            
            // 备注
            var remarksLabel = new TextBlock { Text = "备注:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var remarksText = new TextBlock { Text = SelectedConnection.Remarks ?? "无", Margin = new Thickness(10, 0, 0, 15), TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(remarksLabel, 3);
            Grid.SetColumn(remarksLabel, 0);
            Grid.SetRow(remarksText, 3);
            Grid.SetColumn(remarksText, 1);
            
            // 创建时间
            var createdLabel = new TextBlock { Text = "创建时间:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var createdText = new TextBlock { Text = SelectedConnection.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), Margin = new Thickness(10, 0, 0, 15) };
            Grid.SetRow(createdLabel, 4);
            Grid.SetColumn(createdLabel, 0);
            Grid.SetRow(createdText, 4);
            Grid.SetColumn(createdText, 1);
            
            // 最后连接时间
            var lastLabel = new TextBlock { Text = "最后连接时间:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var lastText = new TextBlock { Text = SelectedConnection.LastConnectedAt == DateTime.MinValue ? "从未连接" : SelectedConnection.LastConnectedAt.ToString("yyyy-MM-dd HH:mm:ss"), Margin = new Thickness(10, 0, 0, 15) };
            Grid.SetRow(lastLabel, 5);
            Grid.SetColumn(lastLabel, 0);
            Grid.SetRow(lastText, 5);
            Grid.SetColumn(lastText, 1);
            
            // 状态
            var statusLabel = new TextBlock { Text = "状态:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) };
            var statusText = new TextBlock { Text = SelectedConnection.IsEnabled ? "已启用" : "已禁用", Margin = new Thickness(10, 0, 0, 15) };
            statusText.Foreground = SelectedConnection.IsEnabled ? 
                new SolidColorBrush(WpfColor.FromRgb(52, 199, 89)) : 
                new SolidColorBrush(WpfColor.FromRgb(142, 142, 147));
            Grid.SetRow(statusLabel, 6);
            Grid.SetColumn(statusLabel, 0);
            Grid.SetRow(statusText, 6);
            Grid.SetColumn(statusText, 1);
            
            // 分隔线
            var separator = new System.Windows.Shapes.Rectangle 
            { 
                Height = 1, 
                Fill = new SolidColorBrush(WpfColor.FromRgb(229, 229, 234)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(separator, 7);
            Grid.SetColumn(separator, 0);
            Grid.SetColumnSpan(separator, 2);
            
            // 添加所有元素到网格
            detailsGrid.Children.Add(nameLabel);
            detailsGrid.Children.Add(nameText);
            detailsGrid.Children.Add(idLabel);
            detailsGrid.Children.Add(idText);
            detailsGrid.Children.Add(codeLabel);
            detailsGrid.Children.Add(codeText);
            detailsGrid.Children.Add(remarksLabel);
            detailsGrid.Children.Add(remarksText);
            detailsGrid.Children.Add(createdLabel);
            detailsGrid.Children.Add(createdText);
            detailsGrid.Children.Add(lastLabel);
            detailsGrid.Children.Add(lastText);
            detailsGrid.Children.Add(statusLabel);
            detailsGrid.Children.Add(statusText);
            detailsGrid.Children.Add(separator);
            
            ConnectionDetailsPanel.Children.Add(detailsGrid);
        }
        
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchPlaceholderControl != null)
            {
                SearchPlaceholderControl.Visibility = Visibility.Collapsed;
            }
        }
        
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                if (SearchPlaceholderControl != null)
                {
                    SearchPlaceholderControl.Visibility = Visibility.Visible;
                }
            }
            FilterConnections();
        }
        
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 显示或隐藏占位符
            if (SearchPlaceholderControl != null)
            {
                SearchPlaceholderControl.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            
            FilterConnections();
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}