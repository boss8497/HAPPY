using Script.GUI.Screen.Enum;
using UnityEngine;

namespace Script.GUI.Screen {
    public abstract partial class Screen {
        [SerializeField]
        private ScreenOption option;
        
        public bool DontClose => option.HasFlag(ScreenOption.DontClose);
    }
}