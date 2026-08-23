using System;
using System.Collections.Generic;
using Script.GamePlay.Pool;
using Script.LifetimeScope.Locator;
using Script.Utility.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Script.GamePlay.Audio {
    /// <summary>
    /// UIPooling/StagePooling과 동일한 패턴 — GameObjectPool + IPoolMember를 재사용해
    /// AudioPlayerPrefab(Addressable key="AudioPlayerPrefab")을 풀링한다.
    /// IStagePooling을 구현하는 이유는 GameObjectPool 생성자가 해당 타입을 요구하기 때문(이름과 달리 Stage 전용이 아님).
    /// </summary>
    public class AudioPooling : IAudioPooling, IInitializable, IDisposable {
        private const string AudioPlayerPrefabKey = "AudioPlayerPrefab";

        // ※ GameObjectPoolDebugWindow.cs가 리플렉션으로 직접 참조함 (Editor/README.md 체크리스트 참고)
        private readonly Dictionary<string, GameObjectPool> _objectPools = new(StringComparer.Ordinal);
        private readonly IScopeLocator                      _locator;

        public Transform Root { get; private set; }

        // App 단계에서 등록되므로 UIPooling과 동일하게 "가장 최근에 생성된 자식 Scope"의 Container를 참조한다.
        public IObjectResolver Resolver => _locator?.GetLastChildScope()?.Container;

        public AudioPooling(IScopeLocator locator) {
            _locator = locator;
        }

        public void Initialize() {
            var root = new GameObject("AudioPoolingRoot");
            Root          = root.transform;
            Root.position = new Vector3(int.MaxValue, int.MaxValue, 0);
            Object.DontDestroyOnLoad(root);
        }

        public IAudioPlayer Pop(Transform parent = null) {
            var obj = Pop(AudioPlayerPrefabKey, parent, active: true, worldPositionStays: false);
            return obj.GetComponent<IAudioPlayer>();
        }

        public void Push(IAudioPlayer player) {
            Push(player.GameObject);
        }

        public GameObject Pop(string key, Transform parent = null, bool active = true, bool worldPositionStays = true) {
            if (_objectPools.TryGetValue(key, out var pool) == false) {
                pool = CreatePool(key);
            }

            var obj = pool.Pop();
            obj.SetActiveSafe(active);
            obj.transform.SetParent(parent, worldPositionStays);
            return obj;
        }

        private GameObjectPool CreatePool(string key) {
            var pool = new GameObjectPool(this, key);
            _objectPools.Add(key, pool);
            return pool;
        }

        public bool Push(GameObject obj) {
            obj.SetActiveSafe(false);

            if (obj.TryGetComponent<IPoolMember>(out var member) == false) {
                Object.Destroy(obj);
                return false;
            }

            if (_objectPools.TryGetValue(member.Key, out var pool)) {
                pool.Push(obj);
                return true;
            }

            Object.Destroy(obj);
            return false;
        }

        public bool Exists(string key) {
            return _objectPools.ContainsKey(key);
        }

        public void Dispose() {
            foreach (var pool in _objectPools) {
                pool.Value.Dispose();
            }

            _objectPools.Clear();

            if (Root) {
                Object.Destroy(Root.gameObject);
            }
        }
    }
}
