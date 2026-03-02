using System;
using System.Text.Json.Serialization;
using SunloginManager.Services;

namespace SunloginManager.Models
{
    /// <summary>
    /// 远程连接模型类
    /// </summary>
    public class RemoteConnection
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 识别码
        /// </summary>
        public string IdentificationCode { get; set; } = string.Empty;

        private string _connectionCode = string.Empty;
        
        /// <summary>
        /// 连接码（加密存储）
        /// </summary>
        [JsonPropertyName("connectionCode")]
        public string ConnectionCode
        {
            get
            {
                // 如果是加密的，解密后返回
                if (!string.IsNullOrEmpty(_connectionCode) && EncryptionService.IsEncrypted(_connectionCode))
                {
                    return EncryptionService.Decrypt(_connectionCode);
                }
                return _connectionCode;
            }
            set
            {
                // 存储时加密
                if (!string.IsNullOrEmpty(value))
                {
                    _connectionCode = EncryptionService.Encrypt(value);
                }
                else
                {
                    _connectionCode = value;
                }
            }
        }

        /// <summary>
        /// 加密的连接码（用于JSON序列化）
        /// </summary>
        [JsonPropertyName("encryptedConnectionCode")]
        public string EncryptedConnectionCode
        {
            get => _connectionCode;
            set => _connectionCode = value;
        }

        /// <summary>
        /// 验证码
        /// </summary>
        public string VerificationCode { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后连接时间
        /// </summary>
        public DateTime LastConnectedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 所属分组ID（0表示未分组）
        /// </summary>
        public int GroupId { get; set; } = 0;

        /// <summary>
        /// 重写ToString方法，返回连接名称
        /// </summary>
        /// <returns>连接名称</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}