# Scene — 씬별 진입 로직

각 씬이 로드된 후 실행되는 초기화 로직 모음.  
`IScopeLocator` / `IScopeFactory`를 통해 LifetimeScope를 생성하고 파괴한다.

## 씬 실행 순서

```
StartUp → Title → Lobby → GameScene(게임플레이 씬들)
```

## 씬별 로직 파일

### StartUpLogic.cs (`StartUp.unity`)
- `AppLifetimeScope`에서 주입받아 실행 (EntryPoint)
- 흐름:
  1. `IAddressable.InitializeAsync()` — Addressable 초기화
  2. `IGameSetting.InitializeAsync()` — 게임 설정 로드
  3. `IAudioManager.InitializeAudioManager()` — AudioMixer 로드 + 그룹 볼륨/뮤트 설정 적용
  4. `IScopeFactory.CreateScope(ScopeType.Client)` — `ClientLifetimeScope` 생성
  5. `ISceneLoader.LoadScene("Title")` — Title 씬으로 전환

### TitleLogic.cs / TitleHUD (`Title.unity`)
- Title 씬 UI 및 로직 처리
- "Start" 버튼 클릭 시:
  1. `IScopeFactory.CreateScope(ScopeType.Group)` — `GroupLifetimeScope` 생성
  2. `IGroupService` 초기화 (Req_Group, Req_Inventory)
  3. `IGroupService.EnterDungeon(lobbyDungeon)` → Lobby 씬 전환

### LobbyLogic.cs (`Lobby.unity`)
- Lobby 씬 UI 및 던전 선택 처리
- 던전 선택 → `IGroupService.EnterDungeon(dungeonInfo, stage)` → GameScene 전환
  - GroupService 내부에서 `ISceneLoader.LoadScene(stage.scenePath)` 호출

### GameScene (`MountainScene.unity` 등)
- `StartUpLogic` / `TitleLogic` / `LobbyLogic` 과 달리 **씬 안에 StageLifetimeScope GameObject가 직접 배치**
- 이유: `mainCamera`, `CinemachineTargetGroup`, `CinemachineCamera` 등 씬에 존재하는 GameObject를 Inspector에서 직접 참조해야 하기 때문
- 씬 로드 시 Unity가 자동으로 StageLifetimeScope 초기화 → `IStageManager.StartAsync()` 실행
- 씬 언로드 시 `StageLifetimeScope.OnDestroy()` → `ScopeLocator.ReleaseChildScope(Stage)`

## Scope 생성/파괴 책임 분리

| 씬 | 생성 주체 | Scope |
|---|---|---|
| StartUp | `StartUpLogic` | `ClientLifetimeScope` |
| Title | `TitleHUD` ("Start" 클릭) | `GroupLifetimeScope` |
| GameScene | Unity (GameObject 자동 초기화) | `StageLifetimeScope` |

## 연관 경로

- Scope 정의: `Assets/Script/LifetimeScope/`
- 씬 파일: `Assets/Scenes/`
