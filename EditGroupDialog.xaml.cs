using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SunloginManager.Models;

namespace SunloginManager
{
    public partial class EditGroupDialog : Window
    {
        private ConnectionGroup? _group;
        private readonly Dictionary<string, string> _colorMap;

        public EditGroupDialog(ConnectionGroup? group = null)
        {
            InitializeComponent();
            
            _group = group;
            
            // 初始化颜色映射
            _colorMap = new Dictionary<string, string>
            {
                { "#007AFF", "蓝色" },
                { "#34C759", "绿色" },
                { "#FF9500", "橙色" },
                { "#FF3B30", "红色" },
                { "#AF52DE", "紫色" },
                { "#FF2D55", "粉色" },
                { "#5AC8FA", "青色" },
                { "#FFCC00", "黄色" }
            };
            
            // 填充颜色选择器
            foreach (var colorHex in _colorMap.Keys)
            {
                ColorComboBox.Items.Add(colorHex);
            }
            
            if (_group != null)
            {
                Title = "编辑分组";
                NameTextBox.Text = _group.Name;
                DescriptionTextBox.Text = _group.Description;
                
                // 选择对应的颜色
                int colorIndex = 0;
                int i = 0;
                foreach (var colorHex in _colorMap.Keys)
                {
                    if (colorHex == _group.Color)
                    {
                        colorIndex = i;
                        break;
                    }
                    i++;
                }
                ColorComboBox.SelectedIndex = colorIndex;
            }
            else
            {
                Title = "添加分组";
                ColorComboBox.SelectedIndex = 0;
            }
        }

        public ConnectionGroup? GetGroup()
        {
            return _group;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                System.Windows.MessageBox.Show("请输入分组名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_group == null)
            {
                _group = new ConnectionGroup();
            }

            _group.Name = NameTextBox.Text.Trim();
            _group.Description = DescriptionTextBox.Text.Trim();
            _group.Color = ColorComboBox.SelectedItem?.ToString() ?? "#007AFF";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
