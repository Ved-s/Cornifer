using Cornifer.Renderers;
using Cornifer.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.MapObjects
{
    public class SlugcatIcon : SelectableIcon
    {
        public readonly Slugcat Slugcat;

        public bool ForceSlugcatIcon;

        public override bool CanSetActive => true;

        public override int ShadeSize => InterfaceState.DrawSlugcatDiamond.Value ? 1 : 2;
        public override bool Active => (InterfaceState.DrawSlugcatIcons.Value || ForceSlugcatIcon) && base.Active;
        public override Vector2 Size => CurrentSprite?.Frame.Size.ToVector2() ?? new(10);

        public AtlasSprite? DiamondSprite;
        public AtlasSprite? IconSprite;

        public AtlasSprite? CurrentSprite => (InterfaceState.DrawSlugcatDiamond.Value && !ForceSlugcatIcon) ? DiamondSprite : IconSprite;

        public SlugcatIcon(string name, Slugcat slugcat)
        {
            Name = name;
            Slugcat = slugcat;

            DiamondSprite = SpriteAtlases.GetSpriteOrNull($"SlugcatDiamond_{slugcat.Id}");
            IconSprite = SpriteAtlases.GetSpriteOrNull($"SlugcatIcon_{slugcat.Id}");
        }

        public override void DrawIcon(Renderer renderer)
        {
            AtlasSprite? sprite = CurrentSprite;
            if (sprite is null)
                return;

            renderer.DrawTexture(sprite.Texture, WorldPosition, sprite.Frame, null, sprite.Color);
        }

        //public static Rectangle GetFrame(int id, bool diamond)
        //{
        //    return diamond ? new(id * 9, 8, 9, 9) : new(id * 8, 0, 8, 8);
        //}
    }
}
