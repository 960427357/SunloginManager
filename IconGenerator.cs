using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace SunloginManager
{
    public class IconGenerator
    {
        public static void GenerateIcon()
        {
            try
            {
                // 创建一个64x64的位图
                using (Bitmap bitmap = new Bitmap(64, 64))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        // 设置高质量渲染
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        
                        // 绘制背景
                        graphics.Clear(Color.FromArgb(0, 120, 215)); // 蓝色背景
                        
                        // 绘制一个简单的太阳图标
                        graphics.FillEllipse(new SolidBrush(Color.White), 15, 15, 34, 34);
                        
                        // 绘制太阳光线
                        Pen pen = new Pen(Color.White, 3);
                        for (int i = 0; i < 8; i++)
                        {
                            double angle = i * Math.PI / 4;
                            int x1 = 32 + (int)(25 * Math.Cos(angle));
                            int y1 = 32 + (int)(25 * Math.Sin(angle));
                            int x2 = 32 + (int)(30 * Math.Cos(angle));
                            int y2 = 32 + (int)(30 * Math.Sin(angle));
                            graphics.DrawLine(pen, x1, y1, x2, y2);
                        }
                        
                        // 绘制一个连接符号
                        graphics.DrawEllipse(new Pen(Color.FromArgb(0, 120, 215), 2), 25, 25, 14, 14);
                        graphics.DrawEllipse(new Pen(Color.FromArgb(0, 120, 215), 2), 25, 35, 14, 14);
                        graphics.DrawLine(new Pen(Color.FromArgb(0, 120, 215), 2), 32, 32, 32, 42);
                    }
                    
                    // 保存为ICO文件
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sunloginManager.ico");
                    
                    // 使用更可靠的方法创建图标
                    using (Icon icon = CreateIcon(bitmap))
                    {
                        using (FileStream stream = new FileStream(iconPath, FileMode.Create))
                        {
                            icon.Save(stream);
                        }
                    }
                    
                    // 同时保存到项目目录
                    string projectIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "sunloginManager.ico");
                    using (Icon icon = CreateIcon(bitmap))
                    {
                        using (FileStream stream = new FileStream(projectIconPath, FileMode.Create))
                        {
                            icon.Save(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"生成图标时出错: {ex.Message}");
            }
        }
        
        private static Icon CreateIcon(Bitmap bitmap)
        {
            // 直接从位图创建图标
            return Icon.FromHandle(bitmap.GetHicon());
        }
    }
}