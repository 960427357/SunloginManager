using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunloginManager.Models;
using SunloginManager.Services;

namespace SunloginManager
{
    public partial class ShortcutSettingsDialog : Window
    {
        private readonly DataService _dataService;
        private ShortcutsSettings _shortcuts;
        private ShortcutItem? _capturingItem;

        public ShortcutSettingsDialog()
        {
            InitializeComponent();
            _dataService = new DataService();
            _shortcuts = _dataService.GetShortcutsSettings();
            LoadShortcuts();
            this.PreviewKeyDown += ShortcutSettingsDialog_PreviewKeyDown;
        }

        private void LoadShortcuts()
        {
            var items = new List<ShortcutItem>();
            foreach (var s in _shortcuts.GetAll())
            {
                items.Add(new ShortcutItem
                {
                    ActionName = s.ActionName,
                    DisplayName = s.DisplayName,
                    Description = s.Description,
                    Key = s.Key,
                    Modifiers = s.Modifiers,
                    IsEnabled = s.IsEnabled,
                    KeyDisplay = FormatKeyDisplay(s)
                });
            }
            ShortcutListView.ItemsSource = items;
        }

        private string FormatKeyDisplay(KeyboardShortcutConfig s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(s.Modifiers) && s.Modifiers != "None")
                parts.Add(s.Modifiers.Replace(", ", "+"));
            parts.Add(s.Key);
            return string.Join(" + ", parts);
        }

        private void ShortcutSettingsDialog_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_capturingItem == null) return;

            e.Handled = true;

            // 忽略单独的修饰键
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LWin || e.Key == Key.RWin)
                return;

            _capturingItem.Key = e.Key.ToString();
            _capturingItem.Modifiers = Keyboard.Modifiers.ToString();
            _capturingItem.KeyDisplay = FormatKeyDisplayForItem(_capturingItem);

            StatusText.Text = $"已设置: {_capturingItem.DisplayName} → {_capturingItem.KeyDisplay}";
            _capturingItem = null;

            ShortcutListView.Items.Refresh();
        }

        private string FormatKeyDisplayForItem(ShortcutItem item)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(item.Modifiers) && item.Modifiers != "None")
                parts.Add(item.Modifiers.Replace(", ", "+"));
            parts.Add(item.Key);
            return string.Join(" + ", parts);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ShortcutItem item)
            {
                _capturingItem = item;
                StatusText.Text = $"正在设置「{item.DisplayName}」... 请按下新的快捷键";
                ShortcutListView.Items.Refresh();
            }
        }

        private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            _shortcuts = new ShortcutsSettings();
            LoadShortcuts();
            StatusText.Text = "已恢复为默认快捷键设置";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查快捷键冲突
            var items = ShortcutListView.ItemsSource as List<ShortcutItem>;
            if (items != null)
            {
                var duplicates = items
                    .GroupBy(i => $"{i.Modifiers}|{i.Key}")
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .ToList();

                if (duplicates.Count > 0)
                {
                    System.Windows.MessageBox.Show("存在重复的快捷键设置，请修改后再保存。", "快捷键冲突",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 将修改后的设置保存回 ShortcutsSettings
                foreach (var item in items)
                {
                    var config = _shortcuts.GetAll().First(s => s.ActionName == item.ActionName);
                    config.Key = item.Key;
                    config.Modifiers = item.Modifiers;
                    config.IsEnabled = item.IsEnabled;
                }
            }

            _dataService.SaveShortcutsSettings(_shortcuts);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ShortcutItem
    {
        public string ActionName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Modifiers { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string KeyDisplay { get; set; } = string.Empty;
    }
}
