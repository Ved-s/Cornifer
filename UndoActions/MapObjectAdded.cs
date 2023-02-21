using Cornifer.MapObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UndoActions
{
    public class MapObjectAdded<T> : UndoAction where T : MapObject
    {
        T Object;
        IList<T> List;
        int Position;

        public MapObjectAdded(T @object, IList<T> list, int? position = null)
        {
            Object = @object;
            List = list;
            Position = position ?? list.IndexOf(@object);
        }

        public override void Redo()
        {
            if (Position < 0)
                List.Add(Object);
            else
                List.Insert(Position, Object);
        }

        public override void Undo()
        {
            List.Remove(Object);
            Main.SelectedObjects.Remove(Object);
        }

        public override string ToString()
        {
            return $"Add {Object} to the map";
        }
    }
}
