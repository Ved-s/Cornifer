using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Cornifer.UndoActions;

namespace Cornifer
{
    public class UndoRedo
    {
        public CircularBuffer<UndoAction> UndoBuffer = new(100);
        public CircularBuffer<UndoAction> RedoBuffer = new(100);

        bool DisableMerge = false;

        public void PreventNextUndoMerge()
        {
            DisableMerge = true;
        }

        public void Do(UndoAction action)
        {
            RedoBuffer.Clear();

            if (!DisableMerge && UndoBuffer.TryPeek(out UndoAction? undo) && undo.TryMerge(action))
                return;

            UndoBuffer.Push(action);
            DisableMerge = false;
        }

        public void Undo()
        {
            if (!UndoBuffer.TryPop(out UndoAction? action))
                return;

            action.Undo();
            RedoBuffer.Push(action);
        }

        public void Redo()
        {
            if (!RedoBuffer.TryPop(out UndoAction? action))
                return;

            action.Redo();
            UndoBuffer.Push(action);
        }
    }
}
