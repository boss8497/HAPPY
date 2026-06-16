# SceneLoader — 씬 전환

Addressable 기반 비동기 씬 전환을 담당한다.  
`AppLifetimeScope`에서 Singleton EntryPoint로 등록된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/ISceneLoader.cs` | 인터페이스 |
| `SceneLoader.cs` | 구현체 |

## LoadScene 동작 순서

```
1. ScreenManager.CloseAllAsync(force: true)   — 열린 화면 전체 강제 닫기
2. ScreenManager.ResourceClear()              — UI 리소스 메모리 해제
3. 현재 활성 씬 참조 저장
4. Addressables.LoadSceneAsync(scenePath, Additive)  — 새 씬 Additive 로드
5. SceneManager.SetActiveScene(new scene)     — 새 씬을 Active로 설정
6. SceneManager.UnloadSceneAsync(prev scene)  — 이전 씬 언로드
```

Additive 로드 방식을 사용해 이전 씬이 완전히 제거되기 전에 새 씬을 준비하므로 전환이 자연스럽다.

## 호출 위치

| 호출처 | 전환 대상 |
|---|---|
| `StartUpLogic` | StartUp → Title |
| `TitleHUD` | Title → Lobby |
| `GroupService.EnterDungeon()` | Lobby → GameScene (stage.scenePath) |

## 연관 경로

- 등록: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
- 씬 파일: `Assets/Scenes/`
- UI 정리: `Assets/Script/GUI/` (IScreenManager)
