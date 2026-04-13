using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Enum;
using UnityEngine;

namespace Script.GUI.Screen {
    public interface IScreen {
        IScreen Previous  { get; set; }
        IScreen Next      { get; set; }
        bool    DontClose { get; }

        string          Key           { get; }
        ScreenLayerType LayerType     { get; }
        RectTransform   RectTransform { get; }
        GameObject      GameObject    { get; }

        UniTask OpenInternal();
        UniTask OpenLateInternal();
        UniTask OpenAnimationAsync();

        void          Back();
        UniTask       BackAsync();
        UniTask       CloseInternal();
        UniTask       CloseLateInternal();
        UniTask       CloseAnimationAsync();
        UniTask<bool> CloseTrigger();


        UniTask Release();
    }
}