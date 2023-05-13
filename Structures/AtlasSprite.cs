using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cornifer.Structures
{
    public record class AtlasSprite(string Name, Texture2D Texture, Rectangle Frame, Color Color, bool Shade);
}
