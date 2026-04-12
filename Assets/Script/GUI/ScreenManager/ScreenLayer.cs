using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Interface;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GUI.Screen {
    public class ScreenLayer {
        private readonly IScreenManager _screenManager;
        private readonly RectTransform  _root;

        private readonly List<Screen> _screens = new();

        public ScreenLayer(IScreenManager screenManager, RectTransform root) {
            _screenManager = screenManager;
            _root          = root;
        }

        public async UniTask OpenScreen(Screen screen) {
            screen.RectTransform.SetParent(_root, false);
            
            _screens.Add(screen);
            
            screen.gameObject.SetActiveSafe(true);
            
            await screen.OpenInternal();
            await screen.OpenAnimationAsync();
            
            await screen.OpenLateInternal();
        }
        
        public async UniTask CloseScreen(Screen screen, bool force = false) {
            _screens.Remove(screen);
            
            if (force == false && screen.DontClose) {
                return;
            }

            await screen.CloseInternal();
            await screen.CloseAnimationAsync();

            await screen.CloseLateInternal();
            screen.gameObject.SetActiveSafe(false);
            // 처음 Insert할 때 null 처리 함
            // screen._previous = _next = null;
        }
    }
}