using Cornifer.Structures;
using Cornifer.UI.Modals;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cornifer
{
    public static class RWAssets
    {
        public static string? RainWorldRoot { get; private set; }
        public static string? RainWorldAssets { get; private set; }
        public static string? SaveFolder;

        public static readonly List<RWMod> Mods = new();

        public static HashSet<string>? EnabledMods;
        public static Dictionary<string, int>? ModLoadOrder;

        public static bool EnableMods = true;

        static Regex SteamLibraryPathRegex = new(@"""path""[ \t]*""([^""]+)""", RegexOptions.Compiled);
        static Regex SteamManifestInstallDirRegex = new(@"""installdir""[ \t]*""([^""]+)""", RegexOptions.Compiled);

        static Regex EnabledModsRegex = new(@"EnabledMods\&lt;optB\&gt;(.+?)(?:\&lt;optA|<)", RegexOptions.Compiled);
        static Regex ModLoadOrderRegex = new(@"ModLoadOrder\&lt;optB\&gt;(.+?)(?:\&lt;optA|<)", RegexOptions.Compiled);

        static Regex ModInfoIdRegex = new(@"""id"":[ \t]+""(.+?)""", RegexOptions.Compiled);
        static Regex ModInfoNameRegex = new(@"""name"":[ \t]+""(.+?)""", RegexOptions.Compiled);
        static Regex ModInfoVersionRegex = new(@"""version"":[ \t]+""(.+?)""", RegexOptions.Compiled);

        const string OptionsListSplitter = "&lt;optC&gt;";
        const string OptionsKeyValueSplitter = "&lt;optD&gt;";

        const string PathFile = "rainworldpath.txt";

        public static void Load()
        {
            SaveFolder = Environment.ExpandEnvironmentVariables("%appdata%/../LocalLow/Videocult/Rain World");
            if (!Directory.Exists(SaveFolder))
                SaveFolder = null;

            if (SaveFolder is not null)
            {
                string optionsPath = Path.Combine(SaveFolder, "options");

                if (File.Exists(optionsPath))
                {
                    string options = File.ReadAllText(optionsPath);

                    Match enabledMods = EnabledModsRegex.Match(options);
                    if (enabledMods.Success)
                        EnabledMods = new(enabledMods.Groups[1].Value.Split(OptionsListSplitter));

                    Match modLoadOrder = ModLoadOrderRegex.Match(options);
                    if (modLoadOrder.Success)
                    {
                        ModLoadOrder = new();

                        foreach (string kvp in modLoadOrder.Groups[1].Value.Split(OptionsListSplitter))
                        {
                            string[] kvpSplit = kvp.Split(OptionsKeyValueSplitter);
                            if (kvpSplit.Length != 2 || !int.TryParse(kvpSplit[1], out int order))
                                continue;

                            ModLoadOrder[kvpSplit[0]] = order;
                        }
                    }
                }
            }

            if (File.Exists(PathFile))
            {
                string path = File.ReadAllText(PathFile);
                if (Directory.Exists(path))
                    SetRainWorldPath(path);
            }
            if (RainWorldRoot is null && SearchRainWorld())
                File.WriteAllText(PathFile, RainWorldRoot);
        }

        static bool SearchRainWorld()
        {
            object? steampathobj =
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) ??
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null);
            if (steampathobj is not string steampath)
                return false;

            string libraryfolders = Path.Combine(steampath, "steamapps/libraryfolders.vdf");
            if (!File.Exists(libraryfolders))
            {
                string rainworld = Path.Combine(steampath, "steamapps/common/Rain World");
                if (Directory.Exists(rainworld))
                {
                    SetRainWorldPath(rainworld);
                    return false;
                }
                return false;
            }

            foreach (Match libmatch in SteamLibraryPathRegex.Matches(File.ReadAllText(libraryfolders)))
            {
                string libpath = Regex.Unescape(libmatch.Groups[1].Value);

                string manifest = Path.Combine(libpath, "steamapps/appmanifest_312520.acf");
                if (!File.Exists(manifest))
                    continue;

                Match manmatch = SteamManifestInstallDirRegex.Match(File.ReadAllText(manifest));
                if (!manmatch.Success)
                    continue;

                string appdir = Regex.Unescape(manmatch.Groups[1].Value);

                string rainworld = Path.Combine(libpath, $"steamapps/common/{appdir}");
                if (Directory.Exists(rainworld))
                {
                    SetRainWorldPath(rainworld);
                    return false;
                }
            }

            return false;
        }

        public static async void ShowDialogs()
        {
            if (RainWorldRoot is not null)
                return;

            if (await MessageBox.Show("Could not find Rain World installation.", new[] { ("Set Rain World path", 1), ("Cancel", 0) }) == 1)
            {
                string? rainWorld = await Platform.OpenFileDialog("Select Rain World executable", "Windows Executable|*.exe");
                if (rainWorld is not null)
                {
                    string root = Path.GetDirectoryName(rainWorld)!;
                    File.WriteAllText(PathFile, root);
                    SetRainWorldPath(root);
                }
            }
        }

        public static void SetRainWorldPath(string? path)
        {
            RainWorldRoot = path;

            Mods.Clear();
            if (path is null)
                return;

            string assets = Path.Combine(path, $"RainWorld_Data/StreamingAssets");
            if (Directory.Exists(assets))
            {
                string rwVersionPath = Path.Combine(assets, "GameVersion.txt");
                string? rwVersion = null;

                if (File.Exists(rwVersionPath))
                    rwVersion = File.ReadAllText(rwVersionPath).TrimStart('v');

                InsertMod(new("rainworld", "Rain World", assets, int.MaxValue, true) 
                {
                    Version = rwVersion
                });
                SetAssetsPath(assets);
            }

            string workshop = Path.Combine(path, "../../workshop/content/312520");
            if (Directory.Exists(workshop))
                LoadModsFolder(workshop);

            foreach (RWMod mod in Mods)
                LoadMod(mod);
        }

        public static void SetAssetsPath(string? path)
        {
            RainWorldAssets = path;

            if (path is null)
                return;

            string mergedmods = Path.Combine(path, "mergedmods");
            if (Directory.Exists(mergedmods))
                InsertMod(new("mergedmods", "Rain World", mergedmods, int.MinValue, true));

            string mods = Path.Combine(path, "mods");
            if (Directory.Exists(mods))
                LoadModsFolder(mods);
        }

        static void LoadModsFolder(string path)
        {
            foreach (string modDir in Directory.EnumerateDirectories(path))
            {
                string modinfoPath = Path.Combine(modDir, "modinfo.json");
                if (!File.Exists(modinfoPath))
                    continue;

                try
                {
                    string modinfo = File.ReadAllText(modinfoPath);
                    
                    Match idMatch = ModInfoIdRegex.Match(modinfo);

                    if (!idMatch.Success)
                        continue;

                    Match nameMatch = ModInfoNameRegex.Match(modinfo);
                    Match versionMatch = ModInfoVersionRegex.Match(modinfo);

                    string id = idMatch.Groups[1].Value;
                    string name = id;
                    if (nameMatch.Success)
                        name = nameMatch.Groups[1].Value;

                    int loadOrder = ModLoadOrder is null ? 0 : ModLoadOrder.GetValueOrDefault(id, 0);
                    bool enabled = EnabledMods is null || EnabledMods.Contains(id);

                    InsertMod(new(id, name, modDir, loadOrder, enabled)
                    {
                        Version = versionMatch.Success ? versionMatch.Groups[1].Value : null,
                    });
                }
                catch (Exception e)
                {
                    Main.LoadErrors.Add($"Could not load mod {Path.GetFileName(modDir)}: {e.Message}");
                }
            }
        }

        static void InsertMod(RWMod mod)
        {
            if (Mods.Count == 0)
            {
                Mods.Add(mod);
                return;
            }

            int index = 0;
            for (int i = 0; i < Mods.Count; i++)
            {
                if (Mods[i].LoadOrder > mod.LoadOrder)
                {
                    index = i;
                    break;
                }
            }
            Mods.Insert(index, mod);
        }

        static void LoadMod(RWMod mod)
        {
            string slugbaseDir = Path.Combine(mod.Path, "slugbase");
            if (Directory.Exists(slugbaseDir))
            {
                foreach (string slugcatFile in Directory.EnumerateFiles(slugbaseDir, "*.json"))
                {
                    try
                    {
                        using FileStream fs = File.OpenRead(slugcatFile);
                        JsonObject? obj = JsonSerializer.Deserialize<JsonObject>(fs);

                        if (obj is null || !obj.TryGet("id", out string? id))
                            continue;

                        Slugcat slugcat = new() { Id = id };

                        if (obj.TryGet("name", out string? name))
                            slugcat.Name = name;

                        if (obj.TryGet("features", out JsonObject? features))
                        {
                            bool setColor = false;
                            if (features.TryGet("color", out string? color))
                            {
                                Color? bodyColor = ColorDatabase.ParseColor(color);
                                setColor = bodyColor.HasValue;
                                if (bodyColor.HasValue)
                                    slugcat.Color = bodyColor.Value;
                            }

                            if (features.TryGet("custom_colors", out JsonArray? customColors))
                            {
                                foreach (JsonNode? node in customColors)
                                {
                                    if (node is not JsonObject || !node.TryGet("name", out string? colorName))
                                        continue;

                                    if (colorName == "Body" && !setColor && node.TryGet("story", out string? storyBodyColor))
                                        slugcat.Color = ColorDatabase.ParseColor(storyBodyColor) ?? Color.White;

                                    if (colorName == "Eyes" && node.TryGet("story", out string? storyEyesColor))
                                    {
                                        slugcat.EyeColor = ColorDatabase.ParseColor(storyEyesColor) ?? Color.Black;
                                        if (slugcat.EyeColor == new Color(16, 16, 16))
                                            slugcat.EyeColor = Color.Black;
                                    }
                                }
                            }

                            if (features.TryGet("start_room", out string? startRoomString))
                                slugcat.PossibleStartingRooms = new[] { startRoomString };
                            else if (features.TryGet("start_room", out JsonArray? startRoomArray))
                                slugcat.PossibleStartingRooms = startRoomArray.Deserialize<string[]>();

                            if (features.TryGet("world_state", out string? worldStateString))
                                slugcat.PossibleWorldStates = new[] { worldStateString };
                            else if (features.TryGet("world_state", out JsonArray? worldStateArray))
                                slugcat.PossibleWorldStates = worldStateArray.Deserialize<string[]>();
                        }

                        slugcat.GenerateIcons();
                        StaticData.Slugcats.Add(slugcat);
                    }
                    catch (Exception ex)
                    {
                        Main.LoadErrors.Add($"Cannot load {Path.GetFileNameWithoutExtension(slugcatFile)} slugcat: {ex.Message}");
                    }
                }
            }
        }

        public static string? ResolveFile(string path)
        {
            foreach (var mod in Mods)
            {
                if (!mod.Active)
                    continue;

                string modfile = Path.Combine(mod.Path, path);
                if (File.Exists(modfile))
                    return modfile;
            }
            return null;
        }

        public static string? ResolveSlugcatFile(string path)
        {
            if (Main.SelectedSlugcat is null)
                return ResolveFile(path);

            string slugcatPath = Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}-{Main.SelectedSlugcat.WorldStateSlugcat}{Path.GetExtension(path)}");
            string? resolved = ResolveFile(slugcatPath);

            if (resolved is not null)
                return resolved;

            return ResolveFile(path);
        }

        public static IEnumerable<(string path, RWMod mod)> EnumerateDirectories(string path)
        {
            HashSet<string> enumerated = new();

            foreach (var mod in Mods)
            {
                if (!mod.Active)
                    continue;

                string modDir = Path.Combine(mod.Path, path);
                if (!Directory.Exists(modDir))
                    continue;

                foreach (string dir in Directory.EnumerateDirectories(modDir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (enumerated.Contains(dirName))
                        continue;

                    enumerated.Add(dirName);
                    yield return (dir, mod);
                }
            }
        }

        public static string? GetRegionDisplayName(string regionId, Slugcat? slugcat)
        {
            string? displayname = null;
            if (slugcat is not null)
                displayname = ResolveFile($"world/{regionId}/displayname-{slugcat.WorldStateSlugcat}.txt");

            if (displayname is null)
                displayname = ResolveFile($"world/{regionId}/displayname.txt");

            return displayname is null ? null : File.ReadAllText(displayname);
        }

        public static IEnumerable<RegionInfo> FindRegions(Slugcat? slugcat = null)
        {
            string? worldSlugcat = slugcat?.WorldStateSlugcat;
            foreach (var (dir, mod) in RWAssets.EnumerateDirectories("world"))
            {
                string properties = Path.Combine(dir, "properties.txt");
                if (!File.Exists(properties))
                    continue;

                string id = Path.GetFileName(dir)!.ToUpper();

                string? displayname = RWAssets.ResolveFile($"world/{id}/displayname.txt");
                if (displayname is null)
                    continue;

                if (slugcat is not null)
                {
                    string? slugcatDisplayName = RWAssets.ResolveFile($"world/{id}/displayname-{worldSlugcat}.txt");
                    if (slugcatDisplayName is not null)
                        displayname = slugcatDisplayName;
                }

                yield return new RegionInfo(id, File.ReadAllText(displayname), mod);
            }
        }
    }
}
