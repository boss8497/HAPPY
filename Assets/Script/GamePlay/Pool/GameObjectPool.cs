using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

namespace Script.GamePlay.Pool {
    [System.Serializable]
    public class GameObjectPool : IGameObjectPool, IDisposable {
        private readonly IStagePooling _manager;
        private readonly string        _key;

        // 대여 가능한 인스턴스 보관 + Push 시 중복 반환 확인을 이 HashSet 하나로 겸한다
        // (Contains == 대여 가능, Add 실패 == 이미 반환된 인스턴스).
        // ※ _pooled/_instance/_isDisposed는 GameObjectPoolDebugWindow.cs(Tools/Debug/GameObject Pool)가
        // 리플렉션으로 직접 참조한다. 이름을 바꾸면 그 파일의 FieldInfo 캐시도 같이 확인할 것 (Editor/README.md 체크리스트 참고).
        private readonly HashSet<GameObject> _pooled = new();

        private GameObject _instance;
        private Vector3    _baseScale;
        private bool       _isDisposed;

        public string  Key       => _key;
        public Vector3 BaseScale => _baseScale;


        public GameObjectPool(IStagePooling manager, string key, int count = 1) {
            _manager = manager;
            _key     = key;
            Initialize();
            CreateInstance(count);
        }

        public void Initialize() {
            var handle = Addressables.LoadAssetAsync<GameObject>(_key);
            handle.WaitForCompletion();

            _instance = _manager.Resolver.Instantiate(handle.Result, _manager.Root);
            _instance.gameObject.SetActive(false);
            _baseScale = _instance.transform.localScale;

            Addressables.Release(handle);
        }

        private void CreateInstance(int count = 1) {
            for (int i = 0; i < count; i++) {
                // 생성될 때 이미 Active는 False인 상태
                var obj = _manager.Resolver.Instantiate(_instance, _manager.Root);
                if (obj.TryGetComponent<IPoolMember>(out var member) == false) {
                    // 방금 복제된 obj가 아니라 템플릿(_instance)에 붙이면, 이번에 생성된 obj 자신은
                    // 여전히 컴포넌트가 없는 채로 풀에 들어가 이후 Push() 시 풀링 대상으로 인식되지 못하고
                    // 그대로 Destroy 되어버린다.
                    member = obj.AddComponent<PoolMember>();
                }

                member.Set(this);
                obj.transform.position = Vector3.zero;
                _pooled.Add(obj);
            }
        }

        public GameObject Pop() {
            if (_pooled.Count <= 0) {
                CreateInstance();
            }

            return Take(_pooled);
        }

        public void Push(GameObject obj) {
            // 이미 반환된 GameObject를 또 반환하면 이후 서로 다른 Pop() 호출이
            // 동일 인스턴스를 나눠 갖게 되는 문제로 이어진다.
            if (!_pooled.Add(obj)) {
                Debug.LogWarning($"[GameObjectPool:{_key}] {obj.name}이(가) 이미 반환된 상태에서 다시 Push 되었습니다. 중복 반환은 무시합니다.");
                return;
            }

            obj.transform.SetParent(_manager.Root);
        }

        // HashSet에는 Stack.Pop() 같은 임의 추출 메서드가 없어 enumerator로 하나를 꺼낸다.
        // 풀링된 인스턴스는 서로 교체 가능하므로 어떤 걸 꺼내는지는 상관없다.
        private static GameObject Take(HashSet<GameObject> set) {
            using var e = set.GetEnumerator();
            e.MoveNext();
            var obj = e.Current;
            set.Remove(obj);
            return obj;
        }

        public void Dispose() {
            if (_isDisposed) return;

            if (_instance != null) {
                UnityEngine.Object.Destroy(_instance);
            }

            foreach (var item in _pooled) {
                UnityEngine.Object.Destroy(item);
            }

            _pooled.Clear();

            _isDisposed = true;
        }
    }
}