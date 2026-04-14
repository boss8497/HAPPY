using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Attribute;
using Script.GamePlay.Stage;
using Script.Utility.Runtime;
using TMPro;
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

        public Button           optionBtn;
        public PlayerHpObject[] playerHps = Array.Empty<PlayerHpObject>();
        public TMP_Text         scoreText;
        public TMP_Text         runningText;


        /// <summary>
        /// private
        /// </summary>
        private float _lastScore = 0f;
        private float _lastRunning = 0f;

        private DisposableBag           _disposableBag;
        private CancellationTokenSource _subscribeCts;

        protected override void AwakeInternal() {
            base.AwakeInternal();
            optionBtn.ClickAddListener(OpenOption);
            SetHp(0, 0);
        }

        public override UniTask OpenInternal() {
            _disposableBag.Dispose();
            _disposableBag = new();

            // 플레이어 추가
            StopSubscribePlayer();
            _subscribeCts = new();
            SubscribePlayer(_subscribeCts.Token).Forget();
            SubscribeScore(_subscribeCts.Token).Forget();
            SubscribeRunning(_subscribeCts.Token).Forget();

            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            StopSubscribePlayer();
            _disposableBag.Dispose();
            return UniTask.CompletedTask;
        }

        private async UniTask SubscribePlayer(CancellationToken ct) {
            var isCancel = await WaitStage(ct);
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

            // Initialize Hp
            SetHp((int)player.MaxHealth.CurrentValue, (int)player.Health.CurrentValue);
            player.MaxHealth.CombineLatest(player.Health, (maxHp, hp) => (maxHp, hp))
                  .Subscribe(tuple => { SetHp((int)tuple.maxHp, (int)tuple.hp); })
                  .AddTo(ref _disposableBag);
        }


        private async UniTask SubscribeScore(CancellationToken ct) {
            // Text가 지정 안됐으면 그냥 return
            if (scoreText == null) return;

            var isCancel = await WaitStage(ct);
            if (isCancel) return;

            _lastScore = 0f;
            UpdateScore(0f, true);

            _stageManager.Score.Subscribe(score => { UpdateScore(score); })
                         .AddTo(ref _disposableBag);
        }
        private async UniTask SubscribeRunning(CancellationToken ct) {
            // Text가 지정 안됐으면 그냥 return
            if (runningText == null) return;

            var isCancel = await WaitStage(ct);
            if (isCancel) return;

            _lastRunning = 0f;
            UpdateRunning(0f, true);

            _stageManager.RunningScore.Subscribe(score => { UpdateRunning(score); })
                         .AddTo(ref _disposableBag);
        }

        private void UpdateScore(float score, bool force = false) {
            // scoreText가 Null이면 호출이 안되기 때문에 Null체크는 제외
            if (force == false && Mathf.Approximately(_lastScore, score))
                return;

            _lastScore = score;
            scoreText.SetText("Score : {0:0.0}", score);
        }
        
        private void UpdateRunning(float running, bool force = false) {
            // scoreText가 Null이면 호출이 안되기 때문에 Null체크는 제외
            if (force == false && Mathf.Approximately(_lastRunning, running))
                return;

            _lastRunning = running;
            runningText.SetText("{0:0.0} m", running);
        }

        private async UniTask<bool> WaitStage(CancellationToken ct) {
            if (_stageManager == null) {
                Debug.LogError($"StageManager가 주입되지 않았습니다.");
                return false;
            }

            var isCancel = await UniTask.WaitUntil(() => _stageManager.Initialized?.CurrentValue ?? false, cancellationToken: ct)
                                        .SuppressCancellationThrow();
            if (isCancel) return true;

            isCancel = await UniTask.WaitUntil(() => (_stageManager.Players?.Count ?? 0) > 0, cancellationToken: ct)
                                    .SuppressCancellationThrow();

            return isCancel;
        }

        private void StopSubscribePlayer() {
            if (_subscribeCts is { IsCancellationRequested: false }) {
                _subscribeCts.Cancel();
                _subscribeCts.Dispose();
                _subscribeCts = null;
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