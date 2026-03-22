using System;
using Script.GameData.Model;
using UnityEngine;


namespace Script.GamePlay.Stage {
    public partial class StageManager : IStageManager, IDisposable {
        private int _systemControlStack = 0;
        
        
        
        public void Initialize(DungeonProgress dungeonProgress) {
            InitializeReactiveProperty(dungeonProgress, StageState.SystemControl);
            
            
            
            AddState(StageState.Initialized);
        }

        public void Begin() {
            throw new System.NotImplementedException();
        }

        public void Start() {
            throw new NotImplementedException();
        }

        public void End() {
            throw new System.NotImplementedException();
        }

        public void Release() {
            ReleaseReactiveProperty();
        }


        public void SetSystemControl(bool isOn) {
            if (isOn) {
                _systemControlStack += 1;
                Debug.Log($"사용자 입력 차단 시작. Stack: {_systemControlStack}");
            }
            else {
                _systemControlStack -= 1;
                Debug.Log($"사용자 입력 차단 해제. Stack: {_systemControlStack}");
            }

            if (_systemControlStack < 0) {
                _systemControlStack = 0;
            }

            if (_systemControlStack <= 0) {
                RemoveState(StageState.SystemControl);
            }
            else {
                AddState(StageState.SystemControl);
            }

            //System Control이 필요한 로직들 설정
        }

        
        public void Dispose() {
            ReleaseReactiveProperty();
            DungeonProgress?.Dispose();
            State?.Dispose();
        }
    }
}