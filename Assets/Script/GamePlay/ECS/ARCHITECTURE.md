# ECS — 고성능 물리/충돌 처리 레이어

Unity DOTS(ECS + Burst + Jobs)를 사용해 이동·점프·충돌을 처리한다.  
모든 시스템은 `[DisableAutoCreation]`으로 자동 생성을 막고 `StageEntityWorld`가 수동 등록한다.

## 폴더 구조

| 폴더 | 설명 |
|---|---|
| `World/` | ECS 월드 생성·등록·해제 (`StageEntityWorld`) |
| `Component/` | ECS 컴포넌트 데이터 정의 |
| `System/` | Burst 컴파일 시스템 구현 |

---

## World

### StageEntityWorld
`IInitializable` / `IDisposable` (VContainer EntryPoint)

- `Initialize()`: 전용 `World` 생성 → 루트 SystemGroup 등록 → 시스템 수동 등록 → PlayerLoop 연결
- `Dispose()`: PlayerLoop 분리 → World 파괴
- 시스템은 반드시 `StageEntityWorld.Initialize()` 안에서 `TypeManager.GetSystemTypeIndex<T>()` 로 추가해야 한다

---

## Component

| 컴포넌트 | 종류 | 설명 |
|---|---|---|
| `UnitEntityTag` | Tag | 유닛 엔티티 식별 |
| `UnitData` | IComponentData | uid·team·GameObject 참조·IsPlayer |
| `RunningData` | IComponentData | Direction·Speed (RunningSystem 입력) |
| `JumpingData` | IComponentData | 점프 물리 전체 상태 (GroundY 포함) |
| `JumpInputData` | IComponentData | 점프 버튼 입력 전달 (Held·ReleaseRequested) |
| `HitBoxData` | IComponentData | 충돌 박스 형태(Rect/Circle/Invisible)·Offset·Size·Radius |
| `UnitCollisionResult` | IBufferElementData | 이번 프레임 충돌 결과 버퍼 |
| `UnitCollisionDelay` | IBufferElementData | 충돌 쿨다운 (OtherUid·ExpireTime) |
| `MapGroundData` | Singleton IComponentData | 스테이지 전역 바닥 Y 값 |
| `FallingData` | IComponentData | 낙하 물리 상태 (FallVelocity·Gravity·FallGravity·FallDetectionThreshold) |
| `EGameTimer` | Singleton IComponentData | 게임 경과 시간 |
| `CameraData` | Singleton IComponentData | 카메라 참조 |

### Enableable 태그 (IEnableableComponent)

| 태그 | 활성 의미 |
|---|---|
| `UnitRunningEnable` | RunningSystem이 이 유닛을 처리 |
| `UnitJumpingEnable` | 현재 점프 중 |
| `UnitFallingEnable` | 현재 낙하 중 (점프 아님) — GravitySystem이 처리 |
| `UnitCollisionEnable` | 충돌 감지 대상 |
| `UnitCollisionResultEnable` | 이번 프레임 충돌 결과 있음 |
| `UnitDieEnable` | 사망 상태 (대부분 시스템에서 제외) |
| `UnitSystemControlEnable` | 시스템 제어 중 (모든 물리 일시정지) |

---

## System — 실행 순서

모두 `FixedStepSimulationSystemGroup` 안에서 동작한다.

```
GameTimerSystem
    ↓
RunningSystem          ← X/Z 이동 + GroundY 변경 시 Player Y 스냅 (큰 낙차 제외)
    ↓
FallDetectionSystem    ← 지면 이탈 감지 → UnitFallingEnable 활성화
    ↓
JumpingSystem          ← 점프 물리 + Player 착지 Y 처리
    ↓
GravitySystem          ← 낙하 물리 + Player 착지 Y 처리 (UnitFallingEnable 활성 엔티티)
    ↓
JumpingResultSystem    ← 착지 완료 시 Character FSM에 알림
    ↓
CollisionSystem        ← 히트박스 교차 판정 (IJobFor 병렬)
    ↓
CollisionResultSystem  ← 충돌 결과 → Character로 전달
    ↓
StageSyncSystem        ← ECS → GameObject Transform 동기화
```

### RunningSystem
- **매 프레임**: `RunningJob` — `RunningData.Direction * Speed * dt` 로 X/Z 이동
- **GroundY 변경 감지 시에만**: `SnapPlayerToGroundJob` — 지면에 있는 플레이어 Y를 스냅
  - `UnitJumpingEnable`, `UnitFallingEnable`, `UnitDieEnable` 비활성 엔티티만 처리
  - 낙차 ≤ `FallingData.FallDetectionThreshold` 이면 스냅 (완만한 경사)
  - 낙차 > threshold 이면 스냅 생략 → `FallDetectionSystem`이 낙하 처리

### FallDetectionSystem
- `UnitJumpingEnable`, `UnitFallingEnable`, `UnitDieEnable`, `UnitSystemControlEnable` 모두 비활성인 플레이어 대상
- `position.y > GroundY + FallingData.FallDetectionThreshold` 조건 시 `UnitFallingEnable` 활성화
- `FallVelocity = 0`으로 초기화 (낙하 속도 초기화)

### JumpingSystem
- 매 프레임 `MapGroundData.GroundY`를 읽어 플레이어(`IsPlayer != 0`)의 `JumpingData.GroundY`를 갱신
- 점프 물리: 상승 구간은 `Gravity * 0.5`, 하강 구간은 `Gravity * FallGravity`
- `position.y <= jumping.GroundY` 도달 시 착지 — `UnitJumpingEnable` 비활성화

### GravitySystem
- `UnitFallingEnable` 활성 엔티티(= 낙하 중) 전용
- 매 프레임 `FallVelocity -= Gravity * FallGravity * dt` 적용 후 Y 이동
- `position.y <= GroundY` 도달 시 착지 — Y 스냅, `FallVelocity = 0`, `UnitFallingEnable` 비활성화
- 낙사 판정은 ECS가 아닌 **Obstacle 충돌**로 처리 (바닥 낙사 위치에 고데미지 Obstacle 배치)

### CollisionSystem
- `IJobFor` 병렬 — 모든 유닛 쌍을 동시에 검사
- 지원 형태: Rect vs Rect (SAT), Circle vs Circle, Rect vs Circle
- 충돌 시 `UnitCollisionResult` 버퍼에 추가 + `UnitCollisionDelay` 쿨다운 등록

---

## 새 시스템 추가 방법

1. `ECS/System/XxxSystem.cs` 작성 (`[DisableAutoCreation]` 필수)
2. `StageEntityWorld.Initialize()` 안에 `systems.Add(TypeManager.GetSystemTypeIndex<XxxSystem>())` 추가
3. 실행 순서가 중요하면 `[UpdateAfter(typeof(YyySystem))]` 어트리뷰트 추가

## 주의사항

- `UnitData.IsPlayer`로 플레이어 전용 처리를 구분한다 (`1` = 플레이어)
- `MapGroundData`는 항상 싱글턴 — `SystemAPI.HasSingleton<MapGroundData>()`로 존재 확인 후 사용
- Tile·Obstacle 엔티티는 GroundY에 영향받지 않는다 (플레이어 전용 설계)
