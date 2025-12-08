using System;
using System.IO;

namespace SunloginManager.Services
{
    /// <summary>
    /// 日志服务类，用于记录应用程序运行日志
    /// </summary>
    public static class LogService
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, $"SunloginManager_{DateTime.Now:yyyyMMdd}.log");

        static LogService()
        {
            // 确保日志目录存在
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常对象</param>
        public static void LogError(string message, Exception exception = null)
        {
            string fullMessage = exception != null ? $"{message}\n异常详情: {exception}" : message;
            WriteLog("ERROR", fullMessage);
        }

        /// <summary>
        /// 写入日志到文件
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        private static void WriteLog(string level, string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                
                // 写入文件
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                
                // 同时输出到控制台（如果可用）
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // 如果日志记录失败，至少尝试输出到控制台
                Console.WriteLine($"日志记录失败: {ex.Message}");
                Console.WriteLine($"原始日志: [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
            }
        }
    }
}