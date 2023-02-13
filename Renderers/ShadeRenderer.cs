using Cornifer.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.Renderers
{
    public class ShadeRenderer : ScreenRenderer
    {
        public bool TargetNeedsClear;

        public override float Scale => 1;
        public override Vector2 Size => MapObject.ShadeRenderTarget?.Size() ?? Vector2.One;

        public ShadeRenderer(SpriteBatch spriteBatch) : base(spriteBatch) { }

        public override void DrawTexture(Texture2D texture, Vector2 worldPos, Rectangle? source, Vector2? worldSize, Color? color, Vector2? scaleOverride = null)
        {
            UI.Structures.SpriteBatchState state = Main.SpriteBatch.GetState();

            Main.SpriteBatch.End();
            RenderTargetBinding[] targets = Main.Instance.GraphicsDevice.GetRenderTargets();
            Main.Instance.GraphicsDevice.SetRenderTarget(MapObject.ShadeRenderTarget);

            if (TargetNeedsClear)
                Main.Instance.GraphicsDevice.Clear(Color.Transparent);

            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            base.DrawTexture(texture, worldPos, source, worldSize, color, scaleOverride);

            Main.SpriteBatch.End();
            Main.Instance.GraphicsDevice.SetRenderTargets(targets);
            Main.SpriteBatch.Begin(state);

            TargetNeedsClear = false;

        }
    }
}
