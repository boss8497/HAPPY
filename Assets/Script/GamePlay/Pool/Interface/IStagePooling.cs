using UnityEngine;
using VContainer;

namespace Script.GamePlay.Pool {
    public interface IStagePooling {
        Transform       Root     { get; }
        IObjectResolver Resolver { get; }

        GameObject Pop(string      key, Transform parent = null, bool active = true);
        bool       Push(GameObject obj);

        bool Exists(string key);
    }
}