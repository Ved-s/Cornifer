namespace Cornifer.Structures
{
    public class Subregion
    {
        public string Name;

        public ColorRef BackgroundColor;
        public ColorRef WaterColor;

        public Subregion(string region, string name)
        {
            Name = name;

            BackgroundColor = ColorDatabase.GetRegionColor(region, name, false);
            WaterColor = ColorDatabase.GetRegionColor(region, name, true);
        }
    }

}