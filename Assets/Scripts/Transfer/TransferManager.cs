using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Data;

// 传输管理器（单例） — 统一的资源飞行动画 + 队列调度
// Transfer manager (singleton) — unified flight animation + queue scheduling for all systems
namespace Transfer
{
    public class TransferManager : MonoBehaviour
    {
        public static TransferManager Instance { get; private set; }

        [Tooltip("资源数据库 / Resource database")]
        [SerializeField] private ResourceDatabase resourceDatabase;

        [Tooltip("传输配置 / Transfer config")]
        [SerializeField] private ResourceTransferConfig config;

        // 传输请求内部结构 / Internal transfer request struct
        private struct TransferRequest
        {
            public ResourceType type;
            public Transform startPoint;
            public Transform endPoint;
            public Action onComplete;
        }

        private readonly Queue<TransferRequest> pendingTransfers = new();
        private float queueTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (resourceDatabase != null)
            {
                int perType = config != null ? Mathf.Max(1, config.initialPoolSize / 3) : 4;
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    ResourceEntry entry = resourceDatabase.GetResourceData(type);
                    if (entry?.transferPrefab != null)
                        ResourceObjectPool.Instance?.Prewarm(entry.transferPrefab, perType);
                }
            }
        }

        // === 立即播放传输动画（供 BuildingProduction 调用）===
        // Play a transfer animation immediately (called by BuildingProduction)
        public void PlayTransfer(ResourceType type, Transform startPoint, Transform endPoint, Action onComplete)
        {
            ResourceEntry entry = resourceDatabase?.GetResourceData(type);
            if (entry?.transferPrefab == null || ResourceObjectPool.Instance == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 start = startPoint != null ? startPoint.position : Vector3.zero;
            Vector3 end = endPoint != null ? endPoint.position : Vector3.zero;

            GameObject obj = ResourceObjectPool.Instance.Get(entry.transferPrefab, start, Quaternion.identity);
            if (obj == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(TransferCoroutine(obj, start, end, onComplete));
        }

        // === 排队传输（供 PlayerWarehouseInteraction 调用）===
        // Enqueue a transfer request (called by PlayerWarehouseInteraction)
        public void EnqueueTransfer(ResourceType type, Transform startPoint, Transform endPoint, Action onComplete)
        {
            pendingTransfers.Enqueue(new TransferRequest
            {
                type = type,
                startPoint = startPoint,
                endPoint = endPoint,
                onComplete = onComplete
            });
        }

        private void Update()
        {
            if (pendingTransfers.Count == 0) return;

            float interval = config != null ? config.transferInterval : 0.3f;
            queueTimer -= Time.deltaTime;

            if (queueTimer <= 0f)
            {
                queueTimer = interval;
                ProcessNext();
            }
        }

        private void ProcessNext()
        {
            if (pendingTransfers.Count == 0) return;

            TransferRequest request = pendingTransfers.Dequeue();
            PlayTransfer(request.type, request.startPoint, request.endPoint, request.onComplete);
        }

        // 传输动画协程 / Transfer animation coroutine
        private IEnumerator TransferCoroutine(GameObject obj, Vector3 startPos, Vector3 endPos, Action onComplete)
        {
            float distance = Vector3.Distance(startPos, endPos);
            float speed = config != null ? config.moveSpeed : 5f;
            float duration = distance / Mathf.Max(speed, 0.01f);
            AnimationCurve curve = config != null ? config.moveCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);

            float arcHeight = config != null ? config.arcHeight : 2f;

            float elapsed = 0f;
            while (elapsed < duration && obj != null)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                Vector3 flatPos = Vector3.Lerp(startPos, endPos, t);
                float arcOffset = arcHeight * 4f * t * (1f - t);
                obj.transform.position = flatPos + Vector3.up * arcOffset;
                yield return null;
            }

            if (obj != null)
            {
                obj.transform.position = endPos;
                ResourceObjectPool.Instance?.Return(obj);
            }

            onComplete?.Invoke();
        }
    }
}
