using UnityEngine;
using VContainer;

namespace Script.GamePlay.Pool {
    public interface IUIPooling : IStagePooling {
        void Clear();
    }
}