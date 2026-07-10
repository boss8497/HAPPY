using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Character;
using Script.GameInfo.Dungeon;
using Script.GameInfo.Enum;
using Script.GamePlay.Character;
using Script.GamePlay.ECS.Component;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using VContainer.Unity;


namespace Script.GamePlay.Stage {
    public partial class StageManager : IStageManager, IInitializable, IDisposable {
        private Dictionary<EventTiming, ClientActionBase[]> _actions;

        private List<Character.ICharacter> _players;
        public  List<Character.ICharacter> Players => _players;

        private List<Character.ICharacter> _enemies;
        public  List<Character.ICharacter> Enemies => _enemies;

        private List<Character.ICharacter> _removeEnemies;

        private Entity _cameraEntity;

        private int _systemControlStack = 0;

        private CancellationTokenSource _updateCts;

        public void Initialize() {
            InitializeAsync().Forget();
        }

        public async UniTask InitializeAsync() {
            // 맵/구조물 스폰, 캐릭터 T포즈 등 준비 과정을 가리기 위해 화면을 얼려둔다.
            // ReStart()에서 이미 얼려둔 경우 재캡처 없이 즉시 반환된다.
            await _screenManager.ShowStageTransitionAsync();

            ResetState();
            _screenManager.OpenAsync(_hudScreenKey).Forget();

            await UniTask.WaitUntil(() => Group?.Initialized ?? false);
            await UniTask.WaitUntil(() => _entityWorld.IsAlive);
            var enterDungeonInfo = Group.GetEnterDungeon();

            AddState(StageState.SystemControl);
            Initialize(enterDungeonInfo.Item1, enterDungeonInfo.Item2);
            AddState(StageState.Initialized);

            await Begin();
            await Start();

            //TODO: 애매한 딜레이 말고 확실한 시작 시점으로 변경 필요
            SetPlayerAnimation(AnimationName.IDLE);
            
            await UniTask.WaitForSeconds(0.5f);
            // 스폰 준비가 끝났으므로 얼려둔 화면을 Fade Out 하여 공개한다.
            await _screenManager.HideStageTransitionAsync();
            
            
            await UniTask.WaitForSeconds(1.5f);
            RemoveState(StageState.SystemControl);
        }

        private float SetPlayerAnimation(AnimationName aniName, bool loop = true) {
            var endTime = 0f;
            foreach (var player in _players) {
                var end = player.SetAnimation(aniName.ToString(), loop);
                if (endTime < end)
                    endTime = end;
            }

            return endTime;
        }

        public void Initialize(DungeonInfo dungeonInfo, GameInfo.Dungeon.Stage stage) {
            InitializeCamera();
            InitializePool();
            InitializeMapGround();
            InitializeReactiveProperty(dungeonInfo, stage);
            InitializeTrigger();
            InitializeAction();
        }

        private void InitializeCamera() {
            var entityManager = _entityWorld.EntityManager;
            if (_cameraEntity == Entity.Null) {
                _cameraEntity = entityManager.CreateSingleton<CameraData>();
            }

            entityManager.SetComponentData(_cameraEntity, new CameraData {
                                               Entity = _cameraEntity,
                                               Camera = CameraControls.MainCamera
                                           });
        }

        public async UniTask Begin() {
            await ExecuteAction(EventTiming.Begin);
            // 시작 시 첫번째 틱 Update
            await ExecuteAction(EventTiming.Update);
        }

        public UniTask Start() {
            foreach (var character in _players) {
                character.AddState(CharacterState.InSideMap);
            }

            StopLoop();
            _updateCts = new();
            UpdateLoop(_updateCts.Token).Forget();
            return UniTask.CompletedTask;
        }

        //Update Loop
        private async UniTask UpdateLoop(CancellationToken ct) {
            var isCancel = false;
            while (ct.IsCancellationRequested == false) {
                UpdateRemoveEnemy();
                UpdateRunningScore();
                await ExecuteAction(EventTiming.Update);

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

        // ECS에서 Update 해줌
        private void UpdateCamera() {
            var entityManager   = _entityWorld.EntityManager;
            var cameraTransform = CameraControls.MainCamera.transform;

            entityManager.SetComponentData(_cameraEntity, new LocalTransform {
                Position = cameraTransform.position,
                Rotation = cameraTransform.rotation,
                Scale    = cameraTransform.localScale.x,
            });

            entityManager.SetComponentData(_cameraEntity, new CameraData {
                Entity = _cameraEntity,
                Camera = CameraControls.MainCamera,
            });
        }

        private void UpdateRemoveEnemy() {
            if (_removeEnemies.Count <= 0) return;
            foreach (var outSideEnemy in _removeEnemies) {
                Debug.LogError($"{outSideEnemy.GameObject.name} OutSideMap Remove!!");
                RemoveEnemy(outSideEnemy);
            }

            _removeEnemies.Clear();
        }

        public void AddRemoveEnemy(ICharacter enemy) {
            if (_removeEnemies.Any(a => ReferenceEquals(a, enemy))) return;
            _removeEnemies.Add(enemy);
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
            // 티어다운으로 T포즈/스폰 과정이 보이기 전에, 지금 화면(마지막 정상 프레임)을 얼려서 덮어둔다.
            await _screenManager.ShowStageTransitionAsync();

            await _screenManager.CloseAllAsync(true);
            StopLoop();

            ResetCamera();
            ReleaseCharacter();
            ResetTrigger();
            ReleaseAction();
            ResetReactive();

            await InitializeAsync();
        }

        private void ResetCamera() {
            _vCamera.LookAt                              = null;
            _vCamera.Follow                              = null;
            _vCamera.transform.position                  = Vector3.zero;
            _targetGroup.Transform.position              = Vector3.zero;
            CameraControls.MainCamera.transform.position = Vector3.zero;
        }

        public void Release() {
            StopLoop();
            ReleaseCharacter();
            ReleaseTrigger();
            ReleaseReactive();
            ReleasePool();
            ReleaseMapGround();
        }

        public void Dispose() {
            Release();
        }

        private void InitializeAction() {
            _actions = PhaseInfo.CurrentValue.actions
                                .Select(ActionFactory.Create)
                                .GroupBy(g => g.Timing)
                                .ToDictionary(r => r.Key, r => r.ToArray());

            foreach (var actionValue in _actions) {
                foreach (var actionBase in actionValue.Value) {
                    actionBase.Initialize(this);
                }
            }
        }

        private void ReleaseAction() {
            foreach (var actionValue in _actions) {
                foreach (var actionBase in actionValue.Value) {
                    actionBase.Release();
                }
            }

            _actions.Clear();
        }


        private async UniTask ExecuteAction(EventTiming timing) {
            foreach (var beginAction in _actions[timing]) {
                await beginAction.ExecuteAsync();
            }
        }
    }
}