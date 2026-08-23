# GameSetting — 게임 설정

프레임레이트, V-Sync, Addressable 중앙 캐시 점검주기/유예시간 등 앱 전역 설정값을 [Addressable 중앙 캐시](../Addressable/README.md)를 통해 로드해 적용한다.
`AppLifetimeScope`에서 Singleton EntryPoint로 등록, `StartUpLogic`에서 명시적으로 초기화된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/IGameSetting.cs` | 인터페이스 |
| `Data/GameSettingData.cs` | 설정값 구조체 (`frameRate`, `vSyncCount`, `addressableCacheCheckIntervalSeconds`, `addressableCacheReleaseGraceSeconds`) |
| `Data/GameSettingAsset.cs` | ScriptableObject 에셋 (Addressable 키: `"GameSettingAsset"`) |
| `GameSetting.cs` | 구현체 |

## 초기화 흐름

```
AppLifetimeScope 생성
    → VContainer IInitializable.Initialize() 호출 (빈 메서드)
    → StartUpLogic에서 InitializeGameSetting() 명시적 호출
        → _addressableService.LoadAsync<GameSettingAsset>("GameSettingAsset") (using으로 즉시 Dispose)
        → GameSettingData 저장 (struct 값 복사이므로 Dispose 이후에도 안전)
        → _addressableService.ConfigureCache(...) — 방금 읽은 캐시 점검주기/유예시간을 Addressable 쪽에 반영
        → Application.targetFrameRate, QualitySettings.vSyncCount 적용
        → Initialized = true
```

로드 실패 시 Exception 발생 — 설정 없이 게임이 진행되지 않도록 의도된 설계.

`GameSetting`이 `IAddressableService.ConfigureCache()`를 호출해 캐시 점검주기/유예시간을 넘겨주는 이유와, 그 반대 방향(Addressable → GameSetting)으로 의존하지 않은 이유는 [`Addressable/README.md`](../Addressable/README.md#캐시-점검-주기--유예시간-설정) 참고 — 순환 의존을 피하기 위한 설계다.

## 연관 경로

- 등록: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
- 초기화 호출: `Assets/Script/Scene/StartUpLogic.cs`
- 캐시 설정값 사용처: `Assets/Script/Addressable/README.md`
