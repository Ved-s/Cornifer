﻿using CommunityToolkit.HighPerformance.Buffers;
using Cornifer.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cornifer.Helpers
{
    public static class FormattedText
    {
        static readonly Vector2[] Offsets = new Vector2[] { new(-1, -1), new(-1, 0), new(-1, 1), new(0, -1), new(0, 1), new(1, -1), new(1, 0), new(1, 1) };
        static Dictionary<SpriteFont, FontCache> Cache = new();
        internal static StringPool StringPool = new();
        static StringPool NoContentTags = new();

        static FormattedText()
        {
            NoContentTags.Add("ic");
        }

        public static Dictionary<SpriteFont, float> FontSpaceOverride = new();
        public static Dictionary<SpriteFont, float> FontScaleOverride = new();

        public static Vector2 Draw(ReadOnlySpan<char> text, DrawContext context)
        {
            if (context.SpaceOverride is null && FontSpaceOverride.TryGetValue(context.Font, out float spaceOverrideValue))
                context.SpaceOverride = spaceOverrideValue;

            if (FontScaleOverride.TryGetValue(context.Font, out float scaleOverrideValue))
                context.Scale *= scaleOverrideValue;

            if (context.FontCache is null)
            {
                if (!Cache.TryGetValue(context.Font, out FontCache? cache))
                {
                    context.FontCache = new(context.Font);
                    Cache.Add(context.Font, context.FontCache);
                }
                else
                {
                    context.FontCache = cache;
                }
            }

            TextDrawPos drawPos = new()
            {
                Origin = context.OriginalPos,
            };

            DrawTaggedText(text, ref drawPos, context);

            return drawPos.Size;
        }

        public static void Draw(SpriteBatch spriteBatch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color, Color shadeColor = default, int shade = 0, float scale = 1)
        {
            float? spaceOverride = null;
            if (FontSpaceOverride.TryGetValue(font, out float spaceOverrideValue))
                spaceOverride = spaceOverrideValue;

            if (FontScaleOverride.TryGetValue(font, out float scaleOverrideValue))
                scale *= scaleOverrideValue;

            if (!Cache.TryGetValue(font, out FontCache? cache))
            {
                cache = new(font);
                Cache.Add(font, cache);
            }

            DrawContext context = new()
            {
                SpriteBatch = spriteBatch,
                Font = font,
                FontCache = cache,
                SpaceOverride = spaceOverride,
                OriginalPos = position,
                OriginalScale = scale,
                Color = color,
                ShadeColor = shadeColor,
                Shade = shade,
                Scale = scale,
                MeasuringSize = false,
            };

            TextDrawPos drawPos = new()
            {
                Origin = position
            };

            DrawTaggedText(text, ref drawPos, context);
        }

        public static Vector2 Measure(SpriteFont font, ReadOnlySpan<char> text, float scale = 1)
        {
            float? spaceOverride = null;
            if (FontSpaceOverride.TryGetValue(font, out float spaceOverrideValue))
                spaceOverride = spaceOverrideValue;

            if (FontScaleOverride.TryGetValue(font, out float scaleOverrideValue))
                scale *= scaleOverrideValue;

            if (!Cache.TryGetValue(font, out FontCache? cache))
            {
                cache = new(font);
                Cache.Add(font, cache);
            }

            DrawContext context = new()
            {
                SpriteBatch = null,
                Font = font,
                FontCache = cache,
                SpaceOverride = spaceOverride,
                OriginalPos = Vector2.Zero,
                OriginalScale = scale,
                Color = Color.Transparent,
                Shade = 0,
                Scale = scale,
                MeasuringSize = true,
            };

            TextDrawPos drawPos = new()
            {
                Origin = Vector2.Zero
            };

            DrawTaggedText(text, ref drawPos, context);

            return drawPos.Size;
        }

        public static Vector2 DrawAndMeasure(SpriteBatch spriteBatch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color, Color shadeColor = default, int shade = 0, float scale = 1)
        {
            float? spaceOverride = null;
            if (FontSpaceOverride.TryGetValue(font, out float spaceOverrideValue))
                spaceOverride = spaceOverrideValue;

            if (FontScaleOverride.TryGetValue(font, out float scaleOverrideValue))
                scale *= scaleOverrideValue;

            if (!Cache.TryGetValue(font, out FontCache? cache))
            {
                cache = new(font);
                Cache.Add(font, cache);
            }

            DrawContext context = new()
            {
                SpriteBatch = spriteBatch,
                Font = font,
                FontCache = cache,
                SpaceOverride = spaceOverride,
                OriginalPos = position,
                Color = color,
                ShadeColor = shadeColor,
                Shade = shade,
                Scale = scale,
                MeasuringSize = false,
            };

            TextDrawPos drawPos = new()
            {
                Origin = position
            };

            DrawTaggedText(text, ref drawPos, context);

            return drawPos.Size;
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

                if (text[tagBeginStart + 1] != '/' && (tagBeginStart == 0 || text[tagBeginStart - 1] != '\\'))
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

            tagLength = tagBeginEnd - tagBeginStart + 1;

            if (NoContentTags.TryGet(tagName, out _))
                return tagBeginStart - 1;

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

            tagContent = text.Slice(tagBeginEnd, tagContentEnd - tagBeginEnd);
            tagLength = tagEnd - (tagBeginStart - 1);
            return tagBeginStart - 1;
        }

        static void DrawTaggedText(ReadOnlySpan<char> text, ref TextDrawPos pos, DrawContext context)
        {
            int textPos = 0;

            if (context.Shade > 0 && !context.ShadeRun)
            {
                TextDrawPos posCopy = pos;
                DrawTaggedText(text, ref posCopy, context with { ShadeRun = true });
                context.Shade = 0;
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
                    Color? tagColor = ColorDatabase.ParseColor(tagData);

                    if (!tagColor.HasValue)
                    {
                        tagColor = ColorDatabase.GetColor(StringPool.GetOrAdd(tagData))?.Color;
                    }

                    if (tagColor.HasValue)
                    {
                        DrawTaggedText(tagContent, ref pos, context with { Color = tagColor.Value });
                        tagHandled = true;
                    }
                }
                // [c:COLOR:RAD]
                else if (tagName.Equals("s", StringComparison.InvariantCultureIgnoreCase))
                {
                    int colorDelimeter = tagData.IndexOf(':');
                    ReadOnlySpan<char> color = colorDelimeter < 0 ? tagData : tagData.Slice(0, colorDelimeter);
                    ReadOnlySpan<char> rad = colorDelimeter < 0 ? ReadOnlySpan<char>.Empty : tagData.Slice(colorDelimeter + 1);

                    Color? tagColor = color.Length == 0 ? Color.Black : ColorDatabase.ParseColor(color);
                    int? shade = rad.Length == 0 ? 1 : int.TryParse(rad, out int radv) ? radv : null;

                    if (tagColor.HasValue && shade.HasValue)
                    {
                        DrawTaggedText(tagContent, ref pos, context with { ShadeColor = tagColor.Value, Shade = shade.Value });
                        tagHandled = true;
                    }
                }
                else if (tagName.Equals("ns", StringComparison.InvariantCultureIgnoreCase))
                {
                    DrawTaggedText(tagContent, ref pos, context with { Shade = 0 });
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
                else if (tagName.Equals("sc", StringComparison.InvariantCultureIgnoreCase) && tagData.Length > 0)
                {
                    bool relative = tagData[0] == 'x';
                    ReadOnlySpan<char> scaleData = relative ? tagData.Slice(1) : tagData;
                    if (float.TryParse(scaleData, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                    {
                        if (relative)
                            DrawTaggedText(tagContent, ref pos, context with { Scale = context.Scale * scale * context.OriginalScale });
                        else
                            DrawTaggedText(tagContent, ref pos, context with { Scale = scale * context.OriginalScale });
                        tagHandled = true;
                    }
                }
                else if (tagName.Equals("a", StringComparison.InvariantCultureIgnoreCase) && tagData.Length > 0)
                {
                    if (float.TryParse(tagData, NumberStyles.Float, CultureInfo.InvariantCulture, out float align))
                    {
                        DrawTaggedText(tagContent, ref pos, context with { LineHeightAlign = align });
                        tagHandled = true;
                    }
                }
                //[ic:NAME:COLOR], optional COLOR
                else if (tagName.Equals("ic", StringComparison.InvariantCultureIgnoreCase) && tagData.Length > 0)
                {
                    int colorDelimeter = tagData.IndexOf(':');
                    ReadOnlySpan<char> name = colorDelimeter < 0 ? tagData : tagData.Slice(0, colorDelimeter);
                    ReadOnlySpan<char> color = colorDelimeter < 0 ? ReadOnlySpan<char>.Empty : tagData.Slice(colorDelimeter + 1);

                    string nameStr = StringPool.GetOrAdd(name);
                    if (SpriteAtlases.Sprites.TryGetValue(nameStr, out AtlasSprite? sprite))
                    {
                        Color? iconColor = color.Length == 0 ? sprite.Color : ColorDatabase.ParseColor(color);

                        if (iconColor.HasValue)
                        {
                            Vector2 size = sprite.Frame.Size.ToVector2() * context.Scale;
                            if (context.SpriteBatch is not null && !context.MeasuringSize)
                            {
                                Vector2 iconPos = pos.GetPos(size.Y, context);

                                if (context.ShadeRun && context.Shade > 0 && sprite.Shade)
                                {
                                    for (int i = 0; i < Offsets.Length; i++)
                                        for (int j = 1; j <= context.Shade; j++)
                                        {
                                            Vector2 off = Offsets[i] * context.Scale * j;
                                            context.SpriteBatch.Draw(sprite.Texture, iconPos + off, sprite.Frame, context.ShadeColor, 0f, Vector2.Zero, context.Scale, SpriteEffects.None, 0f);

                                            if (context.DropShadowColor.HasValue)
                                            {
                                                off += new Vector2(-1, 1) * context.Scale;
                                                context.SpriteBatch.Draw(sprite.Texture, iconPos + off, sprite.Frame, context.ShadeColor, 0f, Vector2.Zero, context.Scale, SpriteEffects.None, 0f);
                                            }
                                        }
                                }
                                else if (!context.ShadeRun)
                                {
                                    if (context.DropShadowColor.HasValue)
                                    {
                                        Vector2 off = new Vector2(-1, 1) * context.Scale;
                                        context.SpriteBatch.Draw(sprite.Texture, iconPos + off, sprite.Frame, context.DropShadowColor.Value, 0f, Vector2.Zero, context.Scale, SpriteEffects.None, 0f);
                                    }

                                    context.SpriteBatch.Draw(sprite.Texture, iconPos, sprite.Frame, iconColor.Value, 0f, Vector2.Zero, context.Scale, SpriteEffects.None, 0f);
                                }
                            }
                            pos.Advance(size);
                            tagHandled = true;
                        }
                    }
                }
                else if (tagName.Equals("ds", StringComparison.InvariantCultureIgnoreCase))
                {
                    Color? tagColor = ColorDatabase.ParseColor(tagData);

                    if (tagColor.HasValue)
                    {
                        DrawTaggedText(tagContent, ref pos, context with { DropShadowColor = tagColor.Value });
                        tagHandled = true;
                    }
                }
                if (!tagHandled)
                    DrawSimpleText(text.Slice(textPos, tagLength), ref pos, context);

                textPos += tagLength;
            }

            if (textPos < text.Length)
                DrawSimpleText(text.Slice(textPos), ref pos, context);
        }

        static void DrawSimpleText(ReadOnlySpan<char> text, ref TextDrawPos pos, DrawContext context)
        {
            int linePos = 0;
            while (true)
            {
                int lineLength = text.Slice(linePos).IndexOf('\n');
                if (lineLength < 0)
                    break;

                float width = DrawLine(text.Slice(linePos, lineLength), pos, context);
                pos.Advance(new(width, context.Font.LineSpacing * context.Scale));
                pos.NewLine();
                linePos += lineLength + 1;
            }

            if (linePos < text.Length)
            {
                float width = DrawLine(text.Slice(linePos), pos, context);
                pos.Advance(new(width, context.Font.LineSpacing * context.Scale));
            }
        }

        static float DrawLine(ReadOnlySpan<char> text, TextDrawPos pos, DrawContext context)
        {
            Vector2 drawPos = Vector2.Zero;
            Vector2 linePos = pos.GetPos(context.Font.LineSpacing * context.Scale, context);
            bool flag = true;
            bool escaped = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }
                escaped = false;

                if (!context.FontCache.Glyphs.TryGetValue(c, out SpriteFont.Glyph glyph)
                 && (context.Font.DefaultCharacter is null || !context.FontCache.Glyphs.TryGetValue(context.Font.DefaultCharacter.Value, out glyph)))
                {
                    continue;
                }

                if (flag)
                {
                    drawPos.X = Math.Max(glyph.LeftSideBearing, 0f) * context.Scale;
                    flag = false;
                }
                else
                {
                    drawPos.X += glyph.LeftSideBearing * context.Scale;
                }

                float glyphWidth = glyph.Width;
                if (c == ' ' && context.SpaceOverride.HasValue)
                    glyphWidth = context.SpaceOverride.Value;
                else if (!context.MeasuringSize)
                {
                    if (context.DropShadowColor.HasValue)
                    {
                        DrawGlyph(glyph, linePos + drawPos + new Vector2(-1, 1) * context.Scale, context with { Color = context.DropShadowColor.Value });
                        if (context.Bold)
                            DrawGlyph(glyph, linePos + drawPos + new Vector2(0.5f) + new Vector2(-1, 1) * context.Scale, context with { Color = context.DropShadowColor.Value });
                    }

                    DrawGlyph(glyph, linePos + drawPos, context);
                    if (context.Bold)
                        DrawGlyph(glyph, linePos + drawPos + new Vector2(0.5f), context);
                }
                drawPos.X += (glyphWidth + glyph.RightSideBearing + context.Font.Spacing) * context.Scale;
            }

            if (context.Underline && !context.MeasuringSize && context.SpriteBatch is not null)
            {
                float lineY = linePos.Y + (context.Font.LineSpacing - 4) * context.Scale;

                if (context.ShadeRun && context.Shade > 0)
                {
                    context.SpriteBatch.DrawRect(new(linePos.X - context.Shade, lineY - context.Shade), new(drawPos.X + context.Shade * 2, context.Shade * 2 + context.Scale), context.ShadeColor);
                    if (context.DropShadowColor.HasValue)
                    {
                        context.SpriteBatch.DrawRect(new Vector2(linePos.X - context.Shade, lineY - context.Shade) + new Vector2(-1, 1) * context.Scale, new(drawPos.X + context.Shade * 2, context.Shade * 2 + context.Scale), context.ShadeColor);
                    }
                }
                else if (!context.ShadeRun)
                {
                    if (context.DropShadowColor.HasValue)
                    {
                        context.SpriteBatch.DrawRect(new Vector2(linePos.X, lineY) + new Vector2(-1, 1) * context.Scale, new Vector2(drawPos.X, context.Scale), context.DropShadowColor);
                    }
                    context.SpriteBatch.DrawRect(new(linePos.X, lineY), new Vector2(drawPos.X, context.Scale), context.Color);
                }
            }

            return drawPos.X;
        }

        static void DrawGlyph(SpriteFont.Glyph glyph, Vector2 pos, DrawContext context)
        {
            if (context.SpriteBatch is null)
                return;

            Vector2 dp = pos + glyph.Cropping.Location.ToVector2() * context.Scale;

            Vector2 tl = dp;
            Vector2 tr = dp + new Vector2(glyph.BoundsInTexture.Width * context.Scale, 0);
            Vector2 bl = dp + new Vector2(0, glyph.BoundsInTexture.Height * context.Scale);
            Vector2 br = dp + glyph.BoundsInTexture.Size.ToVector2() * context.Scale;

            if (context.Italic)
            {
                tl.X += 2 * context.Scale;
                tr.X += 2 * context.Scale;
            }

            if (context.ShadeRun && context.Shade > 0)
            {
                for (int i = 0; i < Offsets.Length; i++)
                    for (int j = 1; j <= context.Shade; j++)
                    {
                        Vector2 off = Offsets[i] * context.Scale * j;
                        context.SpriteBatch.Draw(context.Font.Texture, tl + off, tr + off, bl + off, br + off, glyph.BoundsInTexture, context.ShadeColor);
                    }
            }
            else if (!context.ShadeRun)
            {
                context.SpriteBatch.Draw(context.Font.Texture, tl, tr, bl, br, glyph.BoundsInTexture, context.Color);
            }
        }

        public class FontCache
        {
            public Dictionary<char, SpriteFont.Glyph> Glyphs;

            public FontCache(SpriteFont font)
            {
                Glyphs = font.GetGlyphs();
            }
        }

        public struct DrawContext
        {
            public SpriteBatch? SpriteBatch;
            public SpriteFont Font;
            public FontCache FontCache;

            public Vector2 OriginalPos;
            public float OriginalScale;

            public float? SpaceOverride;

            public Color Color;
            public Color ShadeColor;
            public Color? DropShadowColor;
            public int Shade;
            public bool ShadeRun;

            public bool Italic;
            public bool Bold;
            public bool Underline;

            public float LineHeightAlign;
            public float Scale;

            public bool MeasuringSize;
        }

        struct TextDrawPos
        {
            public Vector2 Origin;
            public Vector2 Offset;
            public float LineHeight;
            public Vector2 Size;

            public void NewLine()
            {
                Offset.X = 0;
                Offset.Y += LineHeight;
                LineHeight = 0;
            }

            public void Advance(Vector2 size)
            {
                Offset.X += size.X;
                LineHeight = Math.Max(LineHeight, size.Y);
                Size.X = Math.Max(Size.X, Offset.X);
                Size.Y = Math.Max(Size.Y, Offset.Y + size.Y);
            }

            public Vector2 GetPos(float height, DrawContext context)
            {
                Vector2 vec = Origin + new Vector2(Offset.X, Offset.Y + Math.Max(0, (LineHeight - height) * context.LineHeightAlign));
                vec.Floor();
                return vec;
            }
        }
    }
}
