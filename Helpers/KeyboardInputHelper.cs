using System;
using System.Threading.Tasks;
using SunloginManager.Constants;
using SunloginManager.Services;

namespace SunloginManager.Helpers
{
    /// <summary>
    /// 键盘输入帮助类
    /// </summary>
    public static class KeyboardInputHelper
    {
        /// <summary>
        /// 发送单个按键（按下并释放）
        /// </summary>
        public static async Task SendKeyAsync(byte virtualKey, int delayMs = TimingConstants.KEY_PRESS_DELAY)
        {
            uint scanCode = WindowsApiHelper.GetScanCode(virtualKey);
            
            // 按下
            WindowsApiHelper.SendKeyEvent(virtualKey, (byte)scanCode, 0, 0);
            await Task.Delay(delayMs);
            
            // 释放
            WindowsApiHelper.SendKeyEvent(virtualKey, (byte)scanCode, KeyboardConstants.KEYEVENTF_KEYUP, 0);
        }
        
        /// <summary>
        /// 发送Tab键
        /// </summary>
        public static async Task SendTabKeyAsync()
        {
            LogService.LogInfo("发送Tab键");
            await SendKeyAsync(KeyboardConstants.VK_TAB);
        }
        
        /// <summary>
        /// 发送回车键
        /// </summary>
        public static async Task SendEnterKeyAsync()
        {
            LogService.LogInfo("发送回车键");
            await SendKeyAsync(KeyboardConstants.VK_RETURN);
        }
        
        /// <summary>
        /// 发送Delete键
        /// </summary>
        public static async Task SendDeleteKeyAsync()
        {
            await SendKeyAsync(KeyboardConstants.VK_DELETE);
        }
        
        /// <summary>
        /// 发送Ctrl+A（全选）
        /// </summary>
        public static async Task SendSelectAllAsync()
        {
            uint ctrlScan = WindowsApiHelper.GetScanCode(KeyboardConstants.VK_CONTROL);
            uint aScan = WindowsApiHelper.GetScanCode(KeyboardConstants.VK_A);

            // 按下Ctrl
            WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_CONTROL, (byte)ctrlScan, 0, 0);
            await Task.Delay(10);

            // 按下A
            WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_A, (byte)aScan, 0, 0);
            await Task.Delay(10);

            // 释放A
            WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_A, (byte)aScan, KeyboardConstants.KEYEVENTF_KEYUP, 0);
            await Task.Delay(10);

            // 释放Ctrl
            WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_CONTROL, (byte)ctrlScan, KeyboardConstants.KEYEVENTF_KEYUP, 0);
        }
        
        /// <summary>
        /// 清空输入框（Ctrl+A + Delete）
        /// </summary>
        public static async Task ClearInputAsync()
        {
            LogService.LogInfo("清空输入框");

            // Ctrl+A 全选
            await SendSelectAllAsync();
            await Task.Delay(50);

            // Delete 删除
            await SendDeleteKeyAsync();
            await Task.Delay(TimingConstants.CLEAR_INPUT_DELAY);
        }

        /// <summary>
        /// 通过剪贴板粘贴发送文本（比逐字符输入快）
        /// </summary>
        public static async Task SendTextViaPasteAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                LogService.LogWarning("SendTextViaPaste: 文本为空，跳过发送");
                return;
            }

            LogService.LogInfo($"SendTextViaPaste: 准备粘贴文本 '{text}' (长度: {text.Length})");

            // 复制到剪贴板
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    break;
                }
                catch
                {
                    await Task.Delay(50);
                }
            }

            await Task.Delay(50);

            // 发送 Ctrl+V 粘贴
            uint ctrlScan = WindowsApiHelper.GetScanCode(KeyboardConstants.VK_CONTROL);
            uint vScan = WindowsApiHelper.GetScanCode((byte)0x56); // VK_V

            WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_CONTROL, (byte)ctrlScan, 0, 0);
            await Task.Delay(10);
            WindowsApiHelper.SendKeyEvent((byte)0x56, (byte)vScan, 0, 0);
            await Task.Delay(10);
            WindowsApiHelper.SendKeyEvent((byte)0x56, (byte)vScan, KeyboardConstants.KEYEVENTF_KEYUP, 0);
            await Task.Delay(10);
            WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_CONTROL, (byte)ctrlScan, KeyboardConstants.KEYEVENTF_KEYUP, 0);

            LogService.LogInfo("SendTextViaPaste: 粘贴完成");
        }
        
        /// <summary>
        /// 发送文本（逐字符输入）
        /// </summary>
        public static async Task SendTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                LogService.LogWarning("SendText: 文本为空，跳过发送");
                return;
            }

            LogService.LogInfo($"SendText: 准备发送文本 '{text}' (长度: {text.Length})");
            
            foreach (char c in text)
            {
                try
                {
                    // 转换字符为虚拟键码
                    short vkCode = WindowsApiHelper.GetVirtualKeyCode(c);
                    if (vkCode == -1)
                    {
                        LogService.LogWarning($"SendText: 无法转换字符 '{c}'，跳过");
                        continue;
                    }
                    
                    byte keyCode = (byte)(vkCode & 0xFF);
                    byte shiftState = (byte)((vkCode >> 8) & 0xFF);
                    
                    // 获取扫描码
                    uint scanCode = WindowsApiHelper.GetScanCode(keyCode);
                    
                    // 检查是否需要按Shift键
                    bool needShift = (shiftState & 0x01) != 0;
                    
                    if (needShift)
                    {
                        // 按下Shift
                        uint shiftScanCode = WindowsApiHelper.GetScanCode(KeyboardConstants.VK_SHIFT);
                        WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_SHIFT, (byte)shiftScanCode, 0, 0);
                        await Task.Delay(10);
                    }
                    
                    // 按下键
                    WindowsApiHelper.SendKeyEvent(keyCode, (byte)scanCode, 0, 0);
                    await Task.Delay(TimingConstants.KEY_INPUT_DELAY);
                    
                    // 释放键
                    WindowsApiHelper.SendKeyEvent(keyCode, (byte)scanCode, KeyboardConstants.KEYEVENTF_KEYUP, 0);
                    await Task.Delay(TimingConstants.KEY_INPUT_DELAY);
                    
                    if (needShift)
                    {
                        // 释放Shift
                        uint shiftScanCode = WindowsApiHelper.GetScanCode(KeyboardConstants.VK_SHIFT);
                        WindowsApiHelper.SendKeyEvent(KeyboardConstants.VK_SHIFT, (byte)shiftScanCode, KeyboardConstants.KEYEVENTF_KEYUP, 0);
                        await Task.Delay(10);
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"SendText: 发送字符 '{c}' 失败: {ex.Message}");
                }
            }
            
            LogService.LogInfo("SendText: 文本发送完成");
        }
    }
}
