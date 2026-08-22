using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.GUI.Screen.Interface;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Script.GUI.Screen {
    /// <summary>
    /// 사용한 Screen에 상속 받아서 구현하는 기본적인 Screen Base
    /// </summary>
    [System.Serializable]
    public abstract partial class Screen : MonoBehaviour, IScreen {
        [ValidateInput(nameof(KeyEmpty), "Key 값은 비어 있으면 안 됩니다.")]
        [SerializeField]
        private string key;


        [SerializeField]
        private ScreenLayerType layerType = ScreenLayerType.None;

        private List<GameObject> _pools;
        
        // 한 화면에서 중복 Back 호출을 막기 위한 스위치
        private bool _backRequested;

        public ScreenLayerType LayerType => layerType;

        public ScreenState State { get; private set; } = ScreenState.None;

        public bool OpeningScreen => (State & ScreenState.Opening) != 0;
        public bool OpenedScreen  => (State & ScreenState.Opened) != 0;
        public bool ClosingScreen => (State & ScreenState.Closing) != 0;
        public bool ClosedScreen  => (State & ScreenState.Closed) != 0;

        public string        Key           => key;
        public RectTransform RectTransform => transform as RectTransform;
        public GameObject    GameObject    => gameObject;


        private   IScreenManager _screenManager;
        protected IScreenManager ScreenManager => _screenManager;

        [Inject]
        public void InjectBase(
            IScreenManager screenManager
        ) {
            _screenManager = screenManager;
        }

        #region Open

        /// <summary>
        /// ScreenOpen 시 제일 먼저 호출되는 메서드, 호출 후 Active가 켜진다!!
        /// </summary>
        public async UniTask OpenAsync(IScreenOption data, CancellationToken ct = default) {
            _pools         = ListPool.Get<GameObject>();
            _backRequested = false;
            await OpenInternal(data, ct);
        }

        /// <summary>
        /// ScreenOpen 시 제일 먼저 호출되는 메서드, 호출 후 Active가 켜진다!!
        /// </summary>
        public abstract UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default);

        /// <summary>
        /// ScreenOpen 시 제일 마지막 호출되는 메서드
        /// </summary>
        public virtual UniTask OpenLateInternal(CancellationToken ct = default) {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OpenAnimationAsync(CancellationToken ct = default) {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OpenChangeOptionAsync(IScreenOption data, CancellationToken ct = default) {
            return UniTask.CompletedTask;
        }

        #endregion


        #region Close

        public void Back() {
            if (_backRequested) return;
            _backRequested = true;
            _screenManager.Back().Forget();
        }

        public async UniTask BackAsync() {
            if (_backRequested) return;
            _backRequested = true;
            await _screenManager.Back();
        }

        /// <summary>
        /// Close를 Ui Event[Unity Event] 접근을 막기 위해서 사용.
        /// 정식 적으로는 Back을 사용하고 필요 시 Close 호출
        /// </summary>
        /// <param name="force"></param>
        protected void Close(bool force = false) {
            if (force == false && DontClose) {
                return;
            }

            _screenManager.CloseAsync(this).Forget();
        }


        /// <summary>
        /// Close 시 제일 먼저 호출되는 메서드
        /// </summary>
        public async UniTask CloseAsync() {
            await CloseInternal();
            foreach (var pool in _pools.ToArray()) {
                PoolPush(pool);
            }

            _pools.Clear();
            ListPool<GameObject>.Release(_pools);
        }

        public abstract UniTask CloseInternal();

        /// <summary>
        /// Close 시 제일 마지막 호출되는 메서드
        /// </summary>
        public virtual UniTask CloseLateInternal() {
            return UniTask.CompletedTask;
        }

        public virtual UniTask CloseAnimationAsync() {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Screen 종료 전 확인 후 종료하는 메서드
        /// </summary>
        /// <returns></returns>
        public virtual UniTask<bool> CloseTrigger() {
            return new UniTask<bool>(true);
        }

        #endregion

        public virtual UniTask Release() {
            return UniTask.CompletedTask;
        }

        public void ResetState() {
            State = ScreenState.None;
        }

        public void AddState(ScreenState state) {
            if (State.HasFlag(state)) return;
            State |= state;
        }

        public void RemoveState(ScreenState state) {
            if (State.HasFlag(state) == false) return;
            State &= ~state;
        }


        #region Pooling

        public GameObject PoolPop(string path, Transform parent = null, bool active = true, bool worldPositionStays = true) {
            var obj = _screenManager.PoolPop(path, parent, active, worldPositionStays);
            _pools.Add(obj);
            return obj;
        }

        public void PoolPush(GameObject obj) {
            _pools.Remove(obj);
            _screenManager.PoolPush(obj);
        }

        #endregion

        #region Inspector

        private bool KeyEmpty() {
            return string.IsNullOrEmpty(key) == false;
        }

        #endregion
    }
}