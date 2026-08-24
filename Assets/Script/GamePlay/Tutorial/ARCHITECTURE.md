# Tutorial — 튜토리얼 오케스트레이션 + Focus/Narration

`TutorialInfo.sets`(`GuideBase[]`)에 나열된 가이드를 순서대로 하나씩 진행시키는 시스템. 가이드는 현재 두 종류 — 특정 UI 요소에 스포트라이트를 비추고 그 요소를 실제로 클릭해야 넘어가는 **Focus**, 캐릭터 대사창처럼 아이콘+텍스트를 보여주고 다음 버튼을 눌러야 넘어가는 **Narration**. `TutorialService`가 이 둘을 가이드 타입별로 분기해 순차 진행시키고, `FocusService`/`NarrationService`는 각자 자신의 화면(Screen) 하나만 책임진다.

기존 `TutorialFocus.cs`(MonoBehaviour, 346줄) 단일 클래스를 걷어내고 `FocusService` + `TutorialFocusScreen`(ScreenManager 기반) + `FocusComponent` 조합으로 재작성한 뒤, 이후 `TutorialService`(오케스트레이션 재도입)와 `NarrationService`/`TutorialNarrationScreen`(Narration 가이드 지원)이 차례로 추가됨.

---

## 오케스트레이션 — `TutorialService`

`ITutorialService.StartTutorial(uid)`(또는 `TutorialInfo` 직접 전달)로 트리거하면, 내부 `_waitQueue`에 uid를 쌓아두고 `UpdateLoop()` 하나가 순차로 소비한다(동시에 여러 튜토리얼이 요청돼도 겹치지 않게 큐잉).

```
StartTutorial(uid) → _waitQueue.Enqueue(uid)
  UpdateLoop (최초 1개만 실행, 큐가 빌 때까지 반복):
    tutorialInfo.allCloseScreen 이면 CloseAllAsync() 먼저 실행
    sets[] 순회:
      guide is NarrationGuide  → ShowSafeArea → 이전 서비스 정리(타입 다르면 StopAsync) →
                                  _narrationService.StartAsync(guide, onComplete) → HideSafeArea →
                                  onComplete(WaitUntil)까지 대기 → ShowSafeArea → _narrationService.StopAsync(...)
      guide is FocusGuide      → _focusService.GetRetryFocusData()로 대상 확인 →
                                  대상 GameObject가 activeInHierarchy 될 때까지 재시도(최대 100회) →
                                  ShowSafeArea → 이전 서비스 정리 → _focusService.StartAsync(guide, onComplete) → HideSafeArea →
                                  onComplete(WaitUntil)까지 대기 → ShowSafeArea → _focusService.StopAsync(...)
```

- **가이드 전환 시 이전 화면 정리:** `previousService`에 직전에 쓴 서비스(`_focusService`/`_narrationService`)를 기억해두고, 다음 가이드가 다른 타입이면 시작 전에 `previousService.StopAsync(true, ct)`로 이전 화면을 강제로 닫는다 — Focus 화면 위에 Narration 화면이 겹쳐 뜨는 것을 방지.
- **완료 대기:** `_focusService`/`_narrationService`의 `StartAsync(guide, onComplete: () => focusComplete = true)` 콜백이 호출될 때까지 `UniTask.WaitUntil`로 실제로 기다린다 — 즉 한 스텝이 끝나야 다음 스텝으로 넘어간다(과거 `FocusTest.cs`에 있던 "완료를 기다리지 않고 즉시 반환" 문제는 이 오케스트레이션 계층에서 해결됨, 아래 "FocusTest.cs" 절 참고).
- **`FocusGuide` 처리에만 있는 추가 대기:** `_focusService.GetRetryFocusData()`로 대상을 먼저 찾은 뒤, 그 대상의 `RectTransform.gameObject.activeInHierarchy`가 true가 될 때까지 또 한 번 최대 100회 재시도한다. 이후 `_focusService.StartAsync()` 내부에서도 동일한 `GetRetryFocusData()`를 한 번 더 호출하므로 조회가 두 번 일어난다 — 중복이지만 현재는 그대로 둔 상태(대상 등록 시점 타이밍 이슈를 이중으로 흡수하기 위함으로 추정, 정리 여지 있음).
- **`systemControl`:** `TutorialInfo.systemControl`이 켜져 있으면 튜토리얼 진행 중 `StageManager`에 `StageState.SystemControl`을 걸어 캐릭터 조작을 막는다.

---

## 서비스 인터페이스 구조 — 이름이 겹치는 `ITutorial`류 주의

같은 프로젝트 안에 **이름이 같거나 비슷한 인터페이스가 서로 다른 두 계층에 존재**한다. 헷갈리기 쉬우므로 명확히 구분해서 참고할 것:

| 인터페이스 | 네임스페이스 / 파일 | 역할 |
|---|---|---|
| `Script.GamePlay.Service.Interface.ITutorial` | `GamePlay/Service/Interface/ITutorial.cs` | **Service 계층 공통 계약.** `StartAsync(GuideBase, onComplete, onSkip, ct)` / `StopAsync(hide, ct)` — `IFocusService`, `INarrationService`가 이걸 상속해서 `TutorialService`가 둘을 같은 방식으로 다룰 수 있게 함 |
| `Script.GamePlay.Service.Interface.ITutorialService` | `GamePlay/Tutorial/Service/Interface/ITutorialService.cs` | **오케스트레이터 계약.** `StartTutorial(TutorialInfo)`/`StartTutorial(uid)`/`StopTutorial()` — `TutorialService`가 구현, `FocusTest.cs` 등 트리거 쪽에서 참조 |
| `Script.Tutorial.Interface.ITutorialScreen` | `GamePlay/Tutorial/Interface/ITutorial.cs` (⚠️ **파일명은 `ITutorial.cs`인데 실제 인터페이스명은 `ITutorialScreen`** — 리네임 잔재로 보임, 검색 시 주의) | **Screen 계층 공통 계약.** `StopAsync(hide, ct)`만 가짐 — `ITutorialFocus`(Focus 전용, `FocusButton`/`SetFocusAsync` 추가)와 `TutorialNarrationScreen`이 직접 구현 |

즉 "Service가 Service를 다루는 추상화"(`ITutorial`)와 "Service가 Screen을 다루는 추상화"(`ITutorialScreen`)가 이름만 비슷하고 계층이 다르다. `IFocusService : ITutorial`, `INarrationService : ITutorial`이고, `ITutorialFocus : ITutorialScreen`이다.

---

## Focus 흐름

```
[씬에 배치된 UI]                [기획 데이터]                [Service]                    [Screen]
FocusComponent                  TutorialInfo.sets[]          FocusService                 TutorialFocusScreen
  (id/type 보유)                  → GuideBase[]                                             (Tutorial Layer)
      │                            (FocusGuide)                    │                            │
      │ Start() 시점 자동 등록                                       │                            │
      ├─ RegisterFocusData(data) ─────────────────────────────────▶│                            │
      │                                                            │                            │
      │                          StartAsync(guide) ─────────────▶│                            │
      │                                                            ├─ id/focusGuid로 data 조회    │
      │                                                            ├─ ShowSafeAreaAsync()         │
      │                                                            ├─ OpenAsync(FocusOption) ────▶│ 스포트라이트 표시
      │                                                            ├─ HideSafeAreaAsync()         │ + 가이드 텍스트
      │                                                            ├─ FocusButton에 AsyncClick    │
      │                                                            │   리스너 등록                 │
      │                                                            │                            │
      │◀─────────────────── (오버레이의 FocusButton 클릭) ───────────────────────────────────────┤
      │  실제 target.Click() 전달                                    │                            │
      │                                                            ├─ onComplete 콜백 호출         │
```

---

## 등록 — `FocusComponent`

씬에 배치된 버튼/이미지/토글에 붙이는 MonoBehaviour. `TutorialFocusData`(id, type, RectTransform, target 등)를 들고 있다가 `Start()` 시점에 `IFocusService.RegisterFocusData()`로 자기 자신을 등록하고, `OnDestroy()`에서 `UnRegisterFocusData()`로 해제한다.

`type`(`FocusType.Button`/`Image`/`Toggle`)에 따라 `target`이 비어있으면 같은 GameObject에서 `SW_GUI_BUTTON_BASE`/`Image`/`SW_GUI_TOGGLE_BASE`를 자동으로 찾아 채운다 — 인스펙터에서 수동으로 연결하지 않아도 되게 하기 위함.

`sizeOffset`/`positionOffset`으로 스포트라이트 크기·위치를 실제 RectTransform보다 크거나 작게 미세 조정할 수 있고, `OnDrawGizmos()`(showGizmos 켰을 때)로 씬 뷰에서 실제 스포트라이트 영역을 노란 사각형으로 미리 확인할 수 있다.

---

## 기획 데이터 — `TutorialInfo` / `GuideBase` / `FocusGuide` / `NarrationGuide`

- `TutorialInfo`(GameInfo, 테이블 자동 생성 대상): `GuideBase[] sets` — 튜토리얼 한 세트를 순서대로 나열, `systemControl`/`allCloseScreen` 등 진행 옵션 보유
- `GuideBase`(abstract): `id`, `delayTime`, `fadeInTime`, `fadeOutTime` — 공통 필드. 코드 주석엔 아직 "현재는 Focus만 존재"라고 남아있지만 실제로는 `FocusGuide`/`NarrationGuide` 2종이 이 클래스를 상속한다(주석이 최신화되지 않은 상태)
- `FocusGuide : GuideBase`: `focusGuid`(`[Focus]` 어트리뷰트로 인스펙터에서 등록된 Focus 대상을 드롭다운으로 선택), `name`, `guideText`, `iconPath`, `flip`
- `NarrationGuide : GuideBase`: `name`, `guideText`, `iconPath`, `flip` — 필드 구성은 `FocusGuide`와 거의 동일하지만 `focusGuid`(클릭 대상 지정)가 없다. 클릭할 실제 UI 대상이 없는, 순수 "말풍선 + 다음 버튼" 가이드이기 때문

**대상 매칭 우선순위** (`FocusService.GetFocusData()`, Focus 전용 — Narration은 클릭 대상이 없어 매칭 자체가 필요 없음):
1. `focusGuide.id`가 있으면 `TutorialFocusData.id`로 직접 조회
2. 없으면 `focusGuide.focusGuid` ↔ `TutorialFocusData.Guid`(`SerializeGuid`)로 조회

---

## 진행 — `FocusService.StartAsync()`

`IFocusService`/`INarrationService`가 공통 `ITutorial`(`Script.GamePlay.Service.Interface.ITutorial`)을 상속하면서 메서드명이 `StartFocusAsync`/`StopFocusAsync`에서 **`StartAsync`/`StopAsync`로 통일**됐다(위 "이름이 겹치는 `ITutorial`류" 참고). `TutorialService`가 주 호출자지만 직접 호출도 가능하다.

1. `SafeArea(true)` — 전환 중 다른 곳 클릭 방지
2. `StopAsync(false, ct)` — 이전 단계가 열려 있었다면 정리(화면은 유지, `hide=false`)
3. `GetRetryFocusData()` — 대상 `TutorialFocusData`를 최대 100회, 2프레임 간격으로 재시도 조회
   - **왜 재시도가 필요한가:** `StartAsync`가 호출되는 시점에 대상 UI(Screen)가 아직 로딩/오픈 중이라 `FocusComponent.Start()`(등록)가 안 끝났을 수 있다. 못 찾으면 그냥 실패 처리하는 대신 짧게 재시도해 타이밍 문제를 흡수한다.
   - 100회 재시도 후에도 못 찾으면 `onComplete`만 호출하고 스텝을 건너뛴다(스포트라이트 없이 통과) — 씬에 대상이 아예 없는 경우 튜토리얼 전체가 멈추지 않도록 하는 안전장치.
   - `TutorialService.UpdateLoop()`가 `FocusGuide`를 처리할 때 이 조회를 먼저 한 번 호출하고, 대상의 `activeInHierarchy`까지 확인한 뒤 `StartAsync()`를 호출하면 여기서 또 한 번 호출된다(중복, 위 오케스트레이션 절 참고)
4. `IScreenManager.OpenAsync(FocusOption, "TutorialFocus", ct)` — `TutorialFocusScreen`을 열고 결과를 `ITutorialFocus`로 캐스팅해 보관 (`OpenAsync`가 `UniTask<IScreen>`을 반환하도록 바뀐 이유가 여기서 쓰기 위함)
5. `SafeArea(false)` — 스포트라이트가 뜬 뒤 다시 입력 허용
6. `SetFocusCompleteCallBack()` — `FocusType`별로 완료 조건을 건다

---

## 완료 조건 — `FocusType`별 분기 (`SetFocusCompleteCallBack`)

| FocusType | 완료 조건 |
|---|---|
| `Button` | `TutorialFocusScreen.FocusButton`(오버레이의 대리 버튼)에 비동기 클릭 리스너를 걸고, 클릭되면 실제 `target.Click()`을 대신 호출한 뒤 완료 처리 |
| `Toggle` | 위와 동일하되 `target.OnClick()`(토글 반전)을 호출 |
| `Image` / `None` | 클릭 자체가 완료 조건(스포트라이트 영역을 탭하면 바로 다음 단계로) |

**왜 실제 target을 직접 클릭 가능하게 두지 않고 대리 버튼(FocusButton)을 쓰는가:** `TutorialFocusScreen`은 `Tutorial` Layer(다른 화면들보다 위)에 4방향 마스크(`top`/`bottom`/`left`/`right`)로 스포트라이트를 그리는데, 대상 버튼은 그 아래 레이어에 그대로 남아있다. 마스크 위에 정확히 같은 크기/위치로 겹쳐지는 투명 `focusButton`을 두고 그걸 클릭점으로 삼아야 레이어 순서와 무관하게 안정적으로 입력을 받을 수 있다. 클릭되면 `AddClickAsyncListener`로 등록해둔 비동기 콜백이 `SafeArea(true)` → 실제 `target.Click()` 전달 → 완료 콜백 순으로 실행된다 (`SW_GUI/README.md`의 "Click의 비동기 버전" 참고).

`useGard`(TutorialFocusData)가 꺼져 있으면 `SetGardAlpha(false)`로 마스크를 투명하게 만들어 스포트라이트 연출 없이 클릭만 가로채는 것도 가능하다.

---

## 렌더링 — `TutorialFocusScreen`

- `ReSizeFocus()`: 대상 `RectTransform`의 world position/size를 `focus` RectTransform에 그대로 반영 (pivot 차이 보정 포함)
- `ResizeGard()`: `focus` 사각형을 기준으로 `top`/`bottom`/`left`/`right` 4개 RectTransform의 `sizeDelta`/`offsetMin`/`offsetMax`를 계산해 "가운데만 뚫린 어두운 마스크" 형태를 만든다 (4개의 사각형 조각으로 감싸는 방식 — 별도 stencil/shader 불필요)
- `SetSpeechPosition()`: 가이드 텍스트 말풍선을 스포트라이트 위/아래, 화면 밖으로 넘치지 않게 좌우 클램프해서 배치
- `Update()`에서 매 프레임 재계산(`_updateFocus`가 true인 동안) — 대상이 애니메이션/리스트 스크롤 등으로 움직여도 스포트라이트가 따라간다

---

## Narration 흐름

Focus와 달리 클릭할 실제 UI 대상이 없는, "아이콘 + 이름 + 대사 텍스트를 보여주고 다음 버튼을 누르면 넘어가는" 단순한 가이드. 구조는 Focus보다 훨씬 얇다.

- `NarrationService : INarrationService`(`ITutorial` 상속) — `StartAsync()`에서 곧바로 `TutorialNarrationScreen`을 `NarrationOption`(가이드 데이터 + 완료 콜백)과 함께 `OpenAsync`. 대상 탐색/재시도 로직 자체가 없음(클릭 대상이 없으니 불필요)
- `TutorialNarrationScreen : Screen, ITutorialScreen` — `NarrationGuide.name`/`guideText`를 R3 `ReactiveProperty`로 감싸 텍스트 UI에 바인딩, `NarrationGuide.iconPath`를 Addressable로 로드해 아이콘 표시(`flip`이면 좌우 반전). `nextButton`(`SW_GUI_BUTTON_BASE`)의 비동기 클릭 리스너가 곧바로 완료 콜백(`_completeCallBack`)을 호출 — Focus의 "대리 버튼 + SafeArea 재오픈" 없이 단순 클릭 즉시 완료 처리
- 같은 가이드가 연달아 나올 때는 `OpenChangeOptionAsync()`로 이미 열려있는 화면의 텍스트/아이콘만 갱신(재오픈 없음) — `TutorialService`가 다음 가이드도 `NarrationGuide`면 이전 화면을 닫지 않고 이 경로를 타도록 설계된 것으로 보임

Focus/Narration 화면 전환 시 어느 쪽이 열려있었든 `TutorialService`가 타입이 바뀌는 시점에만 이전 서비스를 `StopAsync`하므로, Narration↔Narration 연속은 화면을 유지한 채 내용만 바뀌고 Focus↔Narration 전환은 화면이 완전히 교체된다.

---

## SafeArea 연동

`FocusService`/`NarrationService` 둘 다 스텝 전환마다 `ShowSafeAreaAsync()`/`HideSafeAreaAsync()`를 짝지어 호출해 "화면이 준비되는 동안", "클릭 처리 중" 구간에는 다른 곳을 못 누르게 막는다. `SafeArea` Screen 자체에 자동 복구 워치독(기본 5초, `GUI/ARCHITECTURE.md` 참고)이 있어 `HideSafeAreaAsync()` 호출을 누락하는 실수가 있어도 입력이 영구히 막히지는 않는다 — 워치독이 만료되면 자기 자신을 강제로 닫는다(2026-08-24 수정, 예전엔 스택 최상단의 엉뚱한 화면을 닫는 버그가 있었음).

---

## `FocusTest.cs` — 디버그 트리거 (해결됨)

`FocusTest.Test()`(Odin `[Button]`)는 더 이상 `FocusService`를 직접 호출하지 않고 **`ITutorialService.StartTutorial(uid)`만 호출**한다. 실제 순차 진행(한 스텝의 완료를 기다린 뒤 다음 스텝 시작)은 위 "오케스트레이션 — `TutorialService`" 절에서 설명한 `UpdateLoop()`가 전담한다. 과거 이 문서에 있던 "`StartFocusAsync`가 완료를 기다리지 않고 즉시 반환해서 여러 스텝을 실제로 순차 진행시킬 방법이 없다"는 제약은 `TutorialService` 도입으로 해결됐다.

---

## 파일 구조

| 경로 | 역할 |
|---|---|
| `GamePlay/Tutorial/Service/TutorialService.cs` / `Service/Interface/ITutorialService.cs` | 오케스트레이션 — `sets[]` 순차 진행, Focus/Narration 분기 |
| `GamePlay/Tutorial/FocusComponent.cs` | 씬 배치용 등록 컴포넌트 |
| `GamePlay/Tutorial/FocusTest.cs` | 디버그 테스트 트리거 — `ITutorialService.StartTutorial(uid)` 호출만 |
| `GamePlay/Tutorial/Data/TutorialFocusData.cs` | 등록되는 대상 정보 (`FocusType` enum 포함) |
| `GamePlay/Tutorial/Interface/ITutorial.cs` | ⚠️ 파일명과 다르게 `ITutorialScreen` 인터페이스 정의 (`StopAsync`만) |
| `GamePlay/Tutorial/Interface/ITutorialFocus.cs` | `ITutorialScreen` 확장 — Focus Screen 전용 (`FocusButton`, `SetFocusAsync`, `SetGardAlpha`, `SetButton`) |
| `GamePlay/Tutorial/Editor/TutorialFocusDataManager.cs` | 에디터 전용 관리 도구 |
| `GamePlay/Service/Interface/ITutorial.cs` | Service 계층 공통 계약 (`StartAsync`/`StopAsync`) — `IFocusService`/`INarrationService`가 상속 |
| `GamePlay/Service/FocusService.cs` / `Interface/IFocusService.cs` | Focus 진행 (대상 조회, Screen 오픈, 완료 콜백 연결) |
| `GamePlay/Service/NarrationService.cs` / `Interface/INarrationService.cs` | Narration 진행 (Screen 오픈/옵션 갱신만, 대상 조회 없음) |
| `GUI/Screen/Tutorial/TutorialFocusScreen.cs` | 스포트라이트/가이드 렌더링, `ITutorialFocus` 구현 |
| `GUI/Screen/Tutorial/TutorialNarrationScreen.cs` | 아이콘+텍스트+다음버튼 렌더링, `ITutorialScreen` 구현 |
| `GUI/ScreenOption/FocusOption.cs` | Focus `OpenAsync`에 전달하는 파라미터 (`TutorialFocusData` + `FocusGuide`) |
| `GUI/ScreenOption/NarrationOption.cs` | Narration `OpenAsync`에 전달하는 파라미터 (`NarrationGuide` + 완료 콜백) |
| `GameInfo/Tutorial/TutorialInfo.cs` / `GuideBase.cs` / `FocusGuide.cs` / `NarrationGuide.cs` | 기획 데이터 (테이블 자동 생성 대상) |

## 연관 경로

- Screen 관리 시스템 전반: `Assets/Script/GUI/ARCHITECTURE.md`, `Assets/Script/GUI/README.md`
- SafeArea: `Assets/Script/GUI/ARCHITECTURE.md`의 SafeArea 섹션
- 비동기 클릭 리스너: `Assets/Script/GUI/SW_GUI/README.md`의 "Click의 비동기 버전"
- `[Focus]` 인스펙터 어트리뷰트: `Assets/Script/GameInfo/Attribute/`, 구현: `Assets/Script/Editor/Attribute/`
- DI 등록: `Assets/Script/LifetimeScope/GroupLifetimeScope.cs` — `TutorialService`/`FocusService`/`NarrationService` 전부 Group Scope에 `RegisterEntryPoint`
