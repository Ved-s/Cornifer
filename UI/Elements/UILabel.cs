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
    public class UILabel : UIElement
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
        public Color TextShadowColor = Color.Transparent;

        public bool WordWrap = true;

        private string[]? Lines;
        private float MaxLineWidth;
        private string? text;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Lines is null || Font is null)
                return;

            Vec2 pos = TextAlign * (ScreenRect.Size - new Vec2(MaxLineWidth, Lines.Length * Font.LineSpacing));

            foreach (string line in Lines)
            {
                float lineWidth = Font.MeasureString(line).X;
                float xOff = TextAlign.X * (MaxLineWidth - lineWidth);
                Vec2 off = new(xOff, 0);
                if (TextShadowColor == Color.Transparent)
                    spriteBatch.DrawString(Font, line, (ScreenRect.Position + pos + off).Rounded(), TextColor);
                else
                    spriteBatch.DrawStringShaded(Font, line, (ScreenRect.Position + pos + off).Rounded(), TextColor, TextShadowColor);
                pos.Y += Font.LineSpacing;
            }
        }

        public override void Recalculate()
        {
            MinWidth = 0;
            MinHeight = 0;

            base.Recalculate();

            if (Text is null || Font is null || ScreenRect.Width <= 0)
            {
                Lines = null;
                return;
            }

            if (!WordWrap)
            {
                Lines = Text.Split('\n');
                MinHeight = Lines.Length * Font.LineSpacing;
                MaxLineWidth = Lines.Select(l => Font.MeasureString(l).X).Max();
                return;
            }

            List<string> lines = new();
            int lastSplit = 0;
            int lineLength = 0;

            float maxLineWidth = 0;
            Vec2 currentPos = Vec2.Zero;

            void NewLine()
            {
                lines.Add(Text!.Substring(lastSplit, lineLength));
                lastSplit += lineLength;
                lineLength = 0;
                maxLineWidth = Math.Max(maxLineWidth, currentPos.X);
                currentPos.X = 0;
                currentPos.Y += Font.LineSpacing;

            }

            foreach (string word in EnumerateWords(Text))
            {
                Vec2 wordSize = (Vec2)Font.MeasureString(word);

                if (currentPos.X > 0 && currentPos.X + wordSize.X > ScreenRect.Width)
                {
                    NewLine();
                }
                currentPos.X += wordSize.X;
                lineLength += word.Length;
                if (word[^1] == '\n')
                {
                    lineLength--;
                    NewLine();
                    lastSplit++;
                    continue;
                }
            }
            if (lineLength > 0)
                lines.Add(Text.Substring(lastSplit));

            Lines = lines.ToArray();

            MinWidth = maxLineWidth;
            MinHeight = lines.Count * Font.LineSpacing;
            MaxLineWidth = maxLineWidth;

            base.Recalculate();
        }

        public IEnumerable<string> EnumerateWords(string text)
        {
            int lastPos = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    yield return text.Substring(lastPos, i - lastPos + 1);
                    lastPos = i + 1;
                }
            }
            if (lastPos < text.Length)
                yield return text.Substring(lastPos);
        }
    }
}
