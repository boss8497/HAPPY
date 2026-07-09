# Client — 서버 통신 인터페이스

서버와의 통신 계약(`IClient`)과 그 구현체(`GameClient`)를 포함한다.  
현재 Server가 없으므로 `GameClient`가 로컬 DB로 모든 요청을 처리한다.  
Server 완성 시 `GameClient`만 실제 통신 구현체로 교체하면 된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/IClient.cs` | 서버 통신 인터페이스 |
| `GameClient.cs` | 플러그인 초기화 (Firebase, Steam 등 — 현재 미구현) |
| `GameClient.Client.cs` | IClient 구현 — 로컬 DB 기반 게임 로직 |

## IClient 메서드

```csharp
Req_Group()                                         → UniTask<GroupModel>   // 그룹 조회 (없으면 신규 생성)
Req_SaveGroup(GroupModel)                           → UniTask               // 그룹 저장
Req_Inventory(long groupUid)                        → UniTask<ItemModel[]>  // 인벤토리 조회
Req_ItemLevelUp(ItemModel, LevelType)               → UniTask<ItemModel>    // 아이템 강화
Req_EnterDungeon(DungeonInfo, Stage, long charUid)  → UniTask<bool>          // 던전 진입 (로컬: 항상 true)
Req_ClearStage(DungeonInfo, Stage, long charUid)    → UniTask<ItemSyncModel> // 스테이지 클리어 + 보상/경험치 동기화
Req_RemoveGroup()                                   → UniTask                // 그룹 + 아이템 전체 삭제
```

## Req_ClearStage 동작 (가장 복잡한 메서드)

```
1. 이전 스테이지 클리어 여부 검증 (부정행위 감지 — 통과 못하면 Exception)
2. 던전 진행도 업데이트
   ├─ 마지막 스테이지 + 다음 던전 있음  → 다음 던전 시작
   ├─ 마지막 스테이지 + 마지막 던전    → 전체 클리어 표시
   └─ 일반 스테이지                    → 다음 스테이지로 진행
3. Req_StageRewards → GameDataBase.StageRewards 호출
   ├─ 스테이지 보상(stage.rewards) 아이템 추가
   └─ 캐릭터 경험치(stage.exps) 추가 (자동 레벨업 포함, characterUid > 0일 때만)
4. GroupModel + ItemTable DB 저장
5. ItemSyncModel { rewardInfoUids, updateItems } 반환
```

호출부(`GroupService.ClearedDungeon`)는 반환된 `ItemSyncModel.updateItems`를
반드시 `IItemService.UpdateItems(...)`로 반영해 로컬 아이템 상태(ItemData)를 동기화한다.
필드 설명은 `Assets/Script/GameData/Model/ItemSyncModel.cs` 참고.

## 주요 설계

- Partial Class: 초기화 로직(`GameClient.cs`)과 통신 로직(`GameClient.Client.cs`)을 파일로 분리
- `_groupModel` 캐싱으로 중복 로드 방지
- `Expression.ValueContext`로 스탯 수식 계산 (아이템 레벨/등급/티어 기반)
- `ListPool`으로 임시 리스트 GC 최소화

## 등록 위치

`Assets/Script/LifetimeScope/ClientLifetimeScope.cs` — Singleton EntryPoint

## 연관 경로

- 데이터 모델: `Assets/Script/GameData/Model/`
- 저장소: `Assets/Script/DataBase/`
- 기획 데이터: `Assets/Script/GameInfo/`
- 사용처: `Assets/Script/GamePlay/Service/GroupService.cs`
