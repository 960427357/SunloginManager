using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SunloginManager.Constants;
using SunloginManager.Helpers;
using SunloginManager.Models;

namespace SunloginManager.Services
{
    /// <summary>
    /// 向日葵服务类
    /// </summary>
    public class SunloginService
    {
        private string _sunloginPath;
        private readonly DataService _dataService;

        public SunloginService(DataService dataService = null)
        {
            LogService.LogInfo("初始化 SunloginService");

            _dataService = dataService ?? new DataService();

            // 加载向日葵路径
            LoadSunloginPath();
        }

        #region 公共方法

        /// <summary>
        /// 检查向日葵是否已安装
        /// </summary>
        public bool IsSunloginInstalled()
        {
            return !string.IsNullOrEmpty(_sunloginPath) && File.Exists(_sunloginPath);
        }

        /// <summary>
        /// 获取向日葵路径
        /// </summary>
        public string GetSunloginPath()
        {
            return _sunloginPath;
        }

        /// <summary>
        /// 设置自定义向日葵路径
        /// </summary>
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

        /// <summary>
        /// 连接到远程主机
        /// </summary>
        public async Task<bool> ConnectToRemoteAsync(RemoteConnection connection)
        {
            LogService.LogInfo($"开始连接到远程主机：{connection.Name} (ID: {connection.Id})");
            LogService.LogInfo($"连接详情 - 识别码：{connection.IdentificationCode}, 连接码：{connection.ConnectionCode}");

            if (!IsSunloginInstalled())
            {
                LogService.LogError("向日葵客户端路径无效或不存在");
                return false;
            }

            try
            {
                // 启动向日葵客户端
                if (!await StartSunloginClientAsync())
                {
                    return false;
                }

                // 等待界面加载
                await Task.Delay(TimingConstants.WINDOW_LOAD_DELAY);

                // 自动输入识别码和连接码
                bool success = await AutoInputCodesAsync(connection);

                if (success)
                {
                    // 更新最后连接时间
                    connection.LastConnectedAt = DateTime.Now;
                    LogService.LogInfo($"连接成功，已更新最后连接时间：{connection.LastConnectedAt}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogService.LogError($"连接失败：{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 连接并返回连接会话进程用于时长监控
        /// </summary>
        public async Task<(bool Success, Process? SessionProcess)> ConnectWithMonitoringAsync(RemoteConnection connection)
        {
            LogService.LogInfo($"开始连接到远程主机（监控模式）：{connection.Name}");

            if (!IsSunloginInstalled())
            {
                LogService.LogError("向日葵客户端路径无效或不存在");
                return (false, null);
            }

            try
            {
                // 启动向日葵客户端
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _sunloginPath,
                    UseShellExecute = true
                };
                await Task.Run(() => Process.Start(startInfo));

                // 等待界面加载
                await Task.Delay(TimingConstants.WINDOW_LOAD_DELAY);

                // 自动输入识别码和连接码
                bool inputSuccess = await AutoInputCodesAsync(connection);

                if (!inputSuccess)
                {
                    LogService.LogError("自动输入识别码和连接码失败");
                    return (false, null);
                }

                // 更新最后连接时间
                connection.LastConnectedAt = DateTime.Now;
                LogService.LogInfo($"连接成功，已更新最后连接时间：{connection.LastConnectedAt}");

                // 等待连接窗口出现（确认连接已建立）
                LogService.LogInfo("等待远程连接窗口出现...");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                Process? sessionProc = null;
                while (sw.ElapsedMilliseconds < 30000)
                {
                    sessionProc = WindowManagerHelper.FindRemoteConnectionProcess(connection.IdentificationCode, timeoutMs: 3000);
                    if (sessionProc != null)
                    {
                        LogService.LogInfo($"找到连接会话进程: PID={sessionProc.Id}, 名称={sessionProc.ProcessName}");
                        break;
                    }
                    await Task.Delay(1000);
                }

                if (sessionProc == null)
                {
                    LogService.LogWarning("未找到连接会话进程（30秒超时）");
                    // 备用方案：找到最新的向日葵进程
                    var procName = Path.GetFileNameWithoutExtension(_sunloginPath);
                    var procs = Process.GetProcessesByName(procName);
                    sessionProc = procs.OrderByDescending(p => p.StartTime).FirstOrDefault();
                    LogService.LogWarning($"使用备用进程: {sessionProc?.ProcessName}, PID={sessionProc?.Id}");
                }

                return (true, sessionProc);
            }
            catch (Exception ex)
            {
                LogService.LogError($"连接失败：{ex.Message}", ex);
                return (false, null);
            }
        }

        /// <summary>
        /// 测试连接：检查客户端是否存在、识别码格式是否正确
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync(RemoteConnection connection)
        {
            // 1. 检查向日葵客户端是否存在
            if (!IsSunloginInstalled())
            {
                return (false, "向日葵客户端未找到，请在设置中配置路径");
            }

            // 2. 检查识别码格式
            if (string.IsNullOrWhiteSpace(connection.IdentificationCode))
            {
                return (false, "识别码为空");
            }

            string code = connection.IdentificationCode.Trim();
            if (!code.All(char.IsDigit))
            {
                return (false, "识别码格式错误：应为纯数字");
            }

            if (code.Length < 4 || code.Length > 12)
            {
                return (false, $"识别码长度异常：当前 {code.Length} 位，应为 4-12 位");
            }

            // 3. 检查向日葵进程是否响应
            bool isRunning = false;
            try
            {
                var procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_sunloginPath));
                isRunning = procs.Length > 0;
            }
            catch { }

            if (isRunning)
            {
                return (true, "连接测试通过（向日葵正在运行）");
            }
            else
            {
                return (true, "识别码格式正确（向日葵未运行，连接时将自动启动）");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载向日葵路径
        /// </summary>
        private void LoadSunloginPath()
        {
            // 首先尝试从设置中加载路径
            _sunloginPath = _dataService.LoadSunloginPath();
            LogService.LogInfo($"从设置加载的向日葵路径：{_sunloginPath}");

            // 如果保存的路径存在，直接使用
            if (!string.IsNullOrEmpty(_sunloginPath) && File.Exists(_sunloginPath))
            {
                LogService.LogInfo($"已验证向日葵路径有效：{_sunloginPath}");
                return;
            }

            // 如果没有保存的路径或路径无效，尝试常见的向日葵安装路径
            LogService.LogInfo("保存的路径无效，搜索常见安装路径...");
            string[] possiblePaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Oray", "SunLogin", "SunloginClient.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Oray", "SunLogin", "SunloginClient.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "SunloginClient", "SunloginClient.exe"),
                @"C:\Program Files\Oray\AweSun\flutter\AweSun.exe"
            };

            string foundPath = possiblePaths.FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(foundPath))
            {
                _sunloginPath = foundPath;
                LogService.LogInfo($"搜索到向日葵路径：{_sunloginPath}");
                _dataService.SaveSunloginPath(_sunloginPath);
                LogService.LogInfo("已保存向日葵路径到设置");
            }
            else
            {
                LogService.LogWarning("未找到向日葵客户端，请手动配置路径");
            }
        }

        /// <summary>
        /// 启动向日葵客户端
        /// </summary>
        private async Task<bool> StartSunloginClientAsync()
        {
            try
            {
                LogService.LogInfo("启动向日葵客户端");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _sunloginPath,
                    UseShellExecute = true
                };

                Process process = await Task.Run(() => Process.Start(startInfo));

                if (process != null)
                {
                    LogService.LogInfo($"向日葵进程已启动，PID: {process.Id}");
                    return true;
                }

                LogService.LogError("启动向日葵失败");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"启动向日葵时出错：{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 自动输入识别码和连接码
        /// </summary>
        private async Task<bool> AutoInputCodesAsync(RemoteConnection connection)
        {
            LogService.LogInfo("===== 开始自动输入识别码和连接码 =====");
            LogService.LogInfo($"目标识别码：{connection.IdentificationCode}");
            LogService.LogInfo($"目标连接码：{connection.ConnectionCode}");

            try
            {
                // 查找向日葵窗口
                IntPtr sunloginWindow = WindowManagerHelper.FindSunloginWindow();
                if (sunloginWindow == IntPtr.Zero)
                {
                    LogService.LogError("无法找到向日葵窗口");
                    return false;
                }

                // 激活窗口
                if (!await WindowManagerHelper.ActivateWindowAsync(sunloginWindow))
                {
                    LogService.LogWarning("窗口激活可能失败");
                }

                // 输入识别码
                if (!string.IsNullOrEmpty(connection.IdentificationCode))
                {
                    await InputIdentificationCodeAsync(connection.IdentificationCode);
                }

                // 切换到连接码输入框
                await SwitchToConnectionCodeInputAsync(sunloginWindow);

                // 输入连接码
                if (!string.IsNullOrEmpty(connection.ConnectionCode))
                {
                    await InputConnectionCodeAsync(connection.ConnectionCode);
                }

                // 按回车确认
                await KeyboardInputHelper.SendEnterKeyAsync();
                LogService.LogInfo("回车键发送成功");

                // 处理验证码（如果需要）
                await HandleVerificationCodeAsync(connection, sunloginWindow);

                await Task.Delay(1000);

                LogService.LogInfo("===== 自动输入完成 =====");
                return true;
            }
            catch (Exception ex)
            {
                LogService.LogError($"自动输入时出错：{ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 输入识别码
        /// </summary>
        private async Task InputIdentificationCodeAsync(string identificationCode)
        {
            LogService.LogInfo($"开始输入识别码：{identificationCode}");

            // 清空输入框
            await KeyboardInputHelper.ClearInputAsync();

            // 通过剪贴板粘贴输入识别码（比逐字符快）
            await KeyboardInputHelper.SendTextViaPasteAsync(identificationCode);
            LogService.LogInfo("识别码输入完成");

            await Task.Delay(TimingConstants.INPUT_COMPLETE_DELAY);
        }

        /// <summary>
        /// 切换到连接码输入框
        /// </summary>
        private async Task SwitchToConnectionCodeInputAsync(IntPtr sunloginWindow)
        {
            LogService.LogInfo("按 Tab 键切换到连接码输入框");

            // 发送 Tab 键切换焦点
            await KeyboardInputHelper.SendTabKeyAsync();
            await Task.Delay(TimingConstants.TAB_KEY_DELAY);

            // 确保窗口在前台
            await WindowManagerHelper.ForceActivateWindowAsync(sunloginWindow);

            LogService.LogInfo("Tab 键已发送，等待焦点切换");
        }

        /// <summary>
        /// 输入连接码
        /// </summary>
        private async Task InputConnectionCodeAsync(string connectionCode)
        {
            LogService.LogInfo($"开始输入连接码：{connectionCode}");

            // 等待焦点切换
            await Task.Delay(TimingConstants.INPUT_COMPLETE_DELAY);

            // 清空输入框
            await KeyboardInputHelper.ClearInputAsync();

            // 通过剪贴板粘贴输入连接码（比逐字符快）
            await KeyboardInputHelper.SendTextViaPasteAsync(connectionCode);
            LogService.LogInfo("连接码输入完成");

            await Task.Delay(TimingConstants.INPUT_COMPLETE_DELAY);
        }

        /// <summary>
        /// 处理验证码输入
        /// </summary>
        private async Task HandleVerificationCodeAsync(RemoteConnection connection, IntPtr sunloginWindow)
        {
            // 等待验证码界面出现
            LogService.LogInfo("等待验证码界面出现...");
            await Task.Delay(TimingConstants.VERIFICATION_WAIT_DELAY);

            // 简单检查（大多数情况不需要验证码）
            bool needsVerificationCode = await CheckIfVerificationCodeNeededAsync(sunloginWindow);

            if (needsVerificationCode && !string.IsNullOrEmpty(connection.VerificationCode))
            {
                LogService.LogInfo("检测到需要输入验证码");
                await InputVerificationCodeAsync(connection.VerificationCode);
            }
            else
            {
                LogService.LogInfo("未检测到需要输入验证码");
            }
        }

        /// <summary>
        /// 检查是否需要输入验证码
        /// </summary>
        private async Task<bool> CheckIfVerificationCodeNeededAsync(IntPtr mainWindowHandle)
        {
            try
            {
                LogService.LogInfo("检查是否需要输入验证码");
                await Task.Delay(1000);

                // 简化版：大多数情况下不需要验证码
                LogService.LogInfo("验证码检查完成");
                return false;
            }
            catch (Exception ex)
            {
                LogService.LogError($"检查验证码时出错：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 输入验证码
        /// </summary>
        private async Task InputVerificationCodeAsync(string verificationCode)
        {
            LogService.LogInfo($"使用配置的验证码：{verificationCode}");

            // 等待验证码界面加载
            await Task.Delay(TimingConstants.VERIFICATION_LOAD_DELAY);

            // 发送 Tab 键切换到验证码输入框
            LogService.LogInfo("发送 Tab 键切换到验证码输入框");
            for (int i = 0; i < 2; i++)
            {
                await KeyboardInputHelper.SendTabKeyAsync();
                await Task.Delay(300);
            }

            await Task.Delay(TimingConstants.INPUT_COMPLETE_DELAY);

            // 清空并输入验证码
            LogService.LogInfo("开始输入验证码");
            await KeyboardInputHelper.ClearInputAsync();
            await KeyboardInputHelper.SendTextAsync(verificationCode);
            LogService.LogInfo("验证码输入完成");

            await Task.Delay(TimingConstants.INPUT_COMPLETE_DELAY);

            // 按回车确认
            LogService.LogInfo("按回车键确认验证码");
            await KeyboardInputHelper.SendEnterKeyAsync();
            LogService.LogInfo("验证码确认回车键发送成功");
        }

        #endregion
    }
}
