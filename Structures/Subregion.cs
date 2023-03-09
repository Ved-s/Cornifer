using System.Drawing;

namespace Cornifer.Structures
{
    public class Subregion
    {
        private string name = null!;
        public string? AltName;
        public int Id = -1;

        public string DisplayName => AltName ?? Name;

        public string Name 
        {
            get => name;
            set
            {
                name = value;

                BackgroundColor = ColorDatabase.GetRegionColor(Region.Id, name, false);
                WaterColor = ColorDatabase.GetRegionColor(Region.Id, name, true);
            }
        }

        public Region Region;

        public ColorRef BackgroundColor = null!;
        public ColorRef WaterColor = null!;

        public Subregion(Region region, string name)
        {
            Region = region;
            Name = name;
        }
    }

}