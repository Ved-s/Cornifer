using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Cornifer.UI.Elements
{
    public class UIResizeablePanel : UIPanel
    {
        public Color GrabFadeColor = Color.White * .7f;

        private float FadeT, FadeTR, FadeR, FadeBR, FadeB, FadeBL, FadeL, FadeTL;

        public virtual bool CanGrabTop { get; set; } = true;
        public virtual bool CanGrabLeft { get; set; } = true;
        public virtual bool CanGrabRight { get; set; } = true;
        public virtual bool CanGrabBottom { get; set; } = true;

        public virtual bool SizingChangesPosition { get; set; } = true;

        public int GrabRange = 10;
        public float FadeStep = 0.08f;

        private bool Grabbed = false;
        private bool GrabbedTop = false;
        private bool GrabbedLeft = false;
        private bool GrabbedRight = false;
        private bool GrabbedBottom = false;
        private Vec2 GrabMousePos;

        protected override void PreUpdateChildren()
        {
            bool top = false;
            bool left = false;
            bool right = false;
            bool bottom = false;

            if (Grabbed && Root.MouseLeftKey != KeybindState.Pressed)
                Grabbed = false;

            if (Grabbed)
            {
                top = GrabbedTop;
                left = GrabbedLeft;
                right = GrabbedRight;
                bottom = GrabbedBottom;
            }

            if (Hovered)
            {
                Vec2 mouse = RelativeMouse;

                if (!Grabbed)
                {
                    top = CanGrabTop && mouse.Y < GrabRange;
                    left = CanGrabLeft && mouse.X < GrabRange;
                    right = CanGrabRight && mouse.X > ScreenRect.Width - GrabRange;
                    bottom = CanGrabBottom && mouse.Y > ScreenRect.Height - GrabRange;
                }

                if (top)
                {
                    if (left) FadeTL = Math.Min(1f, FadeTL + FadeStep);
                    else if (right) FadeTR = Math.Min(1f, FadeTR + FadeStep);
                    else FadeT = Math.Min(1f, FadeT + FadeStep);
                }
                else if (bottom)
                {
                    if (left) FadeBL = Math.Min(1f, FadeBL + FadeStep);
                    else if (right) FadeBR = Math.Min(1f, FadeBR + FadeStep);
                    else FadeB = Math.Min(1f, FadeB + FadeStep);
                }
                else if (left) FadeL = Math.Min(1f, FadeL + FadeStep);
                else if (right) FadeR = Math.Min(1f, FadeR + FadeStep);

                if (Root.MouseLeftKey == KeybindState.JustPressed && (top || left || right || bottom) && !Grabbed)
                {
                    Grabbed = true;
                    GrabMousePos = Root.MousePosition;

                    GrabbedTop = top;
                    GrabbedLeft = left;
                    GrabbedRight = right;
                    GrabbedBottom = bottom;
                }
            }

            if (FadeT > 0 && (!top || right || left)) FadeT = Math.Max(0f, FadeT - FadeStep);
            if (FadeTR > 0 && !(top && right)) FadeTR = Math.Max(0f, FadeTR - FadeStep);
            if (FadeR > 0 && (!right || top || bottom)) FadeR = Math.Max(0f, FadeR - FadeStep);
            if (FadeBR > 0 && !(bottom && right)) FadeBR = Math.Max(0f, FadeBR - FadeStep);
            if (FadeB > 0 && (!bottom || right || left)) FadeB = Math.Max(0f, FadeB - FadeStep);
            if (FadeBL > 0 && !(bottom && left)) FadeBL = Math.Max(0f, FadeBL - FadeStep);
            if (FadeL > 0 && (!left || top || bottom)) FadeL = Math.Max(0f, FadeL - FadeStep);
            if (FadeTL > 0 && !(top && left)) FadeTL = Math.Max(0f, FadeTL - FadeStep);

            if (Grabbed && Root.MousePosition != GrabMousePos)
            {
                Vec2 mouse = Root.MousePosition;
                Vec2 diff = mouse - GrabMousePos;

                if (diff.X > 0 && mouse.X < ScreenRect.Left)
                    diff.X = 0;

                if (diff.Y > 0 && mouse.Y < ScreenRect.Top)
                    diff.Y = 0;

                if (diff.X < 0 && mouse.X > ScreenRect.Right)
                    diff.X = 0;

                if (diff.Y < 0 && mouse.Y > ScreenRect.Bottom)
                    diff.Y = 0;

                if (diff != Vec2.Zero)
                {

                    if (GrabbedTop && SizingChangesPosition)
                    {
                        float max = MaxSize.Y - ScreenRect.Height;
                        float min = MinSize.Y - ScreenRect.Height;
                        Top.Value += Math.Clamp(diff.Y, min, max);
                    }

                    if (GrabbedLeft && SizingChangesPosition)
                    {
                        float max = MaxSize.X - ScreenRect.Width;
                        float min = MinSize.X - ScreenRect.Width;
                        Left.Value += Math.Clamp(diff.X, min, max);
                    }

                    if (GrabbedRight || GrabbedLeft && !SizingChangesPosition)
                    {
                        float max = MaxSize.X - ScreenRect.Width;
                        float min = MinSize.X - ScreenRect.Width;
                        Width.Value += Math.Clamp(-diff.X, min, max);
                    }

                    if (GrabbedBottom || GrabbedTop && !SizingChangesPosition)
                    {
                        float max = MaxSize.Y - ScreenRect.Height;
                        float min = MinSize.Y - ScreenRect.Height;
                        Height.Value += Math.Clamp(-diff.Y, min, max);
                    }

                    Recalculate();
                }

                GrabMousePos = Root.MousePosition;
            }
        }

        protected override void PostDrawChildren(SpriteBatch spriteBatch)
        {
            float w = ScreenRect.Width - GrabRange;
            float h = ScreenRect.Height - GrabRange;

            if (FadeT > 0) spriteBatch.FillRectangle(new(ScreenRect.Position, new(ScreenRect.Width, GrabRange)), GrabFadeColor * FadeT);
            if (FadeR > 0) spriteBatch.FillRectangle(new(ScreenRect.Position + new Vec2(w, 0), new(GrabRange, ScreenRect.Height)), GrabFadeColor * FadeR);
            if (FadeB > 0) spriteBatch.FillRectangle(new(ScreenRect.Position + new Vec2(0, h), new(ScreenRect.Width, GrabRange)), GrabFadeColor * FadeB);
            if (FadeL > 0) spriteBatch.FillRectangle(new(ScreenRect.Position, new(GrabRange, ScreenRect.Height)), GrabFadeColor * FadeL);

            if (FadeTL > 0) spriteBatch.FillRectangle(new(ScreenRect.Position, new(GrabRange)), GrabFadeColor * FadeTL);
            if (FadeTR > 0) spriteBatch.FillRectangle(new(ScreenRect.Position + new Vec2(w, 0), new(GrabRange)), GrabFadeColor * FadeTR);
            if (FadeBL > 0) spriteBatch.FillRectangle(new(ScreenRect.Position + new Vec2(0, h), new(GrabRange)), GrabFadeColor * FadeBL);
            if (FadeBR > 0) spriteBatch.FillRectangle(new(ScreenRect.Position + new Vec2(w, h), new(GrabRange)), GrabFadeColor * FadeBR);
        }
    }
}
