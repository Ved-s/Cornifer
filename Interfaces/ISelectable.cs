using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Cornifer.Interfaces
{
    public interface ISelectable
    {
        public bool Selected => Main.SelectedObjects.Contains(this);

        public bool Active { get; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; }

        public static ISelectable? FindSelectableAtPos(IEnumerable<ISelectable> selectables, Vector2 pos)
        {
            foreach (ISelectable selectable in selectables)
            {
                if (selectable.Position.X <= pos.X
                 && selectable.Position.Y <= pos.Y
                 && selectable.Position.X + selectable.Size.X > pos.X
                 && selectable.Position.Y + selectable.Size.Y > pos.Y)
                    return selectable;
            }
            return null;
        }

        public static IEnumerable<ISelectable> FindIntersectingSelectables(IEnumerable<ISelectable> selectables, Vector2 tl, Vector2 br)
        {
            foreach (ISelectable selectable in selectables)
            {
                bool intersects = selectable.Position.X < br.X
                    && tl.X < selectable.Position.X + selectable.Size.X
                    && selectable.Position.Y < br.Y
                    && tl.Y < selectable.Position.Y + selectable.Size.Y;
                if (intersects)
                    yield return selectable;
            }
        }
    }

    public interface ISelectableContainer
    {
        public IEnumerable<ISelectable> EnumerateSelectables();
    }
}
