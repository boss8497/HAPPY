# GameTimer — 전역 타이머

게임 전체에서 공유하는 시간 값을 제공하는 전역 타이머.  
Unity의 `Time.deltaTime` 대신 이 타이머를 사용하면 일시정지(Pause) 상태를 일관되게 반영할 수 있다.  
`AppLifetimeScope`에서 Singleton EntryPoint로 등록, 앱 시작과 함께 자동 시작된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/IGameTimer.cs` | 인터페이스 |
| `GameTimer.cs` | 구현체 |

## 제공 값

| 프로퍼티 | 설명 |
|---|---|
| `Elapsed` | 앱 시작 후 누적 경과 시간 (초, Pause 중 정지) |
| `FixedElapsed` | FixedUpdate 기준 누적 경과 시간 |
| `DeltaTime` | 프레임당 경과 시간 (Pause 중 0) |
| `FixedTime` | FixedUpdate당 경과 시간 (Pause 중 0) |
| `IsPaused` | 일시정지 상태 여부 |

## 동작 방식

UniTask 비동기 루프 2개를 독립적으로 실행한다.

```
UpdateTimer()      — PlayerLoopTiming.Update 마다 DeltaTime 누적 → Elapsed
UpdateFixedTimer() — PlayerLoopTiming.FixedUpdate 마다 FixedTime 누적 → FixedElapsed
```

`Pause()` 호출 시 DeltaTime / FixedTime 을 0으로 만들어 누적을 중단한다.  
`Dispose()` 시 CancellationToken으로 두 루프를 안전하게 종료한다.

## 제어 메서드

```csharp
Pause()   // IsPaused = true  → DeltaTime, FixedTime = 0
Resume()  // IsPaused = false → 정상 누적 재개
```

## 연관 경로

- 등록: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
