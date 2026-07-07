using System;
using UnityEngine;

namespace Script.GUI.ViewModel {
    public abstract class ViewModelBase : MonoBehaviour, IDisposable {
        private void Awake() {
            Initialize();
        }

        protected abstract void Initialize();
        public abstract void Dispose();
    }
}