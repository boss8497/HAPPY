using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info;
using UnityEngine;

namespace Script.GamePlay.Service {
    public partial class GroupService {
        public bool CanPlayTutorial(TutorialInfo info) => GroupData.CanPlayTutorial(info.type);

        public async UniTask UpdateTutorialProgress(TutorialInfo info, CancellationToken ct = default) {
            // 실행이 가능해서 실행한 튜토리얼인데 여기서 걸리면 테스트거나 해킹일 가능성이 높음
            if (!GroupData.CanPlayTutorial(info.type)) {
                return;
            }

            var (model, result) = await _client.Req_UpdateTutorial(info.type, ct);
            if (!result) return;
            GroupData.Update(model);
        }
    }
}