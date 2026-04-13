using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.GUI.Screen.Interface;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
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

        public ScreenLayerType LayerType     => layerType;
        public string          Key           => key;
        public RectTransform   RectTransform => transform as RectTransform;
        public GameObject      GameObject    => gameObject;


        private IScreenManager _screenManager;

        [Inject]
        public void InjectBase(
            IScreenManager screenManager
        ) {
            _screenManager = screenManager;
        }

        #region Open

        /// <summary>
        /// ScreenOpen 시 제일 먼저 호출되는 메서드
        /// </summary>
        public abstract UniTask OpenInternal();

        /// <summary>
        /// ScreenOpen 시 제일 마지막 호출되는 메서드
        /// </summary>
        public virtual UniTask OpenLateInternal() {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OpenAnimationAsync() {
            return UniTask.CompletedTask;
        }

        #endregion


        #region Close

        public void Back() {
            _screenManager.Back().Forget();
        }

        public async UniTask BackAsync() {
            await _screenManager.Back();
        }

        public void Close(bool force = false) {
            if (force == false && DontClose) {
                return;
            }

            _screenManager.CloseAsync(this).Forget();
        }


        /// <summary>
        /// Close 시 제일 먼저 호출되는 메서드
        /// </summary>
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

        public virtual UniTask<bool> CloseTrigger() {
            return new UniTask<bool>(true);
        }

        #endregion

        public virtual UniTask Release() {
            return UniTask.CompletedTask;
        }


        #region Inspector

        private bool KeyEmpty() {
            return string.IsNullOrEmpty(key) == false;
        }

        #endregion
    }
}