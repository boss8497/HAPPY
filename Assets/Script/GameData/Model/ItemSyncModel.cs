using System;
using MessagePack;

namespace Script.GameData.Model {
    /// <summary>
    /// 서버 → 클라이언트로 아이템/보상 변경사항을 내려줄 때 사용하는 응답 모델.
    /// (현재 사용처: GameDataBase.StageRewards → IClient.Req_ClearStage → GroupService.ClearedDungeon)
    ///
    /// updateItems는 반드시 Client의 IItemService.UpdateItems(updateItems)로 반영해서
    /// GameData/Data(ItemData)의 ReactiveProperty를 갱신해야 UI에 즉시 반영된다.
    /// rewardInfoUids만 보고 updateItems를 직접 만들어 쓰지 말 것 (수량/레벨업 등 서버 계산 결과가 다를 수 있음).
    /// </summary>
    [MessagePackObject]
    public partial class ItemSyncModel {
        /// <summary>
        /// 이번 응답에서 지급 사유가 된 RewardInfo uid 목록 (stage.rewards + stage.exps).
        /// 실제 지급 결과는 updateItems에 이미 반영되어 있으므로, 이 필드는 "무엇을 보상으로 받았는지"
        /// 연출/로그(획득 팝업 등)에 쓰기 위한 참조용 — 하나의 RewardInfo가 itemRewards로 여러 아이템을 포함할 수 있다.
        /// 아이템 수량/레벨업 계산에는 사용하지 말 것 (그건 updateItems가 담당).
        /// TODO: 아직 Client에서 소비하는 곳 없음 — 보상 획득 연출/리스트 UI 추가 시 사용 예정.
        /// </summary>
        [Key(0)] public int[] rewardInfoUids = Array.Empty<int>();

        /// <summary>
        /// 이번 요청으로 값이 변한(또는 새로 생긴) 아이템의 최종 상태 스냅샷.
        /// level/grade/tier/count/exp 등 하나라도 바뀐 아이템은 전부 포함되며, uid로 구분된다
        /// (같은 uid가 여러 번 갱신돼도 최종 1개만 들어감 — 예: 캐릭터 경험치 아이템).
        /// 신규 지급 아이템도 여기 포함되므로 uid가 아직 로컬에 없으면 신규 추가로 처리해야 한다.
        /// </summary>
        [Key(1)] public ItemModel[] updateItems = Array.Empty<ItemModel>();
    }
}