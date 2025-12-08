using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SunloginManager.Models;

namespace SunloginManager.Services
{
    public class SunloginService
    {
        private string _sunloginPath;
        private readonly DataService _dataService;

        // Windows API 声明
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        
        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern bool GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);
        
        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        private const int SW_RESTORE = 9;
        private const uint WM_NEXTDLGCTL = 0x0028;
        private const uint WM_SETTEXT = 0x000C;
        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;
        private const uint WM_CLICK = 0x00F5;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint BM_CLICK = 0x00F5;
        private const byte VK_TAB = 0x09;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const int VK_RETURN = 0x0D;
        
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        
        // 回调函数委托
        private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);
        
        // 存储找到的子窗口句柄
        private static List<IntPtr> childWindows = new List<IntPtr>();

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public SunloginService(DataService dataService = null)
        {
            LogService.LogInfo("初始化SunloginService");
            
            _dataService = dataService ?? new DataService();
            
            // 首先尝试从设置中加载路径
            _sunloginPath = _dataService.LoadSunloginPath();
            LogService.LogInfo($"从设置加载的向日葵路径: {_sunloginPath}");
            
            // 如果没有保存的路径，尝试常见的向日葵安装路径
            if (string.IsNullOrEmpty(_sunloginPath) || !File.Exists(_sunloginPath))
            {
                string[] possiblePaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Oray", "SunLogin", "SunloginClient.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Oray", "SunLogin", "SunloginClient.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "SunloginClient", "SunloginClient.exe")
                };

                _sunloginPath = possiblePaths.FirstOrDefault(File.Exists) ?? string.Empty;
                LogService.LogInfo($"搜索到的向日葵路径: {_sunloginPath}");
                
                // 如果找到了路径，保存它
                if (!string.IsNullOrEmpty(_sunloginPath))
                {
                    _dataService.SaveSunloginPath(_sunloginPath);
                    LogService.LogInfo("已保存向日葵路径到设置");
                }
            }
        }

        public bool IsSunloginInstalled()
        {
            return !string.IsNullOrEmpty(_sunloginPath) && File.Exists(_sunloginPath);
        }

        public async Task<bool> ConnectToRemoteAsync(RemoteConnection connection)
        {
            LogService.LogInfo($"开始连接到远程主机: {connection.Name} (ID: {connection.Id})");
            LogService.LogInfo($"连接详情 - 识别码: {connection.IdentificationCode}, 连接码: {connection.ConnectionCode}");
            
            if (string.IsNullOrEmpty(_sunloginPath) || !File.Exists(_sunloginPath))
            {
                LogService.LogError("向日葵客户端路径无效或不存在");
                return false;
            }

            LogService.LogInfo($"使用向日葵路径: {_sunloginPath}");

            try
            {
                // 优先使用方式3：不使用参数，只启动程序，然后自动输入识别码和连接码
                // 这种方式最可靠，因为自动输入功能已经处理了所有情况
                try
                {
                    LogService.LogInfo("使用方式3: 启动程序并自动输入识别码和连接码");
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = _sunloginPath,
                        UseShellExecute = true
                    };

                    LogService.LogInfo($"执行命令: {_sunloginPath} (无参数)");
                    
                    Process process = await Task.Run(() => Process.Start(startInfo));
                    
                    if (process != null)
                    {
                        LogService.LogInfo($"向日葵进程已启动，PID: {process.Id}");
                        
                        // 等待向日葵界面完全加载
                        await Task.Delay(3000);
                        
                        // 使用自动输入功能
                        bool autoInputSuccess = await AutoInputCodesAsync(connection);
                        
                        if (autoInputSuccess)
                        {
                            // 更新最后连接时间
                            connection.LastConnectedAt = DateTime.Now;
                            LogService.LogInfo($"连接成功，已更新最后连接时间: {connection.LastConnectedAt}");
                            return true;
                        }
                        else
                        {
                            LogService.LogWarning("自动输入失败，尝试其他方式");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"方式3失败: {ex.Message}");
                }
                
                // 备选方式1: 使用 -fastcode 参数 + UseShellExecute = true
                try
                {
                    LogService.LogInfo("尝试备选方式1: 使用 -fastcode 参数启动");
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = _sunloginPath,
                        Arguments = $"-fastcode {connection.ConnectionCode}",
                        UseShellExecute = true
                    };

                    LogService.LogInfo($"执行命令: {_sunloginPath} -fastcode {connection.ConnectionCode} (UseShellExecute=true)");
                    
                    Process process = await Task.Run(() => Process.Start(startInfo));
                    
                    if (process != null)
                    {
                        LogService.LogInfo($"向日葵进程已启动，PID: {process.Id}");
                        
                        // 等待向日葵界面加载
                        await Task.Delay(3000);
                        
                        // 尝试自动输入识别码（如果有）
                        if (!string.IsNullOrEmpty(connection.IdentificationCode))
                        {
                            LogService.LogInfo("尝试自动输入识别码");
                            await AutoInputCodesAsync(connection);
                        }
                        
                        // 更新最后连接时间
                        connection.LastConnectedAt = DateTime.Now;
                        LogService.LogInfo($"连接成功，已更新最后连接时间: {connection.LastConnectedAt}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"备选方式1失败: {ex.Message}");
                }
                
                // 备选方式2: 使用 -fastcode 参数 + UseShellExecute = false
                try
                {
                    LogService.LogInfo("尝试备选方式2: 使用 -fastcode 参数启动 (UseShellExecute=false)");
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = _sunloginPath,
                        Arguments = $"-fastcode {connection.ConnectionCode}",
                        UseShellExecute = false
                    };

                    LogService.LogInfo($"执行命令: {_sunloginPath} -fastcode {connection.ConnectionCode}");
                    
                    Process process = await Task.Run(() => Process.Start(startInfo));
                    
                    if (process != null)
                    {
                        LogService.LogInfo($"向日葵进程已启动，PID: {process.Id}");
                        
                        // 等待向日葵界面加载
                        await Task.Delay(3000);
                        
                        // 尝试自动输入识别码（如果有）
                        if (!string.IsNullOrEmpty(connection.IdentificationCode))
                        {
                            LogService.LogInfo("尝试自动输入识别码");
                            await AutoInputCodesAsync(connection);
                        }
                        
                        // 更新最后连接时间
                        connection.LastConnectedAt = DateTime.Now;
                        LogService.LogInfo($"连接成功，已更新最后连接时间: {connection.LastConnectedAt}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"备选方式2失败: {ex.Message}");
                }
                
                LogService.LogError($"所有启动方式都失败了");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"启动向日葵时出错: {ex.Message}", ex);
                return false;
            }
        }

        public string GetSunloginPath()
        {
            return _sunloginPath;
        }

        public bool SetCustomSunloginPath(string path)
        {
            if (File.Exists(path))
            {
                _sunloginPath = path;
                _dataService.SaveSunloginPath(path);
                return true;
            }
            return false;
        }

        private async Task<bool> SendText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                LogService.LogWarning("SendText: 文本为空，跳过发送");
                return true;
            }

            LogService.LogInfo($"SendText: 准备发送文本 '{text}' (长度: {text.Length})");
            
            try
            {
                // 首先尝试使用SendKeys
                LogService.LogInfo("SendText: 尝试使用SendKeys方法");
                SendKeys.SendWait(text);
                LogService.LogInfo("SendText: SendKeys方法成功");
                return true;
            }
            catch (Exception ex)
            {
                LogService.LogWarning($"SendText: SendKeys方法失败: {ex.Message}");
                
                // 如果SendKeys失败，尝试使用keybd_event逐个发送字符
                LogService.LogInfo("SendText: 回退到keybd_event方法");
                foreach (char c in text)
                {
                    try
                    {
                        // 转换字符为虚拟键码
                        short vkCode = VkKeyScan(c);
                        byte scanCode = (byte)((vkCode >> 8) & 0xFF);
                        byte keyCode = (byte)(vkCode & 0xFF);
                        
                        LogService.LogInfo($"SendText: 发送字符 '{c}' (VK码: {keyCode}, 扫描码: {scanCode})");
                        
                        // 按下键
                        keybd_event(keyCode, scanCode, 0, 0);
                        await Task.Delay(10); // 短暂延迟
                        
                        // 释放键
                        keybd_event(keyCode, scanCode, KEYEVENTF_KEYUP, 0);
                        await Task.Delay(10); // 短暂延迟
                    }
                    catch (Exception charEx)
                    {
                        LogService.LogError($"SendText: 发送字符 '{c}' 失败: {charEx.Message}");
                    }
                }
                
                LogService.LogInfo("SendText: keybd_event方法完成");
                return true;
            }
        }

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        // 检查是否需要输入验证码
        private async Task<bool> CheckIfVerificationCodeNeeded()
        {
            try
            {
                LogService.LogInfo("检查是否需要输入验证码...");
                
                // 等待一段时间，让验证码界面有时间出现
                await Task.Delay(2000);
                
                // 查找可能的验证码窗口或控件
                // 这里我们检查窗口标题是否包含"验证码"、"验证"等关键词
                var allProcesses = Process.GetProcesses();
                foreach (var process in allProcesses)
                {
                    try
                    {
                        if ((process.ProcessName.Contains("AweSun") || process.ProcessName.Contains("Sunlogin")) &&
                            !string.IsNullOrEmpty(process.MainWindowTitle))
                        {
                            LogService.LogInfo($"检查窗口标题: {process.MainWindowTitle}");
                            if (process.MainWindowTitle.Contains("验证") || 
                                process.MainWindowTitle.Contains("验证码") ||
                                process.MainWindowTitle.Contains("安全") ||
                                process.MainWindowTitle.Contains("身份验证"))
                            {
                                LogService.LogInfo("检测到验证码相关窗口");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.LogWarning($"检查进程 {process.Id} 时出错: {ex.Message}");
                    }
                }
                
                // 尝试通过窗口类名或其他方式检测验证码界面
                // 这里可以添加更多的检测逻辑
                
                LogService.LogInfo("未检测到验证码界面");
                return false; // 如果没有检测到验证码，返回false
            }
            catch (Exception ex)
            {
                LogService.LogError($"检查是否需要验证码时出错: {ex.Message}", ex);
                return false; // 出错时假设不需要验证码
            }
        }

        // 使用Windows API枚举子窗口的回调函数
        private static bool EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            childWindows.Add(hWnd);
            return true;
        }
        
        // 检测是否需要验证码
        private async Task<bool> CheckIfVerificationCodeNeeded(IntPtr mainWindowHandle)
        {
            try
            {
                // 等待一段时间，让验证码界面有时间加载
                await Task.Delay(2000);
                
                // 获取窗口标题
                StringBuilder windowTitle = new StringBuilder(256);
                GetWindowText(mainWindowHandle, windowTitle, windowTitle.Capacity);
                string title = windowTitle.ToString();
                
                // 检查窗口标题是否包含验证码相关的关键词
                if (title.Contains("验证码") || title.Contains("身份验证") || title.Contains("验证"))
                {
                    LogService.LogInfo($"检测到需要验证码，窗口标题: {title}");
                    return true;
                }
                
                // 枚举子窗口，查找验证码输入框
                childWindows.Clear();
                EnumChildWindows(mainWindowHandle, EnumChildWindowsCallback, IntPtr.Zero);
                
                foreach (IntPtr childWindow in childWindows)
                {
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(childWindow, className, className.Capacity);
                    
                    // 查找可能的验证码输入框
                    if (className.ToString().Contains("Edit"))
                    {
                        StringBuilder windowText = new StringBuilder(256);
                        GetWindowText(childWindow, windowText, windowText.Capacity);
                        
                        // 检查输入框的标签或提示文本
                        if (windowText.ToString().Contains("验证码") || 
                            windowText.ToString().Contains("验证") ||
                            windowText.ToString().Contains("身份验证"))
                        {
                            LogService.LogInfo($"通过子窗口检测到需要验证码: {windowText}");
                            return true;
                        }
                    }
                }
                
                LogService.LogInfo("未检测到需要验证码");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"检测验证码需求失败: {ex.Message}");
                return false;
            }
        }
        
        // 检查当前焦点是否在预期的输入框上
        private static bool IsFocusOnExpectedInput(IntPtr mainWindowHandle, string expectedInputType)
        {
            try
            {
                // 获取当前具有焦点的窗口
                IntPtr focusedWindow = GetFocus();
                if (focusedWindow == IntPtr.Zero)
                {
                    LogService.LogInfo("无法获取当前焦点窗口");
                    return false;
                }
                
                // 检查焦点窗口是否是主窗口的子窗口
                IntPtr parentWindow = GetParent(focusedWindow);
                bool isChildOfMain = false;
                
                // 检查父窗口链
                IntPtr currentParent = parentWindow;
                while (currentParent != IntPtr.Zero)
                {
                    if (currentParent == mainWindowHandle)
                    {
                        isChildOfMain = true;
                        break;
                    }
                    currentParent = GetParent(currentParent);
                }
                
                if (!isChildOfMain)
                {
                    LogService.LogInfo("焦点不在主窗口的子窗口上");
                    return false;
                }
                
                // 获取焦点窗口的类名
                StringBuilder className = new StringBuilder(256);
                GetClassName(focusedWindow, className, className.Capacity);
                
                // 检查是否是输入框
                if (className.ToString().Contains("Edit"))
                {
                    LogService.LogInfo($"焦点在输入框上，类型: {expectedInputType}");
                    return true;
                }
                
                LogService.LogInfo($"焦点不在输入框上，当前焦点窗口类名: {className}");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"检查焦点状态失败: {ex.Message}");
                return false;
            }
        }

        // 使用Windows API查找并设置焦点到连接码输入框
        private static async Task<bool> SetFocusToConnectionCodeInput(IntPtr mainWindowHandle)
        {
            try
            {
                // 清空子窗口列表
                childWindows.Clear();
                
                // 枚举所有子窗口
                EnumChildWindows(mainWindowHandle, EnumChildWindowsCallback, IntPtr.Zero);
                
                // 查找可能的输入框控件
                foreach (IntPtr childWindow in childWindows)
                {
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(childWindow, className, className.Capacity);
                    
                    // 查找Edit类控件（输入框）
                    if (className.ToString().Contains("Edit"))
                    {
                        StringBuilder windowText = new StringBuilder(256);
                        GetWindowText(childWindow, windowText, windowText.Capacity);
                        
                        LogService.LogInfo($"找到输入框: 类名={className}, 文本={windowText}");
                        
                        // 尝试设置焦点到这个输入框
                        IntPtr previousFocus = SetFocus(childWindow);
                        if (previousFocus != IntPtr.Zero || childWindow != IntPtr.Zero)
                        {
                            LogService.LogInfo("成功设置焦点到输入框");
                            await Task.Delay(300); // 等待焦点设置完成
                            return true;
                        }
                    }
                }
                
                // 如果找不到特定的输入框，尝试使用WM_NEXTDLGCTL消息切换焦点
                PostMessage(mainWindowHandle, WM_NEXTDLGCTL, IntPtr.Zero, new IntPtr(1));
                await Task.Delay(300);
                LogService.LogInfo("使用WM_NEXTDLGCTL消息切换焦点");
                return true;
            }
            catch (Exception ex)
            {
                LogService.LogError($"使用Windows API设置焦点失败: {ex.Message}");
                return false;
            }
        }


        private async Task<bool> AutoInputCodesAsync(RemoteConnection connection)
        {
            LogService.LogInfo("===== 开始自动输入识别码和连接码 =====");
            LogService.LogInfo($"目标识别码: {connection.IdentificationCode}");
            LogService.LogInfo($"目标连接码: {connection.ConnectionCode}");
            
            try
            {
                // 等待向日葵窗口出现
                LogService.LogInfo("等待向日葵窗口出现...");
                // await Task.Delay(3000);

                // 查找向日葵窗口
                IntPtr sunloginWindow = IntPtr.Zero;
                string[] possibleWindowTitles = { "向日葵远程控制", "向日葵", "Sunlogin", "向日葵个人版", "向日葵企业版", "AweSun" };
                
                LogService.LogInfo("开始查找向日葵窗口...");
                
                // 首先尝试查找AweSun进程
                var aweSunProcesses = Process.GetProcessesByName("AweSun");
                LogService.LogInfo($"找到 {aweSunProcesses.Length} 个AweSun进程");
                
                foreach (var process in aweSunProcesses)
                {
                    LogService.LogInfo($"检查AweSun进程 PID: {process.Id}, 窗口标题: {process.MainWindowTitle}");
                    if (!string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        sunloginWindow = process.MainWindowHandle;
                        LogService.LogInfo($"找到AweSun匹配窗口，句柄: {sunloginWindow}");
                        break;
                    }
                }
                
                // 如果没找到AweSun进程，再尝试查找SunloginClient进程
                if (sunloginWindow == IntPtr.Zero)
                {
                    foreach (var title in possibleWindowTitles)
                    {
                        LogService.LogInfo($"尝试查找窗口标题: {title}");
                        var processes = Process.GetProcessesByName("SunloginClient");
                        LogService.LogInfo($"找到 {processes.Length} 个SunloginClient进程");
                        
                        foreach (var process in processes)
                        {
                            LogService.LogInfo($"检查进程 PID: {process.Id}, 窗口标题: {process.MainWindowTitle}");
                            if (!string.IsNullOrEmpty(process.MainWindowTitle) && 
                                (process.MainWindowTitle.Contains(title) || process.MainWindowTitle.Contains("向日葵")))
                            {
                                sunloginWindow = process.MainWindowHandle;
                                LogService.LogInfo($"找到匹配窗口，句柄: {sunloginWindow}");
                                break;
                            }
                        }
                        
                        if (sunloginWindow != IntPtr.Zero)
                            break;
                    }
                }

                if (sunloginWindow == IntPtr.Zero)
                {
                    LogService.LogWarning("未找到向日葵窗口，尝试查找所有进程的窗口");
                    var allProcesses = Process.GetProcesses();
                    LogService.LogInfo($"总共找到 {allProcesses.Length} 个进程");
                    
                    foreach (var process in allProcesses)
                    {
                        try
                        {
                            if (process.ProcessName.Contains("AweSun") || 
                                process.ProcessName.Contains("Sunlogin") ||
                                (!string.IsNullOrEmpty(process.MainWindowTitle) && 
                                 (process.MainWindowTitle.Contains("向日葵") || 
                                  process.MainWindowTitle.Contains("AweSun"))))
                            {
                                LogService.LogInfo($"尝试使用进程 PID: {process.Id} ({process.ProcessName}) 的主窗口");
                                sunloginWindow = process.MainWindowHandle;
                                if (sunloginWindow != IntPtr.Zero)
                                {
                                    LogService.LogInfo($"找到可用窗口句柄: {sunloginWindow}");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogService.LogWarning($"访问进程 {process.Id} 失败: {ex.Message}");
                        }
                    }
                }

                if (sunloginWindow == IntPtr.Zero)
                {
                    LogService.LogError("无法找到向日葵窗口");
                    return false;
                }

                // 激活窗口
                LogService.LogInfo("激活向日葵窗口...");
                SetForegroundWindow(sunloginWindow);
                ShowWindow(sunloginWindow, 9); // 9 = SW_RESTORE
                await Task.Delay(1000);

                // 获取当前前台窗口以确认激活成功
                IntPtr foregroundWindow = GetForegroundWindow();
                LogService.LogInfo($"当前前台窗口句柄: {foregroundWindow}, 目标窗口句柄: {sunloginWindow}");
                if (foregroundWindow != sunloginWindow)
                {
                    LogService.LogWarning("窗口激活可能失败，前台窗口不是目标窗口");
                }

                // 输入识别码
                if (!string.IsNullOrEmpty(connection.IdentificationCode))
                {
                    LogService.LogInfo($"开始输入识别码: {connection.IdentificationCode}");
                    // 先清空输入框
                    LogService.LogInfo("清空识别码输入框");
                    SendKeys.SendWait("^{HOME}+^{END}");
                    await Task.Delay(200);
                    
                    // 输入识别码
                    LogService.LogInfo($"发送识别码文本");
                    await SendText(connection.IdentificationCode);
                    LogService.LogInfo($"识别码输入完成");
                    await Task.Delay(500);
                    
                    // 按Tab键切换到连接码输入框
                    LogService.LogInfo("按Tab键切换到连接码输入框");
                    
                    // 等待识别码输入完成
                    await Task.Delay(800);
                    
                    // 使用多次Tab键确保切换到连接码输入框
                    LogService.LogInfo("发送Tab键切换焦点");
                    for (int i = 0; i < 2; i++)
                    {
                        keybd_event(VK_TAB, 0, 0, 0);
                        await Task.Delay(100);
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                        await Task.Delay(300);
                    }
                    
                    // 等待焦点切换完成
                    await Task.Delay(500);
                }
                else
                {
                    LogService.LogWarning("识别码为空，跳过输入");
                }

                // 输入连接码
                if (!string.IsNullOrEmpty(connection.ConnectionCode))
                {
                    // 增加额外延迟，确保焦点已经完全切换到连接码输入框
                    await Task.Delay(500);
                    
                    LogService.LogInfo($"开始输入连接码: {connection.ConnectionCode}");
                    // 先清空输入框
                    LogService.LogInfo("清空连接码输入框");
                    SendKeys.SendWait("^{HOME}+^{END}");
                    await Task.Delay(500); // 增加延迟，确保清空操作完成
                    
                    // 输入连接码
                    LogService.LogInfo($"发送连接码文本");
                    await SendText(connection.ConnectionCode);
                    LogService.LogInfo($"连接码输入完成");
                    
                    // 验证连接码是否输入成功，通过检查文本长度
                    await Task.Delay(500);
                    LogService.LogInfo("连接码输入验证完成");
                }
                else
                {
                    LogService.LogWarning("连接码为空，跳过输入");
                }

                // 按回车键确认
                LogService.LogInfo("按回车键确认连接");
                try
                {
                    SendKeys.SendWait("{ENTER}");
                    LogService.LogInfo("回车键发送成功");
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"发送Enter键失败: {ex.Message}，使用备用方法");
                    // 回退到原始方法
                    keybd_event(VK_RETURN, 0, 0, 0);
                    keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
                    LogService.LogInfo("使用备用方法发送回车键");
                }
                
                // 等待验证码界面出现
                LogService.LogInfo("等待验证码界面出现...");
                await Task.Delay(3000);
                
                // 检查是否需要输入验证码
                bool needsVerificationCode = await CheckIfVerificationCodeNeeded(sunloginWindow);
                if (needsVerificationCode)
                {
                    LogService.LogInfo("检测到需要输入验证码");
                    
                    // 使用配置的验证码，不再弹出对话框
                    if (!string.IsNullOrEmpty(connection.VerificationCode))
                    {
                        LogService.LogInfo($"使用配置的验证码: {connection.VerificationCode}");
                        
                        // 等待验证码界面完全加载
                        await Task.Delay(2000);
                        
                        // 尝试切换焦点到验证码输入框
                        LogService.LogInfo("尝试切换焦点到验证码输入框");
                        
                        // 等待连接码输入完成
                        await Task.Delay(800);
                        
                        // 使用多次Tab键确保切换到验证码输入框
                        LogService.LogInfo("发送Tab键切换焦点");
                        for (int i = 0; i < 2; i++)
                        {
                            keybd_event(VK_TAB, 0, 0, 0);
                            await Task.Delay(100);
                            keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                            await Task.Delay(300);
                        }
                        
                        // 等待焦点切换完成
                        await Task.Delay(500);
                        
                        // 输入验证码
                        LogService.LogInfo("开始输入验证码");
                        SendKeys.SendWait("^{HOME}+^{END}"); // 清空输入框
                        await Task.Delay(300);
                        await SendText(connection.VerificationCode);
                        LogService.LogInfo("验证码输入完成");
                        await Task.Delay(500);
                        
                        // 按回车键确认验证码
                        LogService.LogInfo("按回车键确认验证码");
                        try
                        {
                            SendKeys.SendWait("{ENTER}");
                            LogService.LogInfo("验证码确认回车键发送成功");
                        }
                        catch (Exception ex)
                        {
                            LogService.LogWarning($"发送验证码确认Enter键失败: {ex.Message}，使用备用方法");
                            keybd_event(VK_RETURN, 0, 0, 0);
                            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
                            LogService.LogInfo("使用备用方法发送验证码确认回车键");
                        }
                    }
                    else
                    {
                        LogService.LogWarning("未配置验证码，跳过验证码输入");
                    }
                }
                else
                {
                    LogService.LogInfo("未检测到需要输入验证码");
                }
                
                await Task.Delay(1000);

                LogService.LogInfo("===== 自动输入完成 =====");
                return true;
            }
            catch (Exception ex)
            {
                LogService.LogError($"自动输入识别码和连接码时出错: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 切换到连接码输入框
        /// </summary>
        private async Task<bool> SwitchToConnectionCodeInput(IntPtr sunloginWindow)
        {
            try
            {
                LogService.LogInfo("开始切换到连接码输入框");
                
                // 方法1: 尝试使用Windows API直接设置焦点到连接码输入框
                bool focusSet = await SetFocusToConnectionCodeInput(sunloginWindow);
                if (focusSet)
                {
                    LogService.LogInfo("方法1成功: 使用Windows API设置焦点到连接码输入框");
                    return true;
                }
                
                // 方法2: 尝试使用线程附加方法设置焦点
                try
                {
                    LogService.LogInfo("尝试方法2: 使用线程附加方法设置焦点");
                    
                    // 获取当前线程和目标窗口线程ID
                    uint currentThreadId = GetWindowThreadProcessId(GetForegroundWindow(), out uint currentProcessId);
                    uint targetThreadId = GetWindowThreadProcessId(sunloginWindow, out uint targetProcessId);
                    
                    // 附加线程
                    if (AttachThreadInput(currentThreadId, targetThreadId, true))
                    {
                        // 设置焦点到主窗口
                        BringWindowToTop(sunloginWindow);
                        SetActiveWindow(sunloginWindow);
                        
                        // 查找并设置焦点到连接码输入框
                        focusSet = await SetFocusToConnectionCodeInput(sunloginWindow);
                        
                        // 分离线程
                        AttachThreadInput(currentThreadId, targetThreadId, false);
                        
                        if (focusSet)
                        {
                            LogService.LogInfo("方法2成功: 使用线程附加方法设置焦点");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"方法2失败: {ex.Message}");
                }
                
                // 方法3: 尝试使用鼠标点击方法
                try
                {
                    LogService.LogInfo("尝试方法3: 使用鼠标点击方法设置焦点");
                    
                    // 查找连接码输入框
                    childWindows.Clear();
                    EnumChildWindows(sunloginWindow, EnumChildWindowsCallback, IntPtr.Zero);
                    
                    foreach (IntPtr childWindow in childWindows)
                    {
                        StringBuilder className = new StringBuilder(256);
                        GetClassName(childWindow, className, className.Capacity);
                        
                        if (className.ToString().Contains("Edit"))
                        {
                            // 获取输入框位置
                            if (GetWindowRect(childWindow, out RECT rect))
                            {
                                // 计算点击位置（输入框中心）
                                int x = rect.Left + (rect.Right - rect.Left) / 2;
                                int y = rect.Top + (rect.Bottom - rect.Top) / 2;
                                
                                // 设置鼠标位置
                                Cursor.Position = new Point(x, y);
                                await Task.Delay(100);
                                
                                // 模拟鼠标点击
                                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                await Task.Delay(50);
                                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                await Task.Delay(200);
                                
                                LogService.LogInfo("方法3成功: 使用鼠标点击方法设置焦点");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"方法3失败: {ex.Message}");
                }
                
                // 方法4: 尝试使用多次Tab键
                try
                {
                    LogService.LogInfo("尝试方法4: 使用多次Tab键设置焦点");
                    
                    // 发送多次Tab键，确保焦点切换到连接码输入框
                    for (int i = 0; i < 3; i++)
                    {
                        keybd_event(VK_TAB, 0, 0, 0);
                        await Task.Delay(100);
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                        await Task.Delay(300);
                    }
                    
                    LogService.LogInfo("方法4成功: 使用多次Tab键设置焦点");
                    return true;
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"方法4失败: {ex.Message}");
                }
                
                LogService.LogWarning("所有方法都失败，无法设置焦点到连接码输入框");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"切换到连接码输入框时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 切换到验证码输入框
        /// </summary>
        private async Task<bool> SwitchToVerificationCodeInput(IntPtr sunloginWindow)
        {
            try
            {
                LogService.LogInfo("开始切换到验证码输入框");
                
                // 方法1: 尝试使用Windows API直接设置焦点到验证码输入框
                bool focusSet = await SetFocusToVerificationCodeInput(sunloginWindow);
                if (focusSet)
                {
                    LogService.LogInfo("方法1成功: 使用Windows API设置焦点到验证码输入框");
                    return true;
                }
                
                // 方法2: 尝试使用线程附加方法设置焦点
                try
                {
                    LogService.LogInfo("尝试方法2: 使用线程附加方法设置焦点");
                    
                    // 获取当前线程和目标窗口线程ID
                    uint currentThreadId = GetWindowThreadProcessId(GetForegroundWindow(), out uint currentProcessId);
                    uint targetThreadId = GetWindowThreadProcessId(sunloginWindow, out uint targetProcessId);
                    
                    // 附加线程
                    if (AttachThreadInput(currentThreadId, targetThreadId, true))
                    {
                        // 设置焦点到主窗口
                        BringWindowToTop(sunloginWindow);
                        SetActiveWindow(sunloginWindow);
                        
                        // 查找并设置焦点到验证码输入框
                        focusSet = await SetFocusToVerificationCodeInput(sunloginWindow);
                        
                        // 分离线程
                        AttachThreadInput(currentThreadId, targetThreadId, false);
                        
                        if (focusSet)
                        {
                            LogService.LogInfo("方法2成功: 使用线程附加方法设置焦点");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"方法2失败: {ex.Message}");
                }
                
                // 方法3: 尝试使用鼠标点击方法
                try
                {
                    LogService.LogInfo("尝试方法3: 使用鼠标点击方法设置焦点");
                    
                    // 查找验证码输入框
                    childWindows.Clear();
                    EnumChildWindows(sunloginWindow, EnumChildWindowsCallback, IntPtr.Zero);
                    
                    foreach (IntPtr childWindow in childWindows)
                    {
                        StringBuilder className = new StringBuilder(256);
                        GetClassName(childWindow, className, className.Capacity);
                        
                        if (className.ToString().Contains("Edit"))
                        {
                            StringBuilder windowText = new StringBuilder(256);
                            GetWindowText(childWindow, windowText, windowText.Capacity);
                            
                            // 查找可能的验证码输入框
                            if (windowText.ToString().Contains("验证码") || 
                                windowText.ToString().Contains("验证") ||
                                windowText.ToString().Contains("身份验证"))
                            {
                                // 获取输入框位置
                                if (GetWindowRect(childWindow, out RECT rect))
                                {
                                    // 计算点击位置（输入框中心）
                                    int x = rect.Left + (rect.Right - rect.Left) / 2;
                                    int y = rect.Top + (rect.Bottom - rect.Top) / 2;
                                    
                                    // 设置鼠标位置
                                    Cursor.Position = new Point(x, y);
                                    await Task.Delay(100);
                                    
                                    // 模拟鼠标点击
                                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                    await Task.Delay(50);
                                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                    await Task.Delay(200);
                                    
                                    LogService.LogInfo("方法3成功: 使用鼠标点击方法设置焦点到验证码输入框");
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"方法3失败: {ex.Message}");
                }
                
                // 方法4: 尝试使用多次Tab键
                try
                {
                    LogService.LogInfo("尝试方法4: 使用多次Tab键设置焦点");
                    
                    // 发送多次Tab键，确保焦点切换到验证码输入框
                    for (int i = 0; i < 5; i++) // 验证码输入框可能需要更多Tab键
                    {
                        keybd_event(VK_TAB, 0, 0, 0);
                        await Task.Delay(100);
                        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                        await Task.Delay(300);
                        
                        // 每次Tab后检查是否焦点在验证码输入框上
                        if (IsFocusOnVerificationCodeInput(sunloginWindow))
                        {
                            LogService.LogInfo("方法4成功: 使用多次Tab键设置焦点到验证码输入框");
                            return true;
                        }
                    }
                    
                    LogService.LogWarning("方法4: 使用多次Tab键未能设置焦点到验证码输入框");
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"方法4失败: {ex.Message}");
                }
                
                LogService.LogWarning("所有方法都失败，无法设置焦点到验证码输入框");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"切换到验证码输入框时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用Windows API查找并设置焦点到验证码输入框
        /// </summary>
        private static async Task<bool> SetFocusToVerificationCodeInput(IntPtr mainWindowHandle)
        {
            try
            {
                // 清空子窗口列表
                childWindows.Clear();
                
                // 枚举所有子窗口
                EnumChildWindows(mainWindowHandle, EnumChildWindowsCallback, IntPtr.Zero);
                
                // 查找可能的验证码输入框控件
                foreach (IntPtr childWindow in childWindows)
                {
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(childWindow, className, className.Capacity);
                    
                    // 查找Edit类控件（输入框）
                    if (className.ToString().Contains("Edit"))
                    {
                        StringBuilder windowText = new StringBuilder(256);
                        GetWindowText(childWindow, windowText, windowText.Capacity);
                        
                        // 检查是否是验证码输入框
                        if (windowText.ToString().Contains("验证码") || 
                            windowText.ToString().Contains("验证") ||
                            windowText.ToString().Contains("身份验证"))
                        {
                            LogService.LogInfo($"找到验证码输入框: 类名={className}, 文本={windowText}");
                            
                            // 尝试设置焦点到这个输入框
                            IntPtr previousFocus = SetFocus(childWindow);
                            if (previousFocus != IntPtr.Zero || childWindow != IntPtr.Zero)
                            {
                                LogService.LogInfo("成功设置焦点到验证码输入框");
                                await Task.Delay(300); // 等待焦点设置完成
                                return true;
                            }
                        }
                    }
                }
                
                // 如果找不到特定的验证码输入框，尝试使用WM_NEXTDLGCTL消息切换焦点
                PostMessage(mainWindowHandle, WM_NEXTDLGCTL, IntPtr.Zero, new IntPtr(1));
                await Task.Delay(300);
                LogService.LogInfo("使用WM_NEXTDLGCTL消息切换焦点");
                return true;
            }
            catch (Exception ex)
            {
                LogService.LogError($"使用Windows API设置焦点到验证码输入框失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查当前焦点是否在验证码输入框上
        /// </summary>
        private static bool IsFocusOnVerificationCodeInput(IntPtr mainWindowHandle)
        {
            try
            {
                // 获取当前具有焦点的窗口
                IntPtr focusedWindow = GetFocus();
                if (focusedWindow == IntPtr.Zero)
                {
                    LogService.LogInfo("无法获取当前焦点窗口");
                    return false;
                }
                
                // 检查焦点窗口是否是主窗口的子窗口
                IntPtr parentWindow = GetParent(focusedWindow);
                bool isChildOfMain = false;
                
                // 检查父窗口链
                IntPtr currentParent = parentWindow;
                while (currentParent != IntPtr.Zero)
                {
                    if (currentParent == mainWindowHandle)
                    {
                        isChildOfMain = true;
                        break;
                    }
                    currentParent = GetParent(currentParent);
                }
                
                if (!isChildOfMain)
                {
                    LogService.LogInfo("焦点不在主窗口的子窗口上");
                    return false;
                }
                
                // 获取焦点窗口的类名
                StringBuilder className = new StringBuilder(256);
                GetClassName(focusedWindow, className, className.Capacity);
                
                // 检查是否是输入框
                if (className.ToString().Contains("Edit"))
                {
                    // 获取输入框的文本
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(focusedWindow, windowText, windowText.Capacity);
                    
                    // 检查是否是验证码输入框
                    if (windowText.ToString().Contains("验证码") || 
                        windowText.ToString().Contains("验证") ||
                        windowText.ToString().Contains("身份验证"))
                    {
                        LogService.LogInfo("焦点在验证码输入框上");
                        return true;
                    }
                }
                
                LogService.LogInfo($"焦点不在验证码输入框上，当前焦点窗口类名: {className}");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"检查验证码输入框焦点状态失败: {ex.Message}");
                return false;
            }
        }
    }
}