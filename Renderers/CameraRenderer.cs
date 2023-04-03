using Cornifer.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace Cornifer.Renderers
{
    public class CameraRenderer : ScreenRenderer
    {
        public bool Dragging { get; private set; }

        private Vector2 DragPos;
        private int WheelValue;
        private float WheelZoom;

        const float MaxZoom = 300;
        const float MinZoom = -500;

        public CameraRenderer(SpriteBatch spriteBatch) : base(spriteBatch)
        {
        }

        public void Update()
        {
            MouseState state = Mouse.GetState();

            Vector2 screenPos = state.Position.ToVector2();

            bool drag = Main.Instance.IsActive && !Interface.Hovered && !Main.Dragging && !Main.Selecting && InputHandler.Pan.Pressed;

            UpdateDragging(drag, screenPos);

            float wheel = (state.ScrollWheelValue - WheelValue) / 120;
            WheelValue = state.ScrollWheelValue;

            if (wheel == 0 || Interface.Hovered)
                return;

            WheelZoom += Math.Sign(wheel) * (float)Math.Pow(2, Math.Abs(wheel));

            if (WheelZoom > MaxZoom)
                WheelZoom = MaxZoom;

            if (WheelZoom < MinZoom)
                WheelZoom = MinZoom;

            float zoom = WheelZoom < 0 ? -1 / (0.2f * WheelZoom - 1) : 0.2f * WheelZoom + 1;
            SetScale(zoom, screenPos);
        }

        void SetScale(float scale, Vector2 at)
        {
            if (scale == Scale) return;
            Vector2 atWorldBefore = InverseTransformVector(at);
            Scale = scale;
            Vector2 atWorldAfter = InverseTransformVector(at);
            Position -= atWorldAfter - atWorldBefore;
        }

        void UpdateDragging(bool drag, Vector2 screenPos)
        {
            if (drag && !Dragging)
            {
                DragPos = screenPos;
                Dragging = true;
            }
            else if (drag && Dragging)
            {
                Position -= (screenPos - DragPos) / Scale;
                DragPos = screenPos;
            }
            else if (!drag && Dragging)
            {
                Dragging = false;
            }
        }
    }
}
