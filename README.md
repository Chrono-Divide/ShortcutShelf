# 📁 ShortcutShelf

**ShortcutShelf** is a lightweight Windows desktop application for managing quick-access shortcuts to your frequently used files and folders.
It supports drag-and-drop, dual-view UI, keyboard shortcuts, filtering, reordering, and JSON-based auto-saving.

## 🚀 Features

- ✅ Drag and drop files or folders to add shortcuts
- ✅ Dual views: ListBox (left) and LargeIcon ListView (right)
- ✅ Press `Enter` or double-click to open the shortcut's location
- ✅ Press `F1` to view usage tips
- ✅ Real-time search filtering by name or full path
- ✅ Reorder shortcuts using `Ctrl + Arrow Keys`
- ✅ Automatically saves to and loads from `shortcuts.json`
- ✅ Displays file/folder icons using Windows Shell

## 🖼️ Interface Overview

- Left: ListBox with index and name
- Right: ListView with large icons and names
- Bottom: Path display and activity log (RichTextBox)
- Top: Filter box for instant search

## ⌨️ Keyboard Shortcuts

| Shortcut Key | Action Description |
| --- | --- |
| `F1` | Show usage tips |
| `Enter` / Double-click | Open the selected item's folder |
| `Arrow Keys` | Navigate between items |
| `Ctrl + Arrow Keys` | Move (reorder) the selected item |
| Right-click → Delete | Remove the selected shortcut |

## 📁 Data Storage

All shortcuts are saved in the executable directory as:

```
shortcuts.json
```

Each entry includes: the name (auto-extracted) and full path.
The app auto-loads the data on startup and saves on exit.

## 🛠️ Requirements

- OS: Windows 10 / 11
- .NET Runtime: as configured in the project

## 📄 License

This project is provided under the MIT License. You are free to use, modify, and distribute it.

---

**Organize your shortcuts efficiently with ShortcutShelf!**
