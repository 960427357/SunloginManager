using System;
using System.Runtime.InteropServices;
using SunloginManager.Constants;

namespace SunloginManager.Helpers
{
    /// <summary>
    /// Windows API 帮助类
    /// </summary>
    public static class WindowsApiHelper
    {
        #region Windows API 声明
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
        
        #endregion

        #region 窗口操作方法
        
        /// <summary>
        /// 激活窗口
        /// </summary>
        public static bool ActivateWindow(IntPtr hWnd)
        {
            return SetForegroundWindow(hWnd);
        }
        
        /// <summary>
        /// 显示窗口
        /// </summary>
        public static bool ShowWindowState(IntPtr hWnd, int nCmdShow)
        {
            return ShowWindow(hWnd, nCmdShow);
        }
        
        /// <summary>
        /// 查找窗口
        /// </summary>
        public static IntPtr FindWindowByTitle(string lpClassName, string lpWindowName)
        {
            return FindWindow(lpClassName, lpWindowName);
        }
        
        /// <summary>
        /// 获取前台窗口
        /// </summary>
        public static IntPtr GetActiveWindow()
        {
            return GetForegroundWindow();
        }
        
        /// <summary>
        /// 将窗口置顶
        /// </summary>
        public static bool BringToTop(IntPtr hWnd)
        {
            return BringWindowToTop(hWnd);
        }
        
        #endregion

        #region 键盘操作方法
        
        /// <summary>
        /// 发送键盘事件
        /// </summary>
        public static void SendKeyEvent(byte virtualKey, byte scanCode, uint flags, uint extraInfo)
        {
            keybd_event(virtualKey, scanCode, flags, extraInfo);
        }
        
        /// <summary>
        /// 获取虚拟键的扫描码
        /// </summary>
        public static uint GetScanCode(byte virtualKey)
        {
            return MapVirtualKey(virtualKey, KeyboardConstants.MAPVK_VK_TO_VSC);
        }
        
        /// <summary>
        /// 获取字符的虚拟键码
        /// </summary>
        public static short GetVirtualKeyCode(char ch)
        {
            return VkKeyScan(ch);
        }
        
        #endregion

        #region 线程操作方法
        
        /// <summary>
        /// 获取窗口线程ID
        /// </summary>
        public static uint GetWindowThread(IntPtr hWnd, out uint processId)
        {
            return GetWindowThreadProcessId(hWnd, out processId);
        }
        
        /// <summary>
        /// 附加线程输入
        /// </summary>
        public static bool AttachThread(uint idAttach, uint idAttachTo, bool attach)
        {
            return AttachThreadInput(idAttach, idAttachTo, attach);
        }
        
        #endregion
    }
}
