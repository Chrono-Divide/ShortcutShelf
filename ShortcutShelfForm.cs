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
                catch (Exception ex)
                {
                    _items = new List<ShortcutItem>();
                    Log($"Error loading JSON: {ex.Message}");
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
            catch (Exception ex)
            {
                Log($"Error saving JSON: {ex.Message}");
            }
        }

        private void RefreshViews()
        {
            lbShortcuts.Items.Clear();
            lvShortcuts.Items.Clear();
            imageListLarge.Images.Clear();

            foreach (var item in _items)
            {
                lbShortcuts.Items.Add(item);
                var ico = GetIcon(item.FullPath);
                imageListLarge.Images.Add(item.FullPath, ico);
                lvShortcuts.Items.Add(new ListViewItem(item.Name) { Tag = item, ImageKey = item.FullPath });
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
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var path in files)
                AddShortcut(path);
        }

        private void LvShortcuts_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var path in files)
                AddShortcut(path);
        }

        private void AddShortcut(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return;
            if (_items.Any(i => string.Equals(i.FullPath, path, StringComparison.OrdinalIgnoreCase))) return;

            var item = new ShortcutItem(path);
            _items.Add(item);

            lbShortcuts.Items.Add(item);
            var ico = GetIcon(path);
            imageListLarge.Images.Add(path, ico);
            lvShortcuts.Items.Add(new ListViewItem(item.Name) { Tag = item, ImageKey = path });

            Log($"Added '{path}'");
        }

        private void Control_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = lbShortcuts.SelectedItem as ShortcutItem;
            UpdateSelection(item);
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
            var item = lbShortcuts.SelectedItem as ShortcutItem;
            if (item != null)
                OpenItem(item);
        }

        private void OpenItem(ShortcutItem item)
        {
            if (item == null) return;
            try
            {
                var target = item.FullPath;
                var folder = Directory.Exists(target)
                    ? target
                    : Path.GetDirectoryName(target);
                Process.Start("explorer.exe", folder);
                Log($"Opened '{target}'");
            }
            catch (Exception ex)
            {
                Log($"Error opening '{item.FullPath}': {ex.Message}");
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShortcutItem item = null;
            if (lbShortcuts.Focused && lbShortcuts.SelectedItem != null)
                item = lbShortcuts.SelectedItem as ShortcutItem;
            else if (lvShortcuts.Focused && lvShortcuts.SelectedItems.Count > 0)
                item = lvShortcuts.SelectedItems[0].Tag as ShortcutItem;

            if (item == null) return;
            _items.Remove(item);
            RefreshViews();
            Log($"Deleted '{item.FullPath}'");
        }

        private void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("[HH:mm:ss] ");
            rtbLog.AppendText(timestamp + message + Environment.NewLine);
        }

        #region PInvoke for folder icon
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
                    SHFILEINFO info;
                    SHGetFileInfo(
                        path,
                        FILE_ATTRIBUTE_DIRECTORY,
                        out info,
                        (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                        SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES
                    );
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
    }
}
