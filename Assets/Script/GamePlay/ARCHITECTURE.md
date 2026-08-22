# GamePlay — Unity Client 전용 코드

Unity Client에서만 사용하는 게임 구현 코드.  
`GameInfo`의 기획 데이터를 읽어 Client 전용 객체로 변환하여 사용한다.

## GameInfo → GamePlay 변환 원칙

- `GameInfo`는 순수 데이터 모델 (Unity 비의존)
- `GamePlay`는 `GameInfo`를 읽고 Factory 패턴으로 Client 객체를 생성해 사용
- 예: `GameInfo/Character/Behaviour`, `Node`, `Transition` (기획 데이터)
       → `GamePlay/Character/Behaviour`, `Node`, `Transition` (Client FSM 구현)

## 핵심 디자인 패턴

### 1. Factory Pattern (자동 코드 생성 포함)
`GameInfo`의 기획 데이터 타입 → Client 구현 클래스로 변환

- `ActionFactory.cs` / `TriggerFactory.cs` — partial 클래스 (수동 선언부)
- `ActionFactory.CodeGen.cs` / `TriggerFactory.CodeGen.cs` — switch 분기 (Editor CodeGenerator가 자동 생성, 직접 수정 금지)
- CodeGenerator: `Stage/Editor/ActionFactoryCodeGenerator.cs`, `TriggerFactoryCodeGenerator.cs`
- 새 Action/Trigger 추가 시 CodeGenerator를 재실행하면 자동 반영됨

### 2. FSM Pattern (캐릭터 상태 머신)
`Node` = 상태, `Transition` = 전환 조건

```
ClientStartNode → (Transition 검사: Begin/Update/End 시점)
    → ClientRunNode / ClientCollisionNode / ClientDieNode ...
```

- 전환 검사 시점: `Begin`, `Update`, `End` (EventTiming)
- 우선순위: `Priority`가 높은 Transition이 먼저 평가됨
- Node/Transition 모두 ClassPool로 재사용 (GC 최소화)

### 3. ECS Pattern (고성능 물리/충돌)
이동, 점프, 충돌 감지를 Burst 컴파일된 ECS 시스템으로 처리

```
Unit (MonoBehaviour) → UnitManager.RegisterUnit() → ECS Entity
    Components: RunningData, JumpingData, HitBoxData, UnitData...
    Systems: RunningSystem, JumpingSystem, CollisionSystem (모두 Burst)
```

### 4. Partial Class Pattern
큰 클래스를 관심사별로 파일 분리

- `Character.cs` / `Character.State.cs` / `Character.GamePlay.cs`
  / `Character.Action.cs` / `Character.Buff.cs` / `Character.Entities.cs` 등
- `StageManager.cs` / `StageManager.GamePlay.cs` / `StageManager.Pool.cs` 등

### 5. Object Pool Pattern
`GameObjectPool` (Stack 기반) — Pop으로 꺼내고 Push로 반환

## 폴더 구조

| 폴더 | 역할 |
|---|---|
| `Character/` | 캐릭터 전체 (partial class + FSM 구동) |
| `Character/Node/` | FSM 노드 구현 (`ClientXxxNode.cs`) |
| `Character/Transition/` | FSM 전환 조건 구현 (`ClientXxxTransition.cs`) |
| `Character/Behaviour/` | FSM 마스터 컨트롤러 (`CharacterBehaviour.cs`) |
| `Stage/` | 스테이지 전체 관리 (`StageManager`, partial) (상세: `Stage/ARCHITECTURE.md`) |
| `Stage/Action/` | 스테이지 이벤트 액션 (`ClientXxxAction.cs` + Factory) |
| `Stage/Trigger/` | 스테이지 종료 조건 (`ClientXxxTrigger.cs` + Factory) |
| `Stage/Editor/` | ActionFactory/TriggerFactory CodeGenerator (Editor 전용) |
| `ECS/` | Unity ECS 월드, 컴포넌트, 시스템 (상세: `ECS/ARCHITECTURE.md`) |
| `Unit/` | ECS 엔티티 ↔ GameObject 연결 (`UnitManager`) |
| `Pool/` | GameObject/컴포넌트 풀링 |
| `Buff/` | 버프/디버프 시스템 |
| `Service/` | 게임 데이터 접근 (Item, Group, Focus 등) |
| `Tutorial/` | 튜토리얼 스포트라이트(Focus) 시스템 (상세: `Tutorial/ARCHITECTURE.md`) |
| `Input/` | 플레이어 입력 (`PlayerControls`, New Input System) |
| `Camera/` | 카메라 제어 (Cinemachine) |
| `BackGround/` | 시차 스크롤 배경 (상세: `BackGround/README.md`) |
| `Stat/` | 런타임 스테이터스 (`Status.cs`) |

## 네이밍 패턴

- FSM Node: `Client` 접두사 + `GameInfo` Node명 + `Node` → `ClientRunNode`
- FSM Transition: `Client` 접두사 + `GameInfo` Transition명 → `ClientDieTransition`
- Action/Trigger: `Client` 접두사 + `GameInfo` 클래스명 → `ClientPlayerSpawnAction`

## 연관 경로

- 기획 데이터(GameInfo): `Assets/Script/GameInfo/`
- 테이블 에셋: `Assets/GAME_INFO_TABLE/`
