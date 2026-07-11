# Addressable — Addressable Assets 관리

Unity Addressable Assets 시스템을 래핑해 초기화, 카탈로그 업데이트, 다운로드, 인터넷 연결 확인을 제공한다.  
`AppLifetimeScope`에서 Singleton EntryPoint로 등록되며, 앱 시작 시 가장 먼저 초기화된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/IAddressable.cs` | 인터페이스 |
| `Addressable.cs` | 구현체 |

## 주요 기능

### 초기화
- `Initialize()` (VContainer IInitializable) → `Addressables.InitializeAsync()` 완료 대기
- `StartUpLogic`에서 `InitializeAsync()` 호출 후 명시적 완료 대기

### 카탈로그 업데이트
- `UpdateCatalogsAsync()` — 원격 카탈로그 확인 후 변경된 카탈로그만 업데이트
- `autoCleanBundleCache` 옵션으로 이전 번들 캐시 자동 정리

### 다운로드
- `GetDownloadSizeAsync(key)` — 특정 키의 다운로드 크기 조회
- `DownloadDependenciesAsync(key, IProgress<float>)` — 의존 에셋 다운로드, 진행률 콜백 지원

### 인터넷 연결 확인
- `HasInternetConnectionAsync(timeout)` — 두 단계 확인
  1. `Application.internetReachability` 사전 확인
  2. `https://connectivitycheck.gstatic.com/generate_204` 에 UnityWebRequest로 실제 연결 테스트

### 앱 라벨 로드
- `LoadAppLabelsAsync()` — `"AppLifetimeScope"` 라벨의 에셋 일괄 로드
- `AppLifetimeScope` 라벨이 속한 그룹은 **Local**(빌드에 포함) 이므로 다운로드 시 인터넷 연결이 필요 없다.
  인터넷 확인 없이도 로컬 카탈로그+로컬 번들에서 바로 로드된다.

## 초기화 순서 (StartUpLogic)

```
1. Addressables.InitializeAsync() 완료 대기 (IsInitialized)
2. LoadAppLabelsAsync()          — 앱 에셋 로드 (Local 그룹, 인터넷 불필요)
```

`HasInternetConnectionAsync()` / `UpdateCatalogsAsync()`는 시작 시퀀스에서 더 이상 호출되지 않는다.
(에러 팝업 프리팹 등 앱 필수 UI가 Remote 라벨에 걸려 있어 "인터넷 없음" 팝업 자체를 못 띄우는
순환 문제가 있었음 — 필수 에셋을 Local 그룹으로 옮기는 방식으로 해결) 필요 시 원격 콘텐츠
업데이트 체크 용도로 별도 호출 가능.

## 연관 경로

- 등록 위치: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
- 초기화 호출: `Assets/Script/Scene/StartUpLogic.cs`
