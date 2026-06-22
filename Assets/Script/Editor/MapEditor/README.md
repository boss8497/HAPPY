# MapEditor — 맵 배치 에디터

Unity 에디터 전용 맵 디자인 도구. `Tools > Map Editor` 메뉴로 실행.  
PhaseInfo 안의 `MapSpawnAction` (타일) + `EnemySpawnAction` (오브젝트) 데이터를 시각적으로 편집한다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `MapEditorWindow.cs` | Odin 기반 에디터 창 — 좌측(데이터 선택 + 팔레트), 우측(리스트 + 조작 안내) |
| `MapEditorState.cs` | 에디터 전역 상태 (작업 중 데이터, 팔레트 캐시, 선택 인덱스 등) |
| `MapEditorSceneGUI.cs` | SceneView 오버레이 — HeightPoint Gizmo + 타일/오브젝트 상호작용 핸들 |
| `MapEditorPreview.cs` | 씬에 프리뷰 GameObject를 생성·동기화·정리하는 관리자 |

## 편집 모드

| 모드 | 조작 |
|---|---|
| **Select** | 씬 핸들 클릭 → 선택 / Delete → 삭제 |
| **Tile 배치** | 팔레트에서 프리팹 선택 → 씬 클릭 배치 / Delete → 선택 타일 삭제 |
| **Object 배치** | Object 팔레트에서 오브젝트 선택 → 씬 클릭 배치 / Delete → 선택 오브젝트 삭제 |
| **HeightPoint** | Shift+클릭 → 추가 / 핸들 드래그 → 이동 / Delete → 삭제 (X 기준 자동 정렬) |

## 데이터 흐름

```
PhaseTable.asset (ScriptableObject)
  └─ PhaseInfo.actions[]
       ├─ MapSpawnAction        ← tiles[], heightPoints[]
       └─ EnemySpawnAction      ← spawnData[] (uid + position)

MapEditor 편집 흐름:
  Load  →  WorkingTiles / WorkingObjects / WorkingHeightPoints  →  Save
```

- **Load**: `MapSpawnAction` / `EnemySpawnAction` 데이터를 Working 리스트로 복사
- **편집**: 씬 핸들 드래그, 리스트 직접 수정
- **Save**: Working 리스트 전체에 `ComputeTileBounds()` 실행 (프리팹 임시 인스턴스화 → `Tilemap.cellBounds` 기준 너비/centerX/startX/endX 계산) → 해당 Action에 기록 → `EditorUtility.SetDirty` → `AssetDatabase.SaveAssets`

## Tile Palette

- 소스: Addressables에 등록된 `Assets/GAME_ASSET/Prefab/Tile/` 하위 `.prefab`만 표시
- 키: Asset GUID (= `MapTileData.prefabKey`)
- 팔레트 항목 클릭 시 자동으로 **Tile 배치** 모드로 전환

## Object Palette

- 소스: `CharacterTable`에서 `CharacterType`이 `Obstacle / Buff / Score / Heart / Goal`인 항목
- 키: `CharacterInfo.UID` (= `EnemySpawnAction.SpawnData.uid`)
- `CharacterInfo.prefab` GUID로 에셋 프리뷰 썸네일 로드
- 항목 클릭 시 자동으로 **Object 배치** 모드로 전환

## 씬 프리뷰 시스템 (MapEditorPreview)

타일·오브젝트의 실제 프리팹을 씬에 인스턴스화해 배치 결과를 시각적으로 확인할 수 있다.

### 동작 방식

```
OnSceneGUI 매 프레임 → MapEditorPreview.Sync()
  ├─ 타일/오브젝트 수가 바뀌었거나 프리뷰 GO가 null이면  → Rebuild()
  │    └─ 기존 프리뷰 전체 DestroyImmediate → 새 인스턴스 생성
  └─ 수가 같으면 → SyncPositions() (위치만 동기화, 드래그 중 실시간 반영)
```

### 프리뷰 인스턴스 특성

| 특성 | 이유 |
|---|---|
| `HideFlags.HideAndDontSave` | Hierarchy 미표시, 씬 저장 제외 |
| `Assembly-CSharp` MonoBehaviour 비활성화 | `Character`, `Unit` 등 런타임 스크립트 에러 방지 |
| Spine, Unity 내장 컴포넌트 유지 | `SkeletonAnimation` 등 렌더링이 동작해 실제 모양 표시 |
| `try-catch` 래핑 | 잘못된 프리팹 하나가 전체 빌드 실패를 막음 |

### 씬 핸들 역할 변경

- **HeightPoint**: 기존 Gizmos 유지 (곡선, 구체 핸들, 화살표)
- **Tile / Object**: 프리팹이 시각 표현 담당 → 핸들은 **선택(Dot) + 이동(PositionHandle)**만 오버레이

## 주의사항

- Phase 선택 후 `MapSpawnAction` 또는 `EnemySpawnAction`이 없으면 각각 "추가" 버튼으로 생성 가능
- 에디터 창을 닫으면 `MapEditorPreview.Cleanup()`이 자동 호출되어 프리뷰 오브젝트 정리됨
- 도메인 리로드(스크립트 재컴파일) 후에는 다음 `OnSceneGUI` 프레임에서 자동 재빌드
- `WorkingTiles` / `WorkingObjects`는 저장 전까지 인메모리 상태 — 창을 닫으면 유실되므로 반드시 Save
