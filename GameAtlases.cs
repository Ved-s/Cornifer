using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public static class GameAtlases
    {
        public static Dictionary<string, AtlasSprite> Sprites = new();

        static Dictionary<string, (string Atlas, Color Color)> ObjectSprites = new()
        {
            {"GreenLizard", ("Kill_Green_Lizard", new(51, 255, 0))},
            {"PinkLizard", ("Kill_Standard_Lizard", new(255, 0, 255))},
            {"BlueLizard", ("Kill_Standard_Lizard", new(0, 128, 255))},
            {"WhiteLizard", ("Kill_White_Lizard", new(255, 255, 255))},
            {"BlackLizard", ("Kill_Black_Lizard", new(94, 94, 111))},
            {"YellowLizard", ("Kill_Yellow_Lizard", new(255, 153, 0))},
            {"SpitLizard", ("Kill_Spit_Lizard", new(140, 102, 51))},
            {"ZoopLizard", ("Kill_White_Lizard", new(242, 186, 186))},
            {"CyanLizard", ("Kill_Standard_Lizard", new(0, 232, 230))},
            {"RedLizard", ("Kill_Standard_Lizard", new(230, 14, 14))},
            {"Salamander", ("Kill_Salamander", new(238, 199, 228))},
            {"EelLizard", ("Kill_Salamander", new(5, 199, 51))},
            {"Fly", ("Kill_Bat", new(169, 164, 178))},
            {"CicadaA", ("Kill_Cicada", new(255, 255, 255))},
            {"CicadaB", ("Kill_Cicada", new(94, 94, 111))},
            {"Snail", ("Kill_Snail", new(169, 164, 178))},
            {"Leech", ("Kill_Leech", new(174, 40, 30))},
            {"SeaLeech", ("Kill_Leech", new(13, 77, 179))},
            {"JungleLeech", ("Kill_Leech", new(26, 179, 26))},
            {"PoleMimic", ("Kill_PoleMimic", new(169, 164, 178))},
            {"TentaclePlant", ("Kill_TentaclePlant", new(169, 164, 178))},
            {"Scavenger", ("Kill_Scavenger", new(169, 164, 178))},
            {"ScavengerElite", ("Kill_ScavengerElite", new(169, 164, 178))},
            {"VultureGrub", ("Kill_VultureGrub", new(212, 202, 111))},
            {"Vulture", ("Kill_Vulture", new(212, 202, 111))},
            {"KingVulture", ("Kill_KingVulture", new(212, 202, 111))},
            {"SmallCentipede", ("Kill_Centipede1", new(255, 153, 0))},
            {"MediumCentipede", ("Kill_Centipede2", new(255, 153, 0))},
            {"BigCentipede", ("Kill_Centipede3", new(255, 153, 0))},
            {"RedCentipede", ("Kill_Centipede3", new(230, 14, 14))},
            {"Centiwing", ("Kill_Centiwing", new(14, 178, 60))},
            {"AquaCenti", ("Kill_Centiwing", new(0, 0, 255))},
            {"TubeWorm", ("Kill_Tubeworm", new(13, 77, 179))},
            {"Hazer", ("Kill_Hazer", new(54, 202, 99))},
            {"LanternMouse", ("Kill_Mouse", new(169, 164, 178))},
            {"Spider", ("Kill_SmallSpider", new(169, 164, 178))},
            {"BigSpider", ("Kill_BigSpider", new(169, 164, 178))},
            {"SpitterSpider", ("Kill_BigSpider", new(174, 40, 30))},
            {"MotherSpider", ("Kill_BigSpider", new(26, 179, 26))},
            {"MirosBird", ("Kill_MirosBird", new(169, 164, 178))},
            {"MirosVulture", ("Kill_MirosBird", new(230, 14, 14))},
            {"BrotherLongLegs", ("Kill_Daddy", new(116, 134, 78))},
            {"DaddyLongLegs", ("Kill_Daddy", new(0, 0, 255))},
            {"TerrorLongLegs", ("Kill_Daddy", new(77, 0, 255))},
            {"Inspector", ("Kill_Inspector", new(114, 230, 196))},
            {"Deer", ("Kill_RainDeer", new(169, 164, 178))},
            {"EggBug", ("Kill_EggBug", new(0, 255, 120))},
            {"FireBug", ("Kill_FireBug", new(255, 120, 120))},
            {"DropBug", ("Kill_DropBug", new(169, 164, 178))},
            {"SlugNPC", ("Kill_Slugcat", new(169, 164, 178))},
            {"BigNeedleWorm", ("Kill_NeedleWorm", new(255, 152, 152))},
            {"SmallNeedleWorm", ("Kill_SmallNeedleWorm", new(255, 152, 152))},
            {"JetFish", ("Kill_Jetfish", new(169, 164, 178))},
            {"Yeek", ("Kill_Yeek", new(230, 230, 230))},
            {"BigEel", ("Kill_BigEel", new(169, 164, 178))},
            {"BigJelly", ("Kill_BigJellyFish", new(255, 217, 179))},
            {"Rock", ("Symbol_Rock", new(169, 164, 178))},
            {"Spear", ("Symbol_Spear", new(169, 164, 178))},
            {"FireSpear", ("Symbol_FireSpear", new(230, 14, 14))},
            {"ElectricSpear", ("Symbol_ElectricSpear", new(0, 0, 255))},
            {"HellSpear", ("Symbol_HellSpear", new(255, 120, 120))},
            {"LillyPuck", ("Symbol_LillyPuck", new(44, 245, 255))},
            {"Pearl", ("Symbol_Pearl", new(179, 179, 179))},
            {"ScavengerBomb", ("Symbol_StunBomb", new(230, 14, 14))},
            {"SingularityBomb", ("Symbol_Singularity", new(5, 165, 217))},
            {"FireEgg", ("Symbol_FireEgg", new(255, 120, 120))},
            {"SporePlant", ("Symbol_SporePlant", new(174, 40, 30))},
            {"Lantern", ("Symbol_Lantern", new(255, 146, 81))},
            {"VultureMask", ("Kill_Vulture", new(169, 164, 178))},
            {"FlyLure", ("Symbol_FlyLure", new(173, 68, 54))},
            {"Mushroom", ("Symbol_Mushroom", new(255, 255, 255))},
            {"FlareBomb", ("Symbol_FlashBomb", new(187, 174, 255))},
            {"PuffBall", ("Symbol_PuffBall", new(169, 164, 178))},
            {"GooieDuck", ("Symbol_GooieDuck", new(114, 230, 196))},
            {"WaterNut", ("Symbol_WaterNut", new(13, 77, 179))},
            {"DandelionPeach", ("Symbol_DandelionPeach", new(150, 199, 245))},
            {"FirecrackerPlant", ("Symbol_Firecracker", new(174, 40, 30))},
            {"DangleFruit", ("Symbol_DangleFruit", new(0, 0, 255))},
            {"JellyFish", ("Symbol_JellyFish", new(169, 164, 178))},
            {"BubbleGrass", ("Symbol_BubbleGrass", new(14, 178, 60))},
            {"GlowWeed", ("Symbol_GlowWeed", new(242, 255, 69))},
            {"SlimeMold", ("Symbol_SlimeMold", new(255, 153, 0))},
            {"EnergyCell", ("Symbol_EnergyCell", new(5, 165, 217))},
            {"JokeRifle", ("Symbol_JokeRifle", new(169, 164, 178))},

            {"NeedleEgg", ("needleEggSymbol", new(45, 13, 20))},
            {"WhiteToken", ("Symbol_Satellite", new(255, 255, 255))},
        };

        static Dictionary<string, (Rectangle Frame, Color Color)> ObjectSpriteFrames = new()
        {
            { "KarmaFlower",     (new(76, 0,  23, 23), new(255, 255, 255, 255)) },
            { "SeedCob",         (new(40, 0,  35, 38), new(255, 255, 255, 255)) },
            { "GhostSpot",       (new(0,  0,  38, 48), new(255, 255, 255, 255)) },
            { "BlueToken",       (new(76, 24, 10, 20), new(255, 255, 255, 150)) },
            { "GoldToken",       (new(87, 24, 10, 20), new(255, 255, 255, 150)) },
            { "RedToken",        (new(100, 0, 10, 20), new(255, 255, 255, 150)) },
            { "DevToken",        (new(98, 24, 10, 20), new(255, 255, 255, 150)) },
            { "GreenToken",      (new(111, 0, 10, 20), new(255, 255, 255, 150)) },
            { "DataPearl",       (new(39, 39, 11, 11), new(255, 255, 255, 255)) },
            { "UniqueDataPearl", (new(39, 39, 11, 11), new(255, 255, 255, 255)) },
            { "Slugcat",         (new(51, 39, 20, 19), new(255, 255, 255, 255)) },
        };

        static Dictionary<string, (Rectangle Frame, Color Color)> MiscSpriteFrames = new()
        {
            { "ArrowLeft",     (new(0, 0,  22, 13), new(255, 255, 255, 255)) },
            { "ArrowRight",    (new(0, 13, 22, 13), new(255, 255, 255, 255)) },
        };

        public static void Load()
        {
            foreach (var (objectName, objectSprite) in ObjectSpriteFrames)
            {
                string name = "Object_" + objectName;
                Sprites[name] = new(name, Content.Objects, objectSprite.Frame, objectSprite.Color, false);
            }

            foreach (var (objectName, objectSprite) in MiscSpriteFrames)
            {
                string name = "Misc_" + objectName;
                Sprites[name] = new(name, Content.MiscSprites, objectSprite.Frame, objectSprite.Color, false);
            }

            if (!Directory.Exists("Atlases"))
                return;

            foreach (string atlasFile in Directory.EnumerateFiles("Atlases", "*.txt"))
            {
                string textureFile = Path.ChangeExtension(atlasFile, ".png");
                if (!File.Exists(atlasFile))
                    continue;

                JsonNode json;
                using (FileStream fs = File.OpenRead(atlasFile))
                {
                    json = JsonNode.Parse(fs)!;
                }

                if (json["frames"] is JsonObject frames)
                {
                    Texture2D texture = Texture2D.FromFile(Main.Instance.GraphicsDevice, textureFile);
                    foreach (var (assetName, assetFrameData) in frames)
                        if (assetName is not null && assetFrameData is not null)
                        {
                            string asset = Path.ChangeExtension(assetName, null);

                            try
                            {
                                JsonObject? frame = assetFrameData["frame"] as JsonObject;

                                if (frame is null)
                                    continue;

                                int x = (int)frame["x"]!;
                                int y = (int)frame["y"]!;
                                int w = (int)frame["w"]!;
                                int h = (int)frame["h"]!;

                                Sprites[asset] = new(asset, texture, new(x, y, w, h), Color.White, true);
                            }
                            catch { }
                        }
                }
            }

            foreach (var (spriteName, spriteData) in ObjectSprites)
                if (Sprites.TryGetValue(spriteData.Atlas, out AtlasSprite? sprite))
                {
                    string name = "Object_" + spriteName;
                    Sprites[name] = new(name, sprite.Texture, sprite.Frame, spriteData.Color, true);
                }

            if (Sprites.TryGetValue("Object_Slugcat", out AtlasSprite? slugcat))
            {
                for (int i = 0; i < Main.SlugCatNames.Length; i++)
                {
                    string name = "Slugcat_" + Main.SlugCatNames[i];
                    Sprites[name] = new(name, slugcat.Texture, slugcat.Frame, Main.SlugCatColors[i], false);
                }
            }

            for (int i = 0; i < Main.SlugCatNames.Length; i++)
            {
                string name = "SlugcatIcon_" + Main.SlugCatNames[i];
                Rectangle frame = new(i * 8, 0, 8, 8);
                Sprites[name] = new(name, Content.SlugcatIcons, frame, Color.White, false);

                name = "SlugcatDiamond_" + Main.SlugCatNames[i];
                frame = new(i * 9, 8, 9, 9);
                Sprites[name] = new(name, Content.SlugcatIcons, frame, Color.White, false);
            }
        }

        public static AtlasSprite? GetSpriteOrNull(string name)
        {
            if (!Sprites.TryGetValue(name, out AtlasSprite? sprite))
                sprite = null;
            return sprite;
        }
    }

    public record class AtlasSprite(string Name, Texture2D Texture, Rectangle Frame, Color Color, bool Shade);
}
