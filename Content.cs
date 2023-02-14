using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Cornifer
{
    public static class Content
    {
        static readonly HashSet<Type> ContentTypes = new() { typeof(Texture2D), typeof(SpriteFont) };

#nullable disable
        public static SpriteFont RodondoExt20;
        public static SpriteFont RodondoExt30;
        public static SpriteFont Consolas10;
        public static Texture2D Objects;
        public static Texture2D SlugcatIcons;
        public static Texture2D MiscSprites;

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
                    object contentInstance = loadMethod.MakeGenericMethod(field.FieldType).Invoke(content, new object?[] { field.Name })!;
                    field.SetValue(null, contentInstance);

                    if (contentInstance is SpriteFont font)
                        fonts[field.Name] = font;
                    else if (contentInstance is Texture2D texture)
                        textures[field.Name] = texture;
                }

            Fonts = new(fonts);
            Textures = new(textures);
        }
    }
}
