using System;
using UnityEngine;

namespace Script.GUI.ViewModel {
    public abstract class ViewModelBase : MonoBehaviour, IDisposable {
        public bool IsInitialize { get; protected set; }
        
        private void Awake() {
            Initialize();

            IsInitialize = true;
        }

        protected abstract void Initialize();
        public abstract void Dispose();
    }
}