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
        private List<ShortcutItem> _items = new List<ShortcutItem>();
        private readonly string _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shortcuts.json");

        public ShortcutShelfForm()
        {
            InitializeComponent();
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
                    _items = JsonSerializer.Deserialize<List<ShortcutItem>>(json) ?? new List<ShortcutItem>();
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
                File.WriteAllText(_dataFile, json);
            }
            catch
            {
                Log("Error saving JSON");
            }
        }

        private void RefreshViews()
        {
            lbShortcuts.Items.Clear();
            lvShortcuts.Items.Clear();
            imageListLarge.Images.Clear();

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                lbShortcuts.Items.Add(new BoxItem(item, i + 1));

                var ico = GetIcon(item.FullPath);
                imageListLarge.Images.Add(item.FullPath, ico);

                var lvi = new ListViewItem(item.Name)
                {
                    Tag = item,
                    ImageKey = item.FullPath
                };
                lvShortcuts.Items.Add(lvi);
            }
        }

        private void ApplyFilter(string keyword)
        {
            var lower = (keyword ?? "").ToLower();
            var filtered = string.IsNullOrEmpty(lower)
                ? _items
                : _items.Where(i =>
                    i.Name.ToLower().Contains(lower) ||
                    i.FullPath.ToLower().Contains(lower)
                  ).ToList();

            lbShortcuts.Items.Clear();
            lvShortcuts.Items.Clear();
            imageListLarge.Images.Clear();

            for (int i = 0; i < filtered.Count; i++)
            {
                var item = filtered[i];
                lbShortcuts.Items.Add(new BoxItem(item, i + 1));

                var ico = GetIcon(item.FullPath);
                imageListLarge.Images.Add(item.FullPath, ico);

                var lvi = new ListViewItem(item.Name)
                {
                    Tag = item,
                    ImageKey = item.FullPath
                };
                lvShortcuts.Items.Add(lvi);
            }
        }

        private void ListControl_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void LbShortcuts_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            foreach (var path in (string[])e.Data.GetData(DataFormats.FileDrop))
                AddShortcut(path);
        }

        private void LvShortcuts_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            foreach (var path in (string[])e.Data.GetData(DataFormats.FileDrop))
                AddShortcut(path);
        }

        private void AddShortcut(string path)
        {
            if ((!File.Exists(path) && !Directory.Exists(path)) ||
                _items.Any(i => string.Equals(i.FullPath, path, StringComparison.OrdinalIgnoreCase)))
                return;

            _items.Add(new ShortcutItem(path));
            RefreshViews();
            Log($"Added '{path}'");
        }

        private void Control_SelectedIndexChanged(object sender, EventArgs e)
        {
            var box = lbShortcuts.SelectedItem as BoxItem;
            UpdateSelection(box?.Item);
        }

        private void LvShortcuts_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
                UpdateSelection(e.Item.Tag as ShortcutItem);
        }

        private void UpdateSelection(ShortcutItem item)
        {
            if (item == null) return;
            txtPath.Text = item.FullPath;
            try { Clipboard.SetText(item.FullPath); } catch { }
            Log($"Selected '{item.FullPath}'");
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

        private void OpenItem(ShortcutItem item)
        {
            if (item == null) return;
            var target = item.FullPath;
            var folder = Directory.Exists(target)
                ? target
                : Path.GetDirectoryName(target);
            try
            {
                Process.Start("explorer.exe", folder);
                Log($"Opened '{target}'");
            }
            catch
            {
                Log($"Error opening '{target}'");
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShortcutItem item = null;
            if (lbShortcuts.Focused && lbShortcuts.SelectedItem is BoxItem box)
                item = box.Item;
            else if (lvShortcuts.Focused && lvShortcuts.SelectedItems.Count > 0)
                item = lvShortcuts.SelectedItems[0].Tag as ShortcutItem;

            if (item == null) return;
            _items.Remove(item);
            RefreshViews();
            Log($"Deleted '{item.FullPath}'");
        }

        private void LbShortcuts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (lbShortcuts.SelectedItem is BoxItem box)
                    OpenItem(box.Item);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left ||
                e.KeyCode == Keys.Down || e.KeyCode == Keys.Right)
            {
                if (e.Control)
                {
                    HandleArrowKey(e.KeyCode); // Ctrl+Arrow = reorder
                    e.Handled = true;
                }
                else
                {
                    MoveFocus(lbShortcuts, e.KeyCode);
                    e.Handled = true;
                }
            }
        }

        private void LvShortcuts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (lvShortcuts.SelectedItems.Count > 0)
                    OpenItem(lvShortcuts.SelectedItems[0].Tag as ShortcutItem);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Left ||
                e.KeyCode == Keys.Down || e.KeyCode == Keys.Right)
            {
                if (e.Control)
                {
                    HandleArrowKey(e.KeyCode); // Ctrl+Arrow = reorder
                    e.Handled = true;
                }
                else
                {
                    MoveFocus(lvShortcuts, e.KeyCode);
                    e.Handled = true;
                }
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


        private void HandleArrowKey(Keys key)
        {
            MoveSelection((key == Keys.Up || key == Keys.Left) ? -1 : +1);
        }

        private void MoveSelection(int delta)
        {
            int oldIndex;
            if (lbShortcuts.Focused)
                oldIndex = lbShortcuts.SelectedIndex;
            else if (lvShortcuts.Focused && lvShortcuts.SelectedItems.Count > 0)
                oldIndex = lvShortcuts.SelectedItems[0].Index;
            else
                return;

            int newIndex = oldIndex + delta;
            if (newIndex < 0 || newIndex >= _items.Count) return;
            MoveItem(oldIndex, newIndex);
        }

        private void MoveItem(int oldIndex, int newIndex)
        {
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            RefreshViews();
            lbShortcuts.SelectedIndex = newIndex;
            lvShortcuts.Items[newIndex].Selected = true;
            Log($"Moved '{item.FullPath}' to position {newIndex + 1}");
        }

        private void Log(string message)
        {
            rtbLog.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + message + Environment.NewLine);
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

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );

        private Icon GetIcon(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    SHGetFileInfo(path, FILE_ATTRIBUTE_DIRECTORY, out var info,
                        (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                        SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);
                    return Icon.FromHandle(info.hIcon);
                }
                return Icon.ExtractAssociatedIcon(path);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }
        #endregion

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter(txtFilter.Text);
        }
    }
}
