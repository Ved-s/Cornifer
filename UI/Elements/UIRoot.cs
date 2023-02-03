using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;

namespace Cornifer.UI.Elements
{
    public class UIRoot : UIContainer
    {
        internal readonly Game Game;

        public MouseState OldMouseState;
        public MouseState MouseState;

        public KeyboardState OldKeyboardState;
        public KeyboardState KeyboardState;

        public Vec2 MousePosition => new(MouseState.X, MouseState.Y);
        public int MouseScroll { get; private set; }

        public UIElement? Hover { get; private set; }
        public new UIElement? Active { get; private set; }

        public override UIRoot Root => this;

        public override SpriteFont? Font
        {
            get => base.Font;
            set { base.Font = value; Recalculate(); }
        }

        private bool DebugMode;

        public KeybindState MouseLeftKey => GetKeyState(MouseState.LeftButton == ButtonState.Pressed, OldMouseState.LeftButton == ButtonState.Pressed);
        public KeybindState MouseMiddleKey => GetKeyState(MouseState.MiddleButton == ButtonState.Pressed, OldMouseState.MiddleButton == ButtonState.Pressed);
        public KeybindState MouseRightKey => GetKeyState(MouseState.RightButton == ButtonState.Pressed, OldMouseState.RightButton == ButtonState.Pressed);
        public KeybindState MouseX1Key => GetKeyState(MouseState.XButton1 == ButtonState.Pressed, OldMouseState.XButton1 == ButtonState.Pressed);
        public KeybindState MouseX2Key => GetKeyState(MouseState.XButton2 == ButtonState.Pressed, OldMouseState.XButton2 == ButtonState.Pressed);

        public static readonly Keys[] AnyCtrlKey = new[] { Keys.LeftControl, Keys.RightControl };
        public static readonly Keys[] AnyAltKey = new[] { Keys.LeftAlt, Keys.RightAlt };
        public static readonly Keys[] AnyShiftKey = new[] { Keys.LeftShift, Keys.RightShift };
        public static readonly Keys[] AnyWinKey = new[] { Keys.LeftWindows, Keys.RightWindows };
        public static readonly Keys[] AnyModKey = new[] { Keys.LeftShift, Keys.RightShift, Keys.LeftAlt, Keys.RightAlt, Keys.LeftControl, Keys.RightControl, Keys.LeftWindows, Keys.RightWindows };

        public KeybindState CtrlKey => GetAnyKeyState(AnyCtrlKey);
        public KeybindState AltKey => GetAnyKeyState(AnyAltKey);
        public KeybindState ShiftKey => GetAnyKeyState(AnyShiftKey);
        public KeybindState WinKey => GetAnyKeyState(AnyWinKey);
        public KeybindState ModKey => GetAnyKeyState(AnyModKey);

        public UIRoot(Game game)
        {
            Game = game;

            Top = new(0);
            Left = new(0);

            Width = new(0, 1);
            Height = new(0, 1);
            Recalculate();
        }

        protected override void PreUpdateChildren()
        {
            OldMouseState = MouseState;
            OldKeyboardState = KeyboardState;

            MouseState = Mouse.GetState();
            KeyboardState = Keyboard.GetState();

            MouseScroll = (MouseState.ScrollWheelValue - OldMouseState.ScrollWheelValue) / 120;

            if (MouseState.LeftButton != ButtonState.Pressed)
            {
                UIElement? currentHover = Game.IsActive ? GetElementAt(MousePosition, false) : null;

                if (currentHover != Hover)
                {
                    if (Hover is not null)
                        Hover.Hovered = false;

                    Hover = currentHover;
                    if (currentHover is not null)
                        currentHover.Hovered = true;
                }
            }
            if (MouseLeftKey == KeybindState.JustPressed && Hover != Active)
            {
                if (Active is not null)
                    Active.Active = false;

                Active = null;
                if (Hover is not null && Hover.Enabled && Hover.CanActivate)
                {
                    Active = Hover;
                    Active.Active = true;
                }
            }

            if (GetKeyState(Keys.F9) == KeybindState.JustPressed)
                DebugMode = !DebugMode;
        }
        protected override void PostDrawChildren(SpriteBatch spriteBatch)
        {
            string? hoverText = Hover?.HoverText;

            if (hoverText is not null && Font is not null)
            {
                Vec2 hoverTextSize = (Vec2)Font.MeasureString(hoverText);
                Vec2 pos = MousePosition;

                Vec2 maxPos = ScreenRect.Size - hoverTextSize;
                if (pos.Y > maxPos.Y)
                    pos.Y = maxPos.Y;
                if (pos.X > maxPos.X)
                    pos.X = maxPos.X;

                spriteBatch.DrawStringShaded(Font, hoverText, pos, Color.White, Color.Black);
            }

            if (!DebugMode)
                return;

            if (Hover is not null)
            {
                spriteBatch.DrawRectangle(Hover.ScreenRect, Color.Yellow);

                if (Hover is UIContainer container)
                    spriteBatch.DrawRectangle(Hover.ScreenRect, Color.CornflowerBlue * .3f, container.Padding);
                spriteBatch.DrawRectangle(Hover.ScreenRect - Hover.Margin, Color.Magenta * .3f, Hover.Margin);

                StringBuilder sb = new();

                sb.Append(Hover.GetType().Name);
                if (Hover.Name is not null)
                    sb.Append($" {Hover.Name}");
                if (Hover == Active)
                    sb.Append("\nactive");
                sb.Append($"\n{Hover.ScreenRect.Width:0.##}x{Hover.ScreenRect.Height:0.##} @ {Hover.ScreenRect.X:0.##}, {Hover.ScreenRect.Y:0.##}");

                DrawDebugText(spriteBatch, Hover.ScreenRect.Center, sb.ToString(), Color.Yellow);

                if (Hover.Parent is not null)
                {
                    spriteBatch.DrawRectangle(Hover.Parent.ScreenRect, Color.Orange * .3f);
                }
            }

            if (Active is not null && Active != Hover)
            {
                spriteBatch.DrawRectangle(Active.ScreenRect, Color.Lime * .3f);
                DrawDebugText(spriteBatch, Active.ScreenRect.Center, "active", Color.Lime * .5f);
            }
        }

        public override void Recalculate()
        {
            Viewport vp = Game.GraphicsDevice.Viewport;

            ScreenRect.Width = CalculateSize(Width, MinWidth, MaxWidth, vp.Width - Margin.Horizontal, out MinSize.X, out MaxSize.X);
            ScreenRect.Height = CalculateSize(Height, MinHeight, MaxHeight, vp.Height - Margin.Vertical, out MinSize.Y, out MaxSize.Y);

            ScreenRect.X = Margin.Left + Left.Calculate(vp.Width, ScreenRect.Width - Margin.Horizontal);
            ScreenRect.Y = Margin.Top + Top.Calculate(vp.Height, ScreenRect.Height - Margin.Vertical);

            foreach (UIElement element in Elements)
                element.Recalculate();
        }

        public KeybindState GetKeyState(Keys key)
        {
            return GetKeyState(KeyboardState[key] == KeyState.Down, OldKeyboardState[key] == KeyState.Down);
        }
        public KeybindState GetKeyState(bool newPressed, bool oldPressed)
        {
            return (KeybindState)((newPressed ? 1 : 0) << 1 | (oldPressed ? 1 : 0));
        }
        public KeybindState GetKeybindState(Keys[] keys)
        {
            KeybindState state = KeybindState.Pressed;

            foreach (Keys key in keys)
            {
                KeybindState keyState = GetKeyState(key);
                if (keyState == KeybindState.Released)
                    return KeybindState.Released;

                if (keyState == KeybindState.JustReleased && state >= KeybindState.JustPressed)
                    state = KeybindState.JustReleased;

                if (keyState == KeybindState.JustPressed && state > KeybindState.JustPressed)
                    state = KeybindState.JustPressed;
            }

            return state;
        }
        public KeybindState GetAnyKeyState(Keys[] keys)
        {
            KeybindState state = KeybindState.Released;

            foreach (Keys key in keys)
            {
                KeybindState keyState = GetKeyState(key);
                if (keyState == KeybindState.Pressed)
                    return KeybindState.Pressed;

                if (keyState == KeybindState.Released)
                    continue;

                if (keyState == KeybindState.JustPressed)
                    state = KeybindState.JustPressed;

                if (keyState == KeybindState.JustReleased && state != KeybindState.JustPressed)
                    state = KeybindState.JustReleased;
            }

            return state;
        }

        void DrawDebugText(SpriteBatch spriteBatch, Vec2 center, string text, Color color, bool vertical = false)
        {
            if (Font is null)
                return;

            Vec2 size = (Vec2)Font.MeasureString(text);

            if (vertical)
                (size.X, size.Y) = (size.Y, size.X);

            Vec2 pos = center - size / 2;

            if (pos.X < 0)
                pos.X = 0;
            if (pos.Y < 0)
                pos.Y = 0;
            if (pos.X + size.X > Game.GraphicsDevice.Viewport.Width)
                pos.X = Game.GraphicsDevice.Viewport.Width - size.X;
            if (pos.Y + size.Y > Game.GraphicsDevice.Viewport.Height)
                pos.Y = Game.GraphicsDevice.Viewport.Height - size.Y;

            if (vertical)
                pos.X += size.X;

            float angle = vertical ? -MathF.PI / 2 : 0;

            foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                Vec2 linePos = pos + new Vec2((size.X - Font.MeasureString(line).X) / 2, 0);


                spriteBatch.DrawString(Font, line, linePos + new Vec2(1, 0), Color.Black, angle, Vec2.Zero, 1f, SpriteEffects.None, 0);
                spriteBatch.DrawString(Font, line, linePos + new Vec2(0, 1), Color.Black, angle, Vec2.Zero, 1f, SpriteEffects.None, 0);
                spriteBatch.DrawString(Font, line, linePos + new Vec2(-1, 0), Color.Black, angle, Vec2.Zero, 1f, SpriteEffects.None, 0);
                spriteBatch.DrawString(Font, line, linePos + new Vec2(0, -1), Color.Black, angle, Vec2.Zero, 1f, SpriteEffects.None, 0);
                spriteBatch.DrawString(Font, line, linePos, color, angle, Vec2.Zero, 1f, SpriteEffects.None, 0);

                pos.Y += Font.LineSpacing;
            }
        }
    }
}
