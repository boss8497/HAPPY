# Stage — 스테이지 전체 관리

`StageManager`를 중심으로 스테이지 생명주기·액션·트리거·맵 그라운드를 통합 관리한다.  
관심사별로 Partial Class 파일을 분리한다.

## StageManager Partial 파일 목록

| 파일 | 역할 |
|---|---|
| `StageManager.cs` | 생명주기(Initialize/Begin/Start/End/ReStart/Release), UpdateLoop, Action 실행 |
| `StageManager.Injection.cs` | VContainer 주입 필드 선언 |
| `StageManager.Reactive.cs` | ReactiveProperty 초기화 / 리셋 |
| `StageManager.GamePlay.cs` | 캐릭터 추가·제거, 스코어 처리 |
| `StageManager.Pool.cs` | StagePooling 초기화·해제 |
| `StageManager.Trigger.cs` | Trigger 목록 관리, 매 프레임 평가 |
| `StageManager.Map.cs` | MapGroundData ECS 싱글턴 생성·갱신·해제 |

---

## 생명주기

```
InitializeAsync()
  → ScreenManager.ShowStageTransitionAsync()  ← 화면 캡처해 덮기 (스폰/T포즈 과정 은닉)
  → Initialize()
      → InitializeCamera()
      → InitializePool()
      → InitializeMapGround()   ← MapGroundData 싱글턴 생성 (GroundY = 0)
      → InitializeReactiveProperty()
      → InitializeTrigger()
      → InitializeAction()      ← PhaseInfo.actions → ClientXxxAction 생성·Initialize
  → Begin() → Start()
  → SetPlayerAnimation(IDLE)
  → ScreenManager.HideStageTransitionAsync()  ← Fade Out으로 공개
  → RemoveState(SystemControl)

Begin()
  → ExecuteAction(EventTiming.Begin)
  → ExecuteAction(EventTiming.Update)  ← 첫 틱

Start()
  → UpdateLoop 시작 (UniTask 비동기 루프)

UpdateLoop (매 프레임)
  → ExecuteAction(EventTiming.Update)
  → Trigger 평가

End()
  → ExecuteAction(EventTiming.End)

ReStart()
  → ScreenManager.ShowStageTransitionAsync()  ← 티어다운 전 마지막 정상 프레임을 얼려 덮기
  → StopLoop → 캐릭터·액션·트리거 해제 → InitializeAsync() 재호출

Release() / Dispose()
  → StopLoop → 캐릭터·트리거 해제 → Pool 해제 → ReleaseMapGround()
```

**SystemControl 상태와 StageTransition:** `AddState(StageState.SystemControl)`이 걸려 있는 동안(스폰~애니메이션 안정화 구간)에는
ECS `StageSyncSystem`이 해당 유닛을 동기화·InsideMap 판정 대상에서 제외한다 (`Assets/Script/GamePlay/ECS/ARCHITECTURE.md` 참고).
StageTransition 오버레이가 화면을 가리는 구간과 겹치도록 설계되어 있다.

---

## Action 시스템

### ActionBase (GameInfo)
`EventTiming`(Begin/Update/End)과 GUID를 가진 기획 데이터 베이스 클래스.

### ClientActionBase (GamePlay)
```csharp
public abstract class ClientActionBase {
    public EventTiming Timing { get; }
    public abstract void     Initialize(IStageManager stageManager);
    public abstract UniTask  ExecuteAsync();
    public abstract void     Release();
}
```

### ActionFactory
`GameInfo.ActionBase` → `ClientActionBase` 변환.  
`ActionFactory.CodeGen.cs`는 CodeGenerator가 자동 생성하므로 **직접 수정 금지**.  
새 Action 추가 후 `Generator > ActionFactory 재생성` 메뉴 실행.

### 등록된 Action

| GameInfo | Client 구현 | Timing |
|---|---|---|
| `PlayerSpawnAction` | `ClientPlayerSpawnAction` | Begin |
| `EnemySpawnAction` | `ClientEnemySpawnAction` | Begin/Update |
| `MapSpawnAction` | `ClientMapSpawnAction` | Update |

---

## Trigger 시스템

### TriggerBase (GameInfo)
종료 조건 기획 데이터 베이스 클래스.

### ClientTriggerBase (GamePlay)
```csharp
public abstract class ClientTriggerBase {
    public abstract bool OnTrigger(IStageManager stageManager);
}
```

### TriggerFactory
`TriggerFactory.CodeGen.cs`는 자동 생성 — **직접 수정 금지**.

---

## Map Ground 시스템 (StageManager.Map.cs)

맵 바닥 Y를 ECS 싱글턴으로 관리한다.

```csharp
public float GroundY { get; private set; }
public void  SetGroundY(float groundY)      // MapGroundData 싱글턴 갱신
private void InitializeMapGround()          // 스테이지 시작 시 GroundY = 0으로 초기화
private void ReleaseMapGround()             // 스테이지 종료 시 싱글턴 엔티티 파괴
```

**GroundY 변경 흐름:**
```
ClientMapSpawnAction.ExecuteAsync()
  → ComputeGroundY(playerX)  ← HeightPoint 커브 보간
  → StageManager.SetGroundY()
  → MapGroundData.GroundY 갱신

ECS RunningSystem        → SnapPlayerToGroundJob (낙차 ≤ threshold 만 스냅)
ECS FallDetectionSystem  → 낙차 > threshold 시 UnitFallingEnable 활성화
ECS JumpingSystem        → jumping.GroundY 갱신 + 착지 판정
ECS GravitySystem        → UnitFallingEnable 활성 플레이어 낙하 물리 처리
ParallaxLooper           → ShiftY(delta) (배경 Y 이동)
```

**낙사(Fall Death) 설계:**
- HeightPoint에서 낙사 구간을 매우 낮은 Y(예: `-20`)로 설정하면 플레이어가 자연스럽게 떨어짐
- 낙사 판정은 ECS가 아닌 **Obstacle 충돌**로 처리
  - 낙사 구간 바닥에 `CharacterType.Obstacle` + 높은 `Collision` 데미지 배치 → 기존 충돌 시스템이 사망 처리
- `ConfigurationInfo.fallDetectionThreshold`: 낙사 감지 임계값 (권장 0.5 ~ 1.0)

---

## IStageManager 인터페이스

Stage 외부(Character, GUI, Service 등)는 이 인터페이스를 통해서만 StageManager에 접근한다.

주요 멤버:
- `Players` / `Enemies` — 현재 스테이지의 캐릭터 목록
- `GroundY` / `SetGroundY()` — 맵 바닥 높이
- `StagePooling` — 풀링 접근
- `CameraControls` — 카메라 참조
- ReactiveProperty: `State`, `Score`, `RunningScore`, `Initialized`, `Fail`, `Clear` 등

---

## 연관 경로

- 기획 데이터: `Assets/Script/GameInfo/Dungeon/`
- ECS 연동: `Assets/Script/GamePlay/ECS/Component/MapGroundData.cs`
- 배경 연동: `Assets/Script/GamePlay/BackGround/ParallaxLooper.cs`
- Factory CodeGenerator: `Assets/Script/GamePlay/Stage/Editor/`
