using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GUI.Screen {
    /// <summary>
    /// 사용한 Screen에 상속 받아서 구현하는 기본적인 Screen Base
    /// </summary>
    [System.Serializable]
    public abstract partial class Screen : MonoBehaviour {
        [SerializeField]
        private ScreenLayerType layerType;

        public ScreenLayerType LayerType     => layerType;
        public RectTransform   RectTransform => transform as RectTransform;

        #region Open

        /// <summary>
        /// ScreenManager가 호출하는 Open Ui에서 Call은 따로 구현
        /// </summary>
        public async UniTask OpenAsync() {
            gameObject.SetActiveSafe(true);

            await OpenInternal();
            await OpenAnimationAsync();

            await OpenLateInternal();
        }

        /// <summary>
        /// ScreenOpen 시 제일 먼저 호출되는 메서드
        /// </summary>
        protected abstract UniTask OpenInternal();

        /// <summary>
        /// ScreenOpen 시 제일 마지막 호출되는 메서드
        /// </summary>
        protected virtual UniTask OpenLateInternal() {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OpenAnimationAsync() {
            return UniTask.CompletedTask;
        }

        #endregion


        #region Close

        /// <summary>
        /// ScreenManager가 호출하는 Open Ui에서 Call은 따로 구현
        /// </summary>
        /// <param name="force">DontClose면 Screen을 닫지 않고, force면 그래도 닫음</param>
        public async UniTask CloseAsync(bool force = false) {
            if (force == false && DontClose) {
                return;
            }

            await CloseInternal();
            await CloseAnimationAsync();

            await CloseLateInternal();
            gameObject.SetActiveSafe(false);
            _previous = _next = null;
        }

        /// <summary>
        /// Close 시 제일 먼저 호출되는 메서드
        /// </summary>
        protected abstract UniTask CloseInternal();

        /// <summary>
        /// Close 시 제일 마지막 호출되는 메서드
        /// </summary>
        protected virtual UniTask CloseLateInternal() {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask CloseAnimationAsync() {
            return UniTask.CompletedTask;
        }

        #endregion

        public virtual UniTask Release() {
            return UniTask.CompletedTask;
        }
    }
}