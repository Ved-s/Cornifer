using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer
{
    public abstract class MapObject
    {
        public bool ParentSelected => Parent != null && (Parent.Selected || Parent.ParentSelected);
        public bool Selected => Main.SelectedObjects.Contains(this);

        private bool InternalActive = true;

        public virtual bool Active { get => InternalActive; set => InternalActive = value; }
        public virtual Vector2 ParentPosition { get; set; }
        public virtual Vector2 Size { get; }

        public MapObject? Parent { get; set; }

        public virtual string? Name { get; set; }

        internal UIElement? ConfigCache { get; set; }

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

        protected virtual JsonNode? SaveInnerJson() => null;
        protected virtual void LoadInnerJson(JsonNode node) { }

        protected virtual void BuildInnerConfig(UIList list) { }
        protected virtual void UpdateInnerConfig() { }

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

                if (obj.WorldPosition.X <= pos.X
                 && obj.WorldPosition.Y <= pos.Y
                 && obj.WorldPosition.X + obj.Size.X > pos.X
                 && obj.WorldPosition.Y + obj.Size.Y > pos.Y)
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

                bool intersects = obj.WorldPosition.X < br.X
                    && tl.X < obj.WorldPosition.X + obj.Size.X
                    && obj.WorldPosition.Y < br.Y
                    && tl.Y < obj.WorldPosition.Y + obj.Size.Y;
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
