# DataBase — Client 측 저장소

현재 Server가 없어 Client에서 직접 저장/로드를 구현한다.  
Server가 생기면 이 계층을 Server 통신으로 교체하는 것을 전제로 설계되어 있다.

## 직렬화 전략

| 용도 | 형식 | 이유 |
|---|---|---|
| 현재 파일 저장 | JSON | 디버깅이 편리 |
| 서버 패킷 전송 (예정) | MessagePack | 크기/성능 |

`DataType` enum으로 형식을 선택하며, `IDataBase.LoadAsync<T>()` / `SaveAsync<T>()` 호출 시 지정한다.  
MessagePack은 LZ4 압축 옵션 적용.

## 폴더 구조 / 주요 파일

| 파일 | 역할 |
|---|---|
| `IDataBase.cs` | 저장소 추상 인터페이스 — Load/Save + 아이템 CRUD |
| `IFileStorage.cs` | 파일 I/O 추상화 인터페이스 |
| `GameDataBase.cs` | IDataBase 구현 (partial — 공통 Load/Save) |
| `GameDataBase.Item.cs` | 아이템 테이블 CRUD (partial) |
| `FileStorage.cs` | IFileStorage 구현 — `Application.persistentDataPath` 기준 비동기 I/O |
| `DataType.cs` | `Json` / `MessagePack` enum |

## 저장 위치

`FileStorage`는 `Application.persistentDataPath` 하위에 저장 → 모바일/에디터 모두 호환

## IDataBase 주요 메서드

```
Load/Save
  LoadAsync<T>(path, DataType)   — 파일 → 역직렬화
  SaveAsync<T>(path, obj, DataType) — 직렬화 → 파일
  Exists(path), DeleteAsync(path)

아이템 CRUD
  AddItem(groupUid, itemInfoUid, ...)      — 스택 가능 아이템이면 개수 합산, 아니면 신규 생성
  AddRewards(groupUid, int[] rewardInfoUids) — RewardInfo uid 배열 → 내부 itemRewards 전개해 일괄 지급
  StageRewards(groupUid, dungeonUid, stageGuid, characterUid) — 스테이지 보상 + 캐릭터 경험치(자동 레벨업)
                                            를 한 번에 처리하고 ItemSyncModel로 반환 (IClient.Req_ClearStage가 호출)
  GetInventory(groupUid)               — 그룹의 전체 인벤토리 반환
  GetItem(groupUid, itemInfoUid)       — 특정 아이템 조회
  CharacterExpUp(uid, exp[], LevelType) — 경험치 누적 + 자동 레벨업 반복
  LevelUpItem(uid)                     — grade/level/tier 증가 + exp 초기화
  RemoveGroupItems(groupUid)           — 그룹 아이템 전체 삭제
  SaveItemTable()                      — 변경사항 파일에 영구 저장
```

## 아이템 테이블 인덱스 구조 (GameDataBase.Item.cs)

빠른 조회를 위해 3개의 인덱스를 동시에 유지한다.

```
_itemModelTable          — 전체 테이블 (파일 직렬화 단위, lastUid 포함)
_itemModelByUid          — uid → ItemModel       (단건 조회)
_itemModelByGroupUid     — groupUid → infoUid → ItemModel[]  (인벤토리 조회)
```

새 아이템 생성 시 `lastUid`를 증가시켜 uid를 자동 발급한다.

## 계층 관계

```
GamePlay/Service
    ↓ 주입 (DI)
IDataBase (인터페이스)
    ↓ 구현
GameDataBase  →  IFileStorage
                    ↓ 구현
                FileStorage (Application.persistentDataPath)
```

## 연관 경로

- 저장 대상 모델: `Assets/Script/GameData/Model/`
- 반응형 래퍼: `Assets/Script/GameData/Data/`
- 사용 계층: `Assets/Script/GamePlay/Service/`
