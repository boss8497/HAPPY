using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.GamePlay.Pool;
using UnityEngine;

namespace Script.GamePlay.Character {
    [System.Serializable]
    public partial class Character : Unit.Unit, ICharacter, IPoolMember {
        #region Interface
        private IGameObjectPool _gameObjectPool;
        public  string Key => _gameObjectPool?.Key ?? CharacterInfo?.prefab;
        
        public void   Set(IGameObjectPool gameObjectPool) {
            _gameObjectPool = gameObjectPool;
        }
        
        //캐릭터 소환시 꼭 실행
        public void Initialize(int team, bool isPlayer = false) {
            SetState(CharacterState.None);
            _isPlayer = isPlayer;
            
            // UnRegister는 진짜로 Destroy가 됐을 때 호출해준다.
            // 왠만하면 Pooling으로 사용하기 때문에 Release 호출했다가 
            // 지워지우는 말자.
            _unitManager.RegisterUnit(this, team);
            InitializeEntity();

            InitializeGamePlay();
            InitializeAction();
            InitializeBuff();
            
            // Reactive 초기화, Reactive는 제일 마지막에 초기화가 좋음
            InitializeReactiveProperty();
            
            
            AddState(CharacterState.Initialized);
        }
        
        public void Release() {
            _characterBehaviour?.Stop();
            ReleaseAction();
            ReleaseGamePlay();
            ReleaseBuff();
            ReleaseReactiveProperty();
            _unitManager.UnRegisterUnit(this);
        }

        public UniTask StartAsync() {
            //FSM 실행
            _characterBehaviour.Start();
            
            return UniTask.CompletedTask;
        }

        private void OnDestroy() {
            Release();
        }

        #endregion
    }
}