using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class Content
    {
#nullable disable
        public static SpriteFont RodondoExt20;
        public static SpriteFont Consolas10;
        public static Texture2D Objects;
        public static Texture2D SlugcatIcons;
#nullable restore

        public static void Load(ContentManager content)
        {
            MethodInfo loadMethod = typeof(ContentManager).GetMethod(nameof(ContentManager.Load))!;

            foreach (FieldInfo field in typeof(Content).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                field.SetValue(null, loadMethod.MakeGenericMethod(field.FieldType).Invoke(content, new object?[] { field.Name }));
            }
        }
    }
}
