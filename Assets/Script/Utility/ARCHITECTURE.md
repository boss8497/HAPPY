# Utility — 공통 유틸리티

프로젝트 전반에서 사용하는 범용 유틸리티 모음.  
기능 범위에 따라 `Public`(어디서나 사용), `Runtime`(게임 런타임), `Editor`(에디터 전용)로 구분된다.

## 폴더 구조

| 폴더 | 대상 | 내용 |
|---|---|---|
| `Public/` | 런타임 + 에디터 | `ArrayUtility`, `ListPool` |
| `Runtime/` | 런타임 전용 | `ClassPool`, `TransformUtility`, `ECSUtility`, `ExtensionUtility`, `SpineUtility` |
| `Editor/` | 에디터 전용 | `LocalizeUtility` |

---

## Public

### ArrayUtility.cs — 배열 확장 메서드

`T[]` 배열에 대한 LINQ 없이 사용하는 경량 탐색 메서드. 모두 `AggressiveInlining` 적용.

```csharp
array.Exists(pred)          // 조건 만족 요소 존재 여부
array.Find(pred)            // 첫 번째 일치 요소 (없으면 default)
array.FindAll(pred)         // 조건 만족 요소 전체 배열
array.FindIndex(pred)       // 첫 번째 일치 인덱스 (-1이면 없음)
array.IndexOf(value)        // 특정 값의 인덱스 (EqualityComparer 사용)
```

### ListPool.cs — List\<T\> 풀링

`List<T>` 인스턴스를 재사용해 GC 할당을 줄인다. `Dictionary<Type, Stack<ICollection>>` 구조.

```csharp
var list = ListPool.Get<int>();       // 풀에서 꺼내기 (없으면 new)
ListPool.Return(list);                // 반환 시 자동 Clear()
```

---

## Runtime

### ClassPool.cs — 클래스 인스턴스 풀링

참조 타입 객체를 풀링하는 정적 클래스. `Dictionary<Type, Stack<object>>` 구조.  
FSM Node/Transition 등 매 프레임 생성/소멸이 잦은 객체에 사용한다.

```csharp
var node = ClassPool.Get<ClientRunNode>();        // 풀에서 꺼내기
ClassPool.Release(node);                          // 반환
ClassPool.Get<T>(factory);                        // 커스텀 팩토리로 생성
ClassPool.Clear<T>();                             // 특정 타입 풀 초기화
```

**IClassPool 인터페이스**: 풀링 라이프사이클 훅 제공.

```csharp
void OnRent();    // 풀에서 꺼낼 때 호출
void OnReturn();  // 풀에 반환하기 직전 호출 (상태 초기화용)
```

### TransformUtility.cs — Transform 편의 메서드

Transform의 특정 축만 변경할 때 전체 Vector3를 생성하지 않아도 되는 확장 메서드.  
모두 null-safe.

```csharp
transform.SetPositionX(1f);          // World X만 설정
transform.SetPositionXY(1f, 2f);     // World X, Y만 설정
transform.SetLocalPositionZ(0f);     // Local Z만 설정
transform.SetRotationY(90f);         // World 회전 Y축 (Euler)
transform.SetScaleX(2f);             // LocalScale X만 설정
// ... X/Y/Z, Local/World, Position/Rotation/Scale 조합 전부 지원
```

### ECSUtility.cs — NativeList\<T\> 확장 메서드

Unity DOTS의 `NativeList<T>` 에 탐색 및 제거 편의 메서드를 추가한다.  
제약: `T : unmanaged, IEquatable<T>`

```csharp
// 값 기반
nativeList.FindIndex(target)              // 이분 탐색
nativeList.RemoveValue(target)            // 순서 유지 제거
nativeList.RemoveValueSwapBack(target)    // 빠른 제거 (마지막 요소로 교체)

// Predicate 기반 (위 세 메서드 모두 Predicate 오버로드 있음)
nativeList.FindIndex(pred)
nativeList.RemoveValue(pred)
nativeList.RemoveValueSwapBack(pred)
```

### ExtensionUtility.cs — GameObject/UI 확장 메서드

```csharp
// 이미 같은 상태면 SetActive 호출 생략 (null-safe)
gameObject.SetActiveSafe(true);
component.SetActiveSafe(false);

// 버튼 리스너 교체 (기본적으로 기존 리스너 전체 제거 후 추가)
button.ClickAddListener(() => { }, removeAll: true);
unityEvent.AddListener(() => { }, removeAll: true);
```

### SpineUtility.cs — Spine 애니메이션 확장 메서드

`SkeletonAnimation` / `SkeletonGraphic` 에 애니메이션 재생 편의 메서드 추가.

```csharp
// SkeletonAnimation
skeleton.StartAnimation("RUN", loop: true, hasExit: false);  // hasExit=true면 기존 트랙 초기화
skeleton.GetAnimationTime("ATTACK");                          // 애니메이션 길이(초)

// SkeletonGraphic (동일 애니메이션 재생 중이면 null 반환 — 중복 방지)
skeletonGraphic.StartAnimation("IDLE", loop: true);
```

---

## Editor

### LocalizeUtility.cs — 로컬라이즈 에디터 유틸리티

에디터 전용. Unity Localization 시스템의 StringTable을 코드에서 조작할 때 사용.  
에디터 창(`ErrorMessageEditorWindow`)이나 커스텀 드로어 내부에서 호출한다.

- 기준 로케일: `"ko"` (한국어)
- 키 형식: `"TableName/EntryKey"`

```csharp
LocalizeUtility.GetLocalizeText("ErrorMessage/NetworkError")   // 텍스트 조회
LocalizeUtility.CreateLocalizeText(term, text)                  // 생성 (컬렉션 자동 생성)
LocalizeUtility.SetLocalizeText(term, text)                     // 텍스트 수정
LocalizeUtility.RemoveLocalizeText(term)                        // 삭제
LocalizeUtility.ContainsLocalizeKey(term)                       // 키 존재 여부
LocalizeUtility.SetLocalizeDescription(term, description)       // 주석 추가

// Inspector UI 바인딩
LocalizeUtility.LocalizeTextField(label, term, description)
LocalizeUtility.LocalizeTextArea(term, description)

// 전체 키 조회
LocalizeUtility.GetAllKeys()
```
