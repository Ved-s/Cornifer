using Cornifer.MapObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UndoActions
{
    public class MapObjectsMoved : UndoAction
    {
        static HashSet<MapObject> DiffCheckSet = new();

        MapObject[] Objects;
        Vector2 Difference;

        public MapObjectsMoved(IEnumerable<MapObject> objects, Vector2 diff) 
        {
            Objects = objects.ToArray();
            Difference = diff;
        }

        public override bool TryMerge(UndoAction action)
        {
            if (action is not MapObjectsMoved otherObjectsMoved)
                return false;

            DiffCheckSet.Clear();
            DiffCheckSet.UnionWith(otherObjectsMoved.Objects);
            DiffCheckSet.ExceptWith(Objects);

            if (DiffCheckSet.Count == 0)
            {
                Difference += otherObjectsMoved.Difference;
                DiffCheckSet.Clear();
                return true;
            }
            DiffCheckSet.Clear();
            return false;
        }

        public override void Undo()
        {
            foreach (MapObject obj in Objects) 
                obj.ParentPosition -= Difference;

            foreach (MapObject obj in Objects)
            {
                Vector2 pos = obj.WorldPosition;
                pos.Round();
                obj.WorldPosition = pos;
            }
        }

        public override void Redo()
        {
            foreach (MapObject obj in Objects)
                obj.ParentPosition += Difference;

            foreach (MapObject obj in Objects)
            {
                Vector2 pos = obj.WorldPosition;
                pos.Round();
                obj.WorldPosition = pos;
            }
        }

        public override string ToString()
        {
            if (Objects.Length == 1) 
                return $"Move {Objects[0]} by {Difference}";
            return $"Move {Objects.Length} object(s) by {Difference}";
        }
    }
}
