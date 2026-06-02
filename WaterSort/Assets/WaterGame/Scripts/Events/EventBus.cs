using System;
using System.Collections.Generic;

namespace AsGame.Events
{
    public static class EventBus
    {
        static readonly Dictionary<string, List<Action<object>>> Handlers = new();

        public static void Subscribe(string evt, Action<object> handler)
        {
            if (!Handlers.TryGetValue(evt, out var list))
            {
                list = new List<Action<object>>();
                Handlers[evt] = list;
            }

            if (!list.Contains(handler))
                list.Add(handler);
        }

        public static void Unsubscribe(string evt, Action<object> handler)
        {
            if (Handlers.TryGetValue(evt, out var list))
                list.Remove(handler);
        }

        public static void Publish(string evt, object payload = null)
        {
            if (!Handlers.TryGetValue(evt, out var list)) return;
            var copy = list.ToArray();
            foreach (var h in copy)
                h?.Invoke(payload);
        }
    }
}
