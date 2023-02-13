using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public abstract class MapObject
    {
        static ShadeRenderer? ShadeTextureRenderer;
        internal static RenderTarget2D? ShadeRenderTarget;

        public bool ParentSelected => Parent != null && (Parent.Selected || Parent.ParentSelected);
        public bool Selected => Main.SelectedObjects.Contains(this);

        private bool InternalActive = true;

        public virtual bool Active { get => InternalActive; set => InternalActive = value; }
        public virtual Vector2 ParentPosition { get; set; }
        public virtual Vector2 Size { get; }

        public Vector2 VisualPosition => WorldPosition + VisualOffset;
        public virtual Vector2 VisualSize => Size + new Vector2(ShadeSize * 2);
        public virtual Vector2 VisualOffset => new Vector2(-ShadeSize);

        public virtual int? ShadeCornerRadius { get; set; }
        public virtual int ShadeSize { get; set; }

        public MapObject? Parent { get; set; }

        public virtual string? Name { get; set; }

        internal UIElement? ConfigCache { get; set; }
        internal Texture2D? ShadeTexture;

        public bool ShadeTextureDirty { get; set; }
        protected bool Shading { get; private set; }

        public Vector2 WorldPosition
        {
            get => Parent is null ? ParentPosition : Parent.WorldPosition + ParentPosition;
            set
            {
                if (Parent is not null)
                    ParentPosition = value - Parent.WorldPosition;
                else
                    ParentPosition = value;
            }
        }

        public MapObjectCollection Children { get; }

        public MapObject()
        {
            Children = new(this);
        }

        public void DrawShade(Renderer renderer)
        {
            if (!Active)
                return;

            EnsureCorrectShadeTexture();

            if (ShadeSize > 0 && ShadeTexture is not null)
                renderer.DrawTexture(ShadeTexture, WorldPosition - new Vector2(ShadeSize));

            foreach (MapObject child in Children)
                child.DrawShade(renderer);
        }

        public void Draw(Renderer renderer)
        {
            if (!Active)
                return;

            DrawSelf(renderer);

            foreach (MapObject child in Children)
                child.Draw(renderer);
        }

        protected abstract void DrawSelf(Renderer renderer);

        public UIElement? Config
        {
            get
            {
                ConfigCache ??= BuildConfig();
                UpdateConfig();
                return ConfigCache;
            }
        }

        private UIList? ConfigChildrenList;

        private UIElement? BuildConfig()
        {
            UIList list = new()
            {
                ElementSpacing = 4,

                Elements =
                {
                    new UILabel
                    {
                        Height = 20,
                        Text = Name,
                        WordWrap = false,
                        TextAlign = new(.5f)
                    },

                    new UIResizeablePanel
                    {
                        BorderColor = new(100, 100, 100),
                        Padding = 4,
                        Height = 100,

                        CanGrabLeft = false,
                        CanGrabRight = false,
                        CanGrabTop = false,

                        MinHeight = 30,

                        Elements =
                        {
                            new UILabel
                            {
                                Height = 15,
                                Text = "Children",
                                WordWrap = false,
                                TextAlign = new(.5f)
                            },
                            new UIPanel
                            {
                                BackColor = new(40, 40, 40),

                                Top = 18,
                                Height = new(-18, 1),
                                Padding = 4,
                                Elements =
                                {
                                    new UIList
                                    {
                                        ElementSpacing = 4
                                    }.Assign(out ConfigChildrenList)
                                }
                            }
                        }
                    }
                }
            };
            BuildInnerConfig(list);
            return list;
        }
        private void UpdateConfig()
        {
            if (ConfigChildrenList is not null)
            {
                ConfigChildrenList.Elements.Clear();

                if (Children.Count == 0)
                {
                    ConfigChildrenList.Elements.Add(new UILabel
                    {
                        Text = "Empty",
                        Height = 20,
                        TextAlign = new(.5f)
                    });
                }
                else
                {
                    foreach (MapObject obj in Children)
                    {
                        UIPanel panel = new()
                        {
                            Padding = 2,
                            Height = 22,

                            Elements =
                            {
                                new UILabel
                                {
                                    Text = obj.Name,
                                    Top = 2,
                                    Left = 2,
                                    Height = 16,
                                    WordWrap = false,
                                    AutoSize = false,
                                    Width = new(-22, 1)
                                },
                                new UIButton
                                {
                                    Text = "A",

                                    Selectable = true,
                                    Selected = obj.InternalActive,

                                    SelectedBackColor = Color.White,
                                    SelectedTextColor = Color.Black,

                                    Left = new(0, 1, -1),
                                    Width = 18,
                                }.OnEvent(UIElement.ClickEvent, (btn, _) => obj.InternalActive = btn.Selected),
                            }
                        };

                        ConfigChildrenList.Elements.Add(panel);
                    }
                }
            }

            UpdateInnerConfig();
        }

        public JsonObject? SaveJson()
        {
            JsonNode? inner = SaveInnerJson();

            JsonObject json = new()
            {
                ["name"] = Name ?? throw new InvalidOperationException(
                        $"MapObject doesn't have a name and can't be saved.\n" +
                        $"Type: {GetType().Name}\n" +
                        $"Parent: {Parent?.Name ?? Parent?.GetType().Name ?? "null"}"),
                ["type"] = GetType().FullName,
                ["pos"] = JsonTypes.SaveVector2(ParentPosition),
                ["active"] = InternalActive,
            };
            if (inner is not null)
                json["data"] = inner;
            if (Children.Count > 0)
                json["children"] = new JsonArray(Children.Select(c => c.SaveJson()).OfType<JsonNode>().ToArray());

            return json;
        }
        public void LoadJson(JsonNode json)
        {
            if (json.TryGet("data", out JsonNode? data))
                LoadInnerJson(data);

            if (json.TryGet("name", out string? name))
                Name = name;

            if (json.TryGet("pos", out JsonNode? pos))
                ParentPosition = JsonTypes.LoadVector2(pos);

            if (json.TryGet("active", out bool active))
                InternalActive = active;

            if (json.TryGet("children", out JsonArray? children))
                foreach (JsonNode? childNode in children)
                    if (childNode is not null)
                        LoadObject(childNode, Children);
        }

        public void EnsureCorrectShadeTexture()
        {
            if ((ShadeTexture is null || ShadeTextureDirty) && ShadeSize > 0)
            {
                Shading = true;
                GenerateShadeTexture();
                Shading = false;
            }
            ShadeTextureDirty = false;
        }

        protected virtual void GenerateShadeTexture()
        {
            GenerateDefaultShadeTexture(ref ShadeTexture, this, ShadeSize, ShadeCornerRadius);
        }

        protected static void GenerateDefaultShadeTexture(ref Texture2D? texture, MapObject obj, int shade, int? cornerRadius)
        {
            obj.Shading = true;
            ShadeTextureRenderer ??= new(Main.SpriteBatch);

            Vector2 shadeSize = obj.Size + new Vector2(shade * 2);

            int shadeWidth = (int)Math.Ceiling(shadeSize.X);
            int shadeHeight = (int)Math.Ceiling(shadeSize.Y);

            if (ShadeRenderTarget is null || ShadeRenderTarget.Width < shadeWidth || ShadeRenderTarget.Height < shadeHeight)
            {
                int targetWidth = shadeWidth;
                int targetHeight = shadeHeight;

                if (ShadeRenderTarget is not null)
                {
                    targetWidth = Math.Max(targetWidth, ShadeRenderTarget.Width);
                    targetHeight = Math.Max(targetHeight, ShadeRenderTarget.Height);

                    ShadeRenderTarget?.Dispose();
                }
                ShadeRenderTarget = new(Main.Instance.GraphicsDevice, targetWidth, targetHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            ShadeTextureRenderer.TargetNeedsClear = true;
            ShadeTextureRenderer.Position = obj.WorldPosition - new Vector2(shade);

            obj.DrawSelf(ShadeTextureRenderer);

            int shadePixels = shadeWidth * shadeHeight;
            Color[] pixels = ArrayPool<Color>.Shared.Rent(shadePixels);

            // no texture draw calls
            if (ShadeTextureRenderer.TargetNeedsClear)
            {
                Array.Clear(pixels);
            }
            else
            {
                ShadeRenderTarget.GetData(0, new(0, 0, shadeWidth, shadeHeight), pixels, 0, shadePixels);
                ProcessShade(pixels, shadeWidth, shadeHeight, shade, cornerRadius);
            }
            if (texture is null || texture.Width != shadeWidth || texture.Height != shadeHeight)
            {
                texture?.Dispose();
                texture = new(Main.Instance.GraphicsDevice, shadeWidth, shadeHeight);
            }
            texture.SetData(pixels, 0, shadePixels);
            ArrayPool<Color>.Shared.Return(pixels);
            obj.Shading = false;
        }

        protected virtual JsonNode? SaveInnerJson() => null;
        protected virtual void LoadInnerJson(JsonNode node) { }

        protected virtual void BuildInnerConfig(UIList list) { }
        protected virtual void UpdateInnerConfig() { }

        protected static void ProcessShade(Color[] colors, int width, int height, int size, int? cornerRadius)
        {
            int arraysize = width * height;
            bool[] shade = ArrayPool<bool>.Shared.Rent(arraysize);

            int patternSide = size * 2 + 1;

            bool[] shadePattern = null!;

            if (cornerRadius.HasValue)
            {
                shadePattern = ArrayPool<bool>.Shared.Rent(patternSide * patternSide);

                int patternRadSq = cornerRadius.Value * cornerRadius.Value;

                for (int j = 0; j < patternSide; j++)
                    for (int i = 0; i < patternSide; i++)
                    {
                        float lengthsq = (size - i) * (size - i) + (size - j) * (size - j);
                        shadePattern[i + patternSide * j] = lengthsq <= patternRadSq;
                    }
            }

            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                {
                    int index = width * j + i;

                    shade[index] = false;

                    if (colors[index].A > 0)
                    {
                        shade[index] = true;
                        continue;
                    }

                    if (size <= 0)
                        continue;

                    bool probing = true;
                    for (int l = -size; l <= size && probing; l++)
                        for (int k = -size; k <= size && probing; k++)
                        {
                            if (cornerRadius.HasValue)
                            {
                                int patternIndex = (l + size) * patternSide + k + size;
                                if (!shadePattern[patternIndex])
                                    continue;
                            }

                            int x = i + k;
                            int y = j + l;

                            if (x < 0 || y < 0 || x >= width || y >= height || (k == 0 && l == 0))
                                continue;

                            int testIndex = width * y + x;

                            if (colors[testIndex].A > 0)
                            {
                                shade[index] = true;
                                probing = false;
                                continue;
                            }
                        }
                }

            for (int i = 0; i < arraysize; i++)
                if (shade[i])
                    colors[i] = Color.Black;

            ArrayPool<bool>.Shared.Return(shade);
            if (cornerRadius.HasValue)
                ArrayPool<bool>.Shared.Return(shadePattern);
        }

        public static MapObject? FindSelectableAtPos(IEnumerable<MapObject> objects, Vector2 pos, bool searchChildren)
        {
            foreach (MapObject obj in objects.SmartReverse())
            {
                if (!obj.Active)
                    continue;

                if (searchChildren)
                {
                    MapObject? child = FindSelectableAtPos(obj.Children, pos, true);
                    if (child is not null)
                        return child;
                }

                if (obj.VisualPosition.X <= pos.X
                 && obj.VisualPosition.Y <= pos.Y
                 && obj.VisualPosition.X + obj.VisualSize.X > pos.X
                 && obj.VisualPosition.Y + obj.VisualSize.Y > pos.Y)
                    return obj;
            }
            return null;
        }
        public static IEnumerable<MapObject> FindIntersectingSelectables(IEnumerable<MapObject> objects, Vector2 tl, Vector2 br, bool searchChildren)
        {
            foreach (MapObject obj in objects.SmartReverse())
            {
                if (!obj.Active)
                    continue;

                if (searchChildren)
                    foreach (MapObject child in FindIntersectingSelectables(obj.Children, tl, br, true))
                        yield return child;

                bool intersects = obj.VisualPosition.X < br.X
                    && tl.X < obj.VisualPosition.X + obj.VisualSize.X
                    && obj.VisualPosition.Y < br.Y
                    && tl.Y < obj.VisualPosition.Y + obj.VisualSize.Y;
                if (intersects)
                    yield return obj;
            }
        }

        public static bool LoadObject(JsonNode node, IEnumerable<MapObject> objEnumerable)
        {
            if (!node.TryGet("name", out string? name))
                return false;

            MapObject? obj = objEnumerable.FirstOrDefault(o => o.Name == name);
            if (obj is null)
                return false;

            obj.LoadJson(node);
            return true;
        }
        public static MapObject? CreateObject(JsonNode node)
        {
            if (!node.TryGet("type", out string? typeName))
                return null;

            Type? type = Type.GetType(typeName);
            if (type is null || !type.IsAssignableTo(typeof(MapObject)))
                return null;

            MapObject instance = (MapObject)Activator.CreateInstance(type)!;

            if (node.TryGet("name", out string? name))
                instance.Name = name;

            instance.LoadJson(node);
            return instance;
        }

        public class MapObjectCollection : ICollection<MapObject>
        {
            List<MapObject> Objects = new();
            MapObject Parent;

            public MapObjectCollection(MapObject parent)
            {
                Parent = parent;
            }

            public int Count => Objects.Count;
            public bool IsReadOnly => false;

            public void Add(MapObject item)
            {
                item.Parent?.Children.Remove(item);
                item.Parent = Parent;
                Objects.Add(item);
            }

            public void Clear()
            {
                foreach (MapObject obj in Objects)
                    obj.Parent = null;

                Objects.Clear();
            }

            public bool Remove(MapObject item)
            {
                item.Parent = null;
                return Objects.Remove(item);
            }

            public bool Contains(MapObject item)
            {
                return Objects.Contains(item);
            }

            public void CopyTo(MapObject[] array, int arrayIndex)
            {
                Objects.CopyTo(array, arrayIndex);
            }

            public IEnumerator<MapObject> GetEnumerator()
            {
                return Objects.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Objects.GetEnumerator();
            }
        }
    }
}
