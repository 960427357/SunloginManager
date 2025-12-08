using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Threading;

namespace SunloginManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        internal NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 生成图标文件（如果不存在）
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SunloginManager.ico");
            if (!File.Exists(iconPath))
            {
                IconGenerator.GenerateIcon();
            }
            
            // 初始化系统托盘
            InitializeSystemTray();
            
            // 创建主窗口
            _mainWindow = new MainWindow();
            
            // 处理窗口关闭事件，改为最小化到托盘
            _mainWindow.Closing += (sender, e) =>
            {
                e.Cancel = true; // 取消关闭事件
                _mainWindow.Hide(); // 隐藏窗口
                _mainWindow.ShowInTaskbar = false; // 从任务栏隐藏
                
                // 显示通知提示
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(2000, "向日葵远程连接管理器", "应用程序已最小化到系统托盘", ToolTipIcon.Info);
                }
            };
            
            _mainWindow.Show();
        }
        
        private void InitializeSystemTray()
        {
            // 创建系统托盘图标
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SunloginManager.ico")),
                Text = "向日葵远程连接管理器",
                Visible = true
            };
            
            // 创建上下文菜单
            var contextMenu = new ContextMenuStrip();
            
            // 显示窗口菜单项
            var showMenuItem = new ToolStripMenuItem("显示主窗口", null, OnShowMainWindow);
            contextMenu.Items.Add(showMenuItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // 退出菜单项 - 添加确认对话框
            var exitMenuItem = new ToolStripMenuItem("退出", null, (s, e) => {
                var result = System.Windows.MessageBox.Show("确定要退出向日葵远程连接管理器吗？", "确认退出", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // 强制退出应用程序
                    Environment.Exit(0);
                }
            });
            contextMenu.Items.Add(exitMenuItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // 双击托盘图标显示主窗口
            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
            
            // 处理应用程序退出事件
            this.Exit += OnApplicationExit;
        }
        
        private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }
        
        private void OnShowMainWindow(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }
        
        private void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                // 如果窗口已经关闭，重新创建
                if (!_mainWindow.IsLoaded)
                {
                    _mainWindow = new MainWindow();
                    
                    // 重新设置窗口关闭事件
                    _mainWindow.Closing += (sender, e) =>
                    {
                        e.Cancel = true; // 取消关闭事件
                        _mainWindow.Hide(); // 隐藏窗口
                        _mainWindow.ShowInTaskbar = false; // 从任务栏隐藏
                        
                        // 显示通知提示
                        if (_notifyIcon != null)
                        {
                            _notifyIcon.ShowBalloonTip(2000, "向日葵远程连接管理器", "应用程序已最小化到系统托盘", ToolTipIcon.Info);
                        }
                    };
                }
                
                // 确保窗口显示在任务栏和屏幕上
                _mainWindow.ShowInTaskbar = true;
                
                // 如果窗口已经隐藏，则显示它
                if (_mainWindow.Visibility != Visibility.Visible)
                {
                    _mainWindow.Show();
                }
                
                // 确保窗口状态正常并激活
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Topmost = true;
                _mainWindow.Topmost = false;
                _mainWindow.Focus();
            }
        }
        
        // 退出应用程序
        private void OnExit(object? sender, EventArgs e)
        {
            // 强制退出应用程序
            Environment.Exit(0);
        }
        
        private void OnApplicationExit(object? sender, ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}
