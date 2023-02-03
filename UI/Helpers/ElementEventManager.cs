using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cornifer.UI.Helpers
{
    public class ElementEventManager
    {
        public readonly UIElement Parent;

        public static Dictionary<Type, int> NextEventIds = new();

        List<Delegate>?[][] PreCallbacks;
        List<Delegate>?[][] PostCallbacks;
        bool AnyPreCallbacks = false;
        bool AnyPostCallbacks = false;

        public ElementEventManager(UIElement element)
        {
            Parent = element;

            Type? type = element.GetType();

            List<int> eventCounts = new();

            do
            {
                if (!NextEventIds.TryGetValue(type, out int eventCount))
                    eventCount = 0;

                eventCounts.Insert(0, eventCount);
                type = type.BaseType;
            }
            while (type is not null && type != typeof(object));

            PreCallbacks = new List<Delegate>?[eventCounts.Count][];
            PostCallbacks = new List<Delegate>?[eventCounts.Count][];

            for (int i = 0; i < eventCounts.Count; i++)
            {
                PreCallbacks[i] = new List<Delegate>?[eventCounts[i]];
                PostCallbacks[i] = new List<Delegate>?[eventCounts[i]];
            }
        }

        public void Call<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, TEvent data) where TElement : UIElement
        {
            if (PreCall(@event, data))
                PostCall(@event, data);
        }
        public bool PreCall<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, TEvent data) where TElement : UIElement
        {
            if (!AnyPreCallbacks)
                return true;

            List<Delegate>? delegates = PreCallbacks[@event.ElementLevel][@event.EventId];
            if (delegates is null)
                return true;

            TElement parent = (TElement)Parent;

            foreach (Delegate @delegate in delegates)
            {
                if (!(@delegate as Func<TElement, TEvent, bool>)!(parent, data))
                    return false;
            }

            return true;
        }
        public void PostCall<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, TEvent data) where TElement : UIElement
        {
            if (!AnyPostCallbacks)
                return;

            List<Delegate>? delegates = PostCallbacks[@event.ElementLevel][@event.EventId];
            if (delegates is null)
                return;

            TElement parent = (TElement)Parent;

            foreach (Delegate @delegate in delegates)
                (@delegate as Action<TElement, TEvent>)!(parent, data);
        }

        public void AddPreCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Func<TElement, TEvent, bool> callback)
        {
            if (Parent is not TElement)
                throw new ArgumentException("Provided event does not match this element", nameof(@event));

            List<Delegate>? delegates = PreCallbacks[@event.ElementLevel][@event.EventId];
            if (delegates is null)
            {
                delegates = new();
                PreCallbacks[@event.ElementLevel][@event.EventId] = delegates;
            }

            delegates.Add(callback);
            AnyPreCallbacks = true;
        }
        public void AddPostCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Action<TElement, TEvent> callback)
        {
            if (Parent is not TElement)
                throw new ArgumentException("Provided event does not match this element", nameof(@event));

            List<Delegate>? delegates = PostCallbacks[@event.ElementLevel][@event.EventId];
            if (delegates is null)
            {
                delegates = new();
                PostCallbacks[@event.ElementLevel][@event.EventId] = delegates;
            }

            delegates.Add(callback);
            AnyPostCallbacks = true;
        }

        public void RemovePreCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Func<TElement, TEvent, bool> callback)
        {
            if (Parent is not TElement)
                throw new ArgumentException("Provided event does not match this element", nameof(@event));

            List<Delegate>? delegates = PreCallbacks[@event.ElementLevel][@event.EventId];
            if (delegates is null)
                return;

            delegates.Remove(callback);
            AnyPreCallbacks = PreCallbacks.SelectMany(x => x).Any(l => l is not null && l.Count > 0);
        }
        public void RemovePostCallback<TEvent, TElement>(ElementEvent<TEvent, TElement> @event, Action<TElement, TEvent> callback)
        {
            if (Parent is not TElement)
                throw new ArgumentException("Provided event does not match this element", nameof(@event));

            List<Delegate>? delegates = PostCallbacks[@event.ElementLevel][@event.EventId];
            if (delegates is null)
                return;

            delegates.Remove(callback);
            AnyPostCallbacks = PostCallbacks.SelectMany(x => x).Any(l => l is not null && l.Count > 0); ;
        }
    }
}
