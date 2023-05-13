using Cornifer.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Elements
{
    public class UIFormattedLabel : UIElement
    {
        public string? Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;
                Recalculate();
            }
        }

        public Vec2 TextAlign;
        public Color TextColor = Color.White;
        public Color? TextShadowColor = Color.Transparent;
        public float TextScale = 1;

        private Vec2 TextSize;
        private string? text;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Font is null)
                return;

            SpriteBatchState state = spriteBatch.GetState();

            spriteBatch.End();
            spriteBatch.Begin(state with { SamplerState = SamplerState.PointClamp });

            Vec2 textPos = ScreenRect.Position + (ScreenRect.Size - TextSize) * TextAlign;
            
            FormattedText.Draw(spriteBatch, Font, Text, textPos.Rounded(), TextColor, TextShadowColor ?? default, TextShadowColor.HasValue ? 1 : 0, TextScale);

            spriteBatch.End();
            spriteBatch.Begin(state);
        }

        public override void Recalculate()
        {
            MinWidth = 0;
            MinHeight = 0;

            base.Recalculate();

            TextSize = Font is null ? Vec2.Zero : (Vec2)FormattedText.Measure(Font, Text, TextScale);

            MinWidth = TextSize.X;
            MinHeight = TextSize.Y;

            base.Recalculate();
        }
    }
}
