using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Enum;
using UnityEngine;
using VContainer.Unity;


namespace Script.GamePlay.Stage {
    public partial class StageManager : IStageManager, IInitializable, IDisposable {
        private List<Character.Character> _characters;
        private List<GameObject>          _characterObjects;
        private int                       _systemControlStack = 0;



        public void Initialize() {
            Test().Forget();
        }
        // 테스트 코드
        // StageLoader 부재
        // StageLoader에서 순차 적으로 Call이 되어야 하며
        // DungeonInfo 에는 이미 Scene안에 BackGround 및 StageLifeTimeScope가 있음
        // 그래서 StageManager 에서는 Trigger 및 Action 실행 해서 
        public async UniTaskVoid Test() {
            await UniTask.WaitUntil(() => _group.Initialized);
            await UniTask.WaitUntil(() => _entityWorld.IsAlive);
            var dungeon = _group.GroupData.Model.CurrentValue.dungeonProgresses.FirstOrDefault();
            Initialize(dungeon);
            await Begin();
            await Start();
        }

        public void Initialize(DungeonProgress dungeonProgress) {
            InitializePool();
            InitializeReactiveProperty(dungeonProgress, StageState.SystemControl);

            AddState(StageState.Initialized);
        }

        public async UniTask Begin() {
            await ExecuteAction(EventTiming.Begin);
        }

        public async UniTask Start() {
            RemoveState(StageState.SystemControl);
            foreach (var character in _characters) {
                await character.StartAsync();
            }
        }

        public async UniTask End() {
            await ExecuteAction(EventTiming.End);
        }

        public void Release() {
            ReleaseReactiveProperty();
            ReleasePool();
        }

        public void AddCharacter(GameObject obj, bool isPlayer = false) {
            _characterObjects.Add(obj);
            var characterScript = obj.GetComponent<Character.Character>();
            if (characterScript == null) {
                characterScript = obj.GetComponentInChildren<Character.Character>();
            }
            _targetGroup.AddMember(obj.transform, 1, 1);
            characterScript.Initialize(isPlayer ? 0 : 1);
            _characters.Add(characterScript);
        }

        public void Dispose() {
            Release();

            DungeonProgress?.Dispose();
            State?.Dispose();
            PhaseIndex?.Dispose();
        }


        private async UniTask ExecuteAction(EventTiming timing) {
            foreach (var beginAction in PhaseInfo.CurrentValue.actions
                                                 .Where(r => r.timing == timing)
                                                 .Select(ActionFactory.Create)
                    ) {
                await beginAction.Initialize(this);
                await beginAction.Execute();
            }
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
        }
    }
}