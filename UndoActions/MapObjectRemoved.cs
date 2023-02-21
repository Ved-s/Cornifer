using Cornifer.MapObjects;
using System.Collections.Generic;

namespace Cornifer.UndoActions
{
    public class MapObjectRemoved<T> : UndoAction where T : MapObject
    {
        T Object;
        IList<T> List;
        int Position;

        public MapObjectRemoved(T @object, IList<T> list, int? position = null)
        {
            Object = @object;
            List = list;
            Position = position ?? list.IndexOf(@object);
        }

        public override void Redo()
        {
            List.Remove(Object);
            Main.SelectedObjects.Remove(Object);
        }

        public override void Undo()
        {
            if (Position < 0)
                List.Add(Object);
            else
                List.Insert(Position, Object);
        }

        public override string ToString()
        {
            return $"Remove {Object} from the map";
        }
    }
}
