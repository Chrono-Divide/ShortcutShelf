using System.IO;
using System.Text.Json.Serialization;

namespace ShortcutShelf
{
    public class ShortcutItem
    {
        public string FullPath { get; set; }

        [JsonIgnore]
        public string Name => Path.GetFileName(FullPath);

        public ShortcutItem() { }

        public ShortcutItem(string fullPath)
        {
            FullPath = fullPath;
        }

        public override string ToString() => Name;
    }
}
