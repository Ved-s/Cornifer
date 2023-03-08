using Cornifer.Structures;
using Cornifer.UI.Modals;
using Microsoft.Win32;
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

        static Regex SteamLibraryPath = new(@"""path""[ \t]*""([^""]+)""", RegexOptions.Compiled);
        static Regex SteamManifestInstallDir = new(@"""installdir""[ \t]*""([^""]+)""", RegexOptions.Compiled);
        static Regex EnabledModsRegex = new(@"EnabledMods\&lt;optB\&gt;(.+?)(?:\&lt;optA|<)", RegexOptions.Compiled);
        static Regex ModLoadOrderRegex = new(@"ModLoadOrder\&lt;optB\&gt;(.+?)(?:\&lt;optA|<)", RegexOptions.Compiled);

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

            foreach (Match libmatch in SteamLibraryPath.Matches(File.ReadAllText(libraryfolders)))
            {
                string libpath = Regex.Unescape(libmatch.Groups[1].Value);

                string manifest = Path.Combine(libpath, "steamapps/appmanifest_312520.acf");
                if (!File.Exists(manifest))
                    continue;

                Match manmatch = SteamManifestInstallDir.Match(File.ReadAllText(manifest));
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
                InsertMod(new("rainworld", "Rain World", assets, int.MaxValue, true));
                SetAssetsPath(assets);
            }

            string workshop = Path.Combine(path, "../../workshop/content/312520");
            if (Directory.Exists(workshop))
                LoadModsFolder(workshop);
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
                    using FileStream fs = File.OpenRead(modinfoPath);
                    JsonNode? modinfo = JsonSerializer.Deserialize<JsonNode>(fs);

                    if (modinfo is null)
                        continue;

                    if (!modinfo.TryGet("id", out string? id) || !modinfo.TryGet("name", out string? name))
                        continue;

                    int loadOrder = ModLoadOrder is null ? 0 : ModLoadOrder.GetValueOrDefault(id, 0);
                    bool enabled = EnabledMods is null || EnabledMods.Contains(id);

                    InsertMod(new(id, name, modDir, loadOrder, enabled));
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

            string slugcatPath = Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)}-{Main.SelectedSlugcat}{Path.GetExtension(path)}");
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

        public static string? GetRegionDisplayName(string regionId, string? slugcat)
        {
            string? displayname = null;
            if (slugcat is not null)
                displayname = ResolveFile($"world/{regionId}/displayname-{slugcat}.txt");

            if (displayname is null)
                displayname = ResolveFile($"world/{regionId}/displayname.txt");

            return displayname is null ? null : File.ReadAllText(displayname);
        }

        public static IEnumerable<RegionInfo> FindRegions(string? slugcat = null)
        {
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
                    string? slugcatDisplayName = RWAssets.ResolveFile($"world/{id}/displayname-{slugcat}.txt");
                    if (slugcatDisplayName is not null)
                        displayname = slugcatDisplayName;
                }

                yield return new RegionInfo(id, File.ReadAllText(displayname), mod);
            }
        }
    }
}
