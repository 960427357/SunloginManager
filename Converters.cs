using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace SunloginManager
{
    /// <summary>
    /// 将布尔值转换为状态文本
    /// </summary>
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "已启用" : "已禁用";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将布尔值转换为颜色
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? 
                    new SolidColorBrush(WpfColor.FromRgb(52, 199, 89)) :  // 绿色
                    new SolidColorBrush(WpfColor.FromRgb(142, 142, 147)); // 灰色
            }
            return new SolidColorBrush(WpfColor.FromRgb(142, 142, 147)); // 默认灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}