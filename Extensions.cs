﻿using Cornifer.Helpers;
using Cornifer.Input;
using Cornifer.Json;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public static class Extensions
    {
        public static void SetAlpha(ref this Color color, float alpha)
        {
            color.A = (byte)(Math.Clamp(alpha, 0, 1) * 255);
        }

        public static bool TryGet<T>(this T[] array, int index, out T value)
        {
            if (index < array.Length)
            {
                value = array[index];
                return true;
            }
            value = default!;
            return false;
        }

        public static Vector2 Size(this Texture2D texture)
        {
            return new(texture.Width, texture.Height);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float thickness = 1)
        {
            Vector2 diff = b - a;
            float angle = MathF.Atan2(diff.Y, diff.X);
            spriteBatch.Draw(Main.Pixel, a, null, color, angle, new Vector2(0, .5f), new Vector2(diff.Length(), thickness), SpriteEffects.None, 0);
        }

        public static void DrawDashLine(this SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color dashColor, Color? emptyColor, float dashLength, float? emptyLength = null, float thickness = 1, float? startOffset = null)
        {
            emptyLength ??= dashLength;

            float remainingLength = (a - b).Length();
            bool dash = true;
            Vector2 dir = b - a;
            dir.Normalize();
            Vector2 pos = a;

            if (startOffset.HasValue)
                pos += dir * startOffset.Value;

            while (remainingLength > 0)
            {
                float length = dash ? dashLength : emptyLength.Value;

                if (length > remainingLength)
                    length = remainingLength;

                Vector2 nextPos = pos + dir * length;
                Color? color = dash ? dashColor : emptyColor;

                if (color.HasValue)
                    DrawLine(spriteBatch, pos, nextPos, color.Value, thickness);

                dash = !dash;
                pos = nextPos;
                remainingLength -= length;
            }
        }

        public static void DrawRect(this SpriteBatch spriteBatch, Vector2 pos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
        {
            if (fill.HasValue)
            {
                spriteBatch.Draw(Main.Pixel, pos, null, fill.Value, 0f, Vector2.Zero, size, SpriteEffects.None, 0);
            }
            if (border.HasValue)
            {
                spriteBatch.DrawRect(new(pos.X + thickness, pos.Y), new(size.X - thickness, thickness), border.Value);
                spriteBatch.DrawRect(pos, new(thickness, size.Y - thickness), border.Value);

                if (size.Y > thickness)
                    spriteBatch.DrawRect(new(pos.X, (pos.Y + size.Y) - thickness), new(Math.Max(thickness, size.X - thickness), thickness), border.Value);

                if (size.X > thickness)
                    spriteBatch.DrawRect(new((pos.X + size.X) - thickness, pos.Y + thickness), new(thickness, Math.Max(thickness, size.Y - thickness)), border.Value);
            }
        }

        public static void DrawStringAligned(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position, Color color, Vector2 align, Color? shade = null)
        {
            Vector2 size = spriteFont.MeasureString(text);
            Vector2 pos = position - size * align;

            if (shade.HasValue)
            {
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(0, -1), shade.Value);
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(0, 1), shade.Value);
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(-1, 0), shade.Value);
                spriteBatch.DrawString(spriteFont, text, pos + new Vector2(1, 0), shade.Value);
            }

            spriteBatch.DrawString(spriteFont, text, pos, color);
        }

        public static void DrawStringShaded(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position, Color color, Color? shadeColor = null)
        {
            shadeColor ??= Color.Black;

            spriteBatch.DrawString(spriteFont, text, position + new Vector2(0, -1), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(0, 1), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(-1, 0), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(1, 0), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position, color);
        }

        public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 pos, float size, Color color)
        {
            spriteBatch.DrawRect(pos - new Vector2(size / 2), new Vector2(size), color);
        }

        public static void Clamp01(ref this Vector2 vector)
        {
            vector.X = Math.Clamp(vector.X, 0, 1);
            vector.Y = Math.Clamp(vector.Y, 0, 1);
        }

        delegate void DrawSpriteBatchRawDelegate(SpriteBatch spriteBatch, Texture2D texture, float sortingKey, VertexPositionColorTexture tl, VertexPositionColorTexture tr, VertexPositionColorTexture bl, VertexPositionColorTexture br);
        static DrawSpriteBatchRawDelegate? DrawSpriteBatchRaw;
        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, float sortingKey, VertexPositionColorTexture tl, VertexPositionColorTexture tr, VertexPositionColorTexture bl, VertexPositionColorTexture br)
        {
            if (DrawSpriteBatchRaw is null)
            {
                ParameterExpression spriteBatchParam = Expression.Parameter(typeof(SpriteBatch), "spriteBatch");
                ParameterExpression textureParam = Expression.Parameter(typeof(Texture2D), "texture");
                ParameterExpression sortingKeyParam = Expression.Parameter(typeof(float), "sortingKey");
                ParameterExpression tlParam = Expression.Parameter(typeof(VertexPositionColorTexture), "tl");
                ParameterExpression trParam = Expression.Parameter(typeof(VertexPositionColorTexture), "tr");
                ParameterExpression blParam = Expression.Parameter(typeof(VertexPositionColorTexture), "bl");
                ParameterExpression brParam = Expression.Parameter(typeof(VertexPositionColorTexture), "br");

                ParameterExpression batchItem = Expression.Variable(typeof(SpriteBatch).Assembly.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatchItem")!, "batchItem");
                Expression batcher = Expression.Field(spriteBatchParam, "_batcher");

                Expression itemTexture = Expression.Field(batchItem, "Texture");
                Expression itemSortingKey = Expression.Field(batchItem, "SortKey");
                Expression itemVertexTL = Expression.Field(batchItem, "vertexTL");
                Expression itemVertexTR = Expression.Field(batchItem, "vertexTR");
                Expression itemVertexBL = Expression.Field(batchItem, "vertexBL");
                Expression itemVertexBR = Expression.Field(batchItem, "vertexBR");

                BlockExpression body = Expression.Block(
                    new ParameterExpression[]
                    {
                        batchItem
                    },
                    new Expression[]
                    {
                        Expression.Assign(batchItem, Expression.Call(batcher, "CreateBatchItem", null, null)),
                        Expression.Assign(itemTexture, textureParam),
                        Expression.Assign(itemSortingKey, sortingKeyParam),
                        Expression.Assign(itemVertexTL, tlParam),
                        Expression.Assign(itemVertexTR, trParam),
                        Expression.Assign(itemVertexBL, blParam),
                        Expression.Assign(itemVertexBR, brParam),
                        Expression.Call(spriteBatchParam, "FlushIfNeeded", null, null)
                    });

                DrawSpriteBatchRaw = Expression.Lambda<DrawSpriteBatchRawDelegate>(body, spriteBatchParam, textureParam, sortingKeyParam, tlParam, trParam, blParam, brParam).Compile();
            }
            DrawSpriteBatchRaw.Invoke(spriteBatch, texture, sortingKey, tl, tr, bl, br);
        }

        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, Rectangle? source, Color color)
        {
            VertexPositionColorTexture tlVert = new()
            {
                Color = color,
                Position = new(tl, 0),
                TextureCoordinate = source is null ? new(0, 0) : new Vector2(source.Value.Left, source.Value.Top) / texture.Size(),
            };

            VertexPositionColorTexture trVert = new()
            {
                Color = color,
                Position = new(tr, 0),
                TextureCoordinate = source is null ? new(1, 0) : new Vector2(source.Value.Right, source.Value.Top) / texture.Size(),
            };

            VertexPositionColorTexture blVert = new()
            {
                Color = color,
                Position = new(bl, 0),
                TextureCoordinate = source is null ? new(0, 1) : new Vector2(source.Value.Left, source.Value.Bottom) / texture.Size(),
            };

            VertexPositionColorTexture brVert = new()
            {
                Color = color,
                Position = new(br, 0),
                TextureCoordinate = source is null ? new(1, 1) : new Vector2(source.Value.Right, source.Value.Bottom) / texture.Size(),
            };

            Draw(spriteBatch, texture, 0f, tlVert, trVert, blVert, brVert);
        }

        public static IEnumerable<T> SmartReverse<T>(this IEnumerable<T> ienum)
        {
            if (ienum is T[] array)
                return Reverse(array);

            if (ienum is IList<T> list)
                return Reverse(list);

            if (ienum is CompoundEnumerable<T> compound)
                return compound.EnumerateBackwards();

            return ienum.Reverse();
        }

        public static IEnumerable<T> Reverse<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }

        public static IEnumerable<T> Reverse<T>(this T[] array)
        {
            for (int i = array.Length - 1; i >= 0; i--)
                yield return array[i];
        }

        public static bool TryGet<T>([NotNullWhen(true)] this JsonNode? node, string key, [NotNullWhen(true)] out T? value)
        {
            node = node?[key];
            if (node is not null)
            {
                if (node is T t)
                {
                    value = t;
                    return true;
                }

                if (node is JsonValue jvalue)
                {
                    return jvalue.TryGetValue<T>(out value);
                }
            }

            value = default;
            return false;
        }

        public static T? Get<T>(this JsonNode? node, string key, T? @default = default)
        {
            if (node.TryGet(key, out T? value))
                return value;
            return @default;
        }

        public static TNode SaveProperty<TNode, TProp>(this TNode node, ObjectProperty<TProp> property, bool forCopy = false) where TNode : JsonNode
        {
            property.SaveToJson(node, forCopy);
            return node;
        }

        public static TNode SaveProperty<TNode, TProp, TPropValue>(this TNode node, ObjectProperty<TProp, TPropValue> property, bool forCopy = false) where TNode : JsonNode
        {
            property.SaveToJson(node, forCopy);
            return node;
        }

        public static TElement BindConfig<TElement, TConfig>(this TElement element, InterfaceState.Config<TConfig> config) where TElement : UIElement
        {
            config.Element = element;
            config.UpdateElement();
            config.BindElement();
            return element;
        }

        public static TElement OnConfigChange<TElement, TConfig>(this TElement element, InterfaceState.Config<TConfig> config, Action<TElement, TConfig> handler) where TElement : UIElement
        {
            config.OnChanged += () => handler(element, config.Value);
            return element;
        }

        public static bool IsKeyDown(this MouseState state, MouseKeys key)
        {
            return key switch
            {
                MouseKeys.LeftButton => state.LeftButton == ButtonState.Pressed,
                MouseKeys.RightButton => state.RightButton == ButtonState.Pressed,
                MouseKeys.MiddleButton => state.MiddleButton == ButtonState.Pressed,
                MouseKeys.XButton1 => state.XButton1 == ButtonState.Pressed,
                MouseKeys.XButton2 => state.XButton2 == ButtonState.Pressed,
                _ => false
            };
        }

        public static bool IsKeyUp(this MouseState state, MouseKeys key)
        {
            return key switch
            {
                MouseKeys.LeftButton => state.LeftButton == ButtonState.Released,
                MouseKeys.RightButton => state.RightButton == ButtonState.Released,
                MouseKeys.MiddleButton => state.MiddleButton == ButtonState.Released,
                MouseKeys.XButton1 => state.XButton1 == ButtonState.Released,
                MouseKeys.XButton2 => state.XButton2 == ButtonState.Released,
                _ => false
            };
        }

        public static string ToHexString(this Color color, bool alpha = false)
        {
            if (alpha)
                return $"{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}";

            return $"{color.R:x2}{color.G:x2}{color.B:x2}";
        }

        public static bool IsNullEmptyOrWhitespace(this string? str)
            => string.IsNullOrWhiteSpace(str) || str.Length == 0;

        public static bool TrySplitOnce(this string str, char splitter,
            [NotNullWhen(true)] out string? left,
            [NotNullWhen(true)] out string? right,
            StringSplitOptions options)
        {
            left = null;
            right = null;

            int index = str.IndexOf(splitter);

            if (index < 0)
                return false;

            ReadOnlySpan<char> leftSpan = str.AsSpan()[..index];
            ReadOnlySpan<char> rightSpan = str.AsSpan()[(index + 1)..];

            if ((options & StringSplitOptions.TrimEntries) is not StringSplitOptions.None)
            {
                leftSpan = leftSpan.Trim();
                rightSpan = rightSpan.Trim();
            }

            if ((options & StringSplitOptions.RemoveEmptyEntries) is not StringSplitOptions.None && (leftSpan.Length == 0 || rightSpan.Length == 0))
                return false;

            left = new(leftSpan);
            right = new(rightSpan);

            return true;
        }

        public static JsonSerializerOptions AddDebugIndent(this JsonSerializerOptions options)
        {
            if (Main.DebugMode)
                options.WriteIndented = true;
            return options;
        }

        public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> ienum) where T : struct
        {
            foreach (T t in ienum)
                yield return t;
        }

        public static Span<byte> AsSpan(this MemoryStream ms)
            => ms.GetBuffer().AsSpan().Slice(0, (int)ms.Length);

        public static T CreateInstance<T>(this Type type)
            => (T)Activator.CreateInstance(type)!;

        public static T OnClick<T>(this T element, Action<T> handler) where T : UIElement
        {
            element.AddPostEventCallback(UIElement.ClickEvent, (el, _) => handler((T)el));
            return element;
        }
    }
}
