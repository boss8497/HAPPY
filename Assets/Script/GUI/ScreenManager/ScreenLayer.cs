using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Interface;
using Script.GUI.ScreenData.Interface;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GUI.Screen {
    public class ScreenLayer {
        private readonly IScreenManager _screenManager;
        private readonly RectTransform  _root;

        private readonly List<IScreen> _screens = new();

        public ScreenLayer(IScreenManager screenManager, RectTransform root) {
            _screenManager = screenManager;
            _root          = root;
        }

        public async UniTask OpenScreen(IScreen screen, IScreenOption screenOption, CancellationToken ct = default) {
            screen.RectTransform.SetParent(_root, false);
            
            _screens.Add(screen);
            
            await screen.OpenAsync(screenOption, ct);
            
            screen.GameObject.SetActiveSafe(true);
            await screen.OpenAnimationAsync(ct);
            
            await screen.OpenLateInternal(ct);
        }
        
        public async UniTask CloseScreen(IScreen screen, bool force = false) {
            if (force == false && screen.DontClose) {
                return;
            }
            _screens.Remove(screen);

            await screen.CloseAsync();
            await screen.CloseAnimationAsync();

            await screen.CloseLateInternal();
            screen.GameObject.SetActiveSafe(false);
            // 처음 Insert할 때 null 처리 함
            // screen._previous = _next = null;
        }
    }
}