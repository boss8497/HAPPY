# SW_GUI — 커스텀 UI 위젯 시스템

Unity 기본 UI 컴포넌트(Button 등)를 대체하는 커스텀 위젯 베이스.
`Assets/Script/GUI/`(Screen 관리 시스템)와는 별개로, 개별 UI 요소(버튼, 버튼 그룹 등)의 동작을 직접 구현한다.

---

## 중요
1. 독립적입 라이브러리라고 생각하기
2. 기본 유니티, sdk, Utility 빼고는 어셈블리 참조 금지

## 왜 기본 Button을 안 쓰는가

Unity 기본 `Button`은 안 쓰는 기능(Transition, Navigation 등)까지 프리팹에 직렬화되어 에셋 크기가 늘어나고,
필요한 기능을 얹을 때 기존 컴포넌트와 호환이 어렵다.
`IPointerClickHandler`를 직접 구현해 필요한 기능만 가진 경량 버튼을 만든다.

---

## 폴더 구조

| 경로 | 역할 |
|---|---|
| `Base/SW_GUI_BASE.cs` | 모든 GUI 위젯(Button, Toggle, Slider 등)의 최상위 베이스. `Initialize()`만 선언 |
| `Base/SW_GUI_BUTTON_BASE.cs` | `IPointerClickHandler` 구현. 클릭 이벤트 파이프라인(스크립트 리스너 → `OnClick()` → 인스펙터 이벤트) |
| `Base/Group/SW_GUI_BUTTON_GROUP_BASE.cs` | 버튼 그룹(탭 메뉴 등) 관리자 |
| `Base/Group/SW_GUI_BUTTON_GROUP_ELEMENT_BASE.cs` | 그룹에 속하는 버튼 하나 |
| `Group/SW_GUI_BUTTON_GROUP.cs`, `Group/SW_GUI_BUTTON_GROUP_ELEMENT.cs` | 실제 사용하는 구현체 (UnityEvent 노출) |
| `Interface/` | 현재 미사용 (asmdef만 존재) |

**asmdef 분리:** `SW.GUI.Base` / `SW.GUI` / `SW.GUI.Interface` — Base와 구현체를 분리해 참조 방향을 강제.

---

## Initialize 설계

`SW_GUI_BASE.Initialize()`는 Unity 생명주기 콜백이 아니라 **사용자가 명시적으로 호출**해주는 방식으로 설계.
단, Group만 예외로 `Awake()`에서 자동 호출한다 (`SW_GUI_BUTTON_GROUP_BASE.Awake() → Initialize()`), 인스펙터에 미리 등록해 둔 요소를 씬 시작 시점에 자동 연결하기 위함.

---

## 버튼 그룹 (`SW_GUI_BUTTON_GROUP_BASE`)

탭 메뉴처럼 "여러 버튼 중 상태를 함께 관리해야 하는" 케이스를 위한 컨테이너.

### 등록 흐름

```
인스펙터에 미리 배치한 요소 → _elementsList (SerializeField)
                             → Awake() → Initialize() → element.Group = this, _elementsDictionary 등록

런타임에 생성한 요소        → Register(element)
                             → Key 발급, element.Group = this, 리스트/딕셔너리 등록
```

- `Key`는 `Random.Range` 기반 발급 (충돌 시 재시도). 요소는 `Key == -1`이면 미등록 상태로 취급.
- **`Register()`/`Initialize()` 둘 다 반드시 `element.Group = this`를 설정해야 함** — 이게 빠지면 요소의 `OnClick()`이 `Group == null`로 조용히 무시되어 클릭이 아예 반응 안 하는 버그가 생긴다 (한 번 발생했던 이슈).
- `Unregister()` / `ReleaseSelector()`는 대칭적으로 `element.Group = null` 처리 — 그룹에서 빠진 요소가 죽은 참조를 들고 있지 않도록.

### 선택 모드 (`SelectType`)

| 모드 | 동작 |
|---|---|
| `Single` | `Select(element)` 호출 시 다른 모든 요소를 해제하고 대상만 선택. 이미 선택된 요소를 다시 선택 요청하면 **아무 것도 하지 않음**(중복 이벤트 방지) |
| `Multiple` | `Select(element)` 호출 시 대상의 `Selected`를 **토글**(선택↔해제). 다른 요소에는 영향 없음 |

버튼 클릭 → `Element.OnClick()` → `Group.Select(this)` 로 항상 동일하게 호출되고, 모드별 분기는 Group 내부에서만 처리한다 (Element/구현체는 모드를 몰라도 됨).

### Selected와 이벤트

`Selected` setter가 `true`/`false`로 바뀔 때마다 `OnSelectEvent()`/`OnDeselectEvent()`를 호출한다.
**값이 실제로 바뀔 때만** 이 setter를 거치도록 호출부(`Select`/`DeSelect`)에서 상태를 먼저 확인해야 한다 — 그렇지 않으면 이미 선택된 버튼을 재클릭할 때마다 Select 이벤트가 중복 발생한다.

---

## 아직 비어있는 부분 (의도적)

- `content` (Transform), `Order` (int) — 런타임에 Prefab을 동적으로 생성해서 그룹에 등록하는 기능을 위해 미리 만들어 둔 필드. 아직 그 기능 자체가 설계되지 않아 사용처 없음. 구현 시 `content` 하위에 Instantiate한 요소를 `Register()`로 자동 연결하고, `Order`로 정렬/네비게이션 순서를 매기는 용도로 쓸 예정.
