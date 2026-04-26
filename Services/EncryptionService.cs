using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SunloginManager.Services
{
    /// <summary>
    /// 加密服务类，用于加密和解密敏感数据
    /// </summary>
    public class EncryptionService
    {
        // 使用机器特定的密钥，每台机器的加密结果不同
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("SunloginManager_Entropy_Key_2024");

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>加密后的Base64字符串</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                LogService.LogError($"加密失败: {ex.Message}", ex);
                return plainText; // 加密失败时返回原文
            }
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="encryptedText">加密的Base64字符串</param>
        /// <returns>解密后的明文</returns>
        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                LogService.LogError($"解密失败: {ex.Message}", ex);
                return encryptedText; // 解密失败时返回原文（可能是未加密的旧数据）
            }
        }

        /// <summary>
        /// 判断字符串是否已加密
        /// </summary>
        /// <param name="text">要检查的字符串</param>
        /// <returns>是否已加密</returns>
        public static bool IsEncrypted(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                // 尝试Base64解码，如果成功则可能是加密的
                byte[] data = Convert.FromBase64String(text);
                
                // 尝试解密，如果成功则确认是加密的
                ProtectedData.Unprotect(data, _entropy, DataProtectionScope.CurrentUser);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 加密文件中的敏感数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void EncryptFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                byte[] encryptedBytes = ProtectedData.Protect(fileBytes, _entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(filePath, encryptedBytes);
                LogService.LogInfo($"文件已加密: {filePath}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"文件加密失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void DecryptFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(filePath);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(filePath, decryptedBytes);
                LogService.LogInfo($"文件已解密: {filePath}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"文件解密失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 生成随机盐（32 字节，Base64 编码）
        /// </summary>
        public static string GenerateSalt()
        {
            byte[] salt = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// 使用 SHA256 + Salt 哈希密码
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string salt)
        {
            string computedHash = HashPassword(password, salt);
            return computedHash == storedHash;
        }
    }
}
