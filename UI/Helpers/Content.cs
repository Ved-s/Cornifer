using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.UI.Helpers
{
    public static class Content
    {
        public static Effect Gradient = null!;
        public static Effect Ellipse = null!;

        public static Texture2D AnimationAssets = null!;

        internal static void Load(ContentManager content)
        {
            Gradient = content.Load<Effect>("Gradient");
            Ellipse = content.Load<Effect>("Ellipse");
            AnimationAssets = content.Load<Texture2D>("AnimationAssets");
        }
    }
}
