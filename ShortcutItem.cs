using System.IO;
using System.Text.Json.Serialization;

namespace ShortcutShelf
{
    public class ShortcutItem
    {
        public string FullPath { get; set; } = string.Empty;

        [JsonIgnore]
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullPath))
                    return "(missing path)";

                try
                {
                    var trimmedPath = FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var name = Path.GetFileName(trimmedPath);
                    return string.IsNullOrWhiteSpace(name) ? FullPath : name;
                }
                catch
                {
                    return FullPath;
                }
            }
        }

        public ShortcutItem() { }

        public ShortcutItem(string fullPath)
        {
            FullPath = fullPath;
        }

        public override string ToString() => Name;
    }
}
