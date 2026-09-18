using System.Collections.Generic;
using UnityEngine;

namespace Match3.Utils
{
    public class ComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _inactive = new Stack<T>();

        public ComponentPool(T prefab, Transform parent, int prewarmCount = 0)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < prewarmCount; i++)
            {
                var instance = CreateNew();
                Release(instance);
            }
        }

        public T Get()
        {
            var instance = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_parent, false);
            _inactive.Push(instance);
        }

        private T CreateNew()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            return instance;
        }
    }
}
