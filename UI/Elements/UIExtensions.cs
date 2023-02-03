using Cornifer.UI.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Cornifer.UI.Elements
{
    public static class UIExtensions
    {
        public static T Assign<T>(this T element, out T value) where T : UIElement?
        {
            value = element;
            return element;
        }

        public static T Execute<T>(this T element, Action<T> callback) where T : UIElement
        {
            callback(element);
            return element;
        }

        public static TElement BeforeEvent<TElement, TTarget, TEvent>(this TElement element,
                                                                      ElementEvent<TEvent, TTarget> @event,
                                                                      Func<TElement, TEvent, bool> callback)
            where TTarget : UIElement
            where TElement : UIElement, TTarget
        {
            Func<TTarget, TEvent, bool> trampoline;

            if (callback is Func<TTarget, TEvent, bool> cb)
                trampoline = cb;
            else
                trampoline = (t, e) => callback((TElement)t, e);

            element.AddPreEventCallback(@event, trampoline);
            return element;
        }
        public static TElement OnEvent<TElement, TTarget, TEvent>(this TElement element,
                                                                  ElementEvent<TEvent, TTarget> @event,
                                                                  Action<TElement, TEvent> callback)
            where TTarget : UIElement
            where TElement : UIElement, TTarget
        {
            Action<TTarget, TEvent> trampoline;
            if (callback is Action<TTarget, TEvent> cb)
                trampoline = cb;
            else
                trampoline = (t, e) => callback((TElement)t, e);

            element.AddPostEventCallback(@event, trampoline);
            return element;
        }

        public static TElement BeforeEvent<TElement, TTarget, TEvent>(this TElement element,
                                                                      ElementEvent<TEvent, TTarget> @event,
                                                                      Func<TElement, TEvent, bool> callback,
                                                                      out Action remove)
            where TTarget : UIElement
            where TElement : UIElement, TTarget
        {
            Func<TTarget, TEvent, bool> trampoline;

            if (callback is Func<TTarget, TEvent, bool> cb)
                trampoline = cb;
            else
                trampoline = (t, e) => callback((TElement)t, e);

            element.AddPreEventCallback(@event, trampoline);
            remove = () => element.RemovePreEventCallback(@event, trampoline);
            return element;
        }
        public static TElement OnEvent<TElement, TTarget, TEvent>(this TElement element,
                                                                  ElementEvent<TEvent, TTarget> @event,
                                                                  Action<TElement, TEvent> callback,
                                                                  out Action remove)
            where TTarget : UIElement
            where TElement : UIElement, TTarget
        {
            Action<TTarget, TEvent> trampoline;
            if (callback is Action<TTarget, TEvent> cb)
                trampoline = cb;
            else
                trampoline = (t, e) => callback((TElement)t, e);

            element.AddPostEventCallback(@event, trampoline);
            remove = () => element.RemovePostEventCallback(@event, trampoline);
            return element;
        }
    }
}
