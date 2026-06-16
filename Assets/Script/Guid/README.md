# Guid — SerializeGuid

Unity는 `System.Guid`를 직렬화하지 못한다.  
`SerializeGuid`는 Guid 16바이트를 `uint` 4개로 분할해 Unity가 직렬화할 수 있도록 래핑한 구조체다.

## 왜 uint 4개인가

`System.Guid`는 내부적으로 16바이트(128비트).  
Unity `SerializeField`는 `Guid`를 지원하지 않지만 `uint`(4바이트)는 지원하므로  
16바이트 = `uint` × 4개 로 분할해 저장한다.

```
Guid (16 bytes)
  ├─ v0 : uint  (bytes 0~3)
  ├─ v1 : uint  (bytes 4~7)
  ├─ v2 : uint  (bytes 8~11)
  └─ v3 : uint  (bytes 12~15)
```

## ISerializationCallbackReceiver

Unity 직렬화 전/후 훅을 사용해 변환을 처리한다.

| 훅 | 동작 |
|---|---|
| `OnBeforeSerialize` | `Guid` → uint 4개로 분해해 필드에 저장 |
| `OnAfterDeserialize` | uint 4개 → `Guid`로 복원, 캐시 갱신 |

`_cacheValid` 플래그로 `Guid` 객체를 매번 재생성하지 않고 캐시한다.

## 주요 API

```csharp
SerializeGuid.NewGuid()       // 새 Guid 생성
SerializeGuid.Empty()         // Guid.Empty
guid.IsEmpty                  // Guid.Empty 여부
guid.Value                    // System.Guid 반환 (캐시)
guid.ToString()               // "D" 포맷 (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)

// 암묵적 변환 — Guid와 혼용 가능
Guid g = serializeGuid;
SerializeGuid sg = someGuid;
```

`IEquatable<SerializeGuid>` 구현 + `==` / `!=` 연산자 제공.

## 사용처

FSM Node/Transition의 고유 식별자 및 참조에 사용된다.

```csharp
// NodeBase — 노드 자신의 고유 ID
public SerializeGuid guid = SerializeGuid.NewGuid();

// TransitionBase — 전환할 다음 노드를 GUID로 참조
[NextNode]
public SerializeGuid nextNodeGuid;
```

그 외 `AnimationEvent`, `ActionBase`, `Stage`, `DungeonInfo` 등 고유 ID가 필요한 기획 데이터에서도 사용한다.

## Editor Drawer (`SerializeGuidDrawer.cs`)

위치: `Assets/Script/Editor/ValueDrawer/SerializeGuidDrawer.cs`  
Odin Inspector `OdinValueDrawer<SerializeGuid>` 구현.

Inspector에서 아래와 같이 표시된다.

```
[Label]  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  [Reset] [New]
```

- 텍스트 필드는 읽기 전용 (직접 편집 불가, 오타 방지)
- **Reset**: `Guid.Empty`로 초기화
- **New**: 새 Guid 발급
