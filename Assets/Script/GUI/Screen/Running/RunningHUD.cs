using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Attribute;
using Script.GamePlay.Stage;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.Screen {
    public class RunningHUD : Screen {
        [System.Serializable]
        public class PlayerHpObject {
            public GameObject root;
            public GameObject hp;
        }


        /// <summary>
        /// Inject
        /// </summary>
        private IStageManager _stageManager;

        [Inject]
        public void InjectSelf(
            IStageManager stageManager
        ) {
            _stageManager = stageManager;
        }
        
        


        /// <summary>
        /// Inspector
        /// </summary>
        [ScreenKey]
        public string screenKey;

        public Button optionBtn;

        public PlayerHpObject[] playerHps = Array.Empty<PlayerHpObject>();


        /// <summary>
        /// private
        /// </summary>
        private DisposableBag _disposableBag;

        private CancellationTokenSource _addPlayerCts;

        protected override void AwakeInternal() {
            base.AwakeInternal();
            optionBtn.ClickAddListener(OpenOption);
            SetHp(0, 0);
        }

        public override UniTask OpenInternal() {
            _disposableBag.Dispose();
            _disposableBag = new();

            // 플레이어 추가
            StopAddPlayer();
            _addPlayerCts = new();
            AddPlayer(_addPlayerCts.Token).Forget();

            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            StopAddPlayer();
            _disposableBag.Dispose();
            return UniTask.CompletedTask;
        }

        private async UniTask AddPlayer(CancellationToken ct) {
            if (_stageManager == null) {
                Debug.LogError($"StageManager가 주입되지 않았습니다.");
                return;
            }
            
            var isCancel = await UniTask.WaitUntil(() => _stageManager.Initialized?.CurrentValue ?? false, cancellationToken: ct)
                                        .SuppressCancellationThrow();
            if (isCancel) return;
            
            isCancel = await UniTask.WaitUntil(() => (_stageManager.Players?.Count ?? 0) > 0, cancellationToken: ct)
                                    .SuppressCancellationThrow();
            if (isCancel) return;

            // Running에서는 플레이어 캐릭터가 1명
            var player = _stageManager.Players.FirstOrDefault();
            if (player == null) {
                Debug.LogError($"플레이어가 존재하지 않습니다. 여기에서 Player가 Null이면 안됨");
                return;
            }
            
            
            // 확실히 기다려 준다.
            isCancel = await UniTask.WaitUntil(() => player.Initialized?.CurrentValue ?? false, cancellationToken: ct)
                                    .SuppressCancellationThrow();
            if (isCancel) return;
            
            SetHp((int)player.MaxHealth.CurrentValue, (int)player.Health.CurrentValue);
            
            player.MaxHealth.CombineLatest(player.Health, (maxHp, hp) => (maxHp, hp))
                        .Subscribe(tuple => {
                            SetHp((int)tuple.maxHp, (int)tuple.hp);
                        })
                        .AddTo(ref _disposableBag);

        }

        private void StopAddPlayer() {
            if (_addPlayerCts is { IsCancellationRequested: false }) {
                _addPlayerCts.Cancel();
                _addPlayerCts.Dispose();
                _addPlayerCts = null;
            }
        }

        private void SetHp(int maxHp, int hp) {
            for (int i = 0; i < playerHps.Length; i++) {
                var hpObj = playerHps[i];
                hpObj.root.SetActiveSafe(i < maxHp);
                hpObj.hp.SetActiveSafe(i < hp);
            }
        }

        private void OpenOption() {
            ScreenManager.OpenAsync(screenKey).Forget();
        }
    }
}