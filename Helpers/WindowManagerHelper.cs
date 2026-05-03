using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SunloginManager.Constants;
using SunloginManager.Services;

namespace SunloginManager.Helpers
{
    public static class WindowManagerHelper
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// 查找向日葵主程序窗口（排除远程会话窗口），用于自动输入识别码和连接码
        /// </summary>
        public static IntPtr FindSunloginMainWindow()
        {
            LogService.LogInfo("开始查找向日葵主程序窗口（排除远程会话）...");

            var aweSunProcesses = Process.GetProcessesByName("AweSun");
            LogService.LogInfo($"找到 {aweSunProcesses.Length} 个 AweSun 进程");

            foreach (var process in aweSunProcesses)
            {
                LogService.LogInfo($"检查 AweSun 进程 PID: {process.Id}, 窗口标题：'{process.MainWindowTitle}'");
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    // 排除远程会话窗口（标题包含数字识别码的）
                    string title = process.MainWindowTitle ?? "";
                    if (!IsRemoteSessionWindow(title))
                    {
                        LogService.LogInfo($"找到主程序窗口，句柄：{process.MainWindowHandle}");
                        return process.MainWindowHandle;
                    }
                    LogService.LogInfo($"跳过远程会话窗口: '{title}'");
                }
            }

            // 回退到原有逻辑
            return FindSunloginWindow();
        }

        /// <summary>
        /// 判断窗口标题是否为远程会话窗口（包含较长数字串，像识别码）
        /// </summary>
        private static bool IsRemoteSessionWindow(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return false;

            // 远程会话窗口标题通常包含连续数字（识别码）
            // 检测是否有 >= 6 位连续数字
            int maxDigits = 0, current = 0;
            foreach (char c in windowTitle)
            {
                if (char.IsDigit(c)) current++;
                else { maxDigits = Math.Max(maxDigits, current); current = 0; }
            }
            maxDigits = Math.Max(maxDigits, current);

            return maxDigits >= 6;
        }

        /// <summary>
        /// 查找向日葵窗口
        /// </summary>
        public static IntPtr FindSunloginWindow()
        {
            LogService.LogInfo("开始查找向日葵窗口...");

            // 首先尝试查找 AweSun 进程（向日葵新版本）
            var aweSunProcesses = Process.GetProcessesByName("AweSun");
            LogService.LogInfo($"找到 {aweSunProcesses.Length} 个 AweSun 进程");

            foreach (var process in aweSunProcesses)
            {
                LogService.LogInfo($"检查 AweSun 进程 PID: {process.Id}, 窗口标题：'{process.MainWindowTitle}'");
                // 向日葵新版本可能没有窗口标题，只要有句柄就返回
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    LogService.LogInfo($"找到 AweSun 窗口，句柄：{process.MainWindowHandle}");
                    return process.MainWindowHandle;
                }
            }

            // 如果没找到 AweSun 进程，再尝试查找 SunloginClient 进程
            foreach (var title in WindowConstants.SunloginWindowTitles)
            {
                LogService.LogInfo($"尝试查找窗口标题：{title}");
                var processes = Process.GetProcessesByName("SunloginClient");
                LogService.LogInfo($"找到 {processes.Length} 个 SunloginClient 进程");

                foreach (var process in processes)
                {
                    LogService.LogInfo($"检查进程 PID: {process.Id}, 窗口标题：'{process.MainWindowTitle}'");
                    if (process.MainWindowHandle != IntPtr.Zero &&
                        (string.IsNullOrEmpty(process.MainWindowTitle) ||
                         process.MainWindowTitle.Contains(title) ||
                         process.MainWindowTitle.Contains("向日葵")))
                    {
                        LogService.LogInfo($"找到匹配窗口，句柄：{process.MainWindowHandle}");
                        return process.MainWindowHandle;
                    }
                }
            }

            // 最后尝试查找所有相关进程
            LogService.LogWarning("未找到向日葵窗口，尝试查找所有进程的窗口");
            var allProcesses = Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    if (WindowConstants.SunloginProcessNames.Any(name => process.ProcessName.Contains(name)) ||
                        (!string.IsNullOrEmpty(process.MainWindowTitle) &&
                         WindowConstants.SunloginWindowTitles.Any(title => process.MainWindowTitle.Contains(title))))
                    {
                        LogService.LogInfo($"尝试使用进程 PID: {process.Id} ({process.ProcessName}) 的主窗口");
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            LogService.LogInfo($"找到可用窗口句柄：{process.MainWindowHandle}");
                            return process.MainWindowHandle;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogWarning($"访问进程 {process.Id} 失败：{ex.Message}");
                }
            }

            LogService.LogError("无法找到向日葵窗口");
            return IntPtr.Zero;
        }

        /// <summary>
        /// 激活窗口并确保其处于前台
        /// </summary>
        public static async Task<bool> ActivateWindowAsync(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                LogService.LogError("窗口句柄无效");
                return false;
            }

            LogService.LogInfo("激活向日葵窗口...");
            WindowsApiHelper.ActivateWindow(hWnd);
            WindowsApiHelper.ShowWindowState(hWnd, WindowConstants.SW_RESTORE);
            await Task.Delay(TimingConstants.WINDOW_ACTIVATION_DELAY);

            // 验证窗口是否激活
            IntPtr foregroundWindow = WindowsApiHelper.GetActiveWindow();
            LogService.LogInfo($"当前前台窗口句柄：{foregroundWindow}, 目标窗口句柄：{hWnd}");

            return foregroundWindow == hWnd;
        }

        /// <summary>
        /// 强制激活窗口（使用线程附加）
        /// </summary>
        public static async Task<bool> ForceActivateWindowAsync(IntPtr hWnd)
        {
            try
            {
                LogService.LogInfo("强制激活向日葵窗口");

                uint currentThreadId = WindowsApiHelper.GetWindowThread(WindowsApiHelper.GetActiveWindow(), out uint currentProcessId);
                uint targetThreadId = WindowsApiHelper.GetWindowThread(hWnd, out uint targetProcessId);

                LogService.LogInfo($"当前前台线程：{currentThreadId}, 目标线程：{targetThreadId}");

                if (currentThreadId != targetThreadId)
                {
                    // 附加线程输入
                    if (WindowsApiHelper.AttachThread(currentThreadId, targetThreadId, true))
                    {
                        LogService.LogInfo("线程输入已附加");

                        // 激活窗口
                        WindowsApiHelper.ShowWindowState(hWnd, WindowConstants.SW_RESTORE);
                        WindowsApiHelper.ActivateWindow(hWnd);
                        WindowsApiHelper.BringToTop(hWnd);

                        // 分离线程输入
                        WindowsApiHelper.AttachThread(currentThreadId, targetThreadId, false);

                        await Task.Delay(TimingConstants.THREAD_ATTACH_DELAY);
                        return true;
                    }
                    else
                    {
                        LogService.LogWarning("线程输入附加失败");
                    }
                }
                else
                {
                    LogService.LogInfo("目标窗口已经是前台窗口");
                }

                // 再次确认窗口激活
                WindowsApiHelper.ActivateWindow(hWnd);
                WindowsApiHelper.BringToTop(hWnd);
                await Task.Delay(TimingConstants.WINDOW_ACTIVATION_DELAY);

                // 验证窗口是否真的激活了
                IntPtr currentForegroundWindow = WindowsApiHelper.GetActiveWindow();
                if (currentForegroundWindow == hWnd)
                {
                    LogService.LogInfo("向日葵窗口已成功激活");
                    return true;
                }
                else
                {
                    LogService.LogWarning($"向日葵窗口可能未激活，前台窗口：{currentForegroundWindow}, 目标：{hWnd}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.LogWarning($"强制激活窗口失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 如果当前处于远程连接状态，则返回主界面
        /// 用于在已有活跃连接的情况下发起新连接
        /// </summary>
        public static async Task<bool> ReturnToMainScreenIfNeededAsync()
        {
            LogService.LogInfo("检查是否已有活跃连接...");

            var windows = GetOpenWindows();

            // 查找包含识别码的连接窗口（非主界面）
            // 向日葵/AweSun 连接时窗口标题通常包含识别码
            var connectionWindow = windows.FirstOrDefault(w =>
                IsWindowVisible(w.hWnd) &&
                IsAweSunWindow(w.title) &&
                !string.IsNullOrEmpty(w.title) &&
                w.title.Any(char.IsDigit) &&
                !w.title.Contains("首页", StringComparison.OrdinalIgnoreCase) &&
                !w.title.Contains("Main", StringComparison.OrdinalIgnoreCase));

            if (connectionWindow.hWnd != IntPtr.Zero)
            {
                LogService.LogInfo($"检测到活跃连接窗口: '{connectionWindow.title}'，正在返回主界面...");

                // 激活连接窗口
                SetForegroundWindow(connectionWindow.hWnd);
                await Task.Delay(150);

                // 发送 ESC 键返回主界面
                await KeyboardInputHelper.SendKeyAsync(KeyboardConstants.VK_ESCAPE);
                await Task.Delay(500);

                // 再发送一次确保生效
                await KeyboardInputHelper.SendKeyAsync(KeyboardConstants.VK_ESCAPE);
                await Task.Delay(500);

                LogService.LogInfo("已尝试返回主界面");
                return true;
            }

            LogService.LogInfo("未检测到活跃连接");
            return false;
        }

        /// <summary>
        /// 判断窗口标题是否为 AweSun/向日葵相关窗口
        /// </summary>
        private static bool IsAweSunWindow(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return false;

            string lower = windowTitle.ToLowerInvariant();
            return lower.Contains("awesun")
                   || lower.Contains("sunlogin")
                   || lower.Contains("向日葵")
                   || lower.Contains("oray")
                   || lower.Contains("远程")
                   || lower.Contains("remote")
                   || lower.Contains("桌面")
                   || lower.Contains("control");
        }

        /// <summary>
        /// 判断窗口标题是否为向日葵远程连接窗口（包含指定识别码）
        /// 长识别码（>= 6位）直接匹配即可，误判概率极低
        /// </summary>
        private static bool IsSunloginWindow(string windowTitle, string identificationCode)
        {
            if (string.IsNullOrEmpty(windowTitle) || string.IsNullOrEmpty(identificationCode))
                return false;

            // 短识别码（< 4位）不做匹配，防止误判
            if (identificationCode.Length < 4)
                return false;

            string cleanedTitle = windowTitle.Replace(" ", "");

            // 窗口标题必须包含识别码
            if (!cleanedTitle.Contains(identificationCode))
                return false;

            // 长识别码（>= 6位）直接匹配，误判概率极低
            if (identificationCode.Length >= 6)
                return true;

            // 4-5位短识别码需要额外校验关键词
            return cleanedTitle.Contains("远程")
                   || cleanedTitle.Contains("向日葵")
                   || cleanedTitle.Contains("AweSun")
                   || cleanedTitle.Contains("Sunlogin")
                   || cleanedTitle.Contains("控制")
                   || cleanedTitle.Contains("桌面");
        }

        private static List<(IntPtr hWnd, string title)> GetOpenWindows()
        {
            var result = new List<(IntPtr, string)>();
            try
            {
                // 使用带超时的线程执行 EnumWindows，防止 RDP 断开时卡住
                var thread = new System.Threading.Thread(() =>
                {
                    EnumWindows((hWnd, _) =>
                    {
                        var sb = new System.Text.StringBuilder(256);
                        if (GetWindowText(hWnd, sb, sb.Capacity) > 0)
                        {
                            lock (result)
                            {
                                result.Add((hWnd, sb.ToString()));
                            }
                        }
                        return true;
                    }, IntPtr.Zero);
                })
                { IsBackground = true };

                thread.Start();
                if (!thread.Join(3000)) // 3秒超时
                {
                    LogService.LogWarning("GetOpenWindows 执行超时，可能因 RDP 断开导致阻塞");
                    return new List<(IntPtr, string)>();
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"GetOpenWindows 异常: {ex.Message}");
            }
            return result;
        }

        private static uint GetWindowProcessId(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            return pid;
        }
    }
}
