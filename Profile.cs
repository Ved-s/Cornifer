using Cornifer.Json.Converters;
using Cornifer.Structures;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
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
        public string? OldRainWorldPath { get; set; }

        [JsonPropertyName("keybinds")]
        public List<Keybind>? Keybinds { get; set; }

        [JsonPropertyName("installations")]
        public List<RainWorldInstallation>? Installations { get; set; }

        [JsonPropertyName("currentInstall")]
        public string? CurrentInstall { get; set; }

        [JsonConverter(typeof(XNAColor))]
        [JsonPropertyName("bgColor")]
        public Color BackgroundColor { get; set; } = Color.CornflowerBlue;

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
            JsonSerializer.Serialize(fs, Current, new JsonSerializerOptions 
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }.AddDebugIndent());
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
