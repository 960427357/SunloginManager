namespace SunloginManager.Constants
{
    /// <summary>
    /// 键盘相关常量
    /// </summary>
    public static class KeyboardConstants
    {
        // 虚拟键码
        public const byte VK_TAB = 0x09;
        public const byte VK_RETURN = 0x0D;
        public const byte VK_CONTROL = 0x11;
        public const byte VK_SHIFT = 0x10;
        public const byte VK_DELETE = 0x2E;
        public const byte VK_A = 0x41;
        
        // 键盘事件标志
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        
        // MapVirtualKey 映射类型
        public const uint MAPVK_VK_TO_VSC = 0;
        public const uint MAPVK_VSC_TO_VK = 1;
    }
}
