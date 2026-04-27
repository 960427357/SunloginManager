using System;

namespace SunloginManager.Models
{
    /// <summary>
    /// 连接分组模型类
    /// </summary>
    public class ConnectionGroup
    {
        /// <summary>
        /// 分组ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 分组描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 分组颜色（十六进制，如 #FF5733）
        /// </summary>
        public string Color { get; set; } = "#007AFF";

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否为收藏分组
        /// </summary>
        public bool IsFavoriteGroup { get; set; }

        /// <summary>
        /// 是否为默认分组
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 是否受保护（不可删除）
        /// </summary>
        public bool IsProtected => IsDefault || IsFavoriteGroup;

        /// <summary>
        /// 重写ToString方法，返回分组名称
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
