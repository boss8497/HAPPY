using System;
using R3;
using UnityEngine;

namespace Script.GUI.ViewModel {
    public abstract class ViewModel : MonoBehaviour, IViewModel, IDisposable {
        #region Option
        
        /// <summary>
        /// OnEnable 에서 Initialize를 호출하고 OnDisable에서 Release를 호출함
        /// </summary>
        protected bool objectActiveInitialize = true;

        /// <summary>
        /// 자동 초기화 상태 변경 옵션
        /// </summary>
        protected bool autoInitializeState = true;
        #endregion

        public ReactiveProperty<ViewModelState> State         { get; private set; } = new(ViewModelState.None);
        public ReadOnlyReactiveProperty<bool>   IsInitialized { get; private set; }

        private DisposableBag _reactiveDisposableBag;

        private void Awake() {
            Initialize();
        }

        private void Initialize() {
            if (IsInitialized?.CurrentValue ?? false) {
                return;
            }

            InitializeBaseReactiveProperty();
            InitializeInternal();
            
            if (autoInitializeState)
                AddState(ViewModelState.Initialized);
        }

        public abstract void InitializeInternal();

        private void InitializeBaseReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new();

            IsInitialized = State.Select(i => (i & ViewModelState.Initialized) != 0)
                                 .DistinctUntilChanged()
                                 .ToReadOnlyReactiveProperty()
                                 .AddTo(ref _reactiveDisposableBag);
        }

        private void DisableBaseReactiveProperty() {
            if (IsInitialized == null) return;

            _reactiveDisposableBag.Dispose();
            IsInitialized = null;
        }

        private void OnEnable() {
            if (objectActiveInitialize) {
                Initialize();
            }
        }

        private void OnDisable() {
            if (objectActiveInitialize) {
                Disable();
            }
        }

        private void Disable() {
            DisableInternal();
            DisableBaseReactiveProperty();
            RemoveState(ViewModelState.Initialized);
        }

        /// <summary>
        /// OnDisable 시 ReleaseInternal 호출
        /// </summary>
        public abstract void DisableInternal();

        public void AddState(ViewModelState state) {
            if (State.Value.HasFlag(state)) return;
            State.OnNext(State.Value |= state);
        }

        public void RemoveState(ViewModelState state) {
            if (State.Value.HasFlag(state) == false) return;
            State.OnNext(State.Value &= ~state);
        }


        public void Dispose() {
            DisposeInternal();
            _reactiveDisposableBag.Dispose();
        }

        public abstract void DisposeInternal();
    }
}