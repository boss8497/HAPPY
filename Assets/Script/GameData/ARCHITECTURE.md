# GameData — 런타임 데이터 레이어

Server ↔ Client 간 데이터 전달 모델과, 그것을 Client에서 사용하기 쉽게 감싼 반응형 래퍼로 구성된다.

## 폴더 구조

| 폴더 | 역할 |
|---|---|
| `Model/` | DB에 저장하거나 서버에 전송할 불변 데이터 모델 (MessagePack 직렬화) |
| `Data/` | Model을 R3 ReactiveProperty로 감싸 Client에서 반응형으로 사용하는 클래스 |
| `Diff/` | 서버 요청 전/후 값을 비교(diff)하기 위한 값 타입 스냅샷 |

---

## Model — 저장/전송용 불변 데이터

- MessagePack `[Key(n)]` 어트리뷰트로 직렬화 키를 명시한다
- 순수 데이터 구조체 / 클래스 — 비즈니스 로직을 포함하지 않는다
- JSON / MessagePack 양쪽 직렬화 모두 지원 (DataBase 폴더 참고)

```csharp
[MessagePackObject]
public class ItemModel {
    [Key(0)] public long uid;        // DB 개별 아이템 고유 ID
    [Key(1)] public int  infoUid;    // GameInfo ItemInfo 참조 ID
    [Key(2)] public int  groupUid;   // 소유 그룹 ID
    [Key(3)] public int  level;
    ...
}
```

**주요 모델**
| 클래스 | 설명 |
|---|---|
| `GroupModel` | 유저(그룹) 상태 — uid, 던전 진행도(dungeonProgresses) |
| `ItemModel` | 아이템 인스턴스 — uid, infoUid, groupUid, level, grade, tier, exp[] |
| `ItemSyncModel` | 서버 → 클라이언트 보상/아이템 변경 응답 묶음 — `rewardInfoUids`(지급 사유, 연출/로그용), `updateItems`(최종 상태 스냅샷, 이걸로 로컬 상태 갱신) |

**ItemSyncModel 사용처**: `GameDataBase.StageRewards`가 생성 → `IClient.Req_ClearStage` 반환 →
`GroupService.ClearedDungeon`에서 `updateItems`를 `IItemService.UpdateItems(...)`로 즉시 반영.
서버가 보상/경험치를 한 번에 계산해 내려주는 방식이므로, Client는 `rewardInfoUids`만 보고
수량을 직접 재계산하면 안 되고 `updateItems`의 최종 값을 그대로 신뢰해야 한다.

---

## Data — R3 반응형 래퍼

- `IData<T>` 인터페이스를 구현한다
  - `ReactiveProperty<T> Model` — 모델 변경 알림
  - `void Update(T value)` — 외부에서 모델 갱신
- Model의 변경을 구독해 파생 속성(ReadOnlyReactiveProperty)을 자동 동기화한다
- `GamePlay/Service` 계층에서 이 클래스를 통해 데이터에 접근한다 (직접 Model에 접근하지 않음)

```csharp
// 속성 구독 예시 (UI 등에서)
itemData.ItemInfo.Subscribe(info => { /* UI 갱신 */ });

// 모델 갱신 (서버 응답 수신 시)
itemData.Update(newItemModel);
```

**주요 클래스**
| 클래스 | 설명 |
|---|---|
| `GroupData` | GroupModel을 ReactiveProperty로 래핑 |
| `ItemData` | ItemModel 래핑 + ItemInfo/CharacterInfo/Status 자동 동기화, 경험치/레벨 계산 |

**ItemData 자동 동기화 흐름**
```
ItemModel 갱신
    → infoUid 변경 → ItemInfo 갱신 → CharacterInfo 갱신
                                    → Status 자동 계산 (level/grade/tier 기반)
    → exp[] 변경   → LevelExpMax 갱신
```

---

## Diff — 요청 전/후 값 비교

- `ItemModel` / `IItemData`의 값 타입 필드(uid, level, grade, tier, count, exp[], expMax[])만 복사해두는 `struct ItemDiff`
- 서버 요청 직전/직후 각각 `new ItemDiff(...)`로 스냅샷을 뜬 뒤, `operator -` (`after - before`)로 `ItemDiffResult`(변경량)를 계산한다

```csharp
var before = new ItemDiff(item);                 // 요청 전 스냅샷
var model  = await _client.Req_ItemLevelUp(item.Model.CurrentValue, type);
item.Update(model);
var after  = new ItemDiff(model);                 // 요청 후 스냅샷
var result = after - before;                      // LevelChanged, GradeChanged, ExpChanged 등
```

**주의**: `exp` / `expMax`는 `double[]` 참조를 그대로 복사한다(`exp = model.exp;`). `ItemModel.exp`가 이후 같은 배열 참조를 in-place로 mutate하는 경로(`GameDataBase.LevelUpItem` 등)를 타면 이미 만들어둔 "before" 스냅샷의 배열 내용도 함께 바뀌어버려 diff가 깨질 수 있다. `uid/level/grade/tier/count`는 값 타입이라 안전하지만, 배열 필드는 스냅샷 시점에 `(double[])model.exp.Clone()`처럼 별도 복제가 필요하다.

---

## 계층 관계

```
GamePlay/Service (비즈니스 로직)
    ↓ 사용
GameData/Data   (반응형 래퍼 — R3)
    ↓ 감싸는 대상
GameData/Model  (불변 구조체 — MessagePack)
    ↓ 저장/로드
Assets/Script/DataBase
```

## 연관 경로

- 저장소 구현: `Assets/Script/DataBase/`
- 사용 예시: `Assets/Script/GamePlay/Service/`
- 아이템 기획 데이터: `Assets/Script/GameInfo/Item/ItemInfo.cs`
