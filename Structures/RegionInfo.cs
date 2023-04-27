namespace Cornifer.Structures
{
    public struct RegionInfo
    {
        public string Path;
        public string RoomsPath;

        public string Id;
        public string Displayname;

        public RWMod Mod;

        public RegionInfo(string path, string roomsPath, string id, string displayname, RWMod mod)
        {
            Path = path;
            RoomsPath = roomsPath;
            Id = id;
            Displayname = displayname;
            Mod = mod;
        }
    }
}
