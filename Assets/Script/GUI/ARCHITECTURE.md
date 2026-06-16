# GUI — Screen 관리 시스템

게임의 모든 화면(HUD, 팝업, 로딩 등)을 관리하는 UI 프레임워크.  
`AppLifetimeScope`에서 Addressable로 프리팹을 Instantiate해 생성하며, 게임 실행 직후부터 앱 전체 생존한다.

---

## 핵심 설계 원칙

### LinkedList 기반 Stack

UI가 쌓이는 형태는 Stack이지만 **LinkedList로 구현**한 이유:  
경험상 "특정 UI 뒤에 열고 싶다", "특정 UI만 닫고 싶다" 요청이 빈번하게 발생하며,  
순수 Stack으로는 이 변경이 매우 어렵기 때문. LinkedList지만 **Stack 동작을 지향**한다.

**Node 구조** (`Screen.Node.cs`)
```
[DontClose] → [DontClose] → [Normal] → [Normal] → (tail)
     ↑ 앞쪽 고정                           ↑ 새 화면 append
```

**삽입 규칙:**
- `DontClose` 화면: 항상 리스트 앞부분에 순차 배치 (Normal 화면 앞)
- 일반 화면: 항상 tail에 append
- Close 시 일반 화면은 자신부터 tail까지 모두 닫음 (DontClose는 본인만)

---

## Screen Layer 시스템

Canvas 아래에 Layer별 RectTransform을 생성해 렌더링 순서를 제어한다.

| LayerType | 순서 | 용도 |
|---|---|---|
| `HUD` | 0 | 게임 HUD (체력바, 미니맵 등) |
| `None` | 1 | 기본 레이어 |
| `Popup` | 2 | 팝업 창 |
| `Overlay` | 3 | 오버레이 |
| `Loading` | 4 | 로딩 화면 |
| `SafeArea` | 5 | 입력 차단 레이어 (최상위) |

각 Layer는 앵커 (0,0)~(1,1), 오프셋 0으로 전체 영역을 차지한다.

---

## DontClose (닫히지 않는) Screen

HUD, Navigation 등 항상 표시해야 하는 화면에 사용.

```csharp
// Screen.Option.cs
[SerializeField] private ScreenOption option = ScreenOption.DontClose;
public bool DontClose => option.HasFlag(ScreenOption.DontClose);
```

- LinkedList 앞쪽에 배치되어 일반 Close 영향을 받지 않음
- `force: true`로만 닫을 수 있음
- `CloseAllAsync(force: true)` — 씬 이동 시 전체 강제 닫기에 사용

---

## SafeArea (입력 차단)

Screen이 열리는 동안 사용자 입력을 차단한다.  
`SafeArea` Layer(가장 높은 레이어)에 투명 Screen을 열어 하위 레이어의 GraphicRaycaster를 막는다.

---

## 다중 Open/Close 요청 — Queue 처리

동시에 여러 Open/Close 요청이 들어와도 순서를 보장한다.

```
OpenAsync("A") ─┐
OpenAsync("B") ─┤→ _openWaitQueue → A 완료 → B 완료
OpenAsync("C") ─┘
```

**Open 흐름:**
1. `_openWaitQueue.Enqueue(key)`
2. `WaitUntil(OpeningScreen == false && Peek == key)` — 자신 차례까지 대기
3. `AddState(OpeningScreen)` → Dequeue
4. Addressable 로드 or 캐시 조회 → 링크드 리스트 삽입 → Layer에 추가 → 애니메이션
5. `RemoveState(OpeningScreen)`

**UniTask 비동기 보장:**  
`OpenAsync()`를 await하면 화면이 완전히 열릴 때까지 대기한다.  
(애니메이션 포함, Queue 대기 포함)

---

## Screen 캐싱 전략

```
처음 Open → Addressable 로드 → VContainer Inject → _loadedScreens[key] 저장
재오픈    → _loadedScreens[key] 조회 → 바로 사용 (로딩 없음)
Close     → SetActive(false) — _loadedScreens에서 제거 안함 (메모리 유지)
씬 이동   → ResourceClear() → _loadedScreens 전체 Destroy + Release
```

**Preload를 별도로 만들지 않는 이유:**  
"Preload해야 될 만큼 무거운 UI는 이미 설계 문제"라는 원칙 하에,  
한 번 열린 화면을 메모리에 유지하는 방식으로 재오픈 속도를 보장한다.

**씬 이동 시 정리 순서** (`SceneLoader.cs`에서 호출):
1. `CloseAllAsync(force: true)` — 모든 Screen 닫기
2. `ResourceClear()` — `_loadedScreens` 전체 Destroy + UIPooling Clear

---

## IScreenManager 인터페이스

```csharp
// Open
UniTask OpenAsync(string key, CancellationToken ct = default);
UniTask OpenAsync(IScreenOption screenOption, string key, CancellationToken ct = default);

// Close
UniTask CloseAllAsync(bool force = false);               // 전체 닫기
UniTask Back();                                          // tail(최상위) 닫기
UniTask CloseAsync(ReadOnlyMemory<char> key, bool force = false);
UniTask CloseAsync(IScreen screen, bool force = false);

// 리소스
UniTask ResourceClear();                                 // 씬 이동 시 호출

// UI 풀링 (Pool 기반 동적 UI)
GameObject PoolPop(string key, Transform parent = null, bool active = true, bool worldPositionStays = true);
bool PoolPush(GameObject obj);

// 에러 메시지
UniTask OpenErrorMessage(ErrorMessage errorMessage, CancellationToken ct = default, object[] arguments = null);
```

**ScreenManagerState (Flags enum)**

| State | 설명 |
|---|---|
| `Initialized` | 초기화 완료 |
| `OpeningScreen` | Screen Open 진행 중 (Queue 대기 조건) |
| `ClosingScreen` | Screen Close 진행 중 |

---

## ScreenKey 어트리뷰트

Screen을 문자열 Key로 참조할 때 오타를 방지하기 위한 Inspector 도구.

```csharp
[SerializeField, ScreenKey] private string hudScreenKey;
```

Inspector에서 `ScreenData`에 등록된 Screen 목록을 드롭다운으로 선택할 수 있다.  
`ScreenData`에는 `ScreenAsset[]` (id + AssetReference) 배열이 등록되어 있다.

---

## ViewModel — ListView Element 패턴

현재는 ListView의 각 Element에 데이터를 바인딩하기 위해 사용.

### SelectElement / Selector 패턴

리스트 아이템의 선택 상태를 중앙 `Selector`에서 관리한다.

```
Selector (Screen)
  ├─ Register(element)    → 아이템 등록
  ├─ Select(element)      → 선택 처리 (이전 선택 해제 포함)
  ├─ AllDeselect()        → 전체 선택 해제
  └─ ReleaseSelector()    → 정리
       ↑
SelectElement (MonoBehaviour)
  ├─ _key: int            → 식별 키
  ├─ Selected: bool       → 선택 상태
  └─ Selector 참조
```

### Element 구현 (R3 바인딩)

| 클래스 | 용도 | 주요 ReactiveProperty |
|---|---|---|
| `CharacterElement` | 캐릭터 선택 리스트 아이템 | `CharacterInfo`, `ItemInfo`, `HasItem` |
| `StageElement` | 스테이지 선택 리스트 아이템 | `Stage`, `DungeonInfo`, `CanEnterStage` |

**R3 바인딩 패턴:**
```csharp
// 값 변경 → UI 자동 갱신
CharacterInfo
    .CombineLatest(ItemData, ...)
    .Subscribe(values => { /* UI 갱신 */ })
    .AddTo(ref _disposableBag);   // 소멸 시 자동 해제
```

---

## 파일 구조

| 경로 | 역할 |
|---|---|
| `ScreenManager.cs` | 핵심 관리자 (LinkedList, Queue, 캐싱) |
| `Screen/Screen.cs` | Screen 베이스 클래스 |
| `Screen/Screen.Node.cs` | LinkedList Node (Previous/Next) |
| `Screen/Screen.Option.cs` | DontClose 등 옵션 플래그 |
| `Interface/IScreenManager.cs` | 관리자 인터페이스 |
| `Interface/IScreen.cs` | Screen 인터페이스 |
| `Layer/ScreenLayer.cs` | Layer별 Open/Close 처리 |
| `Layer/ScreenLayerType.cs` | Layer enum (HUD~SafeArea) |
| `ViewModel/Base/SelectElement.cs` | 리스트 아이템 베이스 |
| `ViewModel/Base/Selector.cs` | 선택 상태 중앙 관리 |
| `ViewModel/CharacterElement.cs` | 캐릭터 리스트 아이템 |
| `ViewModel/StageElement.cs` | 스테이지 리스트 아이템 |
| `ScreenData.cs` | ScreenAsset 등록 목록 (Inspector에서 관리) |

## 연관 경로

- 생성 위치: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
- ScreenKey Attribute: `Assets/Script/GameInfo/Attribute/`
- ScreenKey Drawer: `Assets/Script/Editor/Attribute/ScreenKeyDrawer.cs`
- UI 풀링: `Assets/Script/GamePlay/Pool/UIPooling.cs`
