// WpfApp1/Views/LocalModePickerWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1.Views
{
    public record PickerResult(string ActionType, string Target, string DisplayName);

    internal sealed class PickerItem
    {
        public string DisplayName { get; init; } = string.Empty;
        public string Icon       { get; init; } = "◈";
        public string ActionType { get; set; }  = string.Empty;
        public string Target     { get; set; }  = string.Empty;
    }

    public partial class LocalModePickerWindow : Window
    {
        public PickerResult? Result { get; private set; }

        private readonly List<PickerItem> _functions;
        private readonly List<PickerItem> _apps;
        private bool _openMode = true;

        public LocalModePickerWindow(int slotIndex = 0)
        {
            InitializeComponent();
            SlotLabel.Text = $"Slot {slotIndex + 1}";
            _functions = BuildFunctions();
            _apps      = DiscoverApps();
            RefreshLists(string.Empty);
            SearchBox.Focus();
        }

        // ── Data builders ────────────────────────────────────────────────────

        private static List<PickerItem> BuildFunctions() => new()
        {
            new() { DisplayName = "Open Browser",      Icon = "🌐", ActionType = "function", Target = "open_browser" },
            new() { DisplayName = "Open File Manager", Icon = "📁", ActionType = "function", Target = "open_file_manager" },
            new() { DisplayName = "Open Calculator",   Icon = "🔢", ActionType = "function", Target = "open_calculator" },
            new() { DisplayName = "Open Notepad",      Icon = "📝", ActionType = "function", Target = "open_notepad" },
            new() { DisplayName = "Open Task Manager", Icon = "⚙️", ActionType = "function", Target = "open_task_manager" },
            new() { DisplayName = "Show Desktop",      Icon = "🖥️", ActionType = "function", Target = "show_desktop" },
            new() { DisplayName = "Lock Screen",       Icon = "🔒", ActionType = "function", Target = "lock_screen" },
            new() { DisplayName = "Mute / Unmute",     Icon = "🔇", ActionType = "function", Target = "toggle_mute" },
            new() { DisplayName = "Volume Up",         Icon = "🔊", ActionType = "function", Target = "volume_up" },
            new() { DisplayName = "Volume Down",       Icon = "🔉", ActionType = "function", Target = "volume_down" },
            new() { DisplayName = "Play / Pause",      Icon = "⏯",  ActionType = "function", Target = "play_pause" },
            new() { DisplayName = "Next Track",        Icon = "⏭",  ActionType = "function", Target = "next_track" },
            new() { DisplayName = "Take Screenshot",   Icon = "📸", ActionType = "function", Target = "screenshot" },
            new() { DisplayName = "Open Settings",     Icon = "⚙",  ActionType = "function", Target = "open_settings" },
        };

        private static List<PickerItem> DiscoverApps()
        {
            var apps = new List<PickerItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var dirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            };

            foreach (var dir in dirs.Where(Directory.Exists))
            {
                foreach (var lnk in Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(lnk);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (name.Contains("uninstall", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("update",    StringComparison.OrdinalIgnoreCase)) continue;
                    if (!seen.Add(name)) continue;

                    apps.Add(new PickerItem { DisplayName = name, Icon = "◈", Target = lnk });
                }
            }

            apps.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName, b.DisplayName));
            return apps;
        }

        // ── Filtering ────────────────────────────────────────────────────────

        private void RefreshLists(string search)
        {
            search = search.Trim();
            bool filtered = !string.IsNullOrEmpty(search);

            var funcs = (filtered
                ? _functions.Where(f => f.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                : (IEnumerable<PickerItem>)_functions).ToList();

            FunctionsList.ItemsSource = funcs;
            FunctionsSectionPanel.Visibility = funcs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            string actionType = _openMode ? "open_app" : "close_app";
            var filteredApps = (filtered
                ? _apps.Where(a => a.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                : (IEnumerable<PickerItem>)_apps)
                .Select(a => new PickerItem { DisplayName = a.DisplayName, Icon = a.Icon,
                                              Target = a.Target, ActionType = actionType })
                .ToList();

            AppsList.ItemsSource = filteredApps;
            AppsSectionPanel.Visibility = filteredApps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Commit(PickerItem item)
        {
            Result = new PickerResult(item.ActionType, item.Target, item.DisplayName);
            DialogResult = true;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Single-click confirm
            if (sender is ListBox lb && lb.SelectedItem is PickerItem item)
                Commit(item);
        }

        private void List_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is PickerItem item)
                Commit(item);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
            RefreshLists(SearchBox.Text);
        }

        private void OpenTab_Click(object sender, RoutedEventArgs e)
        {
            _openMode = true;
            CloseTab.IsChecked = false;
            OpenTab.IsChecked  = true;
            RefreshLists(SearchBox.Text);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            _openMode = false;
            OpenTab.IsChecked  = false;
            CloseTab.IsChecked = true;
            RefreshLists(SearchBox.Text);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}
