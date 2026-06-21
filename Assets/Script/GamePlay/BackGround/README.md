# BackGround — 시차 스크롤 배경 시스템

카메라(또는 플레이어) 이동에 맞춰 배경 레이어를 시차(Parallax) 이동시키고,  
타일을 무한 루프 배치하는 시스템.

## 클래스 구성

| 클래스 | 역할 |
|---|---|
| `ParallaxLooper` | 전체 레이어 관리, 매 프레임 Tick 호출, GroundY 변경 감지 |
| `ParallaxLayer` | 단일 레이어의 Parallax 이동 + 타일 루프 배치 |

---

## ParallaxLooper

VContainer `[Inject]`로 `IStageManager`, `ICameraControls`를 주입받는다.

**초기화 (`Awake → Initialize async`)**
1. `MainCamera`가 준비될 때까지 대기
2. 모든 `ParallaxLayer.Initialize(targetPos)` 호출
3. `_lastGroundY = stageManager.GroundY` 로 초기값 캡처

**매 프레임 (`Update`)**
1. `SystemControl` 활성 시 정지 (스테이지 시스템 제어 중)
2. `stageManager.GroundY` 폴링 — 이전 값과 다르면 `delta` 계산 후 모든 레이어에 `ShiftY(delta)` 호출
3. 각 레이어 `Tick(targetPos, cameraLeftX)` 호출

> 현재 `_target`은 카메라 Transform. 플레이어 Transform으로 교체 가능 (주석 참고).

---

## ParallaxLayer

### 핵심 필드

| 필드 | 설명 |
|---|---|
| `_parallaxFactor` | 카메라 이동 대비 레이어 이동 비율 (0 = 고정, 1 = 1:1, >1 = 빠르게) |
| `_fixedY` | 레이어의 고정 Y 좌표 (GroundY 변경 시 `ShiftY`로 이동) |
| `_fixedZ` | 레이어의 고정 Z 좌표 |
| `_relativeOffset` | `Initialize` 시 캡처한 Target 기준 상대 오프셋 |
| `_loop` | 타일 루프 여부 |
| `_cycleWidth` | 타일 한 사이클의 총 너비 |

### 주요 메서드

| 메서드 | 설명 |
|---|---|
| `Initialize(targetPos)` | 타일 정렬·캐시·오프셋 캡처 후 초기화 |
| `Rebind(targetPos)` | 씬 전환 없이 Target이 바뀔 때 오프셋 재캡처 |
| `Tick(targetPos, cameraLeftX)` | Parallax 이동 + 타일 루프 배치 (매 프레임 호출) |
| `ShiftY(delta)` | `_fixedY += delta` — GroundY 변경 시 배경 Y 이동 |
| `AlignTiles()` | 타일을 순서대로 좌→우 배치 (Context Menu / 초기화 시) |
| `CollectChildSprites()` | 자식 SpriteRenderer를 자동으로 Tile 목록에 등록 |

### 타일 루프 알고리즘

`UpdateLoop`에서 매 프레임 "정답 배치"를 재계산한다.

1. `recycleX` = 카메라 왼쪽 경계 - `_cameraLeftOffset` - `_recycleOffset`
2. 해당 X가 몇 번째 사이클(cycleIndex)의 어느 타일(frontIndex)에 해당하는지 계산
3. `frontIndex`를 기점으로 모든 타일을 순서대로 X 위치 재배치
4. 루프를 돌 때 wrapping이 필요한 타일은 `cycleIndex + 1`에 배치

---

## GroundY 연동

`ParallaxLooper.Update`가 `stageManager.GroundY`를 매 프레임 폴링한다.  
변경 감지 시 `delta = newGroundY - _lastGroundY` 를 계산해 모든 레이어에 전달.

```
StageManager.SetGroundY(5f)
  → ParallaxLooper._lastGroundY = 0 → groundY = 5 → delta = +5
  → layer1.ShiftY(+5)   layer2.ShiftY(+5)   layer3.ShiftY(+5)
  → 각 레이어 _fixedY += 5  (상대적 Y 차이 유지)
```

스테이지 리셋 시 `SetGroundY(0f)`가 호출되면 음수 delta로 자동 원위치된다.

---

## Inspector 설정 예시

```
ParallaxLooper (GameObject)
  ├─ Layers[0]: ParallaxLayer (sky)       parallaxFactor=0.1
  ├─ Layers[1]: ParallaxLayer (mountain)  parallaxFactor=0.3
  └─ Layers[2]: ParallaxLayer (ground)    parallaxFactor=0.8, loop=true
```

- `parallaxFactor`가 낮을수록 멀리 있는 느낌 (느리게 이동)
- `loop=true`인 레이어는 `_tiles`에 반복할 스프라이트를 등록
- `_startXOffset`: 레이어 초기 X 오프셋 (뒤쪽 배경 시작 위치 조정용)
