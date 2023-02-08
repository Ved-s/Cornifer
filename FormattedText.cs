using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Cornifer
{
    public static class FormattedText
    {
        static readonly Vector2[] Offsets = new Vector2[] { new(-1, -1), new(-1, 0), new(-1, 1), new(0, -1), new(0, 1), new(1, -1), new(1, 0), new(1, 1) };
        static Dictionary<SpriteFont, FontCache> Cache = new();
        public static Dictionary<SpriteFont, float> FontSpaceOverride = new();

        public static void Draw(SpriteBatch spriteBatch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color, Color? shadeColor = null)
        {
            float? spaceOverride = null;
            if (FontSpaceOverride.TryGetValue(font, out float spaceOverrideValue))
                spaceOverride = spaceOverrideValue;

            if (!Cache.TryGetValue(font, out FontCache? cache))
            {
                cache = new(font);
                Cache.Add(font, cache);
            }

            Vector2 drawPos = position;
            DrawContext context = new()
            {
                SpriteBatch = spriteBatch,
                Font = font,
                FontCache = cache,
                SpaceOverride = spaceOverride,
                OriginalPos = position,
                Color = color,
                ShadeColor = shadeColor
            };

            DrawTaggedText(text, ref drawPos, context);
        }

        static int FindNextTag(ReadOnlySpan<char> text, out ReadOnlySpan<char> tagName, out ReadOnlySpan<char> tagData, out ReadOnlySpan<char> tagContent, out int tagLength)
        {
            tagName = ReadOnlySpan<char>.Empty;
            tagData = ReadOnlySpan<char>.Empty;
            tagContent = ReadOnlySpan<char>.Empty;
            tagLength = 0;

            int tagReadPos = 0;

            int tagBeginStart;

            while (true)
            {
                tagBeginStart = text.Slice(tagReadPos).IndexOf('[');

                if (tagBeginStart < 0)
                    return -1;
                tagBeginStart += tagReadPos;

                if (tagBeginStart == text.Length - 1)
                    return -1;

                if (text[tagBeginStart+1] != '/')
                    break;

                tagReadPos = tagBeginStart + 1;
            }

            tagLength = 0;
            if (tagBeginStart < 0)
                return -1;

            tagBeginStart++;

            int tagBeginEnd = text.Slice(tagBeginStart).IndexOf("]");
            if (tagBeginEnd < 0)
                return -1;

            tagBeginEnd += tagBeginStart;

            ReadOnlySpan<char> tag = text.Slice(tagBeginStart, tagBeginEnd - tagBeginStart);

            int tagDataDelimeter = tag.IndexOf(':');
            if (tagDataDelimeter < 0)
                tagName = tag;
            else 
            {
                tagName = tag.Slice(0, tagDataDelimeter);
                tagData = tag.Slice(tagDataDelimeter + 1);
            }

            tagBeginEnd++;

            tagLength = tagBeginEnd - tagBeginStart;

            Span<char> endingSeq = stackalloc char[tagName.Length + 3];

            endingSeq[0] = '[';
            endingSeq[1] = '/';
            tagName.CopyTo(endingSeq.Slice(2, tagName.Length));
            endingSeq[^1] = ']';

            int endingPos = text.Slice(tagBeginEnd).IndexOf(endingSeq);

            int tagContentEnd;
            int tagEnd;

            if (endingPos < 0)
            {
                tagContentEnd = text.Length;
                tagEnd = text.Length;
            }
            else
            {
                endingPos += tagBeginEnd;
                tagReadPos = tagBeginEnd;

                tagContentEnd = endingPos;
                tagEnd = endingPos + endingSeq.Length;

                while (true)
                {
                    ReadOnlySpan<char> next = text.Slice(tagReadPos);
                    int nextTag = FindNextTag(next, out _, out _, out _, out int nextTagLength);
                    if (nextTag < 0)
                        break;
                    else
                    {
                        nextTag += tagReadPos;

                        // Ending tag is inside next tag
                        if (nextTag <= endingPos && nextTag + nextTagLength > endingPos)
                        {
                            endingPos = text.Slice(nextTag + nextTagLength).IndexOf(endingSeq);
                            if (endingPos < 0)
                            {
                                tagContentEnd = text.Length;
                                tagEnd = text.Length;
                            }
                            else 
                            {
                                tagContentEnd = endingPos;
                                tagEnd = endingPos + endingSeq.Length;
                            }
                        }
                    }

                    tagReadPos = nextTag + nextTagLength;
                }
            }
            //while (true)
            //{
            //    int tagEndStart = text.Slice(tagReadPos).IndexOf(endingSeq);
            //    if (tagEndStart < 0)
            //    {
            //        tagContentEnd = text.Length;
            //        tagEnd = text.Length;
            //        break;
            //    }
            //    tagEndStart += tagReadPos;
            //    tagEndStart += 2;
            //    tagReadPos = tagEndStart;
            //
            //    int tagEndEnd = text.Slice(tagReadPos).IndexOf(']');
            //    if (tagEndEnd < 0)
            //    {
            //        tagContentEnd = text.Length;
            //        tagEnd = text.Length;
            //        break;
            //    }
            //
            //    tagEndEnd += tagReadPos;
            //
            //    ReadOnlySpan<char> endTagName = text.Slice(tagEndStart, tagEndEnd - tagEndStart);
            //    if (endTagName.Equals(tagName, StringComparison.InvariantCulture))
            //    {
            //        tagContentEnd = tagEndStart - 2;
            //        tagEnd = (tagEndEnd + 1);
            //        break;
            //    }
            //}

            tagContent = text.Slice(tagBeginEnd, tagContentEnd - tagBeginEnd);
            tagLength = tagEnd - (tagBeginStart - 1);
            return tagBeginStart-1;
        }

        static void DrawTaggedText(ReadOnlySpan<char> text, ref Vector2 pos, DrawContext context)
        {
            int textPos = 0;

            if (context.ShadeColor.HasValue && !context.ShadeRun)
            {
                Vector2 posCopy = pos;
                DrawTaggedText(text, ref posCopy, context with { ShadeRun = true });
                context.ShadeColor = null;
            }

            while (true)
            {
                int tagPos = FindNextTag(text.Slice(textPos), out ReadOnlySpan<char> tagName, out ReadOnlySpan<char> tagData, out ReadOnlySpan<char> tagContent, out int tagLength);
                if (tagPos < 0)
                    break;

                tagPos += textPos;

                if (tagPos > textPos)
                    DrawSimpleText(text.Slice(textPos, tagPos - textPos), ref pos, context);

                textPos = tagPos;

                bool tagHandled = false;

                if (tagName.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                {
                    Color? tagColor = ParseColor(tagData);

                    if (tagColor.HasValue)
                    {
                        DrawTaggedText(tagContent, ref pos, context with { Color = tagColor.Value });
                        tagHandled = true;
                    }
                }
                else if (tagName.Equals("s", StringComparison.InvariantCultureIgnoreCase))
                {
                    Color? tagColor = tagData.Length == 0 ? Color.Black : ParseColor(tagData);

                    if (tagColor.HasValue)
                    {
                        DrawTaggedText(tagContent, ref pos, context with { ShadeColor = tagColor.Value });
                        tagHandled = true;
                    }
                }
                else if (tagName.Equals("ns", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!context.ShadeRun)
                        DrawTaggedText(tagContent, ref pos, context with { ShadeColor = null });
                    tagHandled = true;
                }
                else if (tagName.Equals("i", StringComparison.InvariantCultureIgnoreCase))
                {
                    DrawTaggedText(tagContent, ref pos, context with { Italic = true });
                    tagHandled = true;
                }
                else if (tagName.Equals("b", StringComparison.InvariantCultureIgnoreCase))
                {
                    DrawTaggedText(tagContent, ref pos, context with { Bold = true });
                    tagHandled = true;
                }
                else if (tagName.Equals("u", StringComparison.InvariantCultureIgnoreCase))
                {
                    DrawTaggedText(tagContent, ref pos, context with { Underline = true });
                    tagHandled = true;
                }

                if (!tagHandled)
                    DrawSimpleText(text.Slice(textPos, tagLength), ref pos, context);

                textPos += tagLength;
            }

            if (textPos < text.Length)
                DrawSimpleText(text.Slice(textPos), ref pos, context);
        }

        static void DrawSimpleText(ReadOnlySpan<char> text, ref Vector2 pos, DrawContext context)
        {
            int linePos = 0;
            while (true)
            {
                int lineLength = text.Slice(linePos).IndexOf('\n');
                if (lineLength < 0)
                    break;

                DrawLine(text.Slice(linePos, lineLength), pos, context);
                pos.X = context.OriginalPos.X;
                pos.Y += context.Font.LineSpacing;

                linePos += lineLength + 1;
            }

            if (linePos < text.Length)
                pos.X += DrawLine(text.Slice(linePos), pos, context);
        }

        static float DrawLine(ReadOnlySpan<char> text, Vector2 pos, DrawContext context)
        {
            Vector2 drawPos = Vector2.Zero;
            bool flag = true;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (!context.FontCache.Glyphs.TryGetValue(c, out SpriteFont.Glyph glyph) 
                 && (context.Font.DefaultCharacter is null || !context.FontCache.Glyphs.TryGetValue(context.Font.DefaultCharacter.Value, out glyph)))
                {
                    continue;
                }

                if (flag)
                {
                    drawPos.X = Math.Max(glyph.LeftSideBearing, 0f);
                    flag = false;
                }
                else
                {
                    drawPos.X += context.Font.Spacing + glyph.LeftSideBearing;
                }

                float glyphWidth = glyph.Width;
                if (c == ' ' && context.SpaceOverride.HasValue)
                    glyphWidth = context.SpaceOverride.Value;
                else
                {
                    DrawGlyph(glyph, pos + drawPos, context);
                    if (context.Bold)
                        DrawGlyph(glyph, pos + drawPos + new Vector2(0.5f), context);
                }
                drawPos.X += glyph.Width + glyph.RightSideBearing;
            }

            if (context.Underline)
            {
                float lineY = pos.Y + context.Font.LineSpacing - 4;

                if (context.ShadeRun && context.ShadeColor.HasValue)
                {
                    context.SpriteBatch.DrawRect(new(pos.X-1, lineY-1), new(drawPos.X + 2, 3), context.ShadeColor);
                }
                else
                {
                    context.SpriteBatch.DrawLine(new(pos.X, lineY), new(pos.X + drawPos.X, lineY), context.Color);
                }
            }

            return drawPos.X;
        }

        static void DrawGlyph(SpriteFont.Glyph glyph, Vector2 pos, DrawContext context)
        {
            Vector2 dp = pos + glyph.Cropping.Location.ToVector2();

            Vector2 tl = dp;
            Vector2 tr = dp + new Vector2(glyph.BoundsInTexture.Width, 0);
            Vector2 bl = dp + new Vector2(0, glyph.BoundsInTexture.Height);
            Vector2 br = dp + glyph.BoundsInTexture.Size.ToVector2();

            if (context.Italic)
            {
                tl.X += 2;
                tr.X += 2;
            }

            if (context.ShadeRun && context.ShadeColor.HasValue)
            {
                for (int i = 0; i < Offsets.Length; i++)
                {
                    Vector2 off = Offsets[i];
                    context.SpriteBatch.Draw(context.Font.Texture, tl + off, tr + off, bl + off, br + off, glyph.BoundsInTexture, context.ShadeColor.Value);
                }
            }
            else
            {
                context.SpriteBatch.Draw(context.Font.Texture, tl, tr, bl, br, glyph.BoundsInTexture, context.Color);
            }
        }

        public static Color? ParseColor(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c  = text[i];
                if (!char.IsDigit(c) && (c < 'A' || c > 'F') && (c < 'a' || c > 'f'))
                    return null;
            }

            byte r, g, b, a = 255;

            if (text.Length == 1)
            {
                r = ParseHexChar(text[0]);
                r += (byte)(r << 4);

                g = r;
                b = r;
            }
            else if (text.Length == 3)
            {
                r = ParseHexChar(text[0]);
                g = ParseHexChar(text[1]);
                b = ParseHexChar(text[2]);

                r += (byte)(r << 4);
                g += (byte)(g << 4);
                b += (byte)(b << 4);
            }
            else if (text.Length == 6)
            {
                r = (byte)(ParseHexChar(text[0]) << 4 + ParseHexChar(text[1]));
                g = (byte)(ParseHexChar(text[2]) << 4 + ParseHexChar(text[3]));
                b = (byte)(ParseHexChar(text[4]) << 4 + ParseHexChar(text[5]));
            }
            else if (text.Length == 8)
            {
                r = (byte)(ParseHexChar(text[0]) << 4 + ParseHexChar(text[1]));
                g = (byte)(ParseHexChar(text[2]) << 4 + ParseHexChar(text[3]));
                b = (byte)(ParseHexChar(text[4]) << 4 + ParseHexChar(text[5]));
                a = (byte)(ParseHexChar(text[6]) << 4 + ParseHexChar(text[7]));
            }
            else
            {
                return null;
            }

            return new(r, g, b, a);
        }

        static byte ParseHexChar(char c)
        {
            if (char.IsDigit(c))
                return (byte)(c - '0');

            if (c >= 'A' && c <= 'F')
                return (byte)(c - 'A' + 10);

            if (c >= 'a' && c <= 'f')
                return (byte)(c - 'a' + 10);

            return 0;
        }

        class FontCache
        {
            public Dictionary<char, SpriteFont.Glyph> Glyphs;

            public FontCache(SpriteFont font)
            {
                Glyphs = font.GetGlyphs();
            }
        }

        struct DrawContext
        {
            public SpriteBatch SpriteBatch;
            public SpriteFont Font;
            public FontCache FontCache;

            public Vector2 OriginalPos;
            public float? SpaceOverride;

            public Color Color;
            public Color? ShadeColor;
            public bool ShadeRun;

            public bool Italic;
            public bool Bold;
            public bool Underline;
        }
    }
}
