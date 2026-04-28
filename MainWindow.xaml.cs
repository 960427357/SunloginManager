using System;
using System.Diagnostics;
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
using SunloginManager.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using WpfColor = System.Windows.Media.Color;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MessageBox = System.Windows.MessageBox;

namespace SunloginManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DataService _dataService;
        private int _favoriteGroupId = 0;
        private int _defaultGroupId = 1;
        private readonly SunloginService _sunloginService;
        private readonly HistoryService _historyService;
        private RemoteConnection? _selectedConnection;
        private TextBlock? _searchPlaceholderControl;
        private DispatcherTimer _statusTimer;
        private DispatcherTimer? _autoLockTimer;
        private readonly Dictionary<int, CancellationTokenSource> _activeMonitors = new();
        private string _currentSortColumn = "";
        private bool _sortAscending = true;
        private bool _isLocked;
        private bool _lockPasswordVisible;
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
            _historyService = new HistoryService(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
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

            // 快捷键处理
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            // 自动锁定处理
            this.PreviewMouseMove += MainWindow_PreviewMouseMove;

            InitializeAutoLock();

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

        #region 快捷键处理

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 锁定状态下禁止所有快捷键
            if (_isLocked)
                return;

            // 如果焦点在 TextBox/ComboBox 等输入控件内，不触发快捷键
            var focused = Keyboard.FocusedElement as UIElement;
            if (focused is System.Windows.Controls.TextBox || focused is System.Windows.Controls.PasswordBox || focused is System.Windows.Controls.ComboBox)
                return;

            // 必须有选中的连接
            if (ConnectionsListView.SelectedItem is not RemoteConnection conn)
                return;

            var shortcuts = _dataService.GetShortcutsSettings();
            foreach (var shortcut in shortcuts.GetAll())
            {
                if (!shortcut.IsEnabled) continue;
                if (!MatchesShortcut(shortcut, e)) continue;

                e.Handled = true;
                ExecuteShortcutAction(shortcut.ActionName, conn);
                break;
            }
        }

        private bool MatchesShortcut(KeyboardShortcutConfig shortcut, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Input.Key targetKey;
            try { targetKey = (System.Windows.Input.Key)Enum.Parse(typeof(System.Windows.Input.Key), shortcut.Key); }
            catch { return false; }

            System.Windows.Input.ModifierKeys targetModifiers = System.Windows.Input.ModifierKeys.None;
            if (!string.IsNullOrEmpty(shortcut.Modifiers))
            {
                try { targetModifiers = (System.Windows.Input.ModifierKeys)Enum.Parse(typeof(System.Windows.Input.ModifierKeys), shortcut.Modifiers); }
                catch { }
            }

            if (e.Key != targetKey) return false;
            System.Windows.Input.ModifierKeys currentModifiers = System.Windows.Input.Keyboard.Modifiers;
            return currentModifiers == targetModifiers;
        }

        private async void ExecuteShortcutAction(string actionName, RemoteConnection conn)
        {
            try
            {
                string textToCopy = actionName switch
                {
                    "CopyIdentificationCode" => conn.IdentificationCode,
                    "CopyConnectionCode" => conn.ConnectionCode,
                    "CopyRemarks" => string.IsNullOrEmpty(conn.Remarks) ? "无" : conn.Remarks,
                    "CopyAllInfo" => FormatConnectionInfo(conn),
                    _ => null
                };

                if (string.IsNullOrEmpty(textToCopy))
                {
                    UpdateStatusText("复制失败：内容为空");
                    return;
                }

                // 异步设置剪贴板，带重试机制
                bool success = await SetClipboardTextAsync(textToCopy);

                string displayName = actionName switch
                {
                    "CopyIdentificationCode" => "识别码",
                    "CopyConnectionCode" => "连接码",
                    "CopyRemarks" => "备注",
                    "CopyAllInfo" => "全部信息",
                    _ => actionName
                };

                UpdateStatusText(success ? $"已复制: {displayName} 到剪贴板" : "复制失败：剪贴板被占用");
            }
            catch (Exception ex)
            {
                LogService.LogError($"快捷键复制失败: {ex.Message}");
                UpdateStatusText("复制失败");
            }
        }

        private string FormatConnectionInfo(RemoteConnection conn)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"名称: {conn.Name}");
            sb.AppendLine($"识别码: {conn.IdentificationCode}");
            sb.AppendLine($"连接码: {conn.ConnectionCode}");
            if (!string.IsNullOrEmpty(conn.Remarks))
                sb.AppendLine($"备注: {conn.Remarks}");
            sb.AppendLine($"分组: {conn.GroupName}");
            sb.AppendLine($"状态: {(conn.IsEnabled ? "已启用" : "已禁用")}");
            sb.AppendLine($"收藏: {(conn.IsFavorite ? "是" : "否")}");
            if (conn.LastConnectedAt != default)
                sb.AppendLine($"最后连接: {conn.LastConnectedAt:yyyy-MM-dd HH:mm:ss}");
            return sb.ToString();
        }

        private async Task<bool> SetClipboardTextAsync(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return true;
                }
                catch
                {
                    await Task.Delay(50);
                }
            }
            return false;
        }

        #endregion

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
            if (_isLocked) return;
            RefreshConnectionList();
            UpdateStatusText("连接列表已刷新");
        }
        
        // 设置按钮点击事件
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
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
            if (_isLocked) return;
            var aboutWindow = new Views.AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // 分组管理按钮点击事件
        private void ManageGroupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection connection)
            {
                await ConnectAndMonitorAsync(connection);
            }
            else
            {
                UpdateStatusText("请先选择一个连接");
            }
        }
        
        // 从详情面板连接按钮点击事件
        private async void ConnectFromDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (SelectedConnection != null)
            {
                await ConnectAndMonitorAsync(SelectedConnection);
            }
            else
            {
                UpdateStatusText("请先选择一个连接");
            }
        }
        
        // 从详情面板编辑按钮点击事件
        private void EditFromDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
            if (sender is System.Windows.Controls.Button button && button.DataContext is RemoteConnection connection)
            {
                await ConnectAndMonitorAsync(connection);
            }
        }
        
        // 表格中的编辑按钮点击事件
        private void EditFromTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
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
            if (_isLocked) return;
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

                // 复制到剪贴板（剪贴板被占用时重试）
                bool clipboardSuccess = false;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(shareText.ToString());
                        clipboardSuccess = true;
                        break;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        System.Threading.Thread.Sleep(200);
                    }
                }
                if (!clipboardSuccess)
                {
                    UpdateStatusText("分享失败：无法访问剪贴板");
                    LogService.LogWarning("复制失败：剪贴板被其他程序占用");
                    return;
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

            // 查找收藏分组和默认分组的ID
            var favoriteGroup = groups.FirstOrDefault(g => g.IsFavoriteGroup);
            _favoriteGroupId = favoriteGroup?.Id ?? 0;
            var defaultGroup = groups.FirstOrDefault(g => g.IsDefault);
            _defaultGroupId = defaultGroup?.Id ?? 1;

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
                    bool groupMatch;
                    if (selectedGroupId == _favoriteGroupId)
                    {
                        // 收藏分组显示所有标记为收藏的连接
                        groupMatch = connection.IsFavorite;
                    }
                    else
                    {
                        groupMatch = selectedGroupId == 0 || connection.GroupId == selectedGroupId;
                    }
                    
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

        private void SortConnections(string column)
        {
            if (_currentSortColumn == column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _currentSortColumn = column;
                _sortAscending = true;
            }

            var view = CollectionViewSource.GetDefaultView(Connections);
            var connections = view.Cast<RemoteConnection>().ToList();

            switch (column)
            {
                case "Name":
                    connections = _sortAscending
                        ? connections.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
                        : connections.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                case "LastConnectedAt":
                    connections = _sortAscending
                        ? connections.OrderBy(c => c.LastConnectedAt).ToList()
                        : connections.OrderByDescending(c => c.LastConnectedAt).ToList();
                    break;
                default:
                    return;
            }

            Connections.Clear();
            foreach (var conn in connections)
            {
                Connections.Add(conn);
            }
            FilterConnections();
        }

        private void RefreshConnectionList()
        {
            LoadConnections();
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.GridViewColumnHeader header && header.Tag is string column)
            {
                // 更新所有列的排序箭头
                foreach (var child in GetColumnHeaders(header))
                {
                    var arrow = FindSortArrow(child);
                    if (arrow != null) arrow.Visibility = Visibility.Collapsed;
                }

                // 设置当前列的箭头
                var currentArrow = FindSortArrow(header);
                if (currentArrow != null)
                {
                    currentArrow.Visibility = Visibility.Visible;
                    currentArrow.Text = _sortAscending ? "▲" : "▼";
                }

                SortConnections(column);
            }
        }

        private System.Collections.Generic.IEnumerable<System.Windows.Controls.GridViewColumnHeader> GetColumnHeaders(System.Windows.Controls.GridViewColumnHeader clicked)
        {
            var headers = new System.Collections.Generic.List<System.Windows.Controls.GridViewColumnHeader>();
            var parent = FindVisualParent<System.Windows.Controls.ListView>(clicked);
            if (parent != null)
            {
                var presenter = FindVisualChild<System.Windows.Controls.Primitives.UniformGrid>(parent);
                if (presenter != null)
                {
                    foreach (var child in presenter.Children)
                    {
                        if (child is System.Windows.Controls.GridViewColumnHeader h)
                            headers.Add(h);
                    }
                }
            }
            return headers;
        }

        private TextBlock? FindSortArrow(System.Windows.Controls.GridViewColumnHeader header)
        {
            var template = header.Template;
            return template?.FindName("SortArrow", header) as TextBlock;
        }

        private T? FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent == null) return null;
            if (parent is T t) return t;
            return FindVisualParent<T>(parent);
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
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

        // ========== v2.0 新功能 ==========

        // 双击连接
        private async void ConnectionsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection connection)
            {
                await ConnectAndMonitorAsync(connection);
            }
        }

        // 右键菜单 - 阻止默认行为，确保选中项正确
        private void ConnectionsListView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked) return;
            if (e.Source is FrameworkElement fe && fe.DataContext is RemoteConnection conn)
            {
                ConnectionsListView.SelectedItem = conn;
            }
        }

        // 右键菜单 - 连接
        private async void ContextConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                await ConnectAndMonitorAsync(conn);
            }
        }

        // 右键菜单 - 切换收藏
        private void ContextToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                ToggleFavorite(conn);
            }
        }

        // 右键菜单 - 测试连接
        private async void ContextTestConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                UpdateStatusText("正在测试连接...");
                var (success, message) = await _sunloginService.TestConnectionAsync(conn);
                UpdateStatusText(message);
                MessageBox.Show(message, success ? "测试通过" : "测试失败", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
        }

        // 右键菜单 - 编辑
        private void ContextEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                var dialog = new EditConnectionDialog(conn);
                if (dialog.ShowDialog() == true)
                {
                    var updated = dialog.GetConnection();
                    if (updated != null)
                    {
                        _dataService.UpdateConnection(updated);
                        RefreshConnectionList();
                        UpdateStatusText($"连接 '{updated.Name}' 已更新");
                    }
                }
            }
        }

        // 右键菜单 - 分享
        private void ContextShare_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                ShareConnection(conn);
            }
        }

        // 右键菜单 - 删除
        private void ContextDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                if (MessageBox.Show($"确定要删除连接 '{conn.Name}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteConnection(conn.Id.ToString());
                    RefreshConnectionList();
                    UpdateStatusText($"已删除连接: {conn.Name}");
                }
            }
        }

        // 切换收藏
        public void ToggleFavorite(RemoteConnection connection)
        {
            connection.IsFavorite = !connection.IsFavorite;
            if (connection.IsFavorite)
            {
                // 收藏时移到收藏分组
                if (_favoriteGroupId > 0)
                {
                    connection.GroupId = _favoriteGroupId;
                }
            }
            else
            {
                // 取消收藏时移回默认分组
                connection.GroupId = _defaultGroupId;
            }
            _dataService.UpdateConnection(connection);
            RefreshConnectionList();
            UpdateStatusText(connection.IsFavorite ? $"已收藏: {connection.Name}" : $"取消收藏: {connection.Name}");
        }

        // 连接并监控时长
        private async Task ConnectAndMonitorAsync(RemoteConnection connection)
        {
            try
            {
                LogService.LogInfo($"开始连接: {connection.Name} (ID: {connection.Id})");
                UpdateStatusText($"正在连接 {connection.Name}...");

                var (success, process) = await _sunloginService.ConnectWithMonitoringAsync(connection);

                if (success)
                {
                    _dataService.UpdateConnection(connection);
                    LoadConnections();
                    UpdateStatusText($"已连接到 {connection.Name}");

                    // 为每个连接创建独立的监控任务
                    var startTime = DateTime.Now;
                    _historyService.RecordStart(connection.Id, connection.Name, connection.IdentificationCode, startTime);
                    LogService.LogInfo($"开始监控连接时长: {connection.Name}, 识别码: {connection.IdentificationCode}");

                    // 如果该连接已有监控，先取消旧任务
                    if (_activeMonitors.TryGetValue(connection.Id, out var oldCts))
                    {
                        oldCts.Cancel();
                        _activeMonitors.Remove(connection.Id);
                    }

                    var cts = new CancellationTokenSource();
                    _activeMonitors[connection.Id] = cts;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var windowExisted = await WindowManagerHelper.WaitForConnectionSessionAsync(
                                connection.IdentificationCode,
                                cancellationToken: cts.Token,
                                pollIntervalMs: 10000,
                                closeTimeoutMs: 3600000);
                            var endTime = DateTime.Now;
                            _historyService.RecordEnd(connection.Id, endTime);
                            LogService.LogInfo($"连接结束: {connection.Name}, 时长: {endTime - startTime}");
                        }
                        catch (OperationCanceledException)
                        {
                            LogService.LogInfo($"连接监控已取消: {connection.Name}");
                        }
                        catch (Exception ex)
                        {
                            LogService.LogError($"监控连接时长异常: {connection.Name}, {ex.Message}");
                        }
                        finally
                        {
                            _activeMonitors.Remove(connection.Id);
                            cts.Dispose();
                        }
                    }, cts.Token);
                }
                else
                {
                    UpdateStatusText("连接失败");
                    _historyService.RecordFailed(connection.Id, connection.Name, connection.IdentificationCode, "连接未成功", ConnectionStatus.Failed);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"连接失败: {ex.Message}");
                LogService.LogError($"连接失败: {ex.Message}", ex);
            }
        }

        // 顶部栏 - 历史记录
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            var dialog = new ConnectionHistoryDialog(_historyService);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        // 顶部栏 - 统计
        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            var stats = _historyService.GetStats();
            var dialog = new ConnectionStatsDialog(stats);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        // 顶部栏 - 测试连接
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLocked) return;
            if (ConnectionsListView.SelectedItem is RemoteConnection conn)
            {
                UpdateStatusText("正在测试连接...");
                var (success, message) = await _sunloginService.TestConnectionAsync(conn);
                UpdateStatusText(message);
                MessageBox.Show(message, success ? "测试通过" : "测试失败", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            else
            {
                UpdateStatusText("请先选择一个连接");
            }
        }

        // 点击收藏星号
        private void ConnectionsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked) return;
            var element = e.OriginalSource as FrameworkElement;
            if (element?.DataContext is not RemoteConnection conn) return;

            // 获取点击位置相对于行的X坐标
            var listView = sender as System.Windows.Controls.ListView;
            if (listView == null) return;

            var point = e.GetPosition(listView);
            // 收藏列宽度约40px，检查是否点击在第一列
            if (point.X < 50)
            {
                ToggleFavorite(conn);
                e.Handled = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region 自动锁定

        private void InitializeAutoLock()
        {
            int minutes = _dataService.GetAutoLockMinutes();
            if (minutes <= 0)
                return;

            _autoLockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(minutes)
            };
            _autoLockTimer.Tick += (sender, e) => TriggerLock();
            _autoLockTimer.Start();
        }

        private void MainWindow_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isLocked)
                ResetAutoLockTimer();
        }

        private void ResetAutoLockTimer()
        {
            if (_autoLockTimer == null) return;
            _autoLockTimer.Stop();
            _autoLockTimer.Start();
        }

        private void TriggerLock()
        {
            if (_isLocked || _autoLockTimer == null) return;
            _isLocked = true;
            _autoLockTimer.Stop();

            LockErrorText.Visibility = Visibility.Collapsed;
            LockPasswordBox.Password = string.Empty;
            LockPasswordTextBox.Text = string.Empty;
            LockOverlay.Visibility = Visibility.Visible;
            LockPasswordBox.Focus();

            LogService.LogInfo("屏幕已自动锁定");
        }

        private void LockEyeButton_Click(object sender, RoutedEventArgs e)
        {
            _lockPasswordVisible = !_lockPasswordVisible;
            if (_lockPasswordVisible)
            {
                LockPasswordTextBox.Text = LockPasswordBox.Password;
                LockPasswordBox.Visibility = Visibility.Collapsed;
                LockPasswordTextBox.Visibility = Visibility.Visible;
                LockPasswordTextBox.Focus();
            }
            else
            {
                LockPasswordBox.Password = LockPasswordTextBox.Text;
                LockPasswordBox.Visibility = Visibility.Visible;
                LockPasswordTextBox.Visibility = Visibility.Collapsed;
                LockPasswordBox.Focus();
            }
        }

        private void LockPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                UnlockScreen();
        }

        private void LockUnlockButton_Click(object sender, RoutedEventArgs e)
        {
            UnlockScreen();
        }

        private void UnlockScreen()
        {
            string password = LockPasswordBox.Visibility == Visibility.Visible
                ? LockPasswordBox.Password
                : LockPasswordTextBox.Text;

            if (_dataService.VerifyMasterPassword(password))
            {
                _isLocked = false;
                LockOverlay.Visibility = Visibility.Collapsed;
                LockErrorText.Visibility = Visibility.Collapsed;
                _lockPasswordVisible = false;
                LockPasswordBox.Visibility = Visibility.Visible;
                LockPasswordTextBox.Visibility = Visibility.Collapsed;
                _autoLockTimer?.Start();
                LogService.LogInfo("屏幕已解锁");
            }
            else
            {
                LockErrorText.Text = "密码错误";
                LockErrorText.Visibility = Visibility.Visible;
                LockPasswordBox.Password = string.Empty;
                LockPasswordTextBox.Text = string.Empty;
                LockPasswordBox.Focus();
            }
        }

        private void LockExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要退出程序吗？", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (App.Current is App app && app._notifyIcon != null)
                {
                    app._notifyIcon.Visible = false;
                    app._notifyIcon.Dispose();
                }
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 窗口从托盘恢复显时调用
        /// </summary>
        public void OnWindowShown()
        {
            LogService.LogInfo("OnWindowShown 调用");
            if (_autoLockTimer != null && !_isLocked)
            {
                _autoLockTimer.Stop();
                _autoLockTimer.Start();
            }
        }

        /// <summary>
        /// 窗口隐藏到托盘时调用
        /// </summary>
        public void OnWindowHidden()
        {
            LogService.LogInfo("OnWindowHidden 调用");
            _autoLockTimer?.Stop();
            // 取消所有连接监控任务
            foreach (var cts in _activeMonitors.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _activeMonitors.Clear();
        }

        #endregion
    }
}