using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cornifer.Structures
{
    public class RainWorldInstallation
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = null!;

        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("features")]
        public RainWorldFeatures Features { get; set; }

        [JsonIgnore]
        public bool CanSave = true;

        [JsonIgnore]
        public bool IsRemix => (Features & RainWorldFeatures.Remix) != RainWorldFeatures.None;

        [JsonIgnore]
        public bool IsSteam => (Features & RainWorldFeatures.Steam) != RainWorldFeatures.None;

        public RainWorldInstallation() { }

        public RainWorldInstallation(string path, string id, string name, RainWorldFeatures features, bool canSave = true)
        {
            Path = path;
            Id = id;
            Name = name;
            Features = features;
            CanSave = canSave;
        }

        public string GetFeaturesString()
        {
            if (Features == RainWorldFeatures.None)
                return "No features";

            List<RainWorldFeatures> features = new();

            for (int i = 0; i < 32; i++)
            {
                if ((int)RainWorldFeatures.All >> i == 0)
                    break;

                int mask = 1 << i;
                if (((int)RainWorldFeatures.All & mask) == 0)
                    continue;

                RainWorldFeatures feature = (RainWorldFeatures)mask;

                if ((Features & feature) != 0)
                    features.Add(feature);
            }

            return string.Join(", ", features);
        }
    }
}
