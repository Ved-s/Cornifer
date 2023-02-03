using System;
using System.Diagnostics;

namespace Cornifer.UI.Helpers
{
    public class ElementEvent<TEvent, TElement>
    {
        public readonly int EventId;
        public readonly int ElementLevel = 0;

        public ElementEvent()
        {
            if (!ElementEventManager.NextEventIds.TryGetValue(typeof(TElement), out int nextId))
                nextId = 0;

            EventId = nextId;
            nextId++;
            ElementEventManager.NextEventIds[typeof(TElement)] = nextId;

            Type type = typeof(TElement);
            while (type.BaseType is not null && type.BaseType != typeof(object))
            {
                ElementLevel++;
                type = type.BaseType;
            }
        }
    }
}
