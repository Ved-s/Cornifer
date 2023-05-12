using System;
using System.Collections.Generic;
using System.IO;
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
        public string AssetsPath { get => assetsPath ?? Path; set => assetsPath = value; }

        [JsonIgnore]
        public bool CanSave = true;

        [JsonIgnore]
        private string? assetsPath;

        [JsonIgnore]
        public bool IsRemix => HasFeature(RainWorldFeatures.Remix);

        [JsonIgnore]
        public bool IsLegacy => HasFeature(RainWorldFeatures.Legacy);

        [JsonIgnore]
        public bool IsSteam => HasFeature(RainWorldFeatures.Steam);

        [JsonIgnore]
        public bool IsDownpour => HasFeature(RainWorldFeatures.Downpour);

        [JsonIgnore]
        public bool HasCRS => HasFeature(RainWorldFeatures.CRS);

        public static RainWorldFeatures StateEssentialFeatures => RainWorldFeatures.Legacy | RainWorldFeatures.Remix | RainWorldFeatures.Downpour;

        public RainWorldInstallation() { }

        public RainWorldInstallation(string path, string id, string name, RainWorldFeatures features, bool canSave = true)
        {
            Path = path;
            Id = id;
            Name = name;
            Features = features;
            CanSave = canSave;
        }

        public bool HasFeature(RainWorldFeatures feature) =>
            (Features & feature) != RainWorldFeatures.None;

        public string GetFeaturesString() => GetFeaturesString(Features);

        public static RainWorldInstallation CreateFromPath(string path)
        {
            RainWorldInstallation install = new(System.IO.Path.GetFullPath(path), Random.Shared.Next().ToString("x"), "Unnamed", RainWorldFeatures.None);

            if (Directory.Exists(System.IO.Path.Combine(path, "RainWorld_Data/StreamingAssets/mods")))
            {
                install.AssetsPath = System.IO.Path.Combine(path, "RainWorld_Data/StreamingAssets");
                install.Features |= RainWorldFeatures.Remix;

                if (Directory.Exists(System.IO.Path.Combine(path, "RainWorld_Data/StreamingAssets/mods/moreslugcats")))
                    install.Features |= RainWorldFeatures.Downpour;
            }
            else if (Directory.Exists(System.IO.Path.Combine(path, "World")) && Directory.Exists(System.IO.Path.Combine(path, "Assets")))
            {
                install.Features |= RainWorldFeatures.Legacy;

                if (Directory.Exists(System.IO.Path.Combine(path, "Mods/CustomResources")))
                {
                    install.Features |= RainWorldFeatures.CRS;
                }
            }
            return install;
        }

        public static string GetFeaturesString(RainWorldFeatures features)
        {
            if (features == RainWorldFeatures.None)
                return "No features";

            List<RainWorldFeatures> featureList = new();

            for (int i = 0; i < 32; i++)
            {
                if ((int)RainWorldFeatures.All >> i == 0)
                    break;

                int imask = 1 << i;
                if (((int)RainWorldFeatures.All & imask) == 0)
                    continue;

                RainWorldFeatures feature = (RainWorldFeatures)imask;

                if ((features & feature) != 0)
                    featureList.Add(feature);
            }

            return string.Join(", ", features);
        }
    }
}
