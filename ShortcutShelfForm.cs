// ShortcutShelfForm.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace ShortcutShelf
{
    public partial class ShortcutShelfForm : Form
    {
        private const int FilterDelayMilliseconds = 200;
        private const int ClipboardDelayMilliseconds = 150;
        private const int MaxLogLines = 300;

        private List<ShortcutItem> _items = new List<ShortcutItem>();
        private readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly string _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shortcuts.json");
        private readonly System.Windows.Forms.Timer _filterTimer;
        private readonly System.Windows.Forms.Timer _clipboardTimer;
        private bool _isRebuildingViews;
        private int _logLineCount;
        private string? _pendingClipboardText;
        private string? _lastClipboardText;
        private string? _lastSelectedPath;

        public ShortcutShelfForm()
        {
            InitializeComponent();
            components ??= new System.ComponentModel.Container();

            _filterTimer = new System.Windows.Forms.Timer(components) { Interval = FilterDelayMilliseconds };
            _filterTimer.Tick += (_, _) =>
            {
                _filterTimer.Stop();
                ApplyFilter(txtFilter.Text);
            };

            _clipboardTimer = new System.Windows.Forms.Timer(components) { Interval = ClipboardDelayMilliseconds };
            _clipboardTimer.Tick += (_, _) =>
            {
                _clipboardTimer.Stop();
                CopyPendingPathToClipboard();
            };

            FormClosed += (_, _) => DisposeIconCache();
        }

        private void ShortcutShelfForm_Load(object sender, EventArgs e)
        {
            LoadShortcuts();
            // Add usage tips to log
            Log("📌 Press F1 to view usage tips.");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                ShowUsageTips();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowUsageTips()
        {
            string tips = string.Join(Environment.NewLine, new[]
            {
                "📌 ShortcutShelf Usage Tips:",
                "• Arrow keys = Select item",
                "• Enter / Double-click = Open item",
                "• Drag & Drop = Add shortcut",
                "• Right-click = Delete shortcut",
                "• Ctrl + Arrow = Move (reorder)",
                "• Filter box = Search by name or path"
            });
            MessageBox.Show(tips, "Usage Tips", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShortcutShelfForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveShortcuts();
        }

        private void LoadShortcuts()
        {
            if (File.Exists(_dataFile))
            {
                try
                {
                    var json = File.ReadAllText(_dataFile);
                    var loadedItems = JsonSerializer.Deserialize<List<ShortcutItem>>(json) ?? new List<ShortcutItem>();
                    _items = NormalizeShortcuts(loadedItems);
                }
                catch
                {
                    _items = new List<ShortcutItem>();
                    Log("Error loading JSON");
                }
            }
            RefreshViews();
        }

        private void SaveShortcuts()
        {
            try
            {
                var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
                var tempFile = _dataFile + ".tmp";
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, _dataFile, true);
            }
            catch
            {
                Log("Error saving JSON");
            }
        }

        private void RefreshViews()
        {
            RebuildViews(_items);
        }

        private void ApplyFilter(string keyword)
        {
            var trimmedKeyword = keyword?.Trim();
            var filtered = string.IsNullOrEmpty(trimmedKeyword)
                ? _items
                : _items.Where(i => MatchesFilter(i, trimmedKeyword)).ToList();

            RebuildViews(filtered);
        }

        private static List<ShortcutItem> NormalizeShortcuts(IEnumerable<ShortcutItem?> items)
        {
            var normalized = new List<ShortcutItem>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var path = item?.FullPath;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (!seenPaths.Add(path))
                    continue;

                normalized.Add(new ShortcutItem(path));
            }

            return normalized;
        }

        private static bool MatchesFilter(ShortcutItem item, string keyword)
        {
            return item.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                   item.FullPath.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshCurrentView()
        {
            ApplyFilter(txtFilter.Text);
        }

        private void RebuildViews(IReadOnlyList<ShortcutItem> visibleItems)
        {
            _isRebuildingViews = true;
            lbShortcuts.BeginUpdate();
            lvShortcuts.BeginUpdate();

            try
            {
                lbShortcuts.Items.Clear();
                lvShortcuts.Items.Clear();
                imageListLarge.Images.Clear();

                for (int i = 0; i < visibleItems.Count; i++)
                {
                    var item = visibleItems[i];
                    lbShortcuts.Items.Add(new BoxItem(item, i + 1));

                    var imageIndex = imageListLarge.Images.Count;
                    imageListLarge.Images.Add(GetIconImage(item.FullPath));

                    var lvi = new ListViewItem(item.Name)
                    {
                        Tag = item,
                        ImageIndex = imageIndex
                    };
                    lvShortcuts.Items.Add(lvi);
                }
            }
            finally
            {
                lvShortcuts.EndUpdate();
                lbShortcuts.EndUpdate();
                _isRebuildingViews = false;
            }
        }

        private void ListControl_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void LbShortcuts_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                AddShortcuts(paths);
        }

        private void LvShortcuts_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                AddShortcuts(paths);
        }

        private void AddShortcuts(IEnumerable<string> paths)
        {
            var existingPaths = new HashSet<string>(_items.Select(i => i.FullPath), StringComparer.OrdinalIgnoreCase);
            var added = 0;

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path) ||
                    existingPaths.Contains(path) ||
                    (!File.Exists(path) && !Directory.Exists(path)))
                    continue;

                _items.Add(new ShortcutItem(path));
                existingPaths.Add(path);
                added++;
            }

            if (added == 0)
                return;

            RefreshCurrentView();
            Log(added == 1 ? "Added 1 shortcut" : $"Added {added} shortcuts");
        }

        private void Control_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isRebuildingViews)
                return;

            var box = lbShortcuts.SelectedItem as BoxItem;
            UpdateSelection(box?.Item);
        }

        private void LvShortcuts_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!_isRebuildingViews && e.IsSelected)
                UpdateSelection(e.Item?.Tag as ShortcutItem);
        }

        private void UpdateSelection(ShortcutItem? item)
        {
            if (item == null) return;
            var path = item.FullPath;
            if (string.IsNullOrWhiteSpace(path))
                return;

            txtPath.Text = path;

            if (!string.Equals(_lastSelectedPath, path, StringComparison.OrdinalIgnoreCase))
            {
                _lastSelectedPath = path;
                Log($"Selected '{path}'");
                QueueClipboardCopy(path);
            }
        }

        private void LvShortcuts_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var info = lvShortcuts.HitTest(e.Location);
            if (info.Item != null)
                OpenItem(info.Item.Tag as ShortcutItem);
        }

        private void LbShortcuts_DoubleClick(object sender, EventArgs e)
        {
            var box = lbShortcuts.SelectedItem as BoxItem;
            if (box != null)
                OpenItem(box.Item);
        }

        private void OpenItem(ShortcutItem? item)
        {
            if (item == null) return;
            var target = item.FullPath;
            if (string.IsNullOrWhiteSpace(target))
                return;

            try
            {
                var folder = Directory.Exists(target)
                    ? target
                    : Path.GetDirectoryName(target);

                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    Log($"Path not found '{target}'");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
                Log($"Opened '{target}'");
            }
            catch
            {
                Log($"Error opening '{target}'");
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShortcutItem? item = null;
            if (lbShortcuts.Focused && lbShortcuts.SelectedItem is BoxItem box)
                item = box.Item;
            else if (lvShortcuts.Focused && lvShortcuts.SelectedItems.Count > 0)
                item = lvShortcuts.SelectedItems[0].Tag as ShortcutItem;

            if (item == null) return;
            _items.Remove(item);
            PruneIconCache();
            RefreshCurrentView();
            Log($"Deleted '{item.FullPath}'");
        }

        // Handle key events for the ListBox: ←/→ move to previous/next item, ↑/↓ move normally, Ctrl+Arrow for reordering
        private void LbShortcuts_KeyDown(object sender, KeyEventArgs e)
        {
            // Open item on Enter
            if (e.KeyCode == Keys.Enter)
            {
                if (lbShortcuts.SelectedItem is BoxItem box)
                    OpenItem(box.Item);
                e.Handled = true;
                return;
            }

            // Intercept all arrow keys
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left ||
                e.KeyCode == Keys.Down || e.KeyCode == Keys.Right)
            {
                if (e.Control)
                {
                    // Ctrl + Arrow: reorder items
                    HandleArrowKey(e.KeyCode);
                }
                else
                {
                    // Map Left to Up, Right to Down
                    var logicalKey = (e.KeyCode == Keys.Left) ? Keys.Up
                                    : (e.KeyCode == Keys.Right) ? Keys.Down
                                    : e.KeyCode;
                    MoveFocus(lbShortcuts, logicalKey);
                }
                e.Handled = true;
            }
        }

        // Handle key events for the ListView: Enter to open, Ctrl+Arrow for reordering, default behavior for other arrows
        private void LvShortcuts_KeyDown(object sender, KeyEventArgs e)
        {
            // Open item on Enter
            if (e.KeyCode == Keys.Enter)
            {
                if (lvShortcuts.SelectedItems.Count > 0)
                    OpenItem(lvShortcuts.SelectedItems[0].Tag as ShortcutItem);
                e.Handled = true;
                return;
            }

            // Use Ctrl+Arrow for reordering only
            if (e.Control && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left ||
                              e.KeyCode == Keys.Down || e.KeyCode == Keys.Right))
            {
                HandleArrowKey(e.KeyCode);
                e.Handled = true;
            }
        }

        private void MoveFocus(ListBox box, Keys key)
        {
            int index = box.SelectedIndex;
            if (index == -1) return;

            int count = box.Items.Count;
            if (key == Keys.Up && index > 0)
                box.SelectedIndex = index - 1;
            else if (key == Keys.Down && index < count - 1)
                box.SelectedIndex = index + 1;
        }

        private void MoveFocus(ListView view, Keys key)
        {
            if (view.SelectedItems.Count == 0) return;
            var item = view.SelectedItems[0];
            int index = item.Index;
            int count = view.Items.Count;

            view.SelectedItems.Clear();
            if (key == Keys.Up && index > 0)
                view.Items[index - 1].Selected = true;
            else if (key == Keys.Down && index < count - 1)
                view.Items[index + 1].Selected = true;
        }

        /// <summary>
        /// Handles Ctrl+Arrow key reordering in the ListView.
        /// In LargeIcon view, Up/Down move by one visual row (based on actual layout),
        /// Left/Right move by one item.
        /// In other views, all arrows move by one item.
        /// </summary>
        /// <param name="key">The arrow key that was pressed.</param>
        private void HandleArrowKey(Keys key)
        {
            int delta = 0;

            if (lvShortcuts.View == View.LargeIcon)
            {
                // Determine how many items are in the first visual row
                int columns = 1;
                if (lvShortcuts.Items.Count > 0)
                {
                    // Find the smallest Y-coordinate among all item bounds
                    int minY = lvShortcuts.Items
                        .Cast<ListViewItem>()
                        .Min(item => item.Bounds.Top);

                    // Count how many items share that Y-coordinate (i.e. items in the first row)
                    columns = lvShortcuts.Items
                        .Cast<ListViewItem>()
                        .Count(item => item.Bounds.Top == minY);

                    // Ensure at least one column
                    columns = Math.Max(1, columns);
                }

                switch (key)
                {
                    case Keys.Up:
                        // Move up by one row
                        delta = -columns;
                        break;
                    case Keys.Down:
                        // Move down by one row
                        delta = columns;
                        break;
                    case Keys.Left:
                        // Move left by one item
                        delta = -1;
                        break;
                    case Keys.Right:
                        // Move right by one item
                        delta = +1;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                // In non-icon views, all arrows move by one item
                delta = (key == Keys.Up || key == Keys.Left) ? -1 : +1;
            }

            // Perform the move; out-of-range deltas are ignored by MoveSelection
            MoveSelection(delta);
        }



        private void MoveSelection(int delta)
        {
            var selectedItem = GetSelectedShortcutItem();
            if (selectedItem == null)
                return;

            var visibleItems = GetVisibleShortcutItems();
            var visibleIndex = visibleItems.IndexOf(selectedItem);
            if (visibleIndex < 0)
                return;

            var newVisibleIndex = visibleIndex + delta;
            if (newVisibleIndex < 0 || newVisibleIndex >= visibleItems.Count)
                return;

            var oldIndex = _items.IndexOf(selectedItem);
            var newIndex = _items.IndexOf(visibleItems[newVisibleIndex]);
            if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
                return;

            MoveItem(oldIndex, newIndex, selectedItem);
        }

        private void MoveItem(int oldIndex, int newIndex, ShortcutItem item)
        {
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            RefreshCurrentView();
            SelectShortcutItem(item);
            Log($"Moved '{item.FullPath}' to position {newIndex + 1}");
        }

        private ShortcutItem? GetSelectedShortcutItem()
        {
            if (lbShortcuts.Focused && lbShortcuts.SelectedItem is BoxItem box)
                return box.Item;

            if (lvShortcuts.Focused && lvShortcuts.SelectedItems.Count > 0)
                return lvShortcuts.SelectedItems[0].Tag as ShortcutItem;

            return null;
        }

        private List<ShortcutItem> GetVisibleShortcutItems()
        {
            return lvShortcuts.Items
                .Cast<ListViewItem>()
                .Select(item => item.Tag as ShortcutItem)
                .Where(item => item != null)
                .Cast<ShortcutItem>()
                .ToList();
        }

        private void SelectShortcutItem(ShortcutItem item)
        {
            for (var i = 0; i < lbShortcuts.Items.Count; i++)
            {
                if (lbShortcuts.Items[i] is BoxItem box && ReferenceEquals(box.Item, item))
                {
                    lbShortcuts.SelectedIndex = i;
                    break;
                }
            }

            foreach (ListViewItem listViewItem in lvShortcuts.Items)
            {
                if (!ReferenceEquals(listViewItem.Tag, item))
                    continue;

                listViewItem.Selected = true;
                listViewItem.Focused = true;
                listViewItem.EnsureVisible();
                break;
            }
        }

        private void Log(string message)
        {
            rtbLog.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + message + Environment.NewLine);
            _logLineCount++;
            TrimLogIfNeeded();
        }

        private void TrimLogIfNeeded()
        {
            if (_logLineCount <= MaxLogLines)
                return;

            var linesToRemove = _logLineCount - MaxLogLines;
            var removeThroughIndex = rtbLog.GetFirstCharIndexFromLine(linesToRemove);
            if (removeThroughIndex <= 0)
            {
                _logLineCount = rtbLog.Lines.Length;
                return;
            }

            rtbLog.Select(0, removeThroughIndex);
            rtbLog.SelectedText = string.Empty;
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
            _logLineCount = MaxLogLines;
        }

        private void QueueClipboardCopy(string path)
        {
            _pendingClipboardText = path;
            _clipboardTimer.Stop();
            _clipboardTimer.Start();
        }

        private void CopyPendingPathToClipboard()
        {
            if (string.IsNullOrWhiteSpace(_pendingClipboardText) ||
                string.Equals(_lastClipboardText, _pendingClipboardText, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                Clipboard.SetText(_pendingClipboardText);
                _lastClipboardText = _pendingClipboardText;
            }
            catch
            {
                Log("Clipboard is currently unavailable");
            }
        }

        private class BoxItem
        {
            public ShortcutItem Item { get; }
            private readonly string _text;
            public BoxItem(ShortcutItem item, int index)
            {
                Item = item;
                _text = $"[{index}] {item.Name}";
            }
            public override string ToString() => _text;
        }

        #region PInvoke
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private Image GetIconImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return SystemIcons.Application.ToBitmap();

            if (!_iconCache.TryGetValue(path, out var image))
            {
                image = CreateIconImage(path);
                _iconCache[path] = image;
            }

            return image;
        }

        private Image CreateIconImage(string path)
        {
            try
            {
                using var icon = TryGetShellIcon(path);
                return icon.ToBitmap();
            }
            catch
            {
                return SystemIcons.Application.ToBitmap();
            }
        }

        private Icon TryGetShellIcon(string path)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                var attributes = Directory.Exists(path)
                    ? FILE_ATTRIBUTE_DIRECTORY
                    : FILE_ATTRIBUTE_NORMAL;

                SHGetFileInfo(path, attributes, out var info,
                    (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                    SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);

                hIcon = info.hIcon;
                if (hIcon != IntPtr.Zero)
                {
                    using var icon = Icon.FromHandle(hIcon);
                    return (Icon)icon.Clone();
                }
            }
            finally
            {
                if (hIcon != IntPtr.Zero)
                    DestroyIcon(hIcon);
            }

            using var extractedIcon = Icon.ExtractAssociatedIcon(path);
            return extractedIcon == null
                ? (Icon)SystemIcons.Application.Clone()
                : (Icon)extractedIcon.Clone();
        }

        private void PruneIconCache()
        {
            var activePaths = new HashSet<string>(_items.Select(item => item.FullPath), StringComparer.OrdinalIgnoreCase);
            foreach (var key in _iconCache.Keys.Where(key => !activePaths.Contains(key)).ToList())
            {
                _iconCache[key].Dispose();
                _iconCache.Remove(key);
            }
        }

        private void DisposeIconCache()
        {
            foreach (var image in _iconCache.Values)
                image.Dispose();

            _iconCache.Clear();
        }
        #endregion

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            _filterTimer.Start();
        }
    }
}
