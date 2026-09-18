using System;
using System.Collections.Generic;

namespace Match3.Core.Events
{
    public interface IGameEvent { }

    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _subscribers.Remove(type);
                }
            }
        }

        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                return;
            }

            var handlers = list.ToArray();
            foreach (var handler in handlers)
            {
                ((Action<T>)handler).Invoke(gameEvent);
            }
        }

        public void Clear()
        {
            _subscribers.Clear();
        }
    }
}
