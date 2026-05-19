using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.GUI.ScreenData.Interface;
using UnityEngine;

namespace Script.GUI.Screen.Interface {
    public interface IScreenManager {
        ScreenManagerState State       { get; }
        bool               Initialized { get; }

        void    Initialize();
        UniTask OpenAsync(string        key,          CancellationToken ct                        = default);
        UniTask OpenAsync(IScreenOption screenOption, string            key, CancellationToken ct = default);


        UniTask CloseAllAsync(bool force = false);
        UniTask Back();
        UniTask CloseAsync(ReadOnlyMemory<char> key,    bool force = false);
        UniTask CloseAsync(IScreen              screen, bool force = false);
        UniTask ResourceClear();

        GameObject PoolPop(string      key, Transform parent = null, bool active = true);
        bool       PoolPush(GameObject obj);
    }
}