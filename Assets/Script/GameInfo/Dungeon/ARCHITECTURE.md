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
시각적 배경 타일 한 장의 데이터를 보관한다.  
**너비/범위는 MapEditor Save 시 계산되어 필드에 저장**되며, 런타임은 이 값을 직접 읽는다.

```csharp
public class MapTileData {
    public Vector3 position;  // 타일 루트 월드 좌표 (MapEditor에서 직접 배치)
    public string  prefabKey; // Addressable 키 ([AssetPath(typeof(GameObject))])

    // MapEditor Save 시 Tilemap.cellBounds 기준으로 계산
    public float width;   // 타일 너비 (월드 단위)
    public float centerX; // 시각적 중앙 X (cellBounds.center 기준)
    public float startX;  // 왼쪽 끝 X
    public float endX;    // 오른쪽 끝 X
}
```

> **주의**: 타일 자체에 groundY·충돌 정보는 없다 — 순수 시각 데이터.

### 타일 너비/범위 계산 (MapEditor Save 시)

MapEditor에서 **"Save to PhaseInfo"** 할 때 `MapEditorState.ComputeTileBounds()`가 프리팹을 임시 인스턴스화해 측정하고 기획 데이터에 저장한다.

| 우선순위 | 컴포넌트 | 측정 방식 |
|---|---|---|
| 1 | `SpriteRenderer` | `sr.bounds.size.x` / `sr.bounds.center.x` (world space 직접) |
| 2 | `Tilemap` | `cellBounds.size.x × cellSize.x × lossyScale` / `cellBounds.center → TransformPoint` |

**Tilemap 프리팹 구조** (Grid > TileMap):
```
GameObject <Grid>           ← 루트
  └─ GameObject <Tilemap, TilemapRenderer>
```
`Tilemap.cellBounds.center`가 타일 콘텐츠의 실제 중앙(cell 좌표)이며, `cellBounds.size.x × cellSize.x`가 실제 너비다.  
`GameObject.transform.position`은 그리드 원점이므로 중앙과 다를 수 있다.

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
