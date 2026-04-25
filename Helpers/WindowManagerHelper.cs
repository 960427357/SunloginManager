using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SunloginManager.Constants;
using SunloginManager.Services;

namespace SunloginManager.Helpers
{
    /// <summary>
    /// 窗口管理帮助类
    /// </summary>
    public static class WindowManagerHelper
    {
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
    }
}
