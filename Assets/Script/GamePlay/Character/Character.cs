using Cysharp.Threading.Tasks;
using Script.GamePlay.Character.Interface;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character : MonoBehaviour, ICharacter {
        #region Interface
        //캐릭터 소환시 꼭 실행
        public void Initialize() {
            _state =  CharacterState.None;

            InitializeAction();
            InitializeGamePlay();
            
            // Reactive 초기화, Reactive는 제일 마지막에 초기화가 좋음
            InitializeReactiveProperty();
            
            
            AddState(CharacterState.Initialized);
        }
        
        public void Release() {
            ReleaseAction();
            ReleaseGamePlay();
            ReleaseReactiveProperty();
        }

        public UniTask StartAsync() {
            //FSM 실행
            _characterBehaviour.Start();
            Run();
            return UniTask.CompletedTask;
        }

        private void OnDestroy() {
            Release();
        }

        #endregion
    }
}