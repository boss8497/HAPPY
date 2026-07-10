# Camera — 카메라 제어

`CameraControls`가 메인 카메라 경계 계산과 Camera Shake를 담당하는 Stage 범위 싱글턴 서비스.
MonoBehaviour가 아닌 순수 C# 클래스이며 `StageLifetimeScope`에서 VContainer로 생성·주입된다.

---

## 등록/주입 (`StageLifetimeScope.cs`)

```csharp
builder.Register<ICameraControls, CameraControls>(Lifetime.Singleton)
       .WithParameter<Camera>(mainCamera == null ? Camera.main : mainCamera)
       .WithParameter(nameof(vCamera), vCamera)
       .WithParameter(nameof(cameraShakeMaxAmplitude), cameraShakeMaxAmplitude)
       .WithParameter(nameof(cameraShakeDuration), cameraShakeDuration)
       .WithParameter(nameof(cameraShakeRecoveryDuration), cameraShakeRecoveryDuration);
```

- `mainCamera` / `vCamera`(`CinemachineCamera`)는 `StageLifetimeScope` Inspector 필드로 씬 GameObject를 직접 참조해 주입한다.
- `cameraShakeMaxAmplitude`, `cameraShakeDuration`, `cameraShakeRecoveryDuration`도 Inspector에서 튜닝 가능한 SerializeField.
- `StageManager`도 같은 `vCamera`를 별도로 주입받아 사용한다 (`ResetCamera()` 등, `Assets/Script/GamePlay/Stage/ARCHITECTURE.md` 참고).

## Camera Shake

`Character` Collision 시 `Status.CameraShake` 스탯(`StatType.CameraShake`)이 0보다 크면 `ICameraControls.Shake(amplitude)`를 호출해 카메라를 흔든다.

**Cinemachine 3.x 주의사항:** CM2의 `CinemachineVirtualCamera` 시절엔 Noise 컴포넌트가 숨겨진 child rig에 있었지만,
CM3의 `CinemachineCamera`는 파이프라인 컴포넌트(Body/Aim/Noise)가 **같은 GameObject의 sibling 컴포넌트**로 붙는 구조로 바뀌었다.
따라서 `vCamera.GetComponent<CinemachineBasicMultiChannelPerlin>()`로 바로 가져올 수 있다 (CM2처럼 `GetCinemachineComponent<T>()` 불필요).

**씬 설정 요구사항:** `vCamera` GameObject에 `Cinemachine Basic Multi Channel Perlin` 컴포넌트가 붙어 있고
`NoiseSettings` 프로파일(`Assets/Settings/ShakeCamera.asset`)이 지정돼 있어야 한다. 없으면 `CameraControls` 생성 시 에러 로그만 남기고 Shake는 무시된다.

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

## 데이터 정의

- `Assets/Script/GameInfo/Enum/StatType.cs` — `CameraShake` 값 추가 (`Max` 앞)
- `Assets/Script/GamePlay/Stat/Status.cs` — `public double CameraShake => _calcValue[(int)StatType.CameraShake];`
- 오브젝트(Obstacle 등)의 `StatusInfo`에 `CameraShake` 스탯을 설정하면, Player가 충돌 시 해당 값만큼 카메라가 흔들린다.
- `Assets/GAME_INFO_TABLE/StatusTable.asset` — CameraShake 스탯 데이터 추가됨

## 연관 경로

- `Assets/Script/GamePlay/Camera/CameraControls.cs` / `Interface/ICameraControls.cs`
- `Assets/Script/GamePlay/Character/Character.Action.cs` — `ApplyCollision()` / `ApplyCameraShake()`
- `Assets/Script/LifetimeScope/StageLifetimeScope.cs`
- `Assets/Settings/ShakeCamera.asset` — NoiseSettings 프로파일
- `Assets/GAME_ASSET/Prefab/Logic/StageManager.prefab` — vCamera에 `CinemachineBasicMultiChannelPerlin` 컴포넌트 배치
