# GameSetting — 게임 설정

프레임레이트, V-Sync 등 디바이스 공통 설정을 Addressable에셋에서 로드해 적용한다.  
`AppLifetimeScope`에서 Singleton EntryPoint로 등록, `StartUpLogic`에서 명시적으로 초기화된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/IGameSetting.cs` | 인터페이스 |
| `Data/GameSettingData.cs` | 설정값 구조체 (`frameRate`, `vSyncCount`) |
| `Data/GameSettingAsset.cs` | ScriptableObject 에셋 (Addressable 키: `"GameSettingAsset"`) |
| `GameSetting.cs` | 구현체 |

## 초기화 흐름

```
AppLifetimeScope 생성
    → VContainer IInitializable.Initialize() 호출 (빈 메서드)
    → StartUpLogic에서 InitializeGameSetting() 명시적 호출
        → Addressables.LoadAssetAsync("GameSettingAsset") WaitForCompletion()
        → GameSettingData 저장
        → Application.targetFrameRate, QualitySettings.vSyncCount 적용
        → Initialized = true
```

로드 실패 시 Exception 발생 — 설정 없이 게임이 진행되지 않도록 의도된 설계.

## 연관 경로

- 등록: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
- 초기화 호출: `Assets/Script/Scene/StartUpLogic.cs`
