using System;
using System.Collections.ObjectModel;
using System.Windows;
using SunloginManager.Models;
using SunloginManager.Services;

namespace SunloginManager
{
    public partial class ManageGroupsDialog : Window
    {
        private readonly DataService _dataService;
        public ObservableCollection<ConnectionGroup> Groups { get; set; }

        public ManageGroupsDialog()
        {
            InitializeComponent();
            _dataService = new DataService();
            Groups = new ObservableCollection<ConnectionGroup>();
            
            LoadGroups();
            GroupsListView.ItemsSource = Groups;
        }

        private void LoadGroups()
        {
            Groups.Clear();
            var groups = _dataService.GetAllGroups();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditGroupDialog
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {
                var newGroup = dialog.GetGroup();
                if (newGroup != null)
                {
                    _dataService.SaveGroup(newGroup);
                    LoadGroups();
                }
            }
        }

        private void EditGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is ConnectionGroup group)
            {
                var dialog = new EditGroupDialog(group)
                {
                    Owner = this
                };
                if (dialog.ShowDialog() == true)
                {
                    var updatedGroup = dialog.GetGroup();
                    if (updatedGroup != null)
                    {
                        _dataService.UpdateGroup(updatedGroup);
                        LoadGroups();
                    }
                }
            }
        }

        private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is ConnectionGroup group)
            {
                var result = System.Windows.MessageBox.Show(
                    $"确定要删除分组 '{group.Name}' 吗？\n该分组下的所有连接将移到默认分组。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _dataService.DeleteGroup(group.Id);
                    LoadGroups();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
