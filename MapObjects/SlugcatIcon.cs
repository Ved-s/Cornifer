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
        public override bool Active => InterfaceState.DrawSlugcatIcons.Value 
            && (!Hollow || InterfaceState.DrawHollowSlugcatDiamond.Value) 
            && base.Active;
        public override Vector2 Size => CurrentSprite?.Frame.Size.ToVector2() ?? new(10);

        public AtlasSprite? DiamondSprite;
        public AtlasSprite? HollowDiamondSprite;
        public AtlasSprite? IconSprite;

        public bool Hollow = false;

        public AtlasSprite? CurrentSprite => (InterfaceState.DrawSlugcatDiamond.Value && !ForceSlugcatIcon) ?
            Hollow ? HollowDiamondSprite : DiamondSprite 
            : IconSprite;

        public SlugcatIcon(string name, Slugcat slugcat, bool hollow)
        {
            Name = name;
            Slugcat = slugcat;
            Hollow = hollow;

            DiamondSprite = SpriteAtlases.GetSpriteOrNull($"SlugcatDiamond_{slugcat.Id}");
            HollowDiamondSprite = SpriteAtlases.GetSpriteOrNull($"SlugcatHollowDiamond_{slugcat.Id}");
            IconSprite = SpriteAtlases.GetSpriteOrNull($"SlugcatIcon_{slugcat.Id}");
        }

        public override void DrawIcon(Renderer renderer)
        {
            AtlasSprite? sprite = CurrentSprite;
            if (sprite is null)
                return;

            Color color = sprite.Color;
            if (sprite == IconSprite && Hollow)
                color = Color.Gray;

            renderer.DrawTexture(sprite.Texture, WorldPosition, sprite.Frame, null, color);
        }
    }
}
