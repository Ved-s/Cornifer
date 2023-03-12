using Cornifer.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace Cornifer
{
    public static class StaticData
    {
        public static Dictionary<string, Color> PearlMainColors = new()
        {
            ["SI_west"] = new(0.01f, 0.01f, 0.01f),
            ["SI_top"] = new(0.01f, 0.01f, 0.01f),
            ["SI_chat3"] = new(0.01f, 0.01f, 0.01f),
            ["SI_chat4"] = new(0.01f, 0.01f, 0.01f),
            ["SI_chat5"] = new(0.01f, 0.01f, 0.01f),
            ["Spearmasterpearl"] = new(0.04f, 0.01f, 0.04f),
            ["SU_filt"] = new(1f, 0.75f, 0.9f),
            ["DM"] = new(0.95686275f, 0.92156863f, 0.20784314f),
            ["LC"] = new(0f, 0.4f, 0.01569f),
            ["LC_second"] = new(0.6f, 0f, 0f),
            ["OE"] = new(0.54901963f, 0.36862746f, 0.8f),
            ["MS"] = new(0.8156863f, 0.89411765f, 0.27058825f),
            ["RM"] = new(0.38431373f, 0.18431373f, 0.9843137f),
            ["Rivulet_stomach"] = new(0.5882353f, 0.87058824f, 0.627451f),
            ["CL"] = new(0.48431373f, 0.28431374f, 1f),
            ["VS"] = new(0.53f, 0.05f, 0.92f),
            ["BroadcastMisc"] = new(0.9f, 0.7f, 0.8f),
            ["CC"] = new(0.9f, 0.6f, 0.1f),
            ["LF_west"] = new(1f, 0f, 0.3f),
            ["LF_bottom"] = new(1f, 0.1f, 0.1f),
            ["HI"] = new(0.007843138f, 0.19607843f, 1f),
            ["SH"] = new(0.2f, 0f, 0.1f),
            ["DS"] = new(0f, 0.7f, 0.1f),
            ["SB_filtration"] = new(0.1f, 0.5f, 0.5f),
            ["SB_ravine"] = new(0.01f, 0.01f, 0.01f),
            ["GW"] = new(0f, 0.7f, 0.5f),
            ["SL_bridge"] = new(0.4f, 0.1f, 0.9f),
            ["SL_moon"] = new(0.9f, 0.95f, 0.2f),
            ["SU"] = new(0.5f, 0.6f, 0.9f),
            ["UW"] = new(0.4f, 0.6f, 0.4f),
            ["SL_chimney"] = new(1f, 0f, 0.55f),
            ["Red_stomach"] = new(0.6f, 1f, 0.9f),
            [""] = new(0.7f, 0.7f, 0.7f),
        };
        public static Dictionary<string, Color?> PearlHighlightColors = new()
        {
            ["SI_chat3"] = new(0.4f, 0.1f, 0.6f),
            ["SI_chat4"] = new(0.4f, 0.6f, 0.1f),
            ["SI_chat5"] = new(0.6f, 0.1f, 0.4f),
            ["Spearmasterpearl"] = new(0.95f, 0f, 0f),
            ["RM"] = new(1f, 0f, 0f),
            ["LC_second"] = new(0.8f, 0.8f, 0f),
            ["CL"] = new(1f, 0f, 0f),
            ["VS"] = new(1f, 0f, 1f),
            ["CC"] = new(1f, 1f, 0f),
            ["GW"] = new(0.5f, 1f, 0.5f),
            ["HI"] = new(0.5f, 0.8f, 1f),
            ["SH"] = new(1f, 0.2f, 0.6f),
            ["SI_top"] = new(0.1f, 0.4f, 0.6f),
            ["SI_west"] = new(0.1f, 0.6f, 0.4f),
            ["SL_bridge"] = new(1f, 0.4f, 1f),
            ["SB_ravine"] = new(0.6f, 0.1f, 0.4f),
            ["UW"] = new(1f, 0.7f, 1f),
            ["SL_chimney"] = new(0.8f, 0.3f, 1f),
            ["Red_stomach"] = new(1f, 1f, 1f),
        };

        public static List<Slugcat> Slugcats = new()
        {
            new("Yellow",    "Monk",        true,  new(255, 255, 115), Color.Black, "SU_C04"),
            new("White",     "Survivor",    true,  new(255, 255, 255), Color.Black, "SU_C04"),
            new("Red",       "Hunter",      true,  new(255, 115, 115), Color.Black, "LF_H01"),
            new("Night",     "Nightcat",    false, new(25,  15,  48),  Color.White, null),
            new("Gourmand",  "Gourmand",    true,  new(240, 193, 151), Color.Black, "SH_GOR02"),
            new("Artificer", "Artificer",   true,  new(112, 35,  60) , Color.White, "GW_A24"),
            new("Rivulet",   "Rivulet",     true,  new(145, 204, 240), Color.Black, "DS_RIVSTART"),
            new("Spear",     "Spearmaster", true,  new(79,  46,  105), Color.White, "GATE_OE_SU"),
            new("Saint",     "Saint",       true,  new(170, 241, 86),  Color.Black, "SI_SAINTINTRO"),
            new("Inv",       "Inv",         false, new(0,   19,  58),  Color.White, "SH_E01"),
        };

        public static List<string> PlacedObjectTypes = new()
        {
            "GreenToken","WhiteToken","Germinator","RedToken","OEsphere","MSArteryPush","GooieDuck","LillyPuck",
            "GlowWeed","BigJellyFish","RotFlyPaper","MoonCloak","DandelionPeach","KarmaShrine","Stowaway",
            "HRGuard","DevToken","LightSource","FlareBomb","PuffBall","TempleGuard","LightFixture","DangleFruit",
            "CoralStem","CoralStemWithNeurons","CoralNeuron","CoralCircuit","WallMycelia","ProjectedStars","ZapCoil",
            "SuperStructureFuses","GravityDisruptor","SpotLight","DeepProcessing","Corruption","CorruptionTube",
            "CorruptionDarkness","StuckDaddy","SSLightRod","CentipedeAttractor","DandelionPatch","GhostSpot","DataPearl",
            "UniqueDataPearl","SeedCob","DeadSeedCob","WaterNut","JellyFish","KarmaFlower","Mushroom","SlimeMold",
            "FlyLure","CosmeticSlimeMold","CosmeticSlimeMold2","FirecrackerPlant","VultureGrub","DeadVultureGrub",
            "VoidSpawnEgg","ReliableSpear","SuperJumpInstruction","ProjectedImagePosition","ExitSymbolShelter",
            "ExitSymbolHidden","NoSpearStickZone","LanternOnStick","ScavengerOutpost","TradeOutpost","ScavengerTreasury",
            "ScavTradeInstruction","CustomDecal","InsectGroup","PlayerPushback","MultiplayerItem","SporePlant",
            "GoldToken","BlueToken","DeadTokenStalk","NeedleEgg","BrokenShelterWaterLevel","BubbleGrass","Filter",
            "ReliableIggyDirection","Hazer","DeadHazer","Rainbow","LightBeam","NoLeviathanStrandingZone",
            "FairyParticleSettings","DayNightSettings","EnergySwirl","LightningMachine","SteamPipe","WallSteamer",
            "Vine","VultureMask","SnowSource","DeathFallFocus","CellDistortion","LocalBlizzard","NeuronSpawner",
            "HangingPearls","Lantern","ExitSymbolAncientShelter","BlinkingFlower"
        };

        public static HashSet<string> VanillaRegions = new() { "CC", "DS", "HI", "GW", "SI", "SU", "SH", "SL", "LF", "UW", "SB", "SS" };

        // TODO: equivalences.txt
        public static Dictionary<string, List<string>> RegionEquivalences = new()
        {
            ["LM"] = new() { "SL" },
            ["RM"] = new() { "SS" },
            ["UG"] = new() { "DS" },
            ["CL"] = new() { "SH" },
            ["MS"] = new() { "DM" },
            ["SL"] = new() { "LM" },
            ["SS"] = new() { "RM" },
            ["DS"] = new() { "UG" },
            ["SH"] = new() { "CL" },
            ["DM"] = new() { "MS" },
        };

        // Slugcat -> { DefaultRegion -> StoryRegion }
        // Saint -> { DS -> UG }
        // TODO: equivalences.txt Region.GetProperRegionAcronym
        public static Dictionary<string, Dictionary<string, string>> SlugcatRegionReplacements = new()
        {
            [""] = new()
            {
                ["UX"] = "UW",
                ["SX"] = "SS"
            },

            ["Spear"] = new()
            {
                ["SL"] = "LM"
            },

            ["Artificer"] = new()
            {
                ["SL"] = "LM"
            },

            ["Saint"] = new()
            {
                ["DS"] = "UG",
                ["SS"] = "RM",
                ["SH"] = "CL",
            },

            ["Rivulet"] = new()
            {
                ["SS"] = "RM",
            }
        };

        public static Dictionary<string, List<string>> SlugcatRegionAvailability = new()
        {
            ["Rivulet"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "RM", "SB", "MS" },
            ["Artificer"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "LM", "SI", "LF", "UW", "SS", "SB", "LC" },
            ["Saint"] = new() { "SU", "HI", "UG", "CC", "GW", "VS", "CL", "SL", "SI", "LF", "SB", "HR" },
            ["Spear"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "LM", "SI", "LF", "UW", "SS", "SB", "DM" },
            ["Gourmand"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "SS", "SB", "OE" },
            [""] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "SS", "SB" },
        };

        [return: NotNullIfNotNull(nameof(acronym))]
        public static string? GetProperRegionAcronym(string? acronym, Slugcat? slugcat)
        {
            if (acronym is null)
                return null;

            if (SlugcatRegionReplacements[""].TryGetValue(acronym, out string? defaultAcronym))
                return defaultAcronym;

            if (slugcat is not null
             && SlugcatRegionReplacements.TryGetValue(slugcat.WorldStateSlugcat, out var slugcatAcronyms)
             && slugcatAcronyms.TryGetValue(acronym, out string? slugcatAcronym))
                return slugcatAcronym;

            return acronym;
        }

        public static bool AreRegionsEquivalent(string a, string b)
        {
            if (a.Equals(b, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (RegionEquivalences.TryGetValue(a, out var equivalencesA) && equivalencesA.Contains(b))
                return true;

            if (RegionEquivalences.TryGetValue(b, out var equivalencesB) && equivalencesB.Contains(a))
                return true;

            return false;
        }

        public static Color GetPearlColor(string type)
        {
            Color color = PearlMainColors.GetValueOrDefault(type, PearlMainColors[""]);
            Color? color2 = PearlHighlightColors.GetValueOrDefault(type, null);
            if (color2.HasValue)
            {
                // color = Custom.Screen(color, color2.Value * Custom.QuickSaturation(color2.Value) * 0.5f);

                float max = Math.Max(color2.Value.R, Math.Max(color2.Value.G, color2.Value.B)) / 255f;
                float min = Math.Min(color2.Value.R, Math.Min(color2.Value.G, color2.Value.B)) / 255f;

                float sat = (min - max) / -max;

                Color v = color2.Value * sat * 0.5f;

                color = new Color(1f - (1f - color.R / 255f) * (1f - v.R / 255f), 1f - (1f - color.G / 255f) * (1f - v.G / 255f), 1f - (1f - color.B / 255f) * (1f - v.B / 255f));
            }
            else
            {
                color = Microsoft.Xna.Framework.Color.Lerp(color, Microsoft.Xna.Framework.Color.White, 0.15f);
            }
            if (color.R / 255f < 0.1f && color.G / 255f < 0.1f && color.B / 255f < 0.1f)
            {
                // color = Color.Lerp(color, Menu.MenuRGB(Menu.MenuColors.MediumGrey), 0.3f);

                Color menurgb = new(169, 164, 178);
                color = Microsoft.Xna.Framework.Color.Lerp(color, menurgb, 0.3f);
            }

            return color;
        }
    }
}
