using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GamePlay.Character;
using Script.Utility.Runtime;

namespace Script.GamePlay.Buff {
    public class Buff : IBuff, IClassPool {
        private ICharacter   _owner;
        private BuffInfo     _buffInfo;
        private CancellationTokenSource _cts;
        
        public StatusInfo[] StatusInfos { get; private set; }

        public void Initialize(ICharacter character, int buffUid) {
            _owner     = character;
            _buffInfo  = GameInfoManager.Instance.Get<BuffInfo>(buffUid);
            StatusInfos = _buffInfo.statusUid.Select(i => GameInfoManager.Instance.Get<StatusInfo>(i)).ToArray();
        }

        public void StartBuff() {
            Stop();
            _cts = new();
            Timer(_cts.Token).Forget();
        }

        private async UniTask Timer(CancellationToken ct) {
            var gameTimer = _owner.GameTimer;
            var endTime   = _buffInfo.time + gameTimer.Elapsed;
            while (ct.IsCancellationRequested == false && gameTimer.Elapsed <= endTime) {
                var isCancel = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct).SuppressCancellationThrow();
                if (isCancel) break;
            }
            RemoveBuff();
        }

        private void RemoveBuff() {
            _owner.RemoveBuff(this);
        }

        public void Stop() {
            if (_cts is { IsCancellationRequested: false }) {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public void OnRent() {
        }

        public void OnReturn() {
            Stop();
            _owner     = null;
            _buffInfo  = null;
            StatusInfos = null;
        }
    }
}