using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Enum;
using Script.GUI.Screen.Enum;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using UnityEngine;
using UnityEngine.Events;

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

        /// <summary>
        /// 현재 화면을 캡처해 StageTransition Layer로 덮는다. 이미 덮여 있으면 재캡처 없이 즉시 반환한다.
        /// </summary>
        UniTask ShowStageTransitionAsync();

        /// <summary>
        /// StageTransition Layer로 덮어둔 화면을 Fade Out으로 걷어낸다. 덮여 있지 않으면 아무 것도 하지 않는다.
        /// </summary>
        UniTask HideStageTransitionAsync();

        GameObject PoolPop(string      key, Transform parent = null, bool active = true, bool worldPositionStays = true);
        bool       PoolPush(GameObject obj);

        UniTask OpenErrorMessage(ErrorMessage errorMessage, CancellationToken ct = default, object[] arguments = null);

        UniTask OpenMessage(
            string            title,
            string            message,
            object[]          arguments    = null,
            MessageType       messageType  = MessageType.Ok,
            UnityAction       okAction     = null,
            UnityAction       cancelAction = null,
            UnityAction       yesAction    = null,
            UnityAction       noAction     = null,
            CancellationToken ct           = default
        );
    }
}