using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Cornifer.UI.Elements
{
    public class UIContainer : UIElement
    {
        public ElementCollection Elements { get; protected set; }

        public virtual Rect ChildrenRect => ScreenRect;

        public Offset4 Padding;

        private List<UIElement> UpdateList = new();

        public override UIRoot Root
        {
            get => base.Root;
            protected internal set
            {
                base.Root = value;
                foreach (UIElement element in Elements)
                    element.Root = value;
            }
        }

        protected bool SkipRecalculatingChildren = false;

        public UIContainer()
        {
            Elements = new(this);
        }

        protected override void UpdateSelf()
        {
            PreUpdateChildren();

            UpdateList.Clear();
            UpdateList.AddRange(Elements);

            foreach (UIElement element in UpdateList)
                if (element.Enabled && element.Visible)
                    element.Update();
            PostUpdateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            PreDrawChildren(spriteBatch);

            foreach (UIElement element in Elements)
                if (element.Visible && element is not UIModal)
                    element.Draw(spriteBatch);

            foreach (UIElement element in Elements)
                if (element.Visible && element is UIModal modal)
                {
                    modal.DrawModalBackground(spriteBatch);
                    modal.Draw(spriteBatch);
                    break;
                }

            PostDrawChildren(spriteBatch);
        }

        protected virtual void PreUpdateChildren() { }
        protected virtual void PostUpdateChildren() { }

        protected virtual void PreDrawChildren(SpriteBatch spriteBatch) { }
        protected virtual void PostDrawChildren(SpriteBatch spriteBatch) { }

        protected virtual void ChildHoveredChanged() { }

        public override void Recalculate()
        {
            base.Recalculate();
            if (!SkipRecalculatingChildren)
                foreach (UIElement element in Elements)
                    element.Recalculate();
            SkipRecalculatingChildren = false;
        }

        public UIElement? GetElementAt(Vec2 screenpos, bool canReturnSelf)
        {
            if (!ScreenRect.Contains(screenpos))
            {
                if (this is UIModal)
                    return this;
                return null;
            }

            bool anyModals = false;

            foreach (UIElement element in Elements)
            {
                if (element.Visible && element is UIModal)
                {
                    anyModals = true;
                    break;
                }
            }

            foreach (UIElement element in Elements)
            {
                if (!element.Visible)
                    continue;

                if (anyModals)
                {
                    if (element is not UIModal modal)
                        continue;

                    return modal.GetElementAt(screenpos, true);
                }

                if (!element.ScreenRect.Contains(screenpos))
                    continue;

                if (element is not UIContainer container)
                    return element;

                return container.GetElementAt(screenpos, true);
            }

            return canReturnSelf ? this : null;
        }

        public class ElementCollection : IList<UIElement>
        {
            private static readonly DummyController DummyControllerInstance = new();

            private readonly UIContainer Parent;
            private readonly IElementListController Controller = DummyControllerInstance;
            private List<UIElement> Elements = new();

            public UIElement this[int index]
            {
                get => Elements[index];
                set
                {
                    if (!Controller.CanModifyElements())
                        ThrowReadonlyException();

                    if (index < 0 || index >= Elements.Count)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    if (!Controller.CanSetElement(value, Elements[index], index))
                        ThrowBlockedException();

                    ResetParent(Elements[index]);
                    Elements[index] = value;
                    SetParent(value);
                }
            }

            public int Count => Elements.Count;

            public bool IsReadOnly => !Controller.CanModifyElements();

            public ElementCollection(UIContainer parent)
            {
                Parent = parent;
                if (parent is IElementListController controller)
                    Controller = controller;
            }

            public void Add(UIElement item)
            {
                if (!Controller.CanModifyElements())
                    ThrowReadonlyException();

                if (!Controller.CanAddElement(item))
                    ThrowBlockedException();

                if (Contains(item))
                    return;

                Elements.Add(item);
                SetParent(item);
            }

            public void Clear()
            {
                if (!Controller.CanModifyElements())
                    ThrowReadonlyException();

                int index = 0;
                while (index < Elements.Count)
                {
                    UIElement element = Elements[index];

                    if (!Controller.CanRemoveElement(element))
                    {
                        index++;
                        continue;
                    }

                    ResetParent(element);
                    Elements.RemoveAt(index);
                }
            }

            public bool Contains(UIElement item)
            {
                return Elements.Contains(item);
            }

            public void CopyTo(UIElement[] array, int arrayIndex)
            {
                Elements.CopyTo(array, arrayIndex);
            }

            public IEnumerator<UIElement> GetEnumerator()
            {
                return Elements.GetEnumerator();
            }

            public int IndexOf(UIElement item)
            {
                return Elements.IndexOf(item);
            }

            public void Insert(int index, UIElement item)
            {
                if (!Controller.CanModifyElements())
                    ThrowReadonlyException();

                if (!Controller.CanInsertElement(item, index))
                    ThrowBlockedException();

                Elements.Insert(index, item);
                SetParent(item);
            }

            public bool Remove(UIElement item)
            {
                if (!Controller.CanModifyElements())
                    ThrowReadonlyException();

                if (!Controller.CanRemoveElement(item))
                    ThrowBlockedException();

                ResetParent(item);
                return Elements.Remove(item);
            }

            public void RemoveAt(int index)
            {
                if (!Controller.CanModifyElements())
                    ThrowReadonlyException();

                if (!Controller.CanRemoveElement(Elements[index]))
                    ThrowBlockedException();

                ResetParent(Elements[index]);
                Elements.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Elements.GetEnumerator();
            }

            void SetParent(UIElement element)
            {
                element.Parent = Parent;
                element.Root = Parent.Root;
                if (Parent.Root is not null)
                    element.Recalculate();
            }

            void ResetParent(UIElement element)
            {
                element.Parent = null;
                element.Root = null!;
            }

            [DoesNotReturn]
            static void ThrowBlockedException() => throw new InvalidOperationException("Action blocked by the parent element");

            [DoesNotReturn]
            static void ThrowReadonlyException() => throw new InvalidOperationException("Element collection is read-only");

            class DummyController : IElementListController { }
        }
    }
}
