using Cornifer.MapObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Cornifer.UI.Elements
{
    public class UIDiamondPlacementDisplay : UIElement
    {
        public DiamondPlacement? Placement;
        public List<SlugcatIcon>? Icons;

        public override void Recalculate()
        {
            if (Placement is not null)
            {
                MinWidth = Placement.Size.X;
                MinHeight = Placement.Size.Y;
            }

            base.Recalculate();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Placement is null || Icons is null)
                return;

            Vector2 center = ScreenRect.Center;
            center.Floor();

            spriteBatch.PushAndChangeState(samplerState: SamplerState.PointClamp);

            for (int i = 0; i < Placement.Positions.Length; i++)
            {
                if (i >= Icons.Count)
                    break;

                spriteBatch.Draw(Content.SlugcatIcons, center + Placement.Positions[i], SlugcatIcon.GetFrame(Icons[i].Id, true), Color.White);
            }

            spriteBatch.RestoreState();
        }
    }
}
