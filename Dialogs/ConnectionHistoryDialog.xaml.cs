using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SunloginManager.Models;
using SunloginManager.Services;

namespace SunloginManager
{
    public partial class ConnectionHistoryDialog : Window
    {
        private readonly HistoryService _historyService;
        private List<HistoryItem> _allItems;

        public ConnectionHistoryDialog(HistoryService historyService)
        {
            InitializeComponent();
            _historyService = historyService;
            LoadHistory(0);
        }

        private void LoadHistory(int days)
        {
            var entries = days == 0 ? _historyService.GetAll() : _historyService.GetRecent(days);
            _allItems = new List<HistoryItem>();
            foreach (var e in entries)
            {
                _allItems.Add(new HistoryItem(e));
            }
            HistoryListView.ItemsSource = _allItems;
            CountText.Text = $"共 {_allItems.Count} 条记录";
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is int days)
            {
                LoadHistory(days);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class HistoryItem
    {
        public ConnectionHistory Entry { get; }
        public string ConnectionName => Entry.ConnectionName;
        public string IdentificationCode => Entry.IdentificationCode;
        public System.DateTime StartTime => Entry.StartTime;
        public string DurationText => Entry.Duration.HasValue ? FormatDuration(Entry.Duration.Value) : "—";
        public string StatusText => GetStatusText();
        public string ErrorMessage => Entry.ErrorMessage;

        public HistoryItem(ConnectionHistory entry) { Entry = entry; }

        private string GetStatusText()
        {
            return Entry.Status switch
            {
                ConnectionStatus.Success => "成功",
                ConnectionStatus.Failed => "失败",
                ConnectionStatus.ClientNotFound => "客户端未找到",
                ConnectionStatus.InvalidCode => "识别码无效",
                _ => "未知"
            };
        }

        private string FormatDuration(System.TimeSpan ts)
        {
            if (ts.TotalMinutes < 1) return $"{ts.Seconds}秒";
            if (ts.TotalHours < 1) return $"{ts.Minutes}分{ts.Seconds}秒";
            return $"{ts.Hours}时{ts.Minutes}分";
        }
    }
}
