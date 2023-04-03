using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cornifer
{
    public static class Content
    {
        static readonly HashSet<Type> ContentTypes = new() { typeof(Texture2D), typeof(SpriteFont), typeof(SoundEffect) };

#nullable disable
        [ManualSpriteFont]
        public static SpriteFont RodondoExt20M;
        [ManualSpriteFont]
        public static SpriteFont RodondoExt30M;
        public static SpriteFont Consolas10;
        public static Texture2D Objects;
        public static Texture2D SlugcatIcons;
        public static Texture2D SlugcatIconTemplate;
        public static Texture2D MiscSprites;
        public static SoundEffect Idle;

        public static ReadOnlyDictionary<string, SpriteFont> Fonts;
        public static ReadOnlyDictionary<string, Texture2D> Textures;

#nullable restore

        public static void Load(ContentManager content)
        {
            MethodInfo loadMethod = typeof(ContentManager).GetMethod(nameof(ContentManager.Load))!;

            Dictionary<string, SpriteFont> fonts = new();
            Dictionary<string, Texture2D> textures = new();

            foreach (FieldInfo field in typeof(Content).GetFields(BindingFlags.Static | BindingFlags.Public))
                if (ContentTypes.Contains(field.FieldType))
                {
                    object contentInstance;
                    if (field.GetCustomAttribute<ManualSpriteFontAttribute>() is not null)
                        contentInstance = LoadManualSpritefont(Path.Combine(content.RootDirectory, $"{field.Name}.txt"));
                    else
                        contentInstance = loadMethod.MakeGenericMethod(field.FieldType).Invoke(content, new object?[] { field.Name })!;
                    field.SetValue(null, contentInstance);

                    if (contentInstance is SpriteFont font)
                        fonts[field.Name] = font;
                    else if (contentInstance is Texture2D texture)
                        textures[field.Name] = texture;
                }

            Fonts = new(fonts);
            Textures = new(textures);
        }

        public static SpriteFont GetFontByName(string name, SpriteFont @default)
        {
            if (Fonts.TryGetValue(name, out SpriteFont? font))
                return font;

            font = Fonts.FirstOrDefault(kvp => kvp.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Value;
            if (font is not null)
                return font;

            switch (name)
            {
                case "Rodondo20":
                case "RodondoExt20":
                    return RodondoExt20M;

                case "Rodondo30":
                case "RodondoExt30":
                    return RodondoExt30M;
            }

            return @default;
        }

        static SpriteFont LoadManualSpritefont(string path)
        {
            Texture2D texture = null!;

            Dictionary<char, (Rectangle Bounds, Rectangle Cropping, Vector3 Kerning)> glyphs = new();

            int lineSpacing = 0;
            float spacing = 1;
            char? defaultCharacter = null;

            int glyphSpacing = 0;
            bool ignoreCase = false;

            Point readerPos = new();

            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("//"))
                    continue;

                string[] split = line.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (split.Length < 2)
                    continue;

                switch (split[0])
                {
                    case "texture":
                        texture = Texture2D.FromFile(Main.Instance.GraphicsDevice, Path.Combine(Path.GetDirectoryName(path)!, split[1]));
                        break;

                    case "defaultChar" when split[1].Length == 1:
                        defaultCharacter = split[1][0];
                        break;

                    case "ignoreCase" when bool.TryParse(split[1], out bool ignoreCaseValue):
                        ignoreCase = ignoreCaseValue;
                        break;

                    case "lineHeight" when int.TryParse(split[1], out int lineHeight):
                        lineSpacing = lineHeight;
                        break;

                    case "glyphSpacing" when int.TryParse(split[1], out int glyphSpacingValue):
                        glyphSpacing = glyphSpacingValue;
                        break;

                    case "spacing" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float spacingValue):
                        spacing = spacingValue;
                        break;

                    case "spaceWidth" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float spaceWidth):
                        glyphs[' '] = (new(), new(), new(0, spaceWidth, 0));
                        break;

                    case "pos":
                        string[] pos = split[1].Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (pos.Length < 2 || !int.TryParse(pos[0], out int posX) || !int.TryParse(pos[1], out int posY))
                            break;

                        readerPos = new(posX, posY);
                        break;

                    case "chars":
                        string[] chars = split[1].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < chars.Length; i += 2)
                        {
                            if (i >= chars.Length - 1)
                                break;

                            string charStr = chars[i];
                            if (charStr.Length != 1 || !int.TryParse(chars[i + 1], out int charWidth))
                                continue;

                            char chr = charStr[0];

                            if (ignoreCase && (char.IsLower(chr) || char.IsUpper(chr)))
                            {
                                glyphs[char.ToLower(chr)] = (new(readerPos.X, readerPos.Y, charWidth, lineSpacing), new(), new(0, charWidth, 0));
                                glyphs[char.ToUpper(chr)] = (new(readerPos.X, readerPos.Y, charWidth, lineSpacing), new(), new(0, charWidth, 0));
                            }
                            else
                                glyphs[chr] = (new(readerPos.X, readerPos.Y, charWidth, lineSpacing), new(), new(0, charWidth, 0));



                            readerPos.X += charWidth + glyphSpacing;
                        }

                        break;
                }
            }

            List<char> characters = new();
            List<Rectangle> glyphBounds = new();
            List<Rectangle> cropping = new();
            List<Vector3> kerning = new();

            foreach (var kvp in glyphs.OrderBy(kvp => kvp.Key))
            {
                characters.Add(kvp.Key);
                glyphBounds.Add(kvp.Value.Bounds);
                cropping.Add(kvp.Value.Cropping);
                kerning.Add(kvp.Value.Kerning);
            }

            return new SpriteFont(texture, glyphBounds, cropping, characters, lineSpacing, spacing, kerning, defaultCharacter);
        }

        [AttributeUsage(AttributeTargets.Field)]
        class ManualSpriteFontAttribute : Attribute { }
    }
}
