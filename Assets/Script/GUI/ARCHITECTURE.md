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
| `Tutorial` | 4 | 튜토리얼 Focus 스포트라이트/가이드 오버레이 (`GamePlay/Tutorial/ARCHITECTURE.md` 참고) |
| `StageTransition` | 5 | 스테이지 시작/재시작 시 화면을 얼려 덮는 전환 오버레이 |
| `Loading` | 6 | 로딩 화면 |
| `SafeArea` | 7 | 입력 차단 레이어 (최상위) |

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
`SafeArea` Layer(가장 높은 레이어)에 전용 `SafeArea` Screen(`Screen/SafeArea/SafeArea.cs`)을 열어 하위 레이어의 GraphicRaycaster를 막는다.  
`ScreenManager.ShowSafeAreaAsync()`/`HideSafeAreaAsync()`가 `IScreenManager` 공개 API로 노출되어 있어(`ScreenManager.SafeArea.cs`), 필요한 곳(예: 튜토리얼 Focus 시스템)에서 직접 열고 닫을 수 있다.

**자동 복구(auto-back) 워치독:** `SafeArea.OpenInternal()`이 열릴 때마다 `autoBackTimer`(기본 5초) 카운트다운을 시작하고, 그 안에 `HideSafeAreaAsync()`로 닫히지 않으면 자동으로 `BackAsync()`를 호출해 입력 차단이 영구히 걸리는 사고를 막는다 — `HideSafeAreaAsync()` 호출을 누락하는 버그가 있어도 최악의 경우 5초 후 자동 복구된다. 타이머는 `IGameTimer`(전역 타이머, Pause 상태 반영) 기준으로 흐른다.

---

## StageTransition (스테이지 전환 오버레이)

스테이지 시작/재시작 시 맵/구조물 스폰, 캐릭터 T포즈 등 준비 과정이 화면에 노출되지 않도록  
현재 화면을 캡처해 덮어씌우는 전용 Layer. Screen 프리팹이 아니라 `StageTransition` Layer 오브젝트에  
`RawImage` + `CanvasGroup`을 직접 `AddComponent`해서 구성한다 (`ScreenManager.StageTransition.cs`).

**흐름:**
```
ShowStageTransitionAsync()
  → WaitForEndOfFrame (렌더링 완료 대기)
  → ScreenCapture.CaptureScreenshotAsTexture() → RawImage.texture
  → CanvasGroup.alpha = 1, blocksRaycasts = true

... 스폰/초기화 진행 (화면은 캡처된 스냅샷으로 가려짐) ...

HideStageTransitionAsync()
  → CanvasGroup.alpha를 fadeDuration 동안 1 → 0 (Fade Out)
  → 캡처해둔 Texture2D Destroy
```

**호출 지점:** `StageManager.InitializeAsync()` (시작), `StageManager.ReStart()` (재시작 전) — `Assets/Script/GamePlay/Stage/StageManager.cs`

**주의 — `ScreenCapture.CaptureScreenshotAsTexture()` 호출 타이밍:**  
이 API는 해당 프레임의 GPU 렌더링이 실제로 끝난 뒤(Unity 공식 예제 기준 `WaitForEndOfFrame` 이후)에 호출해야  
유효한 픽셀을 반환한다. 렌더링 완료 전에 호출하면 빈/무효 텍스처가 캡처되어 RawImage에 아무것도 안 보인다.  
`ShowStageTransitionAsync()`는 캡처 직전 `await UniTask.WaitForEndOfFrame(this)`로 이를 보장한다.

**재캡처 방지:** `_stageTransitionSnapshot`이 이미 있으면(직전 Fade Out 도중 다시 Show가 호출된 경우) 재캡처하지 않고 그대로 재사용한다.

---

## Loading (StageTransition 위 로딩 화면)

`ScreenData` id `"Loading"`으로 등록된 일반 Screen(`Screen/Loading/Loading.cs`)이지만,  
`OpenAsync(key)`/`CloseAsync` 같은 공개 API를 타지 않고 `ScreenManager.Loading.cs`가 직접 관리한다.

**핵심 포인트 — `_loadedScreens`(ResourceClear 대상)에 넣지 않음:**  
자주 열고 닫히는 화면이라 매번 Addressable 로드/Destroy하면 낭비이므로,  
`ShowLoadingAsync()`에서 최초 1회만 `LoadScreen()`으로 로드해 `_loadingScreen` 필드에 보관하고  
이후에는 `_layers[Loading].OpenScreen()/CloseScreen()`만 호출해 SetActive만 토글한다.  
`_loadedScreens` Dictionary를 거치지 않으므로 씬 이동 시 `ResourceClear()`가 파괴하지 않는다  
(ScreenManager 자체가 `DontDestroyOnLoad`라 계층에 남아있는 한 계속 생존).

**흐름 (현재는 StageTransition과 연동):**
```
ShowStageTransitionAsync() 끝에서 → ShowLoadingAsync()  (StageTransition 위에 노출, Layer 순서로 자동 보장)
HideStageTransitionAsync() 시작에서 → HideLoadingAsync() (Fade 시작 전 즉시 감춤)
```
`_loadingScreenShown` 플래그로 중복 Open/Close 방지 (StageTransition이 Fade 도중 다시 Show될 때 대비).

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
// Open — 연 Screen 인스턴스를 그대로 반환한다 (Focus 시스템처럼 연 직후 바로 참조가 필요한 경우 대비)
UniTask<IScreen> OpenAsync(string key, CancellationToken ct = default);
UniTask<IScreen> OpenAsync(IScreenOption screenOption, string key, CancellationToken ct = default);

// Close
UniTask CloseAllAsync(bool force = false);               // 전체 닫기
UniTask BackAsync(bool force = false, CancellationToken ct = default);  // tail(최상위) 닫기
UniTask CloseAsync(ReadOnlyMemory<char> key, bool force = false, CancellationToken ct = default);
UniTask CloseAsync(IScreen screen, bool force = false, CancellationToken ct = default);

// 리소스
UniTask ResourceClear();                                 // 씬 이동 시 호출

// 스테이지 전환 오버레이
UniTask ShowStageTransitionAsync();                      // 현재 화면 캡처 후 덮기
UniTask HideStageTransitionAsync();                       // Fade Out 후 걷어내기

// SafeArea (입력 차단) — 아래 SafeArea 섹션 참고
UniTask ShowSafeAreaAsync();
UniTask HideSafeAreaAsync();

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
| `ScreenManager.StageTransition.cs` | StageTransition Layer 캡처/Fade 제어 (partial class) |
| `ScreenManager.Loading.cs` | Loading Screen 상시 보관/토글 (partial class, ResourceClear 미대상) |
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
