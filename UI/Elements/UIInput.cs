using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cornifer.UI.Elements
{
    public class UIInput : UIElement
    {
        public static readonly ElementEvent<Empty, UIInput> TextChangedEvent = new();
        public static readonly ElementEvent<TextInputEventArgs, UIInput> CharacterTypedEvent = new();

        public Offset4 Padding = new(3, 3);
        public Vec2 TextAlign;

        public Color TextColor = Color.White;
        public Color BackColor = new(48, 48, 48);
        public Color BorderColor = new(100, 100, 100);
        public Color SelectionColor = Color.CornflowerBlue;

        public Point CaretPos;
        public Point SelectionStartPos;
        public Point SelectionEndPos;

        public virtual string Text
        {
            get => string.Join('\n', Lines);
            set
            {
                if (!PreTextChanged())
                    return;

                if (!Multiline)
                {
                    if (Lines.Count < 1)
                        Lines.Add(new());
                    while (Lines.Count > 1)
                        Lines.RemoveAt(1);

                    Lines[0].Clear();
                    Lines[0].Append(value);
                    Lines[0].Replace("\n", "");

                    PostTextChanged();

                    return;
                }
                string[] lines = value.Split('\n');

                int max = Math.Max(lines.Length, Lines.Count);

                for (int i = 0; i < max; i++)
                {
                    if (lines.Length <= i)
                    {
                        Lines.RemoveAt(Lines.Count - 1);
                        continue;
                    }
                    if (Lines.Count <= i)
                    {
                        string line = lines[i];
                        Lines.Add(new(line));
                        continue;
                    }
                    StringBuilder builder = Lines[i];
                    builder.Clear();
                    builder.Append(Lines[i]);
                }

                FixCaretPos(ref CaretPos);
                FixCaretPos(ref SelectionStartPos);
                FixCaretPos(ref SelectionEndPos);

                PostTextChanged();
            }
        }

        public virtual bool Multiline
        {
            get => multiline;
            set
            {
                multiline = value;
                if (!value && Lines.Count > 1)
                {
                    StringBuilder fullLine = new();
                    foreach (StringBuilder line in Lines)
                        fullLine.Append(line);
                    Lines.Clear();
                    Lines.Append(fullLine);
                }
            }
        }

        public StringBuilder.ChunkEnumerator? FirstLineChunks => Lines.Count == 0 ? null : Lines[0].GetChunks();
        public IEnumerable<StringBuilder.ChunkEnumerator> LineEnumerator => Lines.Select(sb => sb.GetChunks());

        protected List<StringBuilder> Lines = new();
        float[] LineWidths = Array.Empty<float>();
        float MaxLineWidth;
        StringBuilder? PartialString;
        int BlinkerCounter;
        Point SelectionBegin;
        int KeyRepeatCounter;
        int KeyRepeatCounterMax;
        private bool multiline = true;

        static RasterizerState Scissors = new() { ScissorTestEnable = true };

        protected override void UpdateSelf()
        {
            if (Font is null)
                return;

            if (Hovered && Root.MouseState.LeftButton == ButtonState.Pressed)
            {
                Point caretPos = Point.Zero;

                if (Lines.Count > 0)
                {
                    Vec2 mouse = RelativeMouse;
                    caretPos.Y = Lines.Count == 0 ? 0 : (int)Math.Clamp(RelativeMouse.Y / Font.LineSpacing, 0, Lines.Count - 1);
                    StringBuilder line = Lines[caretPos.Y];
                    float x = (MaxLineWidth - LineWidths[caretPos.Y]) * TextAlign.X + Padding.Left;

                    if (line.Length == 0 || mouse.X <= x)
                        caretPos.X = 0;
                    else if (mouse.X > x + LineWidths[caretPos.Y])
                        caretPos.X = line.Length;
                    else
                    {
                        caretPos.X = 0;
                        float w = GetPartialStringWidth(line, 0, 1);
                        while (mouse.X > x + w)
                        {
                            if (line.Length <= caretPos.X + 1)
                                break;

                            caretPos.X++;
                            w = GetPartialStringWidth(line, 0, caretPos.X + 1);
                        }

                        if (line.Length > caretPos.X + 1)
                        {
                            w = GetPartialStringWidth(line, 0, caretPos.X);
                            float nextW = GetPartialStringWidth(line, 0, caretPos.X + 1);
                            float diffNow = Math.Abs(mouse.X - (x + w));
                            float diffNext = Math.Abs(mouse.X - (x + nextW));

                            if (diffNext < diffNow)
                                caretPos.X++;
                        }
                    }
                }

                CaretPos = caretPos;

                if (Root.MouseLeftKey == KeybindState.JustPressed)
                {
                    SelectionStartPos = caretPos;
                    SelectionEndPos = caretPos;
                    SelectionBegin = caretPos;
                }
                else if (Root.MouseLeftKey == KeybindState.Pressed)
                {
                    int diff = CompareCarets(caretPos, SelectionBegin);
                    if (diff == 0)
                        SelectionEndPos = SelectionStartPos = caretPos;
                    else if (diff < 0)
                    {
                        SelectionStartPos = caretPos;
                        SelectionEndPos = SelectionBegin;
                    }
                    else
                    {
                        SelectionStartPos = SelectionBegin;
                        SelectionEndPos = caretPos;
                    }
                }
                BlinkerCounter = 0;
            }

            FixCaretPos(ref CaretPos);
            FixCaretPos(ref SelectionStartPos);
            FixCaretPos(ref SelectionEndPos);

            if (SelectionStartPos != SelectionEndPos)
            {
                if (SelectionStartPos.Y > SelectionEndPos.Y)
                    (SelectionStartPos.Y, SelectionEndPos.Y) = (SelectionEndPos.Y, SelectionStartPos.Y);

                if (SelectionStartPos.Y == SelectionEndPos.Y)
                {
                    if (SelectionStartPos.X > SelectionEndPos.X)
                        (SelectionStartPos.X, SelectionEndPos.X) = (SelectionEndPos.X, SelectionStartPos.X);
                }
            }

            if (Active)
            {
                if (Lines.Count == 1 && Lines[0].Length > 0 || Lines.Count > 1)
                {
                    if (CheckKeyTriggered(Keys.Right))
                    {
                        BlinkerCounter = 0;
                        StringBuilder line = Lines[CaretPos.Y];
                        if (CaretPos.Y < Lines.Count - 1 || CaretPos.X < line.Length)
                        {
                            CaretPos.X++;
                            if (CaretPos.X > line.Length)
                            {
                                CaretPos.Y++;
                                CaretPos.X = 0;
                            }
                        }
                    }
                    if (CheckKeyTriggered(Keys.Left))
                    {
                        BlinkerCounter = 0;
                        StringBuilder line = Lines[CaretPos.Y];
                        if (CaretPos.Y > 0 || CaretPos.X > 0)
                        {
                            CaretPos.X--;
                            if (CaretPos.X < 0)
                            {
                                CaretPos.Y--;
                                line = Lines[CaretPos.Y];
                                CaretPos.X = line.Length;
                            }
                        }
                    }
                    if (CheckKeyTriggered(Keys.Up))
                    {
                        BlinkerCounter = 0;
                        if (CaretPos.Y > 0)
                        {
                            CaretPos.Y--;
                            StringBuilder line = Lines[CaretPos.Y];
                            CaretPos.X = Math.Clamp(CaretPos.X, 0, line.Length);
                        }
                    }
                    if (CheckKeyTriggered(Keys.Down))
                    {
                        BlinkerCounter = 0;
                        if (CaretPos.Y < Lines.Count - 1)
                        {
                            CaretPos.Y++;
                            StringBuilder line = Lines[CaretPos.Y];
                            CaretPos.X = Math.Clamp(CaretPos.X, 0, line.Length);
                        }
                    }

                    if (CheckKeyTriggered(Keys.Back))
                    {
                        if (SelectionEndPos != SelectionStartPos || CaretPos != Point.Zero)
                        {
                            if (!PreTextChanged())
                                return;

                            if (SelectionEndPos != SelectionStartPos)
                                ClearSelection();

                            else if (CaretPos.Y > 0 || CaretPos.X > 0)
                            {
                                StringBuilder line = Lines[CaretPos.Y];
                                if (CaretPos.X == 0 && CanRemoveChar(new(Lines[CaretPos.Y - 1].Length, CaretPos.Y - 1), '\n'))
                                {
                                    CaretPos.Y--;
                                    StringBuilder lineBefore = Lines[CaretPos.Y];
                                    CaretPos.X = lineBefore.Length;
                                    lineBefore.Append(line);
                                    Lines.RemoveAt(CaretPos.Y + 1);
                                }
                                else if (CanRemoveChar(CaretPos with { X = CaretPos.X - 1 }, line[CaretPos.X - 1]))
                                    line.Remove(--CaretPos.X, 1);
                            }
                            PostTextChanged();
                        }
                    }
                    if (CheckKeyTriggered(Keys.Delete))
                    {
                        if (SelectionEndPos != SelectionStartPos || CaretPos.Y != Lines.Count - 1 || CaretPos.X < Lines[^1].Length)
                        {
                            if (!PreTextChanged())
                                return;

                            if (SelectionEndPos != SelectionStartPos)
                                ClearSelection();

                            else
                            {
                                StringBuilder line = Lines[CaretPos.Y];
                                if (CaretPos.X == line.Length && CanRemoveChar(CaretPos, '\n'))
                                {
                                    StringBuilder lineAfter = Lines[CaretPos.Y + 1];
                                    line.Append(lineAfter);
                                    Lines.RemoveAt(CaretPos.Y + 1);
                                }
                                else if (CanRemoveChar(CaretPos, line[CaretPos.X]))
                                    line.Remove(CaretPos.X, 1);
                            }
                            PostTextChanged();
                        }
                    }

                    if (CheckKeyTriggered(Keys.Home))
                        CaretPos.X = 0;

                    if (CheckKeyTriggered(Keys.End))
                        CaretPos.X = Lines[CaretPos.Y].Length;

                    if (CheckKeyTriggered(Keys.Enter) && Multiline)
                    {
                        if (PreTextChanged())
                        {
                            ClearSelection();

                            StringBuilder line = Lines[CaretPos.Y];
                            StringBuilder newLine = new();
                            newLine.Append(line, CaretPos.X, line.Length - CaretPos.X);
                            line.Remove(CaretPos.X, line.Length - CaretPos.X);
                            Lines.Insert(CaretPos.Y + 1, newLine);
                            CaretPos.X = 0;
                            CaretPos.Y++;
                            PostTextChanged();
                        }
                    }

                    if (Root.CtrlKey > KeybindState.JustPressed)
                    {
                        if (Root.GetKeyState(Keys.A) == KeybindState.JustPressed)
                        {
                            SelectionStartPos = Point.Zero;
                            CaretPos = SelectionEndPos = new(Lines[^1].Length, Lines.Count - 1);
                        }
                    }
                }

            }

            if (LineWidths.Length < Lines.Count)
                LineWidths = new float[Lines.Count];

            MaxLineWidth = ScreenRect.Width - Padding.Horizontal;
            for (int i = 0; i < Lines.Count; i++)
            {
                float lineWidth = Font.MeasureString(Lines[i]).X;
                MaxLineWidth = Math.Max(MaxLineWidth, lineWidth);
                LineWidths[i] = lineWidth;
            }
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            spriteBatch.FillRectangle(ScreenRect, BackColor);
            spriteBatch.DrawRectangle(ScreenRect, BorderColor);

            if (Font is null)
                return;

            Rect rect = ScreenRect;
            rect.X += Padding.Left - 1;
            rect.Y += Padding.Top;
            rect.Width -= Padding.Horizontal - 2;
            rect.Height -= Padding.Vertical;

            spriteBatch.PushAndChangeState(rasterizerState: Scissors);

            Rectangle oldScissors = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = (Rectangle)rect.Intersect(oldScissors);

            float y = ScreenRect.Y + Padding.Top;
            float x;
            if (LineWidths.Length >= Lines.Count)
                for (int i = 0; i < Lines.Count; i++)
                {
                    StringBuilder line = Lines[i];
                    if (y >= ScreenRect.Y + Padding.Top - Font.LineSpacing || y < ScreenRect.Bottom - Padding.Bottom)
                    {
                        x = (MaxLineWidth - LineWidths[i]) * TextAlign.X + ScreenRect.X + Padding.Left;

                        if (SelectionEndPos != SelectionStartPos)
                        {
                            if (SelectionStartPos.Y < i && i < SelectionEndPos.Y) // Line is fully selected
                                spriteBatch.FillRectangle(x - 1, y - 1, LineWidths[i], Font.LineSpacing, SelectionColor);
                            else if (SelectionStartPos.Y == i && i < SelectionEndPos.Y) // Test t[est test test
                            {
                                float start = GetPartialStringWidth(line, 0, SelectionStartPos.X);
                                spriteBatch.FillRectangle(x + start - 1, y - 1, LineWidths[i] - start + 1, Font.LineSpacing, SelectionColor);
                            }
                            else if (SelectionStartPos.Y < i && i == SelectionEndPos.Y) // Test test tes]t test
                            {
                                if (SelectionEndPos.X == 0)
                                {
                                    spriteBatch.FillRectangle(x - 1, y - 1, 1, Font.LineSpacing, SelectionColor);
                                }
                                else
                                {
                                    float start = GetPartialStringWidth(line, 0, SelectionEndPos.X);
                                    spriteBatch.FillRectangle(x - 1, y - 1, start, Font.LineSpacing, SelectionColor);
                                }
                            }
                            else if (SelectionStartPos.Y == i && i == SelectionEndPos.Y) // Test t[est test] test
                            {
                                float start = GetPartialStringWidth(line, 0, SelectionStartPos.X);
                                float len = GetPartialStringWidth(line, SelectionStartPos.X, SelectionEndPos.X - SelectionStartPos.X);
                                spriteBatch.FillRectangle(x - 1 + start, y - 1, len, Font.LineSpacing, SelectionColor);
                            }

                        }

                        spriteBatch.DrawString(Font, line, new Vec2(x, y).Rounded(), TextColor);
                    }
                    y += Font.LineSpacing;
                }

            if (Active)
            {
                if (BlinkerCounter < 30)
                {
                    y = ScreenRect.Y + Padding.Top + CaretPos.Y * Font.LineSpacing;
                    x = (MaxLineWidth - (Lines.Count == 0 ? 0 : LineWidths[CaretPos.Y])) * TextAlign.X + ScreenRect.X + Padding.Left;
                    if (Lines.Count > 0 && CaretPos.X > 0)
                        x += GetPartialStringWidth(Lines[CaretPos.Y], 0, CaretPos.X);

                    spriteBatch.FillRectangle(new(x - 1, y - 1, 1, Font.LineSpacing), Color.White);
                }

                BlinkerCounter++;
                BlinkerCounter %= 60;
            }
            else
                BlinkerCounter = 0;

            spriteBatch.RestoreState();
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissors;
        }

        protected override void ActiveChanged()
        {
            if (Active)
                Root.Game.Window.TextInput += TextInput;
            else
                Root.Game.Window.TextInput -= TextInput;
        }

        protected virtual void TextInput(object? sender, TextInputEventArgs e)
        {
            if (e.Key == Keys.Back || e.Key == Keys.Delete || e.Key == Keys.Enter)
                return;

            if (!Events.PreCall(CharacterTypedEvent, e))
                return;

            if (!PreTextChanged())
                return;

            ClearSelection();

            if (Lines.Count == 0)
                Lines.Add(new());

            StringBuilder line = Lines[CaretPos.Y];
            line.Insert(CaretPos.X, e.Character);
            CaretPos.X++;
            PostTextChanged();
            Events.PostCall(CharacterTypedEvent, e);
        }
        protected virtual bool CanRemoveChar(Point pos, char character) => true;
        protected virtual void ClearSelection()
        {
            if (SelectionEndPos == SelectionStartPos)
                return;

            if (SelectionEndPos.Y == SelectionStartPos.Y) // Text te[xt text tex]t text
            {
                StringBuilder line = Lines[SelectionStartPos.Y];
                line.Remove(SelectionStartPos.X, SelectionEndPos.X - SelectionStartPos.X);
            }
            else
            {
                StringBuilder startLine = Lines[SelectionStartPos.Y];
                startLine.Remove(SelectionStartPos.X, startLine.Length - SelectionStartPos.X);

                StringBuilder endLine = Lines[SelectionEndPos.Y];
                endLine.Remove(0, SelectionEndPos.X);
                startLine.Append(endLine);

                for (int i = SelectionStartPos.Y; i < SelectionEndPos.Y; i++)
                    Lines.RemoveAt(SelectionStartPos.Y + 1);
            }

            CaretPos = SelectionStartPos;
            SelectionStartPos = SelectionEndPos = Point.Zero;
        }

        protected virtual bool PreTextChanged() => Events.PreCall(TextChangedEvent, default);
        protected virtual void PostTextChanged() => Events.PostCall(TextChangedEvent, default);

        float GetPartialStringWidth(StringBuilder builder, int start, int length)
        {
            if (Font is null)
                return 0;

            PartialString ??= new();
            PartialString.Clear();
            PartialString.Append(builder, start, length);

            return Font.MeasureString(PartialString).X;
        }
        int CompareCarets(Point a, Point b)
        {
            if (a.Y < b.Y)
                return -1;
            if (a.Y > b.Y)
                return 1;

            if (a.X < b.X)
                return -1;
            if (a.X > b.X)
                return 1;

            return 0;
        }
        void FixCaretPos(ref Point pos)
        {
            pos.Y = Lines.Count == 0 ? 0 : Math.Clamp(pos.Y, 0, Lines.Count - 1);
            if (pos.X != 0)
            {
                if (Lines.Count == 0)
                    pos.X = 0;
                else
                {
                    StringBuilder line = Lines[pos.Y];
                    if (line.Length == 0)
                        pos.X = 0;
                    else
                        pos.X = Math.Clamp(pos.X, 0, line.Length);
                }
            }
        }
        bool CheckKeyTriggered(Keys key)
        {
            if (Root.KeyboardState[key] != KeyState.Down)
                return false;

            if (Root.GetKeyState(key) == KeybindState.JustPressed)
            {
                KeyRepeatCounter = 0;
                KeyRepeatCounterMax = 45;
                return true;
            }
            KeyRepeatCounter++;

            if (KeyRepeatCounter < KeyRepeatCounterMax)
                return false;

            KeyRepeatCounter = 0;
            KeyRepeatCounterMax = 2;
            return true;
        }
    }
}
