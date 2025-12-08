using System;

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

        /// <summary>
        /// 连接码
        /// </summary>
        public string ConnectionCode { get; set; } = string.Empty;

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
        /// 重写ToString方法，返回连接名称
        /// </summary>
        /// <returns>连接名称</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}