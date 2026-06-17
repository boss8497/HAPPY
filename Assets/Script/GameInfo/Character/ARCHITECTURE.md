# GameInfo/Character — 캐릭터 기획 데이터 계층

## 역할

FSM(유한 상태 기계)을 구성하는 **기획 데이터**를 정의하는 계층.  
서버와 DLL을 공유하므로 Unity 전용 라이브러리를 **절대 사용하지 않는다**.

```
GameInfo/Character  ← 이 계층 (기획 데이터, 불변 정의)
       ↓ Factory
GamePlay/Character  (Client 런타임 구현)
```

---

## 핵심 제약

- `[System.Serializable]` 필수 — Unity `SerializeReference`로 다형성 직렬화
- 허용: `System.*`, `Newtonsoft.Json`, `SerializeField`, `SerializeReference`
- 금지: VContainer, UniTask, UniRx, Addressables, DOTween 등 Unity 전용 패키지

---

## 클래스 구조

### NodeBase — FSM 노드 정의

```
Assets/Script/GameInfo/Character/Node/NodeBase.cs
```

```csharp
[System.Serializable]
public abstract class NodeBase {
    public SerializeGuid  guid;         // 노드 고유 ID (Dictionary 키, 노드 간 참조)
    public string         id;           // 디버깅/로깅용 이름
    [SerializeReference]
    public TransitionBase[] transitions; // 이 노드에서 출발하는 전환 조건 목록
}
```

- `guid` : SerializeGuid로 직렬화된 GUID. `CharacterBehaviour`에서 `Dictionary<Guid, ClientNodeBase>` 키로 사용
- `transitions` : `[SerializeReference]` 다형성 배열. Inspector에서 구체 Transition 타입을 선택해 직렬화

### TransitionBase — 전환 조건 정의

```
Assets/Script/GameInfo/Character/Transition/TransitionBase.cs
```

```csharp
[System.Serializable]
public abstract class TransitionBase {
    public SerializeGuid guid;          // 전환 고유 ID
    public string        id;            // 디버깅/로깅용 이름
    public EventTiming   timing;        // 전환 검사 시점 (Begin / Update / End)
    public bool          value;         // 비교 기준값 (OnTrigger 구현이 이 값과 비교)
    public byte          priority;      // 평가 우선순위 — 높을수록 먼저 평가
    [NextNode]
    public SerializeGuid nextNodeGuid;  // 전환 대상 노드의 GUID
}
```

**EventTiming**

| 값 | 검사 시점 |
|----|-----------|
| `Begin` | 노드 진입 직후 (Enter 전) |
| `Update` | 노드 실행 중 매 프레임 |
| `End` | 노드 종료 직후 (End 후) |

**value + priority 설계 의도**

- `value`: 조건의 기대 결과값. `OnTrigger()`가 `true`를 반환하면 `value`와 일치할 때 전환
- `priority`: 같은 Timing 내 여러 Transition이 동시에 발동될 때 높은 값이 먼저 평가됨

### BehaviourInfo — FSM 전체 정의

```
Assets/Script/GameInfo/Character/Behaviour/BehaviourInfo.cs
```

```csharp
[System.Serializable]
public class BehaviourInfo : InfoBase {
    [SerializeReference]
    public NodeBase[] nodes; // FSM을 구성하는 전체 노드 배열
}
```

- `nodes[0]`이 항상 `StartNode`여야 한다
- 각 `NodeBase`의 `transitions`에 연결 정보가 포함되어 있어 완결된 노드 그래프를 구성

### CharacterInfo — 캐릭터 기획 데이터

```
Assets/Script/GameInfo/Character/CharacterInfo.cs
```

```csharp
[System.Serializable]
public class CharacterInfo : InfoBase {
    public CharacterType type;
    [SerializeReference]
    public AnimationEvent[] animationEvents;
    [Behaviour]
    public int behaviourId;      // BehaviourInfo.UID 참조
    [Status]
    public int[] statusUids;
    [ShowIf("@type == CharacterType.Buff"), Buff]
    public int[] buffUids;
    [AssetPath(typeof(GameObject))]
    public string prefab;
    [AssetPath(typeof(SkeletonDataAsset))]
    public string skeletonDataAsset;
    [SerializeReference]
    public Hitbox hitbox;
    public CharacterHitbox[] hitboxes;
}
```

- `behaviourId` → `BehaviourInfo` 조회 → `NodeBase[]` 로드 → `CharacterBehaviour` 초기화

---

## 구체 Node 클래스

```
Assets/Script/GameInfo/Character/Node/
```

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `StartNode` | `StartNode.cs` | FSM 시작점. Begin Transition으로 즉시 다음 노드 전환 |
| `WaitNode` | `WaitNode.cs` | 유휴/대기 상태 |
| `RunNode` | `RunNode.cs` | 이동/달리기 상태 |
| `DieNode` | `DieNode.cs` | 죽음 상태 |
| `PlayerControlNode` | `PlayerControlNode.cs` | 플레이어 입력 수락 상태 |
| `SystemControlNode` | `SystemControlNode.cs` | 시스템 일시정지 상태 |
| `CollisionNode` | `CollisionNode.cs` | 충돌 피격 상태 |

기획 데이터 클래스이므로 **상태 전용 데이터 필드만 추가** — 런타임 로직은 `GamePlay/Character/Node/ClientXxxNode.cs`에서 구현.

---

## 구체 Transition 클래스

```
Assets/Script/GameInfo/Character/Transition/
```

| 클래스 | 파일 | 전환 조건 |
|--------|------|-----------|
| `PlayerControl` | `PlayerControl.cs` | 플레이어 입력 유무 (`IPlayerControls.HasAnyInput == value`) |
| `SystemControl` | `SystemControl.cs` | 시스템 제어 신호 (`Character.SystemControl.CurrentValue == value`) |
| `EndTransition` | `EndTransition.cs` | 무조건 전환 (항상 true) |
| `DieTransition` | `DieTransition.cs` | 죽음 상태 (`Character.Die.CurrentValue == value`) |
| `CollisionTransition` | `CollisionTransition.cs` | 충돌 상태 (`Character.CollisionState.CurrentValue == value`) |

---

## CharacterState — 상태 Flags Enum

```
Assets/Script/GameInfo/Character/Enum/CharacterState.cs
```

```csharp
[Flags]
public enum CharacterState {
    None          = 0,
    Initialized   = 1 << 0,
    Idling        = 1 << 1,
    Running       = 1 << 2,
    Jumping       = 1 << 3,
    Die           = 1 << 4,
    SystemControl = 1 << 5,
    Sliding       = 1 << 6,
    Collision     = 1 << 7,
    OutSideMap    = 1 << 8,
    InSideMap     = 1 << 9,
}
```

- `[Flags]` — 비트 OR로 복수 상태 동시 보유 가능
- `Character.State` (`ReactiveProperty<CharacterState>`)를 통해 R3 반응형으로 전파

---

## 파일 구조

```
GameInfo/Character/
├── CharacterInfo.cs
├── Behaviour/
│   └── BehaviourInfo.cs          ← FSM 전체 정의 (NodeBase[] nodes)
├── Node/
│   ├── NodeBase.cs               ← FSM 노드 추상 베이스
│   ├── StartNode.cs
│   ├── WaitNode.cs
│   ├── RunNode.cs
│   ├── DieNode.cs
│   ├── PlayerControlNode.cs
│   ├── SystemControlNode.cs
│   └── CollisionNode.cs
├── Transition/
│   ├── TransitionBase.cs         ← 전환 조건 추상 베이스
│   ├── PlayerControl.cs
│   ├── SystemControl.cs
│   ├── EndTransition.cs
│   ├── DieTransition.cs
│   └── CollisionTransition.cs
├── Animation/
│   └── AnimationEvent.cs
├── Hitbox/
│   ├── Base/Hitbox.cs
│   ├── CharacterHitbox.cs
│   └── Enum/HitBoxType.cs
└── Enum/
    ├── CharacterType.cs
    └── CharacterState.cs
```

---

## 새 Node / Transition 추가 방법

### 새 Node

1. `Node/XxxNode.cs` 생성 — `NodeBase` 상속, `[System.Serializable]` 필수
2. `GamePlay/Character/Node/ClientXxxNode.cs` 생성 — `ClientNodeBase` 상속
3. `CharacterBehaviour.Create()` switch 분기 추가

### 새 Transition

1. `Transition/XxxTransition.cs` 생성 — `TransitionBase` 상속, `[System.Serializable]` 필수
2. `GamePlay/Character/Transition/ClientXxxTransition.cs` 생성 — `ClientTransitionBase` 상속
3. `ClientNodeBase.CreateTransition()` switch 분기 추가
