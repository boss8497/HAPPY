# GamePlay/Buff — 버프 시스템

## 역할

캐릭터에 시간 제한 버프를 적용하고 관리하는 시스템.  
`GameInfo/BuffInfo`의 기획 데이터를 읽어 Status에 반영하며, **점진적 속도 변화(fade in/out)** 를 지원한다.  
Player 소유 BuffSystem은 같은 fade 진행도로 **카메라 연출(Zoom/Offset)** 도 함께 재생한다 (`Assets/Script/GamePlay/Camera/ARCHITECTURE.md` 참고).

---

## 구조 개요

```
IBuffOwner (Character)
  └─ BuffSystem                 ← 한 캐릭터당 하나씩
        ├─ List<Buff>           ← 활성 버프 (ClassPool 재사용)
        ├─ List<UmBuff>         ← 시간 추적 (startTime / endTime / fade 시간)
        └─ UniTask Update 루프  ← 매 프레임 만료 확인 + fade 계산
```

---

## 파일 구조

```
GamePlay/Buff/
├── System/
│   ├── BuffSystem.cs                  ← 핵심 로직 (추가/제거/Update/Fade)
│   └── Interface/
│       ├── IBuffSystem.cs             ← IsInitialize 프로퍼티
│       └── IBuffOwner.cs              ← 버프를 받는 객체 인터페이스
├── Buff/
│   ├── Buff.cs                        ← 런타임 버프 인스턴스 (ClassPool 재사용)
│   └── UmBuff.cs                      ← 시간 추적 struct
└── Interface/
    └── IBuff.cs
```

---

## 핵심 흐름

### 버프 추가

```
Character.ApplyBuff(buffUids)
  → BuffSystem.AddBuffs(uids)
    → AddBuff(): UmBuff에 startTime / endTime / fadeInDuration / fadeOutDuration 기록
    → _owner.AddStatus(statInfos)       ← Status.Spd 즉시 최종값으로 증가
    → NotifySpeedFade(elapsed)          ← factor ≈ 0 (fade in 시작)
      → _owner.OnBuffSpeedFade(bonus, 0)
        → Character._buffSpdFade = 0
        → UpdateRunningStatus(): ECS Speed = Status.Spd - bonus ≈ 기본 속도
```

### 매 프레임 Update 루프

```
Update() {
  만료 버프 → RemoveBuff()
  남은 버프 → NotifySpeedFade(elapsed)
    → 각 버프 CalcFadeFactor():
        timePassed < fadeInDuration  → factor = timePassed / fadeInDuration   (fade in)
        timeLeft   < fadeOutDuration → factor = timeLeft   / fadeOutDuration  (fade out)
        otherwise                   → factor = 1 (최대 속도)
    → 가중 평균 factor 계산 → OnBuffSpeedFade(totalBonus, factor) 콜백
      → UpdateRunningStatus():
          effectiveSpd = Status.Spd - bonus * (1 - factor)
          ECS RunningData.Speed = effectiveSpd
}
```

### 버프 제거

```
RemoveBuff(uid) {
  _buffs / _umBuffs에서 제거
  NotifySpeedFade()  ← 남은 버프로 bonus 재계산 (먼저 실행)
  _owner.RemoveStatus()  ← Status.Spd 감소 → UpdateRunningStatus() 호출됨
}
```

---

## 속도 보정 공식

```
ECS Speed = Status.Spd - buffSpdBonus * (1 - fadeFactor)
```

| 상황 | fadeFactor | ECS Speed |
|------|------------|-----------|
| 버프 없음 | 1.0 (bonus=0) | Status.Spd (기본 속도) |
| fade in 시작 | 0.0 | 기본 속도 |
| fade in 50% | 0.5 | 기본 속도 + 보너스 × 0.5 |
| 최대 | 1.0 | Status.Spd (기본 + 보너스) |
| fade out 50% | 0.5 | 기본 속도 + 보너스 × 0.5 |
| fade out 완료 후 Status 제거 | — | Status.Spd (기본 속도) |

---

## 기획 데이터 설정 (BuffInfo)

| 필드 | 설명 |
|------|------|
| `time` | 버프 총 지속 시간 (fadeOut 구간 포함) |
| `fadeInTime` | 버프 발동 후 최대 속도까지 올라가는 시간. 0이면 즉시 |
| `fadeOutTime` | 종료 전 속도가 줄어드는 시간. 0이면 즉시 제거. `time` 안에 포함 |
| `statusUid` | 적용할 StatusInfo uid 목록 |

**예시**: `time=5, fadeInTime=1, fadeOutTime=1`  
→ 0~1초 가속, 1~4초 최대, 4~5초 감속, 5초에 Status 제거

---

## 제약 및 주의사항

- **Spd 절대값(isPercent=false) 버프만 fade 대상**. 퍼센트 Spd나 다른 스탯(Atk, Def 등)은 즉시 적용/제거.
- 버프가 하나도 없으면 Update 루프(CTS)를 정지해 불필요한 프레임 소비를 막는다.
- 여러 버프가 동시에 있을 때 fade factor는 Spd 보너스 가중 평균으로 계산한다.
- `IBuffOwner.OnBuffSpeedFade()`는 **같은 프레임 안에서 여러 번 호출**될 수 있다 (AddBuffs → NotifySpeedFade → RemoveBuff 순서). 마지막 호출이 최종값.

---

## 카메라 연출 연동 (Speed Boost)

Speed 버프의 fade 진행도와 정확히 같은 타이밍으로 카메라 Zoom(OrthographicSize)과 CinemachineFollow X Offset을 함께 재생한다.
Character가 아니라 **`BuffSystem.NotifySpeedFade()`가 직접** `ICameraControls.SetSpeedBoostFade(factor)`를 호출한다 —
Spd 보너스/fade factor를 이미 여기서 계산하고 있어서 카메라 연출 로직도 같은 자리에 두는 게 자연스럽기 때문
([[camera-speed-boost]] 메모리 참고, `Character.Buff.cs`를 경유하던 이전 버전에서 이쪽으로 옮김).

```csharp
public void Initialize(IBuffOwner owner, IGameTimer gameTimer, ICameraControls cameraControls = null)
```

- `cameraControls`는 **Player 소유 BuffSystem에만** 전달한다 (`Character.InitializeBuff()`에서 `IsPlayer ? _stageManager.CameraControls : null`). Enemy BuffSystem은 null이라 카메라를 건드리지 않는다.
- `NotifySpeedFade()` 안에서 `totalSpdBonus > 0`일 때만 `overallFactor`를 전달하고, Spd 버프가 없으면(=totalSpdBonus 0) 카메라 fade는 0으로 취급한다 — 버프가 막 제거된 직후 `(bonus=0, factor=1)`이 오는 케이스에서 카메라가 갑자기 최대로 튀는 걸 방지.
- `Release()`(ClassPool 반납) 시에도 `SetSpeedBoostFade(0f)`를 호출해, 부스트 도중 owner가 해제되는 경우(ReStart 등) 카메라가 확대된 채로 남지 않게 한다.

---

## IBuffOwner 인터페이스

```csharp
public interface IBuffOwner {
    void AddStatus(List<StatusInfo> infos);
    void RemoveStatus(IEnumerable<StatusInfo> infos);
    void OnBuffSpeedFade(float totalSpdBonus, float fadeFactor);
}
```

`Character.Buff.cs`에서 구현. `OnBuffSpeedFade()`에서 `_buffSpdBonus`, `_buffSpdFade` 업데이트 후 `UpdateRunningStatus()` 호출.

---

## Buff 트리거 경로

```
CharacterType.Buff 캐릭터와 충돌
  → Character.Collision()
  → ApplyBuff(otherCharacter.CharacterInfo.buffUids)
  → BuffSystem.AddBuffs(buffUids)
```

버프 캐릭터는 `CharacterInfo.type == CharacterType.Buff` + `buffUids` 배열로 설정.
