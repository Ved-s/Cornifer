using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public class SlugcatIcon : SelectableIcon
    {
        public static bool DrawIcons = true;
        public static bool DrawDiamond;

        public int Id;

        public bool ForceSlugcatIcon;

        public override bool Active => DrawIcons || ForceSlugcatIcon;
        public override Vector2 Size => DrawDiamond && !ForceSlugcatIcon ? new(9) : new(8);

        public override void DrawIcon(Renderer renderer)
        {
            Rectangle frame = DrawDiamond && !ForceSlugcatIcon ? new(Id*9, 8, 9, 9) : new(Id*8, 0, 8, 8);

            renderer.DrawTexture(Content.SlugcatIcons, WorldPosition, frame);
        }
    }
}
