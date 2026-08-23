# Editor — 에디터 전용 도구

Unity Editor에서만 동작하는 커스텀 도구 모음. 런타임에는 포함되지 않는다.

## 폴더 구조

| 폴더/파일 | 역할 |
|---|---|
| `Attribute/` | GameInfo Attribute의 PropertyDrawer 구현 (각 `XxxAttribute` → `XxxDrawer`) |
| `MapEditor/` | 맵 배치 에디터 (`Tools > Map Editor`) — 타일·오브젝트·HeightPoint 시각 편집 |
| `ValueDrawer/SerializeGuidDrawer.cs` | `SerializeGuid` Inspector 커스텀 드로어 |
| `NavigationMenu.cs` | 에디터 상단 메뉴 도구 |
| `ErrorMessageEditorWindow.cs` | 에러 메시지 로컬라이즈 관리 창 |
| `ScreenManagerDebugWindow.cs` | `Tools > Debug > Screen Manager` — 런타임 ScreenManager 상태 뷰어(Odin 미사용, 순수 Unity Editor API) |
| `AddressableServiceDebugWindow.cs` | `Tools > Debug > Addressable Service` — 런타임 Addressable 중앙 캐시 뷰어(Odin 미사용, 순수 Unity Editor API) |
| `GameObjectPoolDebugWindow.cs` | `Tools > Debug > GameObject Pool` — 런타임 UI/Audio/Stage 오브젝트 풀 뷰어(Odin 미사용, 순수 Unity Editor API) |
| `ClassListPoolDebugWindow.cs` | `Tools > Debug > Class & List Pool` — `ClassPool`/`ListPool` static 풀 뷰어(Odin 미사용, 순수 Unity Editor API) |

## Attribute/ — PropertyDrawer 구현

`Assets/Script/GameInfo/Attribute/`에 선언된 Attribute의 Inspector 렌더링 구현.  
대응 관계: `XxxAttribute.cs` → `Editor/Attribute/XxxDrawer.cs`

각 Drawer는 Odin Inspector `OdinAttributeDrawer` 또는 `OdinValueDrawer` 기반.  
Inspector에서 기획 데이터를 편집할 때 드롭다운, 유효성 검사, 경로 선택 등 편의 기능을 제공한다.

## NavigationMenu.cs

Unity 메뉴 `Tools > 데이터 리로드` — 에디터에서 기획 데이터 변경 후 즉시 재로드.

```
GameInfoManager.Instance.Release() → Load()
```

## ErrorMessageEditorWindow.cs

메뉴 `Tools > ErrorMessage` — 에러 메시지 로컬라이즈 테이블 관리 창 (Odin Inspector 기반).

- `ErrorMessage` 열거형 값 기반으로 테이블 항목 자동 생성
- 로케일별 StringTable 자동 생성/관리 (`Assets/Localization/StringTables/`)
- 변경사항 저장 버튼 제공

## Debug 창 4종 (ScreenManager / AddressableService / GameObjectPool / Class&ListPool)

`Tools > Debug/` 하위에 모아둔 런타임 디버그 창들. 넷 다 Odin 없이 순수 `UnityEditor`/`EditorGUILayout` API로만 작성했고,
관찰 대상의 내부 상태가 캡슐화를 위해 대부분 private이라 프로덕션 코드에 디버그용 public 접근자를 추가하는 대신
읽기 전용 리플렉션(`FieldInfo.GetValue`)으로 들여다본다는 설계를 공유한다 — 런타임 동작에는 전혀 관여하지 않고,
내부 필드명이 바뀌면 창 안에서 에러 메시지로 알려준다. 네 창 모두 `UnityEngine.GUI`를 완전히 명시해서 사용하는데,
`Script.Editor` 네임스페이스가 `Script.GUI`(UI 시스템 루트 네임스페이스)의 상위 네임스페이스라서 bare `GUI`가
`UnityEngine.GUI`가 아니라 `Script.GUI` 네임스페이스로 잡혀버리기 때문이다.

**대상을 얻는 방식은 창마다 다르다** — 관찰 대상이 어떻게 존재하느냐에 맞춘다:
- MonoBehaviour면 씬에서 직접 찾는다 (`FindFirstObjectByType`) — ScreenManager
- VContainer Singleton(POCO)이면 그 컨테이너를 들고 있는 MonoBehaviour(`AppLifetimeScope`/`StageLifetimeScope`)를 먼저 찾은 뒤 `Container.Resolve<T>()` — AddressableService, GameObjectPool의 세 매니저
- `static class`의 `static` 필드면 인스턴스 자체가 필요 없어 타입에서 바로 리플렉션 — ClassPool/ListPool (그래서 이 창만 Play 모드가 아니어도 동작함)

### ⚠️ 프로덕션 코드 수정 시 체크리스트 (필독)

리플렉션 기반이라 대상 필드/구조를 바꾸면 **컴파일 에러가 안 나고, IDE "사용처 찾기"에도 안 걸린다** — 조용히 깨진다.
2026-08-24에 `ScreenManager._firstScreen`을 `_rootScreen`으로 리네임했을 때 실제로 이 방식(문자열 리터럴 `"_firstScreen"`)이
조용히 깨지는 걸 겪은 뒤, **필드명 하드코딩을 전부 없애고 `nameof` 기반 `FieldName` 상수로 바꿨다**:

```csharp
// 프로덕션 클래스(ScreenManager, AddressableService, GameObjectPool, UIPooling/AudioPooling/StagePooling,
// ClassPool, ListPool) 쪽에 이렇게 노출해두고
public const string RootScreenFieldName = nameof(_rootScreen);

// Debug Window는 문자열 리터럴이 아니라 이 상수만 참조한다
typeof(ScreenManager).GetField(ScreenManager.RootScreenFieldName, ...)
```
필드가 리네임되면 `nameof(_rootScreen)` 줄 자체가 컴파일 에러가 나므로, **필드명 변경은 이제 조용히 안 깨지고 빌드가 막힌다.**
`private class`(예: `AddressableService.CacheEntry`)의 멤버도 마찬가지 — 그 중첩 타입은 밖에서 이름으로 못 건드리지만,
`nameof(CacheEntry.Handle)`은 **enclosing type인 `AddressableService` 자신의 코드 안에서는** 접근 가능해서 문제없이
컴파일되고, 그 결과 문자열만 `public const`로 바깥에 노출한다(타입 자체의 캡슐화는 그대로 유지).

| 프로덕션 파일 | 리플렉션 대상 (FieldName 상수) | 확인할 창 |
|---|---|---|
| `GUI/ScreenManager/ScreenManager.cs` | `ScreensFieldName`, `LoadedScreensFieldName`, `RootScreenFieldName`, `OpenWaitQueueFieldName`, `CloseWaitQueueFieldName` | Screen Manager |
| `GUI/ScreenManager/ScreenManager.Loading.cs` | `LoadingScreenFieldName`, `LoadingScreenShownFieldName` | Screen Manager |
| `GUI/ScreenManager/ScreenManager.SafeArea.cs` | `SafeAreaScreenFieldName`, `SafeAreaScreenShownFieldName` | Screen Manager |
| `GUI/ScreenManager/ScreenManager.StageTransition.cs` | `StageTransitionSnapshotFieldName` | Screen Manager |
| `GUI/ScreenManager/ScreenManager.State.cs` | `Initialized`/`OpeningScreen`/`ClosingScreen`(public 프로퍼티라 상수 없이 직접 참조) | Screen Manager |
| `GUI/Screen/Interface/IScreen.cs`, `Screen/Base/Screen.cs` | `IScreen`의 `Previous`/`Next`/`Key`/`LayerType`/`State`/`DontClose`/`GameObject`(public) — 특히 **DontClose 화면이 `_rootScreen` 체인에 남아있다는 전제** | Screen Manager |
| `Addressable/AddressableService.cs` | `CacheFieldName`, `CacheCheckIntervalSecondsFieldName`, `CacheReleaseGraceSecondsFieldName`, `CacheMonitorCtsFieldName`, `AppLabelsFieldName`, `CacheEntryTypeName`, `CacheEntryHandleFieldName`, `CacheEntryRefCountFieldName`, `CacheEntryReleasePendingSinceFieldName`(`LoadingTask`는 창이 안 씀) | Addressable Service |
| `GamePlay/Pool/GameObjectPool.cs` | `PooledFieldName`, `InstanceFieldName`, `IsDisposedFieldName` (`Key`/`BaseScale`은 public이라 상수 없이 직접 참조) | GameObject Pool |
| `GamePlay/Pool/UIPooling.cs` / `AudioPooling.cs` / `StagePooling.cs` | `ObjectPoolsFieldName` (세 클래스 각자 선언, 값은 전부 `"_objectPools"`) | GameObject Pool |
| `Utility/Runtime/ClassPool.cs` | `PoolsFieldName`(static) | Class & List Pool |
| `Utility/Public/ListPool.cs` | `PoolsFieldName`(static) — `Get<T>`/`GetCollection<T>` 두 경로가 key 의미를 다르게 씀 | Class & List Pool |

**필드명이 바뀐 경우**: 위 표의 `nameof(...)` 줄이 자동으로 컴파일 에러를 내므로, 그 줄의 `nameof` 인자를 새 이름으로
고치기만 하면 끝난다(Debug Window 쪽은 상수만 참조하므로 손댈 필요 없음). **더 위험한 건 이름은 안 바뀌었는데 의미/구조가
바뀌는 경우**(예: `_rootScreen` 체인이 더 이상 "열려있는 화면 전체"를 뜻하지 않게 된다거나, `GameObjectPool`이 대여
인스턴스를 추적하기 시작하는 등) — `nameof`는 이런 변화를 못 잡으므로 창이 아무 경고 없이 잘못된 정보를 보여줄 수 있다.
그래서 위 표의 파일을 건드릴 때마다(특히 로직/구조 변경 시) 관련 창을 한 번 열어서 눈으로 확인하는 습관은 여전히 필요하다.

또 하나 공유하는 패턴: `[AssetPath]` 기반 필드(`CharacterInfo.skeletonDataAsset` 등)는 Addressable key 자체가 GUID
원본 문자열이라 사람이 못 읽는다. Addressable Service/GameObject Pool 창 둘 다 "로드가 끝났으면 실제 로드된
오브젝트의 이름을 쓰고, 못 구하면 `AssetDatabase.GUIDToAssetPath`로 파일명을 역추적, 그마저 실패하면 원래 key를
그대로 보여준다"는 동일한 `DisplayName` 폴백 전략을 각자 구현에 맞게 반복한다(원래 key는 툴팁으로 유지, 검색은 key/표시이름 둘 다에 매칭).

### `Tools > Debug > Screen Manager`

Play 모드에서 `ScreenManager`의 런타임 상태를 관찰한다.

- **ScreenManager 상태**: `Initialized`/`OpeningScreen`/`ClosingScreen` 플래그, Open/Close 대기열 내용
- **특수 오버레이**: Loading/SafeArea/StageTransition — 이 셋은 `OpenAsync`를 거치지 않고 ScreenManager가 직접 관리해서 아래 스택에는 나타나지 않는다는 걸 창에서 명시. StageTransition은 캡처된 스냅샷을 실시간 썸네일로 미리보기
- **열려있는 Screen 스택**: `_rootScreen`부터 `Next`를 따라간 LinkedList 순회 결과(DontClose는 `[고정]` 표시), 각 Screen의 `ScreenState` 플래그를 색상 칩으로 표시
- **Layer별 보기**: 위 스택을 `ScreenLayerType` 순서로 그룹핑, Loading/SafeArea는 오버레이 항목도 함께 표기
- **로드된 Screen**: `_loadedScreens`(Addressable Instantiate 완료 후 캐시)와 등록 레지스트리를 대조해 Addressable 주소를 표시
- **등록된 전체 Screen 레지스트리**: `ScreenData` 애셋을 직접 읽어오므로 Play 모드가 아니어도 확인 가능
- 각 행의 "선택" 버튼으로 해당 Screen의 GameObject를 Hierarchy에서 Ping/선택 가능, 검색창으로 key 필터링, 자동 새로고침 토글 지원

### `Tools > Debug > Addressable Service`

Play 모드에서 [`AddressableService`](../Addressable/README.md)의 RefCount+유예시간 중앙 캐시를 관찰한다.
`AddressableService`는 `ScreenManager`와 달리 MonoBehaviour가 아니라 VContainer App Scope Singleton이라 씬 탐색으로
찾을 수 없다 — `AppLifetimeScope`(MonoBehaviour인 루트 LifetimeScope)를 찾은 뒤 `Container.Resolve<IAddressableService>()`로
실제 실행 중인 인스턴스를 얻는다.

- **서비스 상태**: `IsInitialized`, 캐시 점검 루프 동작 여부, 현재 설정된 점검 주기/유예시간(`GameSettingData`에서 온 값)
- **캐시 엔트리**: key별로 로딩 중 / 사용 중(pin, RefCount>0) / 유예 대기(RefCount=0, 실제 Release까지 남은 시간 카운트다운) 상태와 로드된 에셋의 타입(Sprite/AudioClip/AudioMixer 등)을 표시
- 사용 중이거나 유예 대기 중인 엔트리는 "선택" 버튼으로 실제 로드된 에셋을 Project 창에서 Ping 가능
- 상단 요약 줄에 로딩 중/사용 중/유예 대기 개수를 한눈에 표시, 검색창으로 key 필터링, 자동 새로고침 토글 지원

### `Tools > Debug > GameObject Pool`

Play 모드에서 [`GameObjectPool`](../GamePlay/Pool/GameObjectPool.cs)을 여러 개 들고 있는 세 매니저(`UIPooling`/`AudioPooling`/`StagePooling`)를
한 화면에서 관찰한다. 세 매니저 모두 MonoBehaviour가 아닌 VContainer Singleton이라 씬 탐색으로 못 찾는다 — `UIPooling`/`AudioPooling`은
`AppLifetimeScope.Container.Resolve<T>()`로, `StagePooling`은 Stage scope 전용이라 게임 씬에만 존재하는 `StageLifetimeScope`를 먼저
찾은 뒤 그 `Container.Resolve<T>()`로 얻는다 — 그래서 Title/Lobby 씬에서는 "Stage Pooling" 섹션이 "스테이지 씬이 아님" 안내만 보여준다.

- 매니저별로 접을 수 있는 섹션(UI/Audio/Stage 각각) — 헤더에 `풀 개수 · 대기 중(재사용 가능) 총합` 요약
- 풀별로 표시 이름(위 `DisplayName` 전략), 대기 중 개수, `BaseScale`, `Disposed` 여부(있으면 경고색)
- 의도적으로 "사용 중(대여됨)" 개수는 보여주지 않는다 — `GameObjectPool`은 대여 나간 인스턴스를 추적하지 않고(반환될 때 `HashSet.Add`가
  실패하는지로만 중복 반환을 확인), 대여된 오브젝트는 풀의 `Root` 밖으로 재부모화되어 버려서 신뢰할 수 있는 방법으로 셀 수가 없다.
  없는 데이터를 억지로 추정해서 보여주느니 정직하게 "대기 중" 하나만 보여주는 쪽을 택함
- "선택" 버튼으로 풀의 템플릿 인스턴스를 Hierarchy에서 Ping, 검색창으로 key/표시이름 필터링, 자동 새로고침 토글 지원

### `Tools > Debug > Class & List Pool`

[`ClassPool`](../Utility/Runtime/ClassPool.cs)(순수 C# 오브젝트 풀 — FSM Node/Transition 등 `ClassPool` 재사용 대상)과
[`ListPool`](../Utility/Public/ListPool.cs)(임시 `List<T>`/컬렉션 재사용)을 한 창에서 관찰한다. 둘 다 `static class`의
`static Dictionary` 하나가 전부라 인스턴스를 찾을 필요가 없다 — 그래서 이 창만 Play 모드가 아니어도 죽지 않는다
(다만 보통 게임이 돌아야 실제로 뭔가 채워짐). 또한 둘 다 순수 POCO를 풀링하는 거라 Hierarchy/Project에 Ping할 대상이
없어서 "선택" 버튼도 없다 — 세 창과 달리 순수 수치 관찰용.

- **ClassPool**: 타입별 대기 중 개수 + 그 타입이 `IClassPool`(`OnRent`/`OnReturn` 훅)을 구현하는지 표시
- **ListPool**: `Get<T>()`/`GetCollection<T>()` 두 경로가 같은 `Dictionary<Type, Stack<ICollection>>`를 공유해서 key의 의미가
  다르다(요소 타입 vs 컬렉션 타입 그 자체) — 그래서 key를 그대로 믿지 않고, 대기 중인 항목이 있으면 실제 저장된 오브젝트의
  런타임 타입(`List<GameObject>`처럼 제네릭 인자까지 풀어서 표시)을 우선 보여준다
- 두 섹션 모두 헤더에 `타입 개수 · 대기 중 총합` 요약, 검색창으로 타입명 필터링, 자동 새로고침 토글 지원

## SerializeGuidDrawer.cs

`SerializeGuid` 필드를 가진 모든 Inspector에 자동 적용.

```
[Label]  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  [Reset] [New]
```

- 텍스트 필드: 읽기 전용 (오타 방지)
- Reset: `Guid.Empty`로 초기화
- New: 새 Guid 발급

## 연관 경로

- Attribute 선언: `Assets/Script/GameInfo/Attribute/`
- SerializeGuid: `Assets/Script/Guid/SerializeGuid.cs`
- 에러 메시지 Enum: `Assets/Script/GameInfo/Enum/`
