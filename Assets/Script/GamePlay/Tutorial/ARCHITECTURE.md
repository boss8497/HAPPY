# Tutorial/Focus — 튜토리얼 스포트라이트 시스템

특정 UI 요소(버튼/이미지/토글)에 스포트라이트를 비추고 가이드 텍스트를 보여준 뒤, 그 요소를 실제로 클릭해야 다음 단계로 넘어가는 튜토리얼 시스템.
기존 `TutorialFocus.cs`(MonoBehaviour, 346줄) + `TutorialService`를 걷어내고 `FocusService` + `TutorialFocusScreen`(ScreenManager 기반) + `FocusComponent` 조합으로 재작성됨.

---

## 전체 흐름

```
[씬에 배치된 UI]                [기획 데이터]                [Service]                    [Screen]
FocusComponent                  TutorialInfo.sets[]          FocusService                 TutorialFocusScreen
  (id/type 보유)                  → GuideBase[]                                             (Tutorial Layer)
      │                            (FocusGuide)                    │                            │
      │ Start() 시점 자동 등록                                       │                            │
      ├─ RegisterFocusData(data) ─────────────────────────────────▶│                            │
      │                                                            │                            │
      │                          StartFocusAsync(guide) ──────────▶│                            │
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

## 기획 데이터 — `TutorialInfo` / `GuideBase` / `FocusGuide`

- `TutorialInfo`(GameInfo, 테이블 자동 생성 대상): `GuideBase[] sets` — 튜토리얼 한 세트를 순서대로 나열
- `GuideBase`(abstract): `id`, `delayTime`, `fadeInTime`, `fadeOutTime` — 공통 필드. "현재는 Focus만 존재"(주석 원문)하지만 다른 가이드 타입 확장을 염두에 둔 추상화
- `FocusGuide : GuideBase`: `focusGuid`(`[Focus]` 어트리뷰트로 인스펙터에서 등록된 Focus 대상을 드롭다운으로 선택), `name`, `guideText`, `iconPath`, `flip`

**대상 매칭 우선순위** (`FocusService.GetFocusData()`):
1. `focusGuide.id`가 있으면 `TutorialFocusData.id`로 직접 조회
2. 없으면 `focusGuide.focusGuid` ↔ `TutorialFocusData.Guid`(`SerializeGuid`)로 조회

---

## 진행 — `FocusService.StartFocusAsync()`

1. `ShowSafeAreaAsync()` — 전환 중 다른 곳 클릭 방지
2. `StopFocusAsync(false, ct)` — 이전 단계가 열려 있었다면 정리(화면은 유지, `hide=false`)
3. `GetRetryFocusData()` — 대상 `TutorialFocusData`를 최대 100회, 2프레임 간격으로 재시도 조회
   - **왜 재시도가 필요한가:** `StartFocusAsync`가 호출되는 시점에 대상 UI(Screen)가 아직 로딩/오픈 중이라 `FocusComponent.Start()`(등록)가 안 끝났을 수 있다. 못 찾으면 그냥 실패 처리하는 대신 짧게 재시도해 타이밍 문제를 흡수한다.
   - 100회 재시도 후에도 못 찾으면 `onComplete`만 호출하고 스텝을 건너뛴다(스포트라이트 없이 통과) — 씬에 대상이 아예 없는 경우 튜토리얼 전체가 멈추지 않도록 하는 안전장치.
4. `IScreenManager.OpenAsync(FocusOption, "TutorialFocus", ct)` — `TutorialFocusScreen`을 열고 결과를 `ITutorialFocus`로 캐스팅해 보관 (`OpenAsync`가 `UniTask<IScreen>`을 반환하도록 바뀐 이유가 여기서 쓰기 위함)
5. `HideSafeAreaAsync()` — 스포트라이트가 뜬 뒤 다시 입력 허용
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

## SafeArea 연동

`FocusService`는 스텝 전환마다 `ShowSafeAreaAsync()`/`HideSafeAreaAsync()`를 짝지어 호출해 "스포트라이트가 준비되는 동안", "대상 클릭 처리 중" 구간에는 다른 곳을 못 누르게 막는다. `SafeArea` Screen 자체에 자동 복구 워치독(5초, `GUI/ARCHITECTURE.md` 참고)이 있어 `HideSafeAreaAsync()` 호출을 누락하는 실수가 있어도 입력이 영구히 막히지는 않는다.

---

## 주의 — `FocusTest.cs`의 현재 동작

`FocusTest.Test()`(Odin `[Button]`, 디버그 트리거)는 `TutorialInfo.sets`를 순회하며 `await _focusService.StartFocusAsync(...)`를 호출하는데, `StartFocusAsync`는 **사용자가 실제로 클릭할 때까지 기다리지 않고** 완료 콜백을 걸어둔 뒤 즉시 반환한다. 따라서 이 루프는 각 스텝의 "셋업"만 순서대로 실행하고, 다음 반복의 `StopFocusAsync(false)`가 이전 스텝의 클릭 리스너를 곧바로 정리해버린다 — 스텝 사이에 실제 사용자 입력을 기다리는 오케스트레이션(예: `UniTaskCompletionSource`로 `onComplete`를 await 가능하게 감싸기)은 아직 없다. 여러 스텝짜리 튜토리얼을 실제로 순차 진행시키려면 이 부분을 먼저 채워야 한다.

---

## 파일 구조

| 경로 | 역할 |
|---|---|
| `GamePlay/Tutorial/FocusComponent.cs` | 씬 배치용 등록 컴포넌트 |
| `GamePlay/Tutorial/FocusTest.cs` | 디버그 테스트 트리거 (위 "주의" 참고) |
| `GamePlay/Tutorial/Data/TutorialFocusData.cs` | 등록되는 대상 정보 (`FocusType` enum 포함) |
| `GamePlay/Tutorial/Interface/ITutorialFocus.cs` | Screen이 구현하는 인터페이스 (`FocusButton`, `SetFocusAsync`, `StopAsync`, `SetGardAlpha`, `SetButton`) |
| `GamePlay/Tutorial/Editor/TutorialFocusDataManager.cs` | 에디터 전용 관리 도구 |
| `GamePlay/Service/FocusService.cs` / `Interface/IFocusService.cs` | 오케스트레이션 (등록 조회, Screen 오픈, 완료 콜백 연결) |
| `GUI/Screen/Tutorial/TutorialFocusScreen.cs` | 스포트라이트/가이드 렌더링, `ITutorialFocus` 구현 |
| `GUI/ScreenOption/FocusOption.cs` | `OpenAsync`에 전달하는 파라미터 (`TutorialFocusData` + `FocusGuide`) |
| `GameInfo/Tutorial/TutorialInfo.cs` / `GuideBase.cs` / `FocusGuide.cs` | 기획 데이터 (테이블 자동 생성 대상) |

## 연관 경로

- Screen 관리 시스템 전반: `Assets/Script/GUI/ARCHITECTURE.md`, `Assets/Script/GUI/README.md`
- SafeArea: `Assets/Script/GUI/ARCHITECTURE.md`의 SafeArea 섹션
- 비동기 클릭 리스너: `Assets/Script/GUI/SW_GUI/README.md`의 "Click의 비동기 버전"
- `[Focus]` 인스펙터 어트리뷰트: `Assets/Script/GameInfo/Attribute/`, 구현: `Assets/Script/Editor/Attribute/`
