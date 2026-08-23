# LifetimeScope — VContainer DI 계층 구조

VContainer를 사용한 의존성 주입 아키텍처.  
Scope를 계층으로 나눠 각 단계에서 필요한 서비스만 생성하고 파괴한다.

---

## Scope 계층 구조

```
AppLifetimeScope      ← StartUp 씬에 GameObject로 배치 (앱 전체 생존)
└── ClientLifetimeScope   ← ScopeFactory로 동적 생성 (로그인 후 생존)
    └── GroupLifetimeScope    ← ScopeFactory로 동적 생성 (그룹 진입 후 생존)
        └── StageLifetimeScope    ← GameScene 씬 안에 GameObject로 배치 (스테이지 생존)
```

**핵심 규칙**: `ScopeType` enum의 숫자 순서 = 계층 순서  
`ScopeLocator.GetParentScope(type)` 는 `(int)type - 1` 인덱스로 부모를 찾는다.

```csharp
public enum ScopeType {
    App    = 0,   // Root
    Client = 1,
    Group  = 2,
    Stage  = 3,
    Max,          // 루프 경계 마커 (실제 Scope 아님)
}
```

---

## Scope별 등록 서비스

### AppLifetimeScope
앱 전체 생명주기 동안 유지되는 글로벌 싱글톤.  
StartUp 씬에 GameObject 컴포넌트로 배치.

| 인터페이스 | 구현체 | 비고 |
|---|---|---|
| `IAddressableService` | `AddressableService` | Addressable 에셋 관리 + 중앙 캐시 |
| `IGameSetting` | `GameSetting` | 게임 설정 |
| `IScopeLocator` | `ScopeLocator` | Scope 중앙 관리 |
| `IGameTimer` | `GameTimer` | 전역 타이머 |
| `IScopeFactory` | `ScopeFactory` | 하위 Scope 동적 생성 |
| `IFileStorage` | `FileStorage` | 로컬 파일 I/O |
| `IDataBase` | `GameDataBase` | 게임 데이터 저장소 |
| `ISceneLoader` | `SceneLoader` | 씬 전환 |
| `IUIPooling` | `UIPooling` | UI 리소스 풀 |
| `IScreenManager` | `ScreenManager` | 화면(HUD/팝업) 관리 (Addressable로 Instantiate) |
| `ILocalize` | `Localize` | 다국어 |

### ClientLifetimeScope
Firebase, Steam 등 클라이언트 플러그인 및 서버 연결.  
ScopeFactory로 동적 생성 (StartUpLogic에서 Addressable/GameSetting 초기화 완료 후).

| 인터페이스 | 구현체 | 비고 |
|---|---|---|
| `IClient` | `GameClient` | 서버 통신 (현재는 로컬 DB 기반 가짜 구현) |

### GroupLifetimeScope
플레이어의 인벤토리, 던전 진행도 등 그룹 단위 데이터.  
ScopeFactory로 동적 생성 (Title 씬에서 "Start" 버튼 클릭 시).

| 인터페이스 | 구현체 | 비고 |
|---|---|---|
| `IGroupService` | `GroupService` | 그룹 정보, 던전 진입/클리어 |
| `IItemService` | `ItemService` | 인벤토리, 아이템 강화 |

### StageLifetimeScope
스테이지 게임플레이에 필요한 서비스.  
**GameScene 씬 안에 GameObject 컴포넌트로 직접 배치** (다른 Scope와 다름).  
→ 이유: `mainCamera`, `CinemachineTargetGroup`, `CinemachineCamera` 등  
   씬에 존재하는 GameObject를 Inspector에서 직접 참조해야 하기 때문.

| 인터페이스 | 구현체 | 비고 |
|---|---|---|
| `IStageEntityWorld` | `StageEntityWorld` | ECS 월드 |
| `IStageManager` | `StageManager` | 스테이지 게임플레이 |
| `IStagePooling` | `StagePooling` | 스테이지 리소스 풀 |
| `ICameraControls` | `CameraControls` | 카메라 제어 |
| `IPlayerControls` | `PlayerControls` | 플레이어 입력 |
| `IUnitManager` | `UnitManager` | 유닛 관리 |

`StageManager`는 `WithParameter`로 `targetGroup`, `vCamera`, 화면 키(ScreenKey)를 주입받는다.

---

## ScopeLocator — 중앙 Scope 관리

`IScopeLocator`를 주입받으면 어디서든 모든 활성 Scope에 접근 가능하다.

```csharp
// Scope 등록 (각 Scope의 Configure()에서)
locator.SetScope(ScopeType.Stage, this);

// Scope 해제 (OnDestroy()에서)
locator.ReleaseChildScope(ScopeType.Stage);  // Stage 이하 전부 Dispose

// 부모 Scope 조회
var parentScope = locator.GetParentScope(ScopeType.Stage); // → GroupLifetimeScope
```

**SetScope() 동작**: 해당 타입과 그 **하위 타입 전체를 먼저 Dispose** 후 새로 등록.  
→ Scope가 교체될 때 자식이 자동 정리됨.

---

## ScopeFactory — 동적 Scope 생성

```csharp
// 부모 Scope를 ScopeLocator에서 찾아 CreateChild<T>()로 자식 생성
factory.CreateScope(ScopeType.Client);  // AppScope.CreateChild<ClientLifetimeScope>()
factory.CreateScope(ScopeType.Group);   // ClientScope.CreateChild<GroupLifetimeScope>()
```

StageLifetimeScope는 씬에 배치된 GameObject이므로 ScopeFactory 대신  
씬 로드 시 Unity가 자동으로 `parent` 설정 후 초기화한다.

---

## 씬별 Scope 생성/파괴 시점

| 씬 | Scope 생성 | 생성 주체 | 파괴 시점 |
|---|---|---|---|
| StartUp | `AppLifetimeScope` | 씬에 GameObject 배치 (자동) | 앱 종료 |
| StartUp → Title | `ClientLifetimeScope` | `StartUpLogic` | `ReleaseChildScope(Client)` |
| Title → Lobby | `GroupLifetimeScope` | `TitleHUD` "Start" 클릭 | `ReleaseChildScope(Group)` |
| GameScene | `StageLifetimeScope` | 씬에 GameObject 배치 (자동) | 씬 언로드 → `OnDestroy()` |

---

## IClient — 서버 통신 인터페이스

현재 Server가 없으므로 `GameClient`가 로컬 DB로 동작한다.  
Server 완성 시 `GameClient`만 실제 통신 구현체로 교체하면 된다.

```
IClient.Req_Group()              → DB에서 GroupModel 로드 (없으면 신규 생성)
IClient.Req_Inventory(groupUid)  → DB에서 아이템 목록 조회
IClient.Req_EnterDungeon(...)    → 로컬: 항상 true 반환
IClient.Req_ClearStage(...)      → 던전 진행도 업데이트 + ItemSyncModel(보상/경험치 갱신 아이템) 반환
IClient.Req_ItemLevelUp(...)     → DB 아이템 강화 처리
IClient.Req_RemoveGroup()        → 그룹 + 아이템 전체 삭제
```

---

## SceneLoader — 씬 전환

Addressable 기반 비동기 씬 전환.

```
1. 현재 화면(Screen) 전부 닫기
2. UI 리소스 정리
3. 새 씬 Additive 로드 (Addressables)
4. 새 씬을 Active 씬으로 설정
5. 이전 씬 UnloadAsync
```

---

## 파일 구조

| 파일 | 역할 |
|---|---|
| `AppLifetimeScope.cs` | Root Scope |
| `ClientLifetimeScope.cs` | Client Scope |
| `GroupLifetimeScope.cs` | Group Scope |
| `StageLifetimeScope.cs` | Stage Scope (씬에 GameObject 배치) |
| `ScopeFactory.cs` | `CreateChild<T>()` 래퍼 |
| `Locator/ScopeLocator.cs` | Dictionary 기반 Scope 중앙 관리 |
| `Locator/Interface/ScopeType.cs` | Scope 계층 순서 정의 Enum |
| `Locator/Interface/IScopeLocator.cs` | Locator 인터페이스 |
| `Interface/IScopeFactory.cs` | Factory 인터페이스 |

## 연관 경로

- 씬별 로직: `Assets/Script/Scene/`
- IClient 구현: `Assets/Script/Client/`
- SceneLoader: `Assets/Script/SceneLoader/`
- 씬 파일: `Assets/Scenes/` (StartUp, Title, Lobby, MountainScene)
