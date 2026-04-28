namespace SunloginManager.Constants
{
    /// <summary>
    /// 时间延迟相关常量（单位：毫秒）
    /// </summary>
    public static class TimingConstants
    {
        // 窗口操作延迟
        public const int WINDOW_ACTIVATION_DELAY = 100;
        public const int WINDOW_LOAD_DELAY = 1500;

        // 键盘输入延迟
        public const int KEY_PRESS_DELAY = 10;
        public const int KEY_INPUT_DELAY = 10;
        public const int TAB_KEY_DELAY = 200;

        // 输入框操作延迟
        public const int CLEAR_INPUT_DELAY = 100;
        public const int INPUT_COMPLETE_DELAY = 100;

        // 验证码相关延迟
        public const int VERIFICATION_WAIT_DELAY = 1500;
        public const int VERIFICATION_LOAD_DELAY = 500;

        // 线程操作延迟
        public const int THREAD_ATTACH_DELAY = 100;
        public const int THREAD_OPERATION_DELAY = 50;
    }
}
