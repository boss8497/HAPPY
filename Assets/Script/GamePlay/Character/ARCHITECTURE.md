# GamePlay/Character — 캐릭터 클라이언트 런타임 계층

## 역할

`GameInfo/Character`의 기획 데이터를 읽어 **Unity Client에서 실제 동작을 구현**하는 계층.  
VContainer, UniTask, R3, ECS 등 Unity 전용 라이브러리를 자유롭게 사용한다.

```
GameInfo/Character  (기획 데이터, 불변 정의)
       ↓ Factory (ClassPool.Get<ClientXxx>)
GamePlay/Character  ← 이 계층 (Client 런타임 구현)
```

---

## 전체 구조

```
Character (MonoBehaviour, Partial Class)
  └─ CharacterBehaviour (FSM 마스터)
        ├─ ClientNodeBase[] _nodes      (노드 인스턴스 배열)
        ├─ Dictionary<Guid, ClientNodeBase> _nodesByGuid  (GUID 조회)
        └─ ClientNodeBase _currentNode  (현재 실행 중 노드)
              └─ Dictionary<EventTiming, ClientTransitionBase[]> _transitionBases
```

---

## ClientNodeBase — FSM 노드 런타임 베이스

```
Assets/Script/GamePlay/Character/Node/ClientNodeBase.cs
```

### 필드 및 프로퍼티

```csharp
protected CharacterBehaviour _characterBehaviour;
protected NodeBase           _nodeBase;
protected Dictionary<EventTiming, ClientTransitionBase[]> _transitionBases;

protected long              nodeGeneration; // 재진입 방지용 세대 카운터
protected CancellationToken playCts;

public bool IsPlay => !playCts.IsCancellationRequested;
```

### 라이프사이클

```
Initialize()  →  Start()  →  [Enter → Update 루프 → End]  →  Release()
```

| 메서드 | 호출 주체 | 역할 |
|--------|-----------|------|
| `Initialize(characterBehaviour, nodeBase)` | `CharacterBehaviour.Create()` | Transition 생성 및 Priority 정렬 |
| `Start(cts)` | `CharacterBehaviour.Start()` / `OnTransition()` | Begin 검사 → Enter → Update 루프 |
| `Enter()` | `Start()` 내부 | 상태 진입 처리 (AnimationState 설정 등) |
| `Update(cts)` | `Start()` 내부 | 노드 주 로직. `abstract` — 반드시 구현 |
| `End()` | `Start()` 내부 | 상태 종료 처리 |
| `Stop()` | `CharacterBehaviour.OnTransition()` | CancellationToken 취소 |
| `Release()` | `CharacterBehaviour.OnReturn()` | Transition 해제 후 ClassPool 반환 |

### Start() 실행 흐름 (UniTask)

```
Start(cts)
  1. CheckTransition(EventTiming.Begin)   ← Begin Transition 평가
       → 발동 시 OnTransition() 호출 후 즉시 return
  2. Enter()
  3. while (!cts.IsCancellationRequested)
       a. await Update(cts)              ← 노드 주 로직
       b. CheckTransition(EventTiming.Update) ← Update Transition 평가
       c. 발동 시 break
  4. End()
  5. CheckTransition(EventTiming.End)    ← End Transition 평가
```

### Transition 평가 (CheckTransition)

```
_transitionBases[timing]  (Priority 내림차순 정렬)
  → First(t => t.OnTrigger() == true)
  → CharacterBehaviour.OnTransition(this, transition)
```

---

## ClientTransitionBase — 전환 조건 런타임 베이스

```
Assets/Script/GamePlay/Character/Transition/ClientTransitionBase.cs
```

```csharp
public abstract class ClientTransitionBase {
    public bool          Value        => _transitionBase.value;
    public SerializeGuid NextNodeGuid => _transitionBase.nextNodeGuid;
    public int           Priority     => _transitionBase.priority;
    public Character     Character    => _node.CharacterBehaviour.Character;

    public virtual ClientTransitionBase Initialize(ClientNodeBase node, TransitionBase transitionBase);
    public virtual void Release();
    public abstract bool OnTrigger();  // 전환 조건 판별 — 구체 클래스에서 구현
}
```

- `OnTrigger()`가 `true`를 반환하면 해당 Transition이 발동됨
- `Value`는 기획 데이터에서 설정한 bool 비교 기준값

---

## CharacterBehaviour — FSM 마스터

```
Assets/Script/GamePlay/Character/Behaviour/CharacterBehaviour.cs
```

### 역할

- `BehaviourInfo.nodes[]` → `ClientNodeBase[]` Factory 변환 및 보관
- `_nodesByGuid` : `Dictionary<Guid, ClientNodeBase>` — GUID 기반 O(1) 노드 조회
- `Start()` : `StartNode` 탐색 → FSM 진입
- `OnTransition()` : 노드 전환 처리

### OnTransition 흐름

```
OnTransition(currentNode, transition)
  1. currentNode.Stop()
  2. nextNode = _nodesByGuid[transition.NextNodeGuid]
  3. _currentNode = nextNode
  4. await nextNode.Start(newCts)
```

### Node Factory (CharacterBehaviour.Create)

```csharp
ClientNodeBase Create(CharacterBehaviour behaviour, NodeBase nodeBase) {
    return nodeBase.GetType() switch {
        var t when t == typeof(StartNode)         => ClassPool.Get<ClientStartNode>().Initialize(...),
        var t when t == typeof(WaitNode)          => ClassPool.Get<ClientWaitNode>().Initialize(...),
        var t when t == typeof(RunNode)           => ClassPool.Get<ClientRunNode>().Initialize(...),
        var t when t == typeof(DieNode)           => ClassPool.Get<ClientDieNode>().Initialize(...),
        var t when t == typeof(PlayerControlNode) => ClassPool.Get<ClientPlayerControlNode>().Initialize(...),
        var t when t == typeof(SystemControlNode) => ClassPool.Get<ClientSystemControlNode>().Initialize(...),
        var t when t == typeof(CollisionNode)     => ClassPool.Get<ClientCollisionNode>().Initialize(...),
        _                                         => null
    };
}
```

### Transition Factory (ClientNodeBase 내부)

```csharp
ClientTransitionBase CreateTransition(ClientNodeBase node, TransitionBase transitionBase) {
    return transitionBase.GetType() switch {
        var t when t == typeof(PlayerControl)       => ClassPool.Get<ClientPlayerControl>().Initialize(...),
        var t when t == typeof(SystemControl)       => ClassPool.Get<ClientSystemControl>().Initialize(...),
        var t when t == typeof(EndTransition)       => ClassPool.Get<ClientEndTransition>().Initialize(...),
        var t when t == typeof(DieTransition)       => ClassPool.Get<ClientDieTransition>().Initialize(...),
        var t when t == typeof(CollisionTransition) => ClassPool.Get<ClientCollisionTransition>().Initialize(...),
        _                                           => null
    };
}
```

---

## 구체 Node 구현

```
Assets/Script/GamePlay/Character/Node/
```

| 클래스 | Enter() | Update() | End() |
|--------|---------|----------|-------|
| `ClientStartNode` | — | `CompletedTask` (즉시 종료) | — |
| `ClientWaitNode` | `AddState(Idling)` + IDLE 애니 | `CompletedTask` | — |
| `ClientRunNode` | `AddState(Running)` + RUN 애니 | `CompletedTask` | — |
| `ClientDieNode` | `RemoveState(Running)` | DIE 애니 재생 → `DieAnimation` → Enemy 제거 대기 | — |
| `ClientPlayerControlNode` | 점프 상태 설정 | `while (HasAnyInput)` 점프 동기화 루프 | `SyncJumpEntity()` |
| `ClientSystemControlNode` | `RemoveState(Running)` | `WaitUntil(SystemControl == false)` | — |
| `ClientCollisionNode` | `RemoveState(Running)` + 점프 정지 | DAMAGE 애니 재생 → `RemoveState(Collision)` | — |

**`IClassPool` 필수 구현**: 모든 `ClientXxxNode`는 `IClassPool` 구현  
- `OnRent()` : 진입 전 초기화가 필요한 경우  
- `OnReturn()` : 보유 참조 null 처리 (GC 누수 방지)

---

## 구체 Transition 구현

```
Assets/Script/GamePlay/Character/Transition/
```

| 클래스 | OnTrigger() 조건 |
|--------|-----------------|
| `ClientPlayerControl` | `_controls.HasAnyInput == Value` (플레이어에 한정) |
| `ClientSystemControl` | `Character.SystemControl.CurrentValue == Value` |
| `ClientEndTransition` | `true` (무조건 전환) |
| `ClientDieTransition` | `Character.Die.CurrentValue == Value` |
| `ClientCollisionTransition` | `Character.CollisionState.CurrentValue == Value` |

---

## Character — 메인 MonoBehaviour (Partial Class)

```
Assets/Script/GamePlay/Character/
```

Partial Class로 관심사별 파일 분리:

| 파일 | 담당 |
|------|------|
| `Character.cs` | 진입점 (`Initialize`, `Release`, `StartAsync`) |
| `Character.GameInfo.cs` | `CharacterInfo`, `BehaviourInfo` 조회 |
| `Character.Injection.cs` | VContainer `[Inject]` 의존성 주입 |
| `Character.ReactiveProperty.cs` | `State` ReactiveProperty + Flags 파생 |
| `Character.State.cs` | `SetState` / `AddState` / `RemoveState` |
| `Character.GamePlay.cs` | 애니메이션, 이동, ECS 연동 |
| `Character.Action.cs` | Buff / Action 처리 |
| `Character.Buff.cs` | Buff 적용/해제 |
| `Character.Entities.cs` | ECS Entity 초기화 |
| `Character.Gizmos.cs` | 에디터 Gizmos |

### 초기화 순서

```
Initialize(team, isPlayer)
  SetState(None)
  InitializeEntity()         ← ECS Entity 생성
  InitializeGamePlay()       ← CharacterBehaviour.Initialize(BehaviourInfo, this)
  InitializeAction()
  InitializeBuff()
  InitializeReactiveProperty()
  AddState(Initialized)

StartAsync()
  CharacterBehaviour.Start() ← FSM 시작
```

### ReactiveProperty 상태 체계

```csharp
ReactiveProperty<CharacterState> State  // 비트 플래그 마스터 상태

// State에서 자동 파생 (R3 Select + DistinctUntilChanged)
ReadOnlyReactiveProperty<bool> Initialized
ReadOnlyReactiveProperty<bool> Running
ReadOnlyReactiveProperty<bool> Jumping
ReadOnlyReactiveProperty<bool> Die
ReadOnlyReactiveProperty<bool> SystemControl
ReadOnlyReactiveProperty<bool> CollisionState
ReadOnlyReactiveProperty<bool> OutSideMap
ReadOnlyReactiveProperty<bool> InSideMap
```

- Node/Transition에서 `Character.Die.CurrentValue` 등으로 상태를 읽음
- UI나 ECS 시스템에서 `Subscribe()`로 변화 구독 가능

---

## ClassPool — 인스턴스 재사용

Node/Transition은 FSM 전환마다 생성/해제되므로 `ClassPool`로 GC 압력을 최소화한다.

```csharp
// 풀에서 꺼내기 (없으면 new)
var node = ClassPool.Get<ClientRunNode>();
node.Initialize(behaviour, nodeBase);

// 사용 후 반환
ClassPool.Release(node);  // Release 내부에서 OnReturn() 호출
```

**규칙**: 모든 `ClientXxxNode`, `ClientXxxTransition`은 `IClassPool`을 구현하고  
`OnReturn()`에서 **모든 참조 필드를 null로 초기화**해야 한다.

---

## FSM 전체 실행 흐름 예시 (Run → Wait 전환)

```
[PlayerControlNode 진입]
  Enter()  → AddState(Jumping) (입력 있을 때)
  Update() → while(HasAnyInput) { SyncJumpEntity(); await Yield(); }
  HasAnyInput 종료 → Update() 완료

CheckTransition(Update)
  PlayerControl(value=false, priority=10) → HasAnyInput==false → true
  → CharacterBehaviour.OnTransition(playerControlNode, playerControlTransition)

OnTransition()
  playerControlNode.Stop()
  nextNode = _nodesByGuid[transition.NextNodeGuid]  // WaitNode → ClientWaitNode
  await nextNode.Start(newCts)

[WaitNode 진입]
  Enter() → AddState(Idling) + IDLE 애니
  Update() → CompletedTask (즉시)
  CheckTransition(Update) → SystemControl, Die, Collision 등 평가
```

---

## 파일 구조

```
GamePlay/Character/
├── Character.cs                     ← MonoBehaviour 진입점
├── Character.GameInfo.cs
├── Character.Injection.cs
├── Character.ReactiveProperty.cs
├── Character.State.cs
├── Character.GamePlay.cs
├── Character.Action.cs
├── Character.Buff.cs
├── Character.Entities.cs
├── Character.Gizmos.cs
├── Interface/
│   └── ICharacter.cs
├── Behaviour/
│   └── CharacterBehaviour.cs        ← FSM 마스터
├── Node/
│   ├── ClientNodeBase.cs            ← UniTask 비동기 FSM 노드 베이스
│   ├── ClientStartNode.cs
│   ├── ClientWaitNode.cs
│   ├── ClientRunNode.cs
│   ├── ClientDieNode.cs
│   ├── ClientPlayerControlNode.cs
│   ├── ClientSystemControlNode.cs
│   └── ClientCollisionNode.cs
├── Transition/
│   ├── ClientTransitionBase.cs      ← 전환 조건 베이스
│   ├── ClientPlayerControl.cs
│   ├── ClientSystemControl.cs
│   ├── ClientEndTransition.cs
│   ├── ClientDieTransition.cs
│   └── ClientCollisionTransition.cs
├── Animation/
│   ├── DieAnimation.cs
│   └── ObstacleDieAnimation.cs
└── Enum/
    └── AnimationName.cs
```

---

## 새 Node / Transition 추가 방법

### 새 Node 추가

1. `GameInfo/Character/Node/XxxNode.cs` — `NodeBase` 상속, `[System.Serializable]`
2. `GamePlay/Character/Node/ClientXxxNode.cs` — `ClientNodeBase` + `IClassPool` 구현
   - `Initialize()`, `Enter()`, `Update()`, `End()`, `OnReturn()` 구현
3. `CharacterBehaviour.Create()` switch에 분기 추가

```csharp
// 최소 구현 템플릿
[System.Serializable]
public class ClientXxxNode : ClientNodeBase, IClassPool {
    private Character _character;

    public override ClientNodeBase Initialize(CharacterBehaviour cb, NodeBase nodeBase) {
        base.Initialize(cb, nodeBase);
        _character = cb.Character;
        return this;
    }

    protected override void Enter() { }

    protected override async UniTask Update(CancellationToken cts) {
        // 주 로직
    }

    protected override void End() { }

    public void OnRent() { }
    public void OnReturn() { _character = null; }
}
```

### 새 Transition 추가

1. `GameInfo/Character/Transition/XxxTransition.cs` — `TransitionBase` 상속, `[System.Serializable]`
2. `GamePlay/Character/Transition/ClientXxxTransition.cs` — `ClientTransitionBase` 구현
   - `OnTrigger()` : 전환 조건 반환 (`Value`와 비교)
3. `ClientNodeBase.CreateTransition()` switch에 분기 추가

```csharp
// 최소 구현 템플릿
[System.Serializable]
public class ClientXxxTransition : ClientTransitionBase, IClassPool {
    public override bool OnTrigger() {
        return Character.SomeState.CurrentValue == Value;
    }

    public void OnRent() { }
    public void OnReturn() { }
}
```
