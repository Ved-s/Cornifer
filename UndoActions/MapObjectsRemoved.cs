using Cornifer.MapObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cornifer.UndoActions
{
    public class MapObjectsRemoved<T> : UndoAction where T : MapObject
    {
        IList<T> List;
        List<(T, int)> Objects;

        public MapObjectsRemoved(IEnumerable<T> objects, IList<T> list)
        {
            List = list;
            Objects = new(objects.Select(o => (o, list.IndexOf(o))).OrderBy(pair => pair.Item2));
        }

        public override void Redo()
        {
            foreach (var (obj, _) in Objects)
                List.Remove(obj);
            Main.SelectedObjects.ExceptWith(List);
        }

        public override void Undo()
        {
            foreach (var (obj, pos) in Objects)
            {
                if (pos < 0)
                    List.Add(obj);
                else
                    List.Insert(pos, obj);
            }
        }

        public override string ToString()
        {
            if (Objects.Count == 1)
                return $"Remove {Objects[0].Item1} from the map";
            return $"Remove {Objects.Count} object(s) from the map";
        }
    }
}
