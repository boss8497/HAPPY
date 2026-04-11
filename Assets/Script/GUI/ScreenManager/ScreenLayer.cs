using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Script.GUI.Screen.Interface;
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
            await screen.OpenAsync();
        }
    }
}