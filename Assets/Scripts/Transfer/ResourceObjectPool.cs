using System.Collections.Generic;
using UnityEngine;

// 中心化对象池 — 建筑物、仓库、玩家统一从此取用/归还资源方块
// Centralized object pool — all systems (building, warehouse, player) share this pool
namespace Transfer
{
    public class ResourceObjectPool : MonoBehaviour
    {
        public static ResourceObjectPool Instance { get; private set; }

        // 对象池：prefab → 闲置实例队列 / Pool: prefab → idle instance queue
        private Dictionary<GameObject, Queue<GameObject>> pool = new();

        // 实例 → 原始 prefab 反向查找 / Instance → original prefab lookup
        private Dictionary<GameObject, GameObject> prefabLookup = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 预热指定 prefab 的对象池 / Prewarm pool for a specific prefab
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;

            if (!pool.ContainsKey(prefab))
                pool[prefab] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                prefabLookup[obj] = prefab;
                pool[prefab].Enqueue(obj);
            }
        }

        // 从池中获取实例 / Get an instance from the pool
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!pool.ContainsKey(prefab))
                pool[prefab] = new Queue<GameObject>();

            GameObject obj;
            if (pool[prefab].Count > 0)
            {
                obj = pool[prefab].Dequeue();
            }
            else
            {
                obj = Instantiate(prefab);
                prefabLookup[obj] = prefab;
            }

            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        // 归还实例到池中 / Return an instance to the pool
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (prefabLookup.TryGetValue(obj, out GameObject prefab))
            {
                if (!pool.ContainsKey(prefab))
                    pool[prefab] = new Queue<GameObject>();
                pool[prefab].Enqueue(obj);
            }
            else
            {
                // 未知来源物体，直接销毁 / Unknown source object, destroy it
                Destroy(obj);
            }
        }
    }
}
