using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using Script.GUI.ScreenData.Interface;
using UnityEngine;

namespace Script.GUI.Screen {
    [Flags]
    public enum ScreenState {
        None,
        Closing, // ScreenManager에서 CloseAsync 호출 시
        Closed,  // ScreenManager에서 CloseAsync 완료 시
        Opening, // ScreenManager에서 OpenAsync 호출 시
        Opened   // ScreenManager에서 OpenAsync 완료 시
    }


    public interface IScreen {
        ScreenState State         { get; }
        bool        OpeningScreen { get; }
        bool        OpenedScreen  { get; }
        bool        ClosingScreen { get; }
        bool        ClosedScreen  { get; }

        IScreen Previous  { get; set; }
        IScreen Next      { get; set; }
        bool    DontClose { get; }

        string          Key           { get; }
        ScreenLayerType LayerType     { get; }
        RectTransform   RectTransform { get; }
        GameObject      GameObject    { get; }

        UniTask OpenAsync(IScreenOption              data,         CancellationToken ct = default);
        UniTask OpenInternal(IScreenOption           screenOption, CancellationToken ct = default);
        UniTask OpenLateInternal(CancellationToken   ct = default);
        UniTask OpenAnimationAsync(CancellationToken ct = default);

        UniTask OpenChangeOptionAsync(IScreenOption data, CancellationToken ct = default);

        void          Back();
        UniTask       BackAsync(CancellationToken ct = default);
        UniTask       CloseAsync();
        UniTask       CloseInternal();
        UniTask       CloseLateInternal();
        UniTask       CloseAnimationAsync();
        UniTask<bool> CloseTrigger();


        UniTask Release();

        void ResetState();
        void AddState(ScreenState    state);
        void RemoveState(ScreenState state);
    }
}