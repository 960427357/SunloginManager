using System.Windows.Input;

namespace SunloginManager.Models
{
    /// <summary>
    /// 单个快捷键配置
    /// </summary>
    public class KeyboardShortcutConfig
    {
        public string ActionName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Modifiers { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        public static KeyboardShortcutConfig CreateDefault(string actionName, string displayName, string description, Key key, ModifierKeys modifiers = ModifierKeys.None)
        {
            return new KeyboardShortcutConfig
            {
                ActionName = actionName,
                DisplayName = displayName,
                Description = description,
                Modifiers = modifiers.ToString(),
                Key = key.ToString(),
                IsEnabled = true
            };
        }
    }

    /// <summary>
    /// 所有快捷键配置
    /// </summary>
    public class ShortcutsSettings
    {
        public KeyboardShortcutConfig CopyIdentificationCode { get; set; } =
            KeyboardShortcutConfig.CreateDefault("CopyIdentificationCode", "复制识别码", "将当前连接的识别码复制到剪贴板", Key.F1);
        public KeyboardShortcutConfig CopyConnectionCode { get; set; } =
            KeyboardShortcutConfig.CreateDefault("CopyConnectionCode", "复制连接码", "将当前连接的连接码（密码）复制到剪贴板", Key.F2);
        public KeyboardShortcutConfig CopyRemarks { get; set; } =
            KeyboardShortcutConfig.CreateDefault("CopyRemarks", "复制备注", "将当前连接的备注信息复制到剪贴板", Key.F3);
        public KeyboardShortcutConfig CopyAllInfo { get; set; } =
            KeyboardShortcutConfig.CreateDefault("CopyAllInfo", "复制全部信息", "将当前连接的所有信息格式化后复制到剪贴板", Key.F4);

        public KeyboardShortcutConfig[] GetAll()
        {
            return new[] { CopyIdentificationCode, CopyConnectionCode, CopyRemarks, CopyAllInfo };
        }

        public ShortcutsSettings Clone()
        {
            return new ShortcutsSettings
            {
                CopyIdentificationCode = new KeyboardShortcutConfig { ActionName = CopyIdentificationCode.ActionName, DisplayName = CopyIdentificationCode.DisplayName, Description = CopyIdentificationCode.Description, Modifiers = CopyIdentificationCode.Modifiers, Key = CopyIdentificationCode.Key, IsEnabled = CopyIdentificationCode.IsEnabled },
                CopyConnectionCode = new KeyboardShortcutConfig { ActionName = CopyConnectionCode.ActionName, DisplayName = CopyConnectionCode.DisplayName, Description = CopyConnectionCode.Description, Modifiers = CopyConnectionCode.Modifiers, Key = CopyConnectionCode.Key, IsEnabled = CopyConnectionCode.IsEnabled },
                CopyRemarks = new KeyboardShortcutConfig { ActionName = CopyRemarks.ActionName, DisplayName = CopyRemarks.DisplayName, Description = CopyRemarks.Description, Modifiers = CopyRemarks.Modifiers, Key = CopyRemarks.Key, IsEnabled = CopyRemarks.IsEnabled },
                CopyAllInfo = new KeyboardShortcutConfig { ActionName = CopyAllInfo.ActionName, DisplayName = CopyAllInfo.DisplayName, Description = CopyAllInfo.Description, Modifiers = CopyAllInfo.Modifiers, Key = CopyAllInfo.Key, IsEnabled = CopyAllInfo.IsEnabled }
            };
        }
    }
}
