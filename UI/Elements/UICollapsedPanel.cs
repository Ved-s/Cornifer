using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;

namespace Cornifer.UI.Elements
{
    public class UICollapsedPanel : UIPanel, ILayoutContainer
    {
        public int HeaderHeight = 20;
        public Vec2 TextAlign = new(0, .5f);
        public string HeaderText = "";

        public UICollapsedPanel()
        {
            Height = HeaderHeight;
        }

        public bool Collapsed
        {
            get => !Content?.Visible ?? false;
            set
            {
                if (Content is null)
                    return;

                Content.Visible = !value;
                PerformLayout();
            }
        }

        [Required]
        public UIElement? Content
        {
            get => content;
            set
            {
                if (content is not null)
                    Elements.Remove(content);

                content = value;

                if (content is not null)
                    Elements.Add(content);

                if (Root is not null)
                    PerformLayout();
            }
        }

        public bool HeaderHovered => Hovered && RelativeMouse.Y < HeaderHeight;

        public override Rect ChildrenRect 
        { 
            get 
            {
                Rect rect = ScreenRect;
                rect.Y += HeaderHeight;
                rect.Height -= HeaderHeight;
                return rect;
            } 
        }

        private UIElement? content;
        private bool PerformingLayout;

        public void LayoutChild(UIElement child, ref Rect screenRect)
        {
            if (PerformingLayout)
                return;

            PerformLayout();
        }

        protected override void PreDrawChildren(SpriteBatch spriteBatch)
        {
            base.PreDrawChildren(spriteBatch);

            Rect innerRect = ScreenRect + Padding;

            if (HeaderHovered)
            {
                spriteBatch.Draw(Main.Pixel, innerRect with { Height = HeaderHeight } + new Offset4(2), Color.White * .1f);
            }

            if (Font is not null)
            {
                Vector2 size = Font.MeasureString(HeaderText);
                Vector2 pos = innerRect.Position + new Vector2(HeaderHeight, 2) + TextAlign * (new Vector2(innerRect.Width - HeaderHeight, HeaderHeight) - size);
                pos.Floor();
                spriteBatch.DrawString(Font, HeaderText, pos, Color.White);
            }

            float triScale = .6f;

            if (Collapsed)
            {
                Vector2 triSize = new Vector2(HeaderHeight * .5f, HeaderHeight) * triScale;
                Vector2 triPos = innerRect.Position + new Vector2(HeaderHeight) / 2 - new Vector2(triSize.X / 2, 0);
                spriteBatch.Draw(Main.Pixel, triPos - new Vector2(0, triSize.Y/2), triPos + new Vector2(triSize.X, 0), triPos, triPos + new Vector2(0, triSize.Y / 2), null, Color.White);
            }
            else
            {
                Vector2 triSize = new Vector2(HeaderHeight, HeaderHeight * .5f) * triScale;
                Vector2 triPos = innerRect.Position + new Vector2(HeaderHeight) / 2 - new Vector2(0, triSize.Y / 2);
                spriteBatch.Draw(Main.Pixel, triPos, triPos + new Vector2(triSize.X / 2, 0), triPos - new Vector2(triSize.X / 2, 0), triPos + new Vector2(0, triSize.Y), null, Color.White);
            }
        }

        protected override void PreUpdateChildren()
        {
            if (HeaderHovered && Root.MouseLeftKey == KeybindState.JustPressed)
                Collapsed = !Collapsed;

            base.PreUpdateChildren();
        }

        public override void Recalculate()
        {
            PerformLayout();
        }


        void PerformLayout()
        {
            if (PerformingLayout)
                return;

            PerformingLayout = true;
            try
            {
                MinHeight = HeaderHeight;

                if (Collapsed)
                {
                    base.Recalculate();
                    return;
                }
                else 
                {
                    SkipContainerLayout = true;
                    base.Recalculate();
                }

                MinHeight = (Content?.ScreenRect.Height ?? 0) + HeaderHeight;
                base.Recalculate();
            }
            finally
            {
                PerformingLayout = false;
            }
        }
    }
}
