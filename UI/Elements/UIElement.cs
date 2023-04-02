using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace Cornifer.UI.Elements
{
    public class UIElement
    {
        public static readonly ElementEvent<Empty, UIElement> UpdateEvent = new();
        public static readonly ElementEvent<SpriteBatch, UIElement> DrawEvent = new();
        public static readonly ElementEvent<Empty, UIElement> ClickEvent = new();

        public static readonly ElementEvent<bool, UIElement> HoveredChangedEvent = new();
        public static readonly ElementEvent<bool, UIElement> ActiveChangedEvent = new();
        public static readonly ElementEvent<Empty, UIElement> RecalculateEvent = new();

        public string? Name;

        public PosDim Top;
        public PosDim Left;

        public SizeDim Width = new(0, 1);
        public SizeDim Height = new(0, 1);

        public SizeDim? MinWidth;
        public SizeDim? MinHeight;

        public SizeDim? MaxWidth;
        public SizeDim? MaxHeight;

        public Offset4 Margin;

        public Rect ScreenRect;
        public Vec2 MinSize;
        public Vec2 MaxSize;

        public bool Visible = true;
        public bool Enabled = true;

        public UIContainer? Parent { get; internal protected set; }
        public virtual UIRoot Root { get; internal protected set; } = null!;
        public virtual SpriteFont? Font
        {
            get => font ?? Parent?.Font;
            set { font = value; Recalculate(); }
        }
        public virtual bool Hovered
        {
            get => hovered;
            internal set
            {
                if (hovered == value)
                    return;

                hovered = value;
                if (Events.PreCall(HoveredChangedEvent, value))
                {
                    HoveredChanged();
                    Events.PostCall(HoveredChangedEvent, value);
                }

                if (Parent is not null)
                    Parent.Hovered = value;
            }
        }
        public bool Active
        {
            get => active;
            internal set
            {
                if (active == value)
                    return;

                active = value;
                if (Events.PreCall(ActiveChangedEvent, value))
                {
                    ActiveChanged();
                    Events.PostCall(ActiveChangedEvent, value);
                }
            }
        }

        public virtual string? HoverText { get; set; }

        public virtual bool CanActivate { get; set; } = true;
        public Vec2 RelativeMouse => Root is null ? Vec2.Zero : Root.MousePosition - ScreenRect.Position;

        private SpriteFont? font;
        private bool hovered;
        private bool active;

        protected bool SkipContainerLayout = false;

        protected readonly ElementEventManager Events;

        public UIElement()
        {
            Type? t = GetType();
            while (t is not null && t.IsAssignableTo(typeof(UIElement)))
            {
                RuntimeHelpers.RunClassConstructor(t.TypeHandle);
                t = t.BaseType;
            }

            Events = new(this);
        }

        public void Update()
        {
            if (Root is null)
                return;

            if (!Events.PreCall(UpdateEvent, default))
                return;

            UpdateSelf();

            if (Hovered && Root is not null && Root.MouseLeftKey == KeybindState.JustPressed)
                Events.PostCall(ClickEvent, default);

            Events.PostCall(UpdateEvent, default);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Events.PreCall(DrawEvent, spriteBatch))
                return;

            DrawSelf(spriteBatch);
            Events.PostCall(DrawEvent, spriteBatch);
        }

        protected virtual void UpdateSelf() { }
        protected virtual void DrawSelf(SpriteBatch spriteBatch) { }

        public virtual void Recalculate()
        {
            if (!Events.PreCall(RecalculateEvent, default))
                return;

            Rect? parentChildrenRect = Parent?.ChildrenRect;

            float? parentWidth = parentChildrenRect?.Width - Margin.Horizontal - Parent?.Padding.Horizontal;
            float? parentHeight = parentChildrenRect?.Height - Margin.Vertical - Parent?.Padding.Vertical;

            ScreenRect.Width = CalculateSize(Width, MinWidth, MaxWidth, parentWidth, out MinSize.X, out MaxSize.X);
            ScreenRect.Height = CalculateSize(Height, MinHeight, MaxHeight, parentHeight, out MinSize.Y, out MaxSize.Y);

            ScreenRect.X = (parentChildrenRect?.X ?? 0) + (Parent?.Padding.Left ?? 0) + Margin.Left + Left.Calculate(parentWidth ?? 0, ScreenRect.Width);
            ScreenRect.Y = (parentChildrenRect?.Y ?? 0) + (Parent?.Padding.Top ?? 0) + Margin.Top + Top.Calculate(parentHeight ?? 0, ScreenRect.Height);

            if (!SkipContainerLayout && Parent is ILayoutContainer layout)
                layout.LayoutChild(this, ref ScreenRect);
            SkipContainerLayout = false;

            Events.PostCall(RecalculateEvent, default);
        }

        protected virtual void HoveredChanged() { }
        protected virtual void ActiveChanged() { }

        protected static float CalculateSize(SizeDim value, SizeDim? min, SizeDim? max, float? parentSize, out float minSize, out float maxSize)
        {
            float parent = parentSize ?? 0;
            float size = value.Calculate(parent);

            minSize = 0;
            maxSize = float.PositiveInfinity;

            if (min.HasValue)
                size = Math.Max(size, minSize = min.Value.Calculate(parent));
            if (max.HasValue)
                size = Math.Min(size, maxSize = max.Value.Calculate(parent));
            return size;
        }

        public void AddPreEventCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Func<TElement, TEvent, bool> callback)
            => Events.AddPreCallback(@event, callback);
        public void AddPostEventCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Action<TElement, TEvent> callback)
            => Events.AddPostCallback(@event, callback);

        public void RemovePreEventCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Func<TElement, TEvent, bool> callback)
            => Events.RemovePreCallback(@event, callback);
        public void RemovePostEventCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Action<TElement, TEvent> callback)
            => Events.RemovePostCallback(@event, callback);
    }
}
