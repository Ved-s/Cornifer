using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class GameAtlases
    {
        public static Dictionary<string, (Texture2D, Rectangle)> Sprites = new();

        public static void Load()
        {
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
                        if (assetName is not null)
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

                                Sprites[asset] = (texture, new(x, y, w, h));

                            }
                            catch { }
                        }
                }
            }
        }
    }
}
