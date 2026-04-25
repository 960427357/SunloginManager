using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Threading;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SunloginManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        internal NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;
        private static Mutex? _mutex;
        private const string MUTEX_NAME = "SunloginManager_SingleInstance_Mutex";
        private const string WINDOW_CLASS_NAME = "SunloginManager_MainWindow";

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Services.LogService.LogError($"未处理的异常：{e.Exception.Message}", e.Exception);
            e.Handled = true;
        }

        // Windows API
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const uint WM_SHOWWINDOW_CUSTOM = 0x0400 + 1;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            // 尝试创建互斥锁
            bool createdNew;
            _mutex = new Mutex(true, MUTEX_NAME, out createdNew);
            
            if (!createdNew)
            {
                // 应用程序已经在运行，激活现有实例
                ActivateExistingInstance();
                
                // 退出当前实例
                Shutdown();
                return;
            }
            
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
            
            // 退出菜单项 - 直接退出
            var exitMenuItem = new ToolStripMenuItem("退出", null, (s, e) => {
                Services.LogService.LogInfo("用户点击退出菜单");
                
                // 清理资源
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                
                // 强制退出应用程序
                Environment.Exit(0);
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
            
            // 释放互斥锁
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
        
        /// <summary>
        /// 激活已存在的实例
        /// </summary>
        private void ActivateExistingInstance()
        {
            try
            {
                Services.LogService.LogInfo("检测到应用程序已在运行，尝试激活现有实例");
                
                // 方法1: 通过窗口标题查找
                IntPtr hWnd = FindWindow(null, "向日葵远程连接管理器");
                
                // 方法2: 如果方法1失败，通过进程查找
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = FindMainWindowByProcess();
                }
                
                if (hWnd != IntPtr.Zero)
                {
                    Services.LogService.LogInfo($"找到现有窗口句柄: {hWnd}");
                    
                    // 发送自定义消息通知窗口显示
                    SendMessage(hWnd, WM_SHOWWINDOW_CUSTOM, IntPtr.Zero, IntPtr.Zero);
                    
                    // 如果窗口最小化，恢复它
                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                        Services.LogService.LogInfo("窗口已从最小化状态恢复");
                    }
                    else
                    {
                        ShowWindow(hWnd, SW_SHOW);
                    }
                    
                    // 将窗口置于前台
                    SetForegroundWindow(hWnd);
                    Services.LogService.LogInfo("已激活现有窗口");
                }
                else
                {
                    Services.LogService.LogWarning("无法找到现有窗口句柄");
                }
            }
            catch (Exception ex)
            {
                Services.LogService.LogError($"激活现有实例失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 通过进程查找主窗口
        /// </summary>
        private IntPtr FindMainWindowByProcess()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                
                foreach (var process in processes)
                {
                    if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        Services.LogService.LogInfo($"通过进程找到窗口: PID={process.Id}, Handle={process.MainWindowHandle}");
                        return process.MainWindowHandle;
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LogService.LogError($"通过进程查找窗口失败: {ex.Message}", ex);
            }
            
            return IntPtr.Zero;
        }
    }
}
