using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Enum;
using UnityEngine;
using VContainer.Unity;


namespace Script.GamePlay.Stage {
    public partial class StageManager : IStageManager, IInitializable, IDisposable {
        private List<Character.ICharacter> _players;
        public  List<Character.ICharacter> Players => _players;

        private List<Character.ICharacter> _enemies;
        public  List<Character.ICharacter> Enemies => _enemies;

        private int _systemControlStack = 0;

        private CancellationTokenSource _updateCts;

        public void Initialize() {
            Test().Forget();
        }

        // 테스트 코드
        // StageLoader 부재
        // StageLoader에서 순차 적으로 Call이 되어야 하며
        // DungeonInfo 에는 이미 Scene안에 BackGround 및 StageLifeTimeScope가 있음
        // 그래서 StageManager 에서는 Trigger 및 Action 실행 해서 
        public async UniTask Test() {
            ResetState();
            _screenManager.OpenAsync(_hudScreenKey).Forget();

            await UniTask.WaitUntil(() => Group?.Initialized ?? false);
            await UniTask.WaitUntil(() => _entityWorld.IsAlive);
            var dungeon = Group.GroupData.Model.CurrentValue.dungeonProgresses.FirstOrDefault();

            AddState(StageState.SystemControl);
            Initialize(dungeon);
            AddState(StageState.Initialized);

            await Begin();
            await Start();

            RemoveState(StageState.SystemControl);
        }

        public void Initialize(DungeonProgress dungeonProgress) {
            InitializePool();
            InitializeReactiveProperty(dungeonProgress);
            InitializeTrigger();
        }

        public async UniTask Begin() {
            await ExecuteAction(EventTiming.Begin);
        }

        public async UniTask Start() {
            foreach (var character in _players) {
                await character.StartAsync();
            }

            foreach (var enemy in _enemies) {
                await enemy.StartAsync();
            }

            StopLoop();
            _updateCts = new();
            UpdateLoop(_updateCts.Token).Forget();
        }

        //Update Loop
        private async UniTask UpdateLoop(CancellationToken ct) {
            var isCancel = false;
            while (ct.IsCancellationRequested == false) {
                UpdateRunningScore();
                var trigger = OnTriggerCheck();
                if (trigger != null) {
                    var loopStop = OnTrigger(trigger);
                    if (loopStop)
                        break;
                }

                isCancel = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct)
                                        .SuppressCancellationThrow();
                if (isCancel) break;
            }

            UpdateRunningScore();
            if (isCancel == false)
                StopLoop();
        }

        private void UpdateRunningScore() {
            // 일단은 0이 시작 지점이라 계산하자.
            var character = _players.FirstOrDefault();
            if (character == null) {
                return;
            }
            RunningScore.OnNext(character.Transform.position.x);
        }

        public async UniTask End() {
            await ExecuteAction(EventTiming.End);
        }

        private void StopLoop() {
            if (_updateCts is { IsCancellationRequested: false }) {
                _updateCts.Cancel();
                _updateCts.Dispose();
                _updateCts = null;
            }
        }

        public async UniTask ReStart() {
            await _screenManager.CloseAllAsync(true);
            StopLoop();
            ResetTrigger();
            ResetReactive();
            ResetPool();

            await Test();
        }

        public void Release() {
            StopLoop();
            ReleaseTrigger();
            ReleaseReactive();
            ReleasePool();
        }

        public bool AddCharacter(GameObject obj) {
            var characterScript = obj.GetComponent<Character.ICharacter>() ?? obj.GetComponentInChildren<Character.ICharacter>();
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다. GameObject: {obj.name}");
                return false;
            }

            //카메라 셋팅
            _targetGroup.AddMember(obj.transform, 1, 1);

            characterScript.Initialize(0, true);
            _players.Add(characterScript);
            return true;
        }

        public bool AddEnemy(GameObject obj) {
            var characterScript = obj.GetComponent<Character.ICharacter>() ?? obj.GetComponentInChildren<Character.ICharacter>();
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다. GameObject: {obj.name}");
                return false;
            }

            characterScript.Initialize(1, false);
            _enemies.Add(characterScript);
            return true;
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
    }
}