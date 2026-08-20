# Camera — 카메라 제어

`CameraControls`가 메인 카메라 경계 계산, Camera Shake, Speed Boost 카메라 연출을 담당하는 Stage 범위 싱글턴 서비스.
MonoBehaviour가 아닌 순수 C# 클래스이며 `StageLifetimeScope`에서 VContainer로 생성·주입된다.

---

## 등록/주입 (`StageLifetimeScope.cs`)

```csharp
builder.Register<ICameraControls, CameraControls>(Lifetime.Singleton)
       .WithParameter<Camera>(mainCamera == null ? Camera.main : mainCamera)
       .WithParameter(nameof(vCamera), vCamera)
       .WithParameter(nameof(cameraShakeMaxAmplitude), cameraShakeMaxAmplitude)
       .WithParameter(nameof(cameraShakeDuration), cameraShakeDuration)
       .WithParameter(nameof(cameraShakeRecoveryDuration), cameraShakeRecoveryDuration)
       .WithParameter(nameof(cameraBoostZoomAmount), cameraBoostZoomAmount)
       .WithParameter(nameof(cameraBoostOffsetXAmount), cameraBoostOffsetXAmount);
```

- `mainCamera` / `vCamera`(`CinemachineCamera`)는 `StageLifetimeScope` Inspector 필드로 씬 GameObject를 직접 참조해 주입한다.
- `cameraShakeMaxAmplitude`, `cameraShakeDuration`, `cameraShakeRecoveryDuration`, `cameraBoostZoomAmount`, `cameraBoostOffsetXAmount`도 Inspector에서 튜닝 가능한 SerializeField.
- `StageManager`도 같은 `vCamera`를 별도로 주입받아 사용한다 (`ResetCamera()` 등, `Assets/Script/GamePlay/Stage/ARCHITECTURE.md` 참고).

## Camera Shake

`Character` Collision 시 `Status.CameraShake` 스탯(`StatType.CameraShake`)이 0보다 크면 `ICameraControls.Shake(amplitude)`를 호출해 카메라를 흔든다.

**Cinemachine 3.x 주의사항:** CM2의 `CinemachineVirtualCamera` 시절엔 Noise 컴포넌트가 숨겨진 child rig에 있었지만,
CM3의 `CinemachineCamera`는 파이프라인 컴포넌트(Body/Aim/Noise)가 **같은 GameObject의 sibling 컴포넌트**로 붙는 구조로 바뀌었다.
따라서 `vCamera.GetComponent<CinemachineBasicMultiChannelPerlin>()`로 바로 가져올 수 있다 (CM2처럼 `GetCinemachineComponent<T>()` 불필요).

**씬 설정 요구사항:** `vCamera` GameObject에 `Cinemachine Basic Multi Channel Perlin` 컴포넌트가 붙어 있고
`NoiseSettings` 프로파일이 지정돼 있어야 한다. 없으면 `CameraControls` 생성 시 에러 로그만 남기고 Shake는 무시된다.
프로파일은 프로젝트 자체 asset이 아니라 Cinemachine 패키지가 기본 제공하는 프리셋을 그대로 사용한다.

### Shake 흐름 (`CameraControls.cs`)

```
Character.ApplyCollision(otherCharacter)
  → damage > 0 이고 IsPlayer(내가 맞은 쪽이 Player)일 때만
  → otherCharacter.Status.CameraShake > 0 이면
  → _stageManager.CameraControls.Shake(amplitude)

CameraControls.Shake(amplitude)
  → _currentAmplitude = min(_currentAmplitude + amplitude, maxAmplitude)   ← 연속 요청 누적 + 상한
  → noise.AmplitudeGain = _currentAmplitude
  → 이전 재생/복구 타이머 취소 후 재시작 (PlayAndRecoverAsync)

PlayAndRecoverAsync
  → WaitForSeconds(shakeDuration)                         ← 최대 세기 유지 구간, 연속 요청 시 매번 duration으로 재시작
  → recoveryDuration 동안 매 프레임 Lerp로 Amplitude → 0   ← 뚝 끊기지 않고 서서히 감쇠
  → 복구 도중 새 Shake() 호출 시 토큰 취소 → 그 시점 잔여 Amplitude 위에 새 amplitude를 누적 (자연스럽게 이어짐)
```

- 상태는 `CameraControls` 인스턴스에 `_currentAmplitude` + `CancellationTokenSource`로 관리 (ScreenManager.StageTransition의 Fade 취소 패턴과 동일한 방식).
- `CameraControls`는 `IDisposable` 구현 — `StageLifetimeScope` 파괴 시 VContainer가 자동으로 진행 중인 Shake 타이머를 취소한다.

## 데이터 정의 (Camera Shake)

- `Assets/Script/GameInfo/Enum/StatType.cs` — `CameraShake` 값 추가 (`Max` 앞)
- `Assets/Script/GamePlay/Stat/Status.cs` — `public double CameraShake => _calcValue[(int)StatType.CameraShake];`
- 오브젝트(Obstacle 등)의 `StatusInfo`에 `CameraShake` 스탯을 설정하면, Player가 충돌 시 해당 값만큼 카메라가 흔들린다.
- `Assets/GAME_INFO_TABLE/StatusTable.asset` — CameraShake 스탯 데이터 추가됨

---

## Speed Boost 카메라 연출

Speed 버프로 이동속도가 점진적으로 빨라질 때(`Assets/Script/GamePlay/Buff/ARCHITECTURE.md` fade 시스템), 같은 fade 진행도로
카메라 시야(OrthographicSize)를 넓히고 `CinemachineFollow` X Offset을 늘려 캐릭터를 뒤로 밀어낸다. 부스터가 약해지면 동일한 타이밍으로 원래 값에 복귀한다.

**호출 주체는 `BuffSystem`이다** (Character를 거치지 않는다) — Spd 보너스와 fade factor를 이미 `BuffSystem.NotifySpeedFade()`에서 계산하고 있어서,
카메라 연출 트리거도 같은 자리에 두는 게 자연스럽기 때문. 자세한 흐름은 `Assets/Script/GamePlay/Buff/ARCHITECTURE.md`의 "카메라 연출 연동" 섹션 참고.

### `CameraControls.SetSpeedBoostFade(float fadeFactor)`

```
생성 시점에 base 값을 캡처:
  _baseOrthographicSize = vCamera.Lens.OrthographicSize
  _baseFollowOffsetX    = vCamera.GetComponent<CinemachineFollow>().FollowOffset.x

SetSpeedBoostFade(fadeFactor):
  vCamera.Lens.OrthographicSize        = _baseOrthographicSize + cameraBoostZoomAmount    * fadeFactor
  follow.FollowOffset.x (y/z는 유지)    = _baseFollowOffsetX    + cameraBoostOffsetXAmount * fadeFactor
```

- fadeFactor는 0(평상시)~1(최대 부스트) 사이 값을 매 프레임 그대로 반영 — 별도 보간 없이 Buff의 fadeIn/fadeOut 곡선을 그대로 따라간다.
- **Cinemachine 3.x 주의사항 (Shake와 동일한 패턴):** `CinemachineFollow`도 Body 파이프라인 컴포넌트라 `vCamera`와 같은 GameObject의 sibling으로 붙어 있다.
  `vCamera.GetComponent<CinemachineFollow>()`로 바로 가져올 수 있다. 없으면 생성 시 에러 로그만 남기고 무시.
- `vCamera.Lens`는 `LensSettings` **구조체 필드**(프로퍼티 아님)다. 구현은 `var lens = vCamera.Lens; lens.OrthographicSize = ...; vCamera.Lens = lens;` 형태의 읽고-수정-재대입 패턴을 사용 — API가 프로퍼티로 바뀌어도 안전한 관용구.

## 연관 경로

- `Assets/Script/GamePlay/Camera/CameraControls.cs` / `Interface/ICameraControls.cs`
- `Assets/Script/GamePlay/Character/Character.Action.cs` — `ApplyCollision()` / `ApplyCameraShake()` (Camera Shake)
- `Assets/Script/GamePlay/Buff/System/BuffSystem.cs` — `NotifySpeedFade()` / `Initialize(..., cameraControls)` / `Release()` (Speed Boost)
- `Assets/Script/GamePlay/Character/Character.Buff.cs` — `InitializeBuff()`에서 Player일 때만 `CameraControls` 전달
- `Assets/Script/LifetimeScope/StageLifetimeScope.cs`
- `Assets/GAME_ASSET/Prefab/Logic/StageManager.prefab` — vCamera에 `CinemachineBasicMultiChannelPerlin`/`CinemachineFollow` 컴포넌트 배치 (NoiseSettings는 Cinemachine 패키지 기본 프리셋 사용)
