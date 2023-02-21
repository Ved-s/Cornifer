using System;
using System.Linq;

namespace Cornifer.UndoActions
{
    public abstract class UndoAction
    {
        public virtual bool TryMerge(UndoAction action) => false;

        public abstract void Undo();
        public abstract void Redo();

        public override string ToString()
        {
            Type type = GetType();
            if (type.GenericTypeArguments.Length == 0)
                return type.Name;

            return $"{type.Name}<{string.Join(", ", type.GenericTypeArguments.Select(t => t.Name))}>";
        }
    }
}
