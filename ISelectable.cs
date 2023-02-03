
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Cornifer
{
    public interface ISelectable
    {
        public bool Selected => Main.SelectedObjects.Contains(this);

        public Vector2 Position { get; set; }
        public Vector2 Size { get; }
    }

    public interface ISelectableContainer
    {
        public IEnumerable<ISelectable> EnumerateSelectables();
    }
}
