namespace Cornifer.Structures
{
    public struct RegionInfo
    {
        public string Id;
        public string Displayname;

        public RWMod Mod;

        public RegionInfo(string id, string displayname, RWMod mod)
        {
            Id = id;
            Displayname = displayname;
            Mod = mod;
        }
    }
}
