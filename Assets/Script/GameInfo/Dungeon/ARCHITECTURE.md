# GameInfo/Dungeon — 던전 기획 데이터

던전 진행에 필요한 기획 데이터를 정의한다.  
Unity 비의존 순수 C# — 서버와 공용으로 사용할 수 있다.

## 폴더 구조

| 폴더/파일 | 설명 |
|---|---|
| `DungeonInfo.cs` | 던전 전체 정보 (Stage 목록 포함) `[AutoEditorTable(true)]` |
| `Stage.cs` | 스테이지 단위 데이터 (PhaseInfo 참조) |
| `PhaseInfo.cs` | 페이즈 단위 — `actions[]` + `triggers[]` 보유 `[AutoEditorTable(true)]` |
| `Action/` | 스테이지 이벤트 액션 기획 데이터 |
| `Trigger/` | 스테이지 종료 조건 기획 데이터 |
| `Map/` | 맵 배경 타일 및 바닥 높이 커브 데이터 |
| `Enum/` | 던전 관련 Enum (`Category` 등) |

---

## Action — 스테이지 이벤트

`ActionBase`를 상속하는 `[Serializable]` 클래스.  
PhaseInfo.actions[] 배열에 등록되며 `EventTiming`(Begin/Update/End)에 따라 실행된다.

| 클래스 | Timing | 설명 |
|---|---|---|
| `PlayerSpawnAction` | Begin | 플레이어 캐릭터 스폰 |
| `EnemySpawnAction` | Begin/Update | 적 캐릭터 스폰 |
| `MapSpawnAction` | **Update** | 맵 타일 스폰/디스폰 + 바닥 Y 높이 갱신 |

```csharp
// 새 Action 추가 방법
// 1. 이 폴더에 XxxAction : ActionBase 작성
// 2. GamePlay/Stage/Action/ClientXxxAction : ClientActionBase 작성
// 3. Unity 메뉴 → Generator > ActionFactory 재생성
```

---

## Trigger — 스테이지 종료 조건

`TriggerBase`를 상속하는 `[Serializable]` 클래스.  
PhaseInfo.triggers[] 배열에 등록되며 매 Update마다 평가된다.

| 클래스 | 설명 |
|---|---|
| `PlayerDieTrigger` | 플레이어가 사망하면 트리거 |

---

## Map — 맵 타일 & 바닥 높이 시스템

### MapTileData
시각적 배경 타일 한 장의 위치와 프리팹 키를 보관한다.

```csharp
public class MapTileData {
    public Vector3 position;  // 타일 중앙 월드 좌표
    public string  prefabKey; // Addressable 키 ([AssetPath(typeof(GameObject))])
}
```

> **주의**: 타일 자체에 groundY·충돌 정보는 없다 — 순수 시각 데이터.

### 타일 너비 측정 (MeasureTileWidth)

`ClientMapSpawnAction`이 프리팹에서 너비를 측정할 때 두 가지 방식을 순서대로 시도한다.

| 우선순위 | 컴포넌트 | 측정 방식 |
|---|---|---|
| 1 | `SpriteRenderer` | `sprite.bounds.size.x × lossyScale.x` |
| 2 | `Tilemap` | `localBounds.size.x × lossyScale.x` |

**Tilemap 프리팹 구조** (Grid > TileMap):
```
GameObject <Grid>           ← 루트
  └─ GameObject <Tilemap, TilemapRenderer>
```
`Tilemap.localBounds`는 배치된 타일 전체의 로컬 공간 경계를 반환하므로, 셀 수 × 셀 크기를 별도 계산하지 않아도 된다.

### HeightPoint — 바닥 Y 커브

플레이어 X 위치에 따라 바닥 높이를 연속적으로 정의하는 제어점.  
X 기준 **오름차순**으로 배치해야 한다.

```csharp
public class HeightPoint {
    public float               x;             // X 좌표 기준점
    public float               groundY;       // 이 지점의 바닥 Y
    public HeightInterpolation interpolation; // 다음 포인트까지의 보간 방식
}
```

### HeightInterpolation

| 값 | 설명 |
|---|---|
| `Linear` | 선형 보간 (`Mathf.Lerp`) |

(추후 `Bezier`, `SCurve` 추가 예정)

### MapSpawnAction — 통합 진입점

`ActionBase` 구현체. timing = Update (기본값 강제).

```csharp
public class MapSpawnAction : ActionBase {
    public MapTileData[]  tiles;        // 배경 타일 목록
    public HeightPoint[]  heightPoints; // 바닥 Y 커브 제어점 (X 오름차순)
}
```

**런타임 동작** (`ClientMapSpawnAction`이 처리):
1. 카메라 범위 기준으로 타일 스폰/디스폰 (풀링)
2. `players[0].Transform.position.x` 기준 HeightPoint 보간 → `groundY` 계산
3. `StageManager.SetGroundY(groundY)` 호출
4. ECS `MapGroundData.GroundY` → `JumpingSystem` / `RunningSystem` / `ParallaxLooper` 연동

```
HeightPoint 보간 예시:
  P0(x=0,  groundY=0)
  P1(x=50, groundY=0)   → P0-P1 구간: groundY = 0 (평지)
  P2(x=100,groundY=10)  → P1-P2 구간: groundY = Lerp(0, 10, t)

  playerX=75 → t=(75-50)/(100-50)=0.5 → groundY=5
```

---

## 연관 경로

- Client 구현: `Assets/Script/GamePlay/Stage/Action/ClientXxxAction.cs`
- Factory CodeGen: `Assets/Script/GamePlay/Stage/Action/Generator/ActionFactory.CodeGen.cs`
- ECS 연동: `Assets/Script/GamePlay/ECS/Component/MapGroundData.cs`
- 배경 연동: `Assets/Script/GamePlay/BackGround/ParallaxLooper.cs`
