using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using SunloginManager.Services;

namespace SunloginManager
{
    public partial class ConnectionStatsDialog : Window
    {
        public ConnectionStatsDialog(ConnectionStats stats)
        {
            InitializeComponent();
            BuildStatsUI(stats);
        }

        private void BuildStatsUI(ConnectionStats stats)
        {
            StatsPanel.Children.Clear();

            var overviewGrid = CreateCard("连接概览");
            AddStatRow(overviewGrid, "总连接次数", stats.TotalConnections.ToString());
            AddStatRow(overviewGrid, "成功次数", stats.SuccessCount.ToString(), "#34C759");
            AddStatRow(overviewGrid, "失败次数", stats.FailedCount.ToString(), stats.FailedCount > 0 ? "#FF3B30" : "#8E8E93");
            AddStatRow(overviewGrid, "成功率", stats.TotalConnections > 0 ? $"{(double)stats.SuccessCount / stats.TotalConnections * 100:F1}%" : "0%");
            if (stats.AverageDuration.HasValue)
                AddStatRow(overviewGrid, "平均时长", FormatDuration(stats.AverageDuration.Value));
            StatsPanel.Children.Add(overviewGrid);

            var recentGrid = CreateCard("近期统计");
            AddStatRow(recentGrid, "最近7天连接", $"{stats.Last7DaysCount} 次（成功 {stats.Last7DaysSuccess} 次）");
            AddStatRow(recentGrid, "最近30天连接", $"{stats.Last30DaysCount} 次（成功 {stats.Last30DaysSuccess} 次）");
            StatsPanel.Children.Add(recentGrid);

            if (stats.TopConnections.Count > 0)
            {
                var topGrid = CreateCard("最常使用的连接 TOP5");
                int rank = 1;
                foreach (var tc in stats.TopConnections)
                {
                    AddStatRow(topGrid, $"{rank}. {tc.Name}", $"{tc.Count} 次");
                    rank++;
                }
                StatsPanel.Children.Add(topGrid);
            }
        }

        private Grid CreateCard(string title)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(15, 10, 15, 5),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(26, 26, 26))
            };
            Grid.SetRow(titleBlock, 0);
            grid.Children.Add(titleBlock);

            var border = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 5, 15, 10)
            };
            var contentPanel = new StackPanel();
            border.Child = contentPanel;
            Grid.SetRow(border, 1);
            grid.Children.Add(border);

            grid.Tag = contentPanel;
            return grid;
        }

        private void AddStatRow(Grid grid, string label, string value, string? valueColor = null)
        {
            var panel = grid.Tag as StackPanel;
            if (panel == null) return;

            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelBlock = new TextBlock { Text = label, FontSize = 13, Foreground = new SolidColorBrush(WpfColor.FromRgb(142, 142, 147)) };
            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(valueColor != null ? (WpfColor)System.Windows.Media.ColorConverter.ConvertFromString(valueColor) : WpfColor.FromRgb(26, 26, 26))
            };
            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(valueBlock, 1);
            row.Children.Add(labelBlock);
            row.Children.Add(valueBlock);
            panel.Children.Add(row);
        }

        private string FormatDuration(System.TimeSpan ts)
        {
            if (ts.TotalMinutes < 1) return $"{ts.Seconds}秒";
            if (ts.TotalHours < 1) return $"{ts.Minutes}分{ts.Seconds}秒";
            return $"{ts.Hours}时{ts.Minutes}分{ts.Seconds}秒";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
