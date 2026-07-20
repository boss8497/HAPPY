using Script.GameInfo.Info;
using Script.GamePlay.Audio.Interface;
using Script.GamePlay.Pool;
using UnityEngine;

namespace Script.GamePlay.Audio {
    /// <summary>
    /// AudioPooling이 관리하는 실제 재생 단위. AudioPlayerPrefab에 부착되어 있어야 한다(AudioSource 필수).
    /// Generation은 ReturnToIdle()마다 증가 — 이미 반환된(재사용된) 인스턴스를 가리키는 옛 AudioHandle을 무효화한다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlayer : MonoBehaviour, IAudioPlayer, IPoolMember {
        private IGameObjectPool _gameObjectPool;


        public GameObject GameObject => gameObject;

        // this의 정적 타입이 AudioPlayer(MonoBehaviour)이므로 여기서만 Unity의 "destroyed == null" 오버로드가
        // 정상적으로 걸린다. IAudioPlayer 인터페이스 타입으로 비교하면 이 오버로드가 적용되지 않아 항상 false가 나온다.
        public bool IsAlive => this != null;

        public AudioSource    Source     { get; private set; }
        public int            Generation { get; private set; }
        public bool           Busy       { get; private set; }
        public string         ClipKey    { get; private set; }
        public bool           Loop       { get; private set; }
        public AudioGroup Group      { get; private set; }

        public string Key => _gameObjectPool?.Key;

        private void Awake() {
            Source = GetComponent<AudioSource>();
        }

        public void Set(IGameObjectPool gameObjectPool) {
            _gameObjectPool = gameObjectPool;
        }

        public AudioHandle Rent(AudioGroup group, string clipKey, bool loop) {
            Busy    = true;
            Group   = group;
            ClipKey = clipKey;
            Loop    = loop;
            return new AudioHandle(GetInstanceID(), Generation);
        }

        public void ReturnToIdle() {
            Busy    = false;
            ClipKey = null;

            Source.Stop();
            Source.clip = null;

            Generation++;
        }

        public bool Matches(AudioHandle handle) {
            return Busy && handle.InstanceId == GetInstanceID() && handle.Generation == Generation;
        }

        /// <summary>
        /// target의 자식으로 붙어 위치를 그대로 따라간다.
        /// 주의: target GameObject가 재생 도중 Destroy되면 자식인 AudioPlayer도 함께 파괴되어 소리가 끊긴다.
        /// 짧게 끝나는 이펙트 사운드에만 사용을 권장한다.
        /// </summary>
        public void Track(Transform target) {
            transform.SetParent(target, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 특정 월드 좌표에 스냅샷으로 배치한다(추적 없음).
        /// </summary>
        public void SetPosition(Vector3 position) {
            transform.SetParent(null);
            transform.position = position;
        }

        public int GetInstance() {
            return GetInstanceID();
        }
    }
}