using System.Linq;
using Script.GamePlay.Character.Interface;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character : MonoBehaviour, ICharacter {


        //Test를 위해 OnEnable에 Character 실행 코드 다 넣어둠
        private void OnEnable() {
            Initialize();
            Start();
        }


        #region Interface
        //캐릭터 소환시 꼭 실행
        public void Initialize() {
            _state =  CharacterState.None;
            
            
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

        public void Start() {
            //FSM 실행
            _characterBehaviour.Start();
            Run();
        }

        #endregion
    }
}