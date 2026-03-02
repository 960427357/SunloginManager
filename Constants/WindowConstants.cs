namespace SunloginManager.Constants
{
    /// <summary>
    /// 窗口相关常量
    /// </summary>
    public static class WindowConstants
    {
        // ShowWindow 命令
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_RESTORE = 9;
        
        // 向日葵窗口标题
        public static readonly string[] SunloginWindowTitles = 
        { 
            "向日葵远程控制", 
            "向日葵", 
            "Sunlogin", 
            "向日葵个人版", 
            "向日葵企业版", 
            "AweSun" 
        };
        
        // 向日葵进程名
        public static readonly string[] SunloginProcessNames = 
        { 
            "AweSun", 
            "SunloginClient" 
        };
    }
}
