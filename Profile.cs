using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cornifer
{
    public class Profile
    {
        public const string Filename = "profile.json";
        public static Profile Current { get; private set; } = null!;

        [JsonPropertyName("rwpath")]
        public string? RainWorldPath { get; set; }

        [JsonPropertyName("keybinds")]
        public List<Keybind>? Keybinds { get; set; }

        public static void Load()
        {
            string filename = Path.Combine(Main.MainDir, Filename);
            if (!File.Exists(filename))
                Current = new();
            else
            {
                if (Main.TryCatchReleaseException(() =>
                {
                    using FileStream fs = File.OpenRead(filename);
                    Current = JsonSerializer.Deserialize<Profile>(fs) ?? new();
                }, "Cannot load profile, restoring to default"))
                {
                    Current = new();
                }
            }
        }

        public static void Save() 
        {
            string filename = Path.Combine(Main.MainDir, Filename);
            using FileStream fs = File.Create(filename);
            JsonSerializer.Serialize(fs, Current);
        }

        public class Keybind 
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("keys")]
            public string[] Keys { get; set; } = null!;
        }
    }
}
