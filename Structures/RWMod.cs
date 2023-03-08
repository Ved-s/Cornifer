namespace Cornifer.Structures
{
    public class RWMod
    {
        public string Id;
        public string Name;
        public string Path;
        public string? Version;

        public int LoadOrder;

        public bool Active => Enabled && (RWAssets.EnableMods || Id == "rainworld");
        public bool Enabled;

        public RWMod(string id, string name, string path, int loadOrder, bool enabled)
        {
            Id = id;
            Name = name;
            Path = path;
            LoadOrder = loadOrder;
            Enabled = enabled;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
