using System.Collections.Generic;
using UnityEngine;

namespace TexasHoldem.Utils
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool;
        private readonly List<T> _activeObjects;
        private readonly int _maxSize;

        public int ActiveCount => _activeObjects.Count;
        public int PooledCount => _pool.Count;
        public int TotalCount => ActiveCount + PooledCount;

        public ObjectPool(T prefab, Transform parent, int initialSize = 10, int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _pool = new Queue<T>();
            _activeObjects = new List<T>();

            for (int i = 0; i < initialSize; i++)
            {
                CreateNew();
            }
        }

        private T CreateNew()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
            return obj;
        }

        public T Get()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else if (TotalCount < _maxSize)
            {
                obj = Object.Instantiate(_prefab, _parent);
            }
            else
            {
                Debug.LogWarning($"ObjectPool for {typeof(T).Name} reached max size");
                return null;
            }

            obj.gameObject.SetActive(true);
            _activeObjects.Add(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);
            _activeObjects.Remove(obj);
            _pool.Enqueue(obj);
        }

        public void ReturnAll()
        {
            foreach (var obj in _activeObjects.ToArray())
            {
                Return(obj);
            }
        }

        public void Clear()
        {
            foreach (var obj in _activeObjects)
            {
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
            _activeObjects.Clear();

            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
        }
    }

    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _pool;
        private readonly List<GameObject> _activeObjects;
        private readonly int _maxSize;

        public int ActiveCount => _activeObjects.Count;
        public int PooledCount => _pool.Count;

        public GameObjectPool(GameObject prefab, Transform parent, int initialSize = 10, int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _pool = new Queue<GameObject>();
            _activeObjects = new List<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                var obj = Object.Instantiate(_prefab, _parent);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public GameObject Get()
        {
            GameObject obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else if (ActiveCount + PooledCount < _maxSize)
            {
                obj = Object.Instantiate(_prefab, _parent);
            }
            else
            {
                return null;
            }

            obj.SetActive(true);
            _activeObjects.Add(obj);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            _activeObjects.Remove(obj);
            _pool.Enqueue(obj);
        }

        public void ReturnAll()
        {
            foreach (var obj in _activeObjects.ToArray())
            {
                Return(obj);
            }
        }

        public void Clear()
        {
            foreach (var obj in _activeObjects)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            _activeObjects.Clear();

            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                    Object.Destroy(obj);
            }
        }
    }

    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<string, GameObjectPool> _pools;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _pools = new Dictionary<string, GameObjectPool>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            if (_pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool {poolName} already exists");
                return;
            }

            var parent = new GameObject($"Pool_{poolName}");
            parent.transform.SetParent(transform);

            _pools[poolName] = new GameObjectPool(prefab, parent.transform, initialSize, maxSize);
        }

        public GameObject Get(string poolName)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                return pool.Get();
            }

            Debug.LogWarning($"Pool {poolName} not found");
            return null;
        }

        public void Return(string poolName, GameObject obj)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                pool.Return(obj);
            }
        }

        public void ReturnAll(string poolName)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                pool.ReturnAll();
            }
        }

        public void ClearPool(string poolName)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                pool.Clear();
                _pools.Remove(poolName);
            }
        }

        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }
    }
}
